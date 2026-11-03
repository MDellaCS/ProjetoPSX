using System.Collections.Generic;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ponte entre o gameplay e o rig Cinemachine. O Cinemachine resolve a camera
/// final; este componente fornece o centro correto do player, a orbita do mouse
/// e os modificadores produzidos pelas mecanicas do jogo.
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Target")]
    [Tooltip("Transform raiz do player acompanhado pela camera.")]
    [SerializeField] private Transform target;
    [Tooltip("Porcentagem da altura real do Character Controller somada ao seu centro. Por exemplo, 10 desloca o alvo para cima em 10% da altura do player.")]
    [Range(-100f, 100f)] [SerializeField] private float targetHeightOffsetPercent;

    [Header("Cinemachine")]
    [Tooltip("Camera Cinemachine usada pelo gameplay. Se estiver vazia, uma camera configurada por este componente sera criada ao iniciar o jogo.")]
    [SerializeField] private CinemachineCamera gameplayCamera;
    [Tooltip("Prioridade da camera de gameplay perante outras cameras Cinemachine. Valores maiores vencem.")]
    [SerializeField] private int gameplayCameraPriority = 10;

    [Header("Orbit")]
    [Tooltip("Distancia base entre a camera e o centro real do player.")]
    [Min(.1f)] [SerializeField] private float distance = 5.7f;
    [Tooltip("Offset horizontal inicial em relacao a direcao para a qual o player esta olhando. Zero inicia a camera exatamente nas costas do player.")]
    [SerializeField] private float initialYaw;
    [Tooltip("Rotacao vertical inicial da camera, em graus positivos olhando de cima.")]
    [SerializeField] private float initialPitch = 18f;
    [Tooltip("Menor angulo vertical permitido ao mover o mouse.")]
    [Range(-89f, 89f)] [SerializeField] private float minimumPitch = -15f;
    [Tooltip("Maior angulo vertical permitido ao mover o mouse.")]
    [Range(-89f, 89f)] [SerializeField] private float maximumPitch = 65f;

    [Header("Input")]
    [Tooltip("Sensibilidade horizontal do mouse em graus por pixel.")]
    [Min(0f)] [SerializeField] private float horizontalMouseSensitivity = .05f;
    [Tooltip("Sensibilidade vertical do mouse em graus por pixel.")]
    [Min(0f)] [SerializeField] private float verticalMouseSensitivity = .04f;
    [Tooltip("Inverte o movimento vertical da camera.")]
    [SerializeField] private bool invertVerticalInput;
    [Tooltip("Prende o cursor ao iniciar. Escape libera e clique esquerdo prende novamente.")]
    [SerializeField] private bool lockCursorOnStart = true;

    [Header("Follow")]
    [Tooltip("Tempo aproximado para a camera acompanhar o centro do player. Valores menores respondem mais rapido.")]
    [Min(0f)] [SerializeField] private float followSmoothTime = .08f;
    [Tooltip("Distancia instantanea tratada como teleporte, removendo o atraso da suavizacao.")]
    [Min(0f)] [SerializeField] private float teleportSnapDistance = 12f;

    [Header("Collision")]
    [Tooltip("Camadas exclusivas do cenario capazes de empurrar a camera. Use uma layer CameraObstacle e nao inclua Player nem Enemy.")]
    [SerializeField] private LayerMask cinemachineCollisionLayers;
    [Tooltip("Raio usado pelo Cinemachine para impedir que a camera atravesse paredes.")]
    [Min(.01f)] [SerializeField] private float collisionRadius = .2f;
    [Tooltip("Tempo para aproximar a camera quando uma parede bloqueia sua posicao.")]
    [Min(0f)] [SerializeField] private float collisionInSmoothTime = .03f;
    [Tooltip("Tempo para a camera voltar para longe depois que a parede deixa de bloquear.")]
    [Min(0f)] [SerializeField] private float collisionOutSmoothTime = .25f;

    [Header("Lens")]
    [Tooltip("Campo de visao base. Modificadores podem somar alteracoes sem substituir este valor.")]
    [Range(1f, 179f)] [SerializeField] private float baseFieldOfView = 60f;

    private PlayerCharacterController targetController;
    private CinemachineThirdPersonFollow thirdPersonFollow;
    private CinemachineCamera menuCamera;
    private bool ownsMenuCamera;
    private CinemachineBlendDefinition gameplayDefaultBlend;
    private bool hasSavedDefaultBlend;
    private CameraEffectStack effectStack;
    private CameraGameplayEffects gameplayEffects;
    private CameraPlayerVisibility playerVisibility;
    private Transform trackingPivot;
    private bool ownsGameplayCamera;
    private bool ownsTrackingPivot;
    private bool initialized;
    private bool fixedCameraActive;
    private Coroutine fixedCameraBlendRestoreRoutine;
    private Vector3 previousTargetCenter;
    private float yaw;
    private float pitch;
    private readonly List<FixedCameraZone> fixedCameraZones = new();
    private FixedCameraZone activeFixedCameraZone;
    private int movementCameraVersion;

    public Transform Target => target;
    public Vector3 TargetCenter => GetTargetCenter();
    public CinemachineCamera GameplayCamera => gameplayCamera;
    internal int GameplayCameraPriority => gameplayCameraPriority;
    internal int MovementCameraVersion => movementCameraVersion;

    /// <summary>
    /// Reassocia a camera persistente ao Player depois de uma troca de cena e
    /// invalida o estado anterior do Cinemachine para nao interpolar da fase
    /// antiga ate a nova.
    /// </summary>
    public void RefreshAfterSceneLoad(Transform persistentPlayer = null)
    {
        if (persistentPlayer != null)
            target = persistentPlayer;
        if (target == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                target = playerObject.transform;
        }

        // A Virtual Camera procedural e o pivot sao criados em runtime. Se a
        // camera principal sobreviveu a uma troca de fase, mas esses objetos
        // eram raiz da cena anterior, o Unity os destroi. Recrie-os antes de
        // reinicializar o follow.
        EnsureCinemachineRig();
        targetController = target != null
            ? target.GetComponent<PlayerCharacterController>() : null;
        initialized = false;
        Initialize();
        if (gameplayCamera != null)
            gameplayCamera.PreviousStateIsValid = false;
    }

    private void Awake()
    {
        Instance = this;
        EnsureCinemachineRig();

        effectStack = GetComponent<CameraEffectStack>();
        if (effectStack == null)
            effectStack = gameObject.AddComponent<CameraEffectStack>();

        gameplayEffects = GetComponent<CameraGameplayEffects>();
        if (gameplayEffects == null)
            gameplayEffects = gameObject.AddComponent<CameraGameplayEffects>();

        playerVisibility = GetComponent<CameraPlayerVisibility>();
        if (playerVisibility == null)
            playerVisibility = gameObject.AddComponent<CameraPlayerVisibility>();
    }

    private void Start()
    {
        Initialize();
        if (lockCursorOnStart)
            LockCursor();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (ownsMenuCamera && menuCamera != null)
            Destroy(menuCamera.gameObject);
        if (ownsGameplayCamera && gameplayCamera != null)
            Destroy(gameplayCamera.gameObject);
        if (ownsTrackingPivot && trackingPivot != null)
            Destroy(trackingPivot.gameObject);
    }

    private void OnValidate()
    {
        maximumPitch = Mathf.Max(minimumPitch, maximumPitch);
        initialPitch = Mathf.Clamp(initialPitch, minimumPitch, maximumPitch);
    }

    private void Update()
    {
        if (!initialized)
            Initialize();
        if (GamePauseController.IsPaused)
            return;
        if (fixedCameraActive)
        {
            if (lockCursorOnStart)
                HandleCursorLock();
            return;
        }
        UpdateMouseOrbit();
    }

    private void LateUpdate()
    {
        UpdateFixedCameraSelection();

        if (!initialized || target == null || trackingPivot == null ||
            thirdPersonFollow == null || gameplayCamera == null)
            return;

        UpdateTrackingPivot();
        ApplyCameraState();
    }

    internal void RegisterFixedCameraZone(FixedCameraZone zone)
    {
        if (zone != null && !fixedCameraZones.Contains(zone))
            fixedCameraZones.Add(zone);
    }

    internal void UnregisterFixedCameraZone(FixedCameraZone zone)
    {
        if (zone == null)
            return;

        fixedCameraZones.Remove(zone);
        if (activeFixedCameraZone == zone)
        {
            movementCameraVersion++;
            PrepareInstantCameraSwitch();
            activeFixedCameraZone.SetCameraSelected(false);
            activeFixedCameraZone = null;
            fixedCameraActive = false;
            ScheduleGameplayBlendRestore();
        }
    }

    private void UpdateFixedCameraSelection()
    {
        Collider playerCollider = GetCameraZonePlayerCollider();
        FixedCameraZone bestZone = null;

        for (int i = fixedCameraZones.Count - 1; i >= 0; i--)
        {
            FixedCameraZone zone = fixedCameraZones[i];
            if (zone == null)
            {
                fixedCameraZones.RemoveAt(i);
                continue;
            }

            if (!zone.IsPlayerInside(playerCollider))
                continue;

            bool hasHigherPriority = bestZone == null ||
                zone.CameraPriority > bestZone.CameraPriority;
            bool enteredMoreRecently = bestZone != null &&
                zone.CameraPriority == bestZone.CameraPriority &&
                zone.LastEnteredFrame > bestZone.LastEnteredFrame;
            bool isMoreSpecific = bestZone != null &&
                zone.CameraPriority == bestZone.CameraPriority &&
                zone.LastEnteredFrame == bestZone.LastEnteredFrame &&
                zone.WorldVolume < bestZone.WorldVolume;
            if (hasHigherPriority || enteredMoreRecently || isMoreSpecific)
                bestZone = zone;
        }

        if (bestZone == activeFixedCameraZone)
            return;

        movementCameraVersion++;

        PrepareInstantCameraSwitch();

        if (activeFixedCameraZone != null)
            activeFixedCameraZone.SetCameraSelected(false);

        activeFixedCameraZone = bestZone;
        fixedCameraActive = activeFixedCameraZone != null;
        if (activeFixedCameraZone != null)
            activeFixedCameraZone.SetCameraSelected(true);
        else
            ScheduleGameplayBlendRestore();
    }

    private Collider GetCameraZonePlayerCollider()
    {
        if (target == null)
            return null;

        if (targetController == null)
            targetController = target.GetComponent<PlayerCharacterController>();

        if (targetController != null && targetController.Collider != null)
            return targetController.Collider;

        return target.GetComponent<Collider>();
    }

    private void EnsureCinemachineRig()
    {
        if (GetComponent<CinemachineBrain>() == null)
            gameObject.AddComponent<CinemachineBrain>();

        if (gameplayCamera == null)
        {
            GameObject cameraObject = new("Gameplay Cinemachine Camera (Runtime)");
            ParentRuntimeObjectToPersistentRoot(cameraObject);
            gameplayCamera = cameraObject.AddComponent<CinemachineCamera>();
            ownsGameplayCamera = true;
        }

        gameplayCamera.Priority = gameplayCameraPriority;
        thirdPersonFollow = gameplayCamera.GetComponent<CinemachineThirdPersonFollow>();
        if (thirdPersonFollow == null)
            thirdPersonFollow = gameplayCamera.gameObject.AddComponent<CinemachineThirdPersonFollow>();

        ConfigureCinemachineComponents();
    }

    /// <summary>Exibe a cena do menu por uma camera Cinemachine estatica.</summary>
    public void EnterStaticMenuCamera(Vector3 worldPosition,
        Vector3 worldEulerAngles)
    {
        EnsureCinemachineRig();
        PrepareInstantCameraSwitch();

        if (menuCamera != null && !ownsMenuCamera)
        {
            menuCamera.Priority = 0;
            menuCamera = null;
        }

        if (menuCamera == null)
        {
            GameObject cameraObject = new("Main Menu Camera (Runtime)");
            ParentRuntimeObjectToPersistentRoot(cameraObject);
            menuCamera = cameraObject.AddComponent<CinemachineCamera>();
            ownsMenuCamera = true;
        }

        menuCamera.Follow = null;
        menuCamera.LookAt = null;
        menuCamera.transform.SetPositionAndRotation(worldPosition,
            Quaternion.Euler(worldEulerAngles));
        menuCamera.Lens.FieldOfView = baseFieldOfView;
        menuCamera.PreviousStateIsValid = false;
        menuCamera.Priority = gameplayCameraPriority + 1;
    }

    /// <summary>Ativa uma CinemachineCamera da cena como camera estatica do menu.</summary>
    public void EnterMenuCamera(CinemachineCamera sceneCamera)
    {
        if (sceneCamera == null)
            return;

        EnsureCinemachineRig();
        PrepareInstantCameraSwitch();
        if (menuCamera != null && menuCamera != sceneCamera)
        {
            if (ownsMenuCamera)
                Destroy(menuCamera.gameObject);
            else
                menuCamera.Priority = 0;
        }

        menuCamera = sceneCamera;
        ownsMenuCamera = false;
        menuCamera.Follow = null;
        menuCamera.LookAt = null;
        menuCamera.PreviousStateIsValid = false;
        menuCamera.Priority = gameplayCameraPriority + 1;
    }

    private void PrepareInstantCameraSwitch()
    {
        if (fixedCameraBlendRestoreRoutine != null)
        {
            StopCoroutine(fixedCameraBlendRestoreRoutine);
            fixedCameraBlendRestoreRoutine = null;
        }

        CinemachineBrain brain = GetComponent<CinemachineBrain>();
        if (brain == null)
            return;

        if (!hasSavedDefaultBlend)
        {
            gameplayDefaultBlend = brain.DefaultBlend;
            hasSavedDefaultBlend = true;
        }

        CinemachineBlendDefinition instantBlend = brain.DefaultBlend;
        instantBlend.Time = 0f;
        brain.DefaultBlend = instantBlend;
    }

    private void ScheduleGameplayBlendRestore()
    {
        if (fixedCameraBlendRestoreRoutine != null)
            StopCoroutine(fixedCameraBlendRestoreRoutine);
        fixedCameraBlendRestoreRoutine =
            StartCoroutine(RestoreGameplayBlendAfterCut());
    }

    private IEnumerator RestoreGameplayBlendAfterCut()
    {
        // Aguarda o Brain confirmar que a camera de follow ja foi escolhida.
        // A coroutine retoma em Update, antes de o Brain avaliar LateUpdate;
        // restaurar no primeiro yield faria a saida usar o blend normal.
        CinemachineBrain brain = GetComponent<CinemachineBrain>();
        int waitedFrames = 0;
        while (!fixedCameraActive && brain != null && gameplayCamera != null &&
            !System.Object.ReferenceEquals(brain.ActiveVirtualCamera,
                gameplayCamera) && waitedFrames < 4)
        {
            waitedFrames++;
            yield return null;
        }

        // Garante que pelo menos um frame renderizado recebeu o corte antes
        // de devolver ao gameplay a configuracao normal de blend.
        yield return new WaitForEndOfFrame();
        if (!fixedCameraActive && hasSavedDefaultBlend)
        {
            if (brain != null)
                brain.DefaultBlend = gameplayDefaultBlend;
            hasSavedDefaultBlend = false;
        }

        fixedCameraBlendRestoreRoutine = null;
    }

    /// <summary>Retoma o follow do jogador depois de sair do menu.</summary>
    public void ExitStaticMenuCamera(Transform persistentPlayer = null)
    {
        CinemachineBrain brain = GetComponent<CinemachineBrain>();
        if (brain != null && hasSavedDefaultBlend)
        {
            brain.DefaultBlend = gameplayDefaultBlend;
            hasSavedDefaultBlend = false;
        }

        if (menuCamera != null)
        {
            if (ownsMenuCamera)
                Destroy(menuCamera.gameObject);
            else
                menuCamera.Priority = 0;
            menuCamera = null;
            ownsMenuCamera = false;
        }

        if (gameplayCamera != null)
            gameplayCamera.Priority = gameplayCameraPriority;
        RefreshAfterSceneLoad(persistentPlayer);
    }

    private void ConfigureCinemachineComponents()
    {
        thirdPersonFollow.Damping = Vector3.one * followSmoothTime;
        thirdPersonFollow.ShoulderOffset = Vector3.zero;
        thirdPersonFollow.VerticalArmLength = 0f;
        thirdPersonFollow.CameraSide = .5f;
        thirdPersonFollow.CameraDistance = distance;
        thirdPersonFollow.AvoidObstacles.Enabled =
            cinemachineCollisionLayers.value != 0;
        thirdPersonFollow.AvoidObstacles.CollisionFilter =
            cinemachineCollisionLayers;
        thirdPersonFollow.AvoidObstacles.CameraRadius = collisionRadius;
        thirdPersonFollow.AvoidObstacles.DampingIntoCollision = collisionInSmoothTime;
        thirdPersonFollow.AvoidObstacles.DampingFromCollision = collisionOutSmoothTime;
        gameplayCamera.Lens.FieldOfView = baseFieldOfView;
    }

    private void Initialize()
    {
        if (target == null || gameplayCamera == null || thirdPersonFollow == null)
            return;

        targetController = target.GetComponent<PlayerCharacterController>();
        yaw = target.eulerAngles.y + initialYaw;
        pitch = Mathf.Clamp(initialPitch, minimumPitch, maximumPitch);
        previousTargetCenter = GetTargetCenter();

        if (trackingPivot == null)
        {
            GameObject pivotObject = new("Camera Tracking Pivot (Runtime)");
            ParentRuntimeObjectToPersistentRoot(pivotObject);
            trackingPivot = pivotObject.transform;
            ownsTrackingPivot = true;
        }

        trackingPivot.SetPositionAndRotation(
            previousTargetCenter,
            Quaternion.Euler(pitch, yaw, 0f)
        );
        gameplayCamera.Follow = trackingPivot;
        gameplayCamera.LookAt = trackingPivot;
        gameplayCamera.PreviousStateIsValid = false;

        effectStack?.Initialize(gameplayCamera);
        gameplayEffects?.Initialize(target, effectStack);
        playerVisibility?.Initialize(target, GetComponent<Camera>());
        initialized = true;
    }

    private void ParentRuntimeObjectToPersistentRoot(GameObject runtimeObject)
    {
        PersistentGameRoot gameRoot = GetComponentInParent<PersistentGameRoot>();
        if (gameRoot != null)
        {
            runtimeObject.transform.SetParent(gameRoot.transform, false);
            return;
        }

        // Mantem o comportamento seguro tambem para cenas antigas que ainda
        // usam CameraFollow sem um GameRoot.
        DontDestroyOnLoad(runtimeObject);
    }

    private void UpdateTrackingPivot()
    {
        Vector3 center = GetTargetCenter();
        Vector3 displacement = center - previousTargetCenter;
        trackingPivot.position = center;

        if (teleportSnapDistance > 0f &&
            displacement.sqrMagnitude >= teleportSnapDistance * teleportSnapDistance)
        {
            gameplayCamera.OnTargetObjectWarped(trackingPivot, displacement);
            gameplayCamera.PreviousStateIsValid = false;
        }

        previousTargetCenter = center;
    }

    private void ApplyCameraState()
    {
        CameraEffectFrame effects = effectStack != null
            ? effectStack.Evaluate() : CameraEffectFrame.Identity;

        trackingPivot.rotation = Quaternion.Euler(
            pitch + effects.pitchOffset,
            yaw + effects.yawOffset,
            effects.rollOffset
        );
        thirdPersonFollow.Damping = Vector3.one * followSmoothTime;
        thirdPersonFollow.ShoulderOffset = effects.cameraLocalOffset;
        thirdPersonFollow.CameraDistance = Mathf.Max(
            .1f,
            distance + effects.distanceOffset
        );
        gameplayCamera.Lens.FieldOfView = Mathf.Clamp(
            baseFieldOfView + effects.fieldOfViewOffset,
            1f,
            179f
        );
        gameplayCamera.Priority = gameplayCameraPriority;
        thirdPersonFollow.AvoidObstacles.Enabled =
            cinemachineCollisionLayers.value != 0;
        thirdPersonFollow.AvoidObstacles.CollisionFilter =
            cinemachineCollisionLayers;
        thirdPersonFollow.AvoidObstacles.CameraRadius = collisionRadius;
        thirdPersonFollow.AvoidObstacles.DampingIntoCollision =
            collisionInSmoothTime;
        thirdPersonFollow.AvoidObstacles.DampingFromCollision =
            collisionOutSmoothTime;
    }

    private Vector3 GetTargetCenter()
    {
        if (target == null)
            return transform.position;
        if (targetController == null)
            targetController = target.GetComponent<PlayerCharacterController>();
        if (targetController != null)
        {
            Bounds bounds = targetController.bounds;
            float heightOffset = bounds.size.y *
                (targetHeightOffsetPercent / 100f);
            return bounds.center + Vector3.up * heightOffset;
        }

        return target.position;
    }

    private void UpdateMouseOrbit()
    {
        if (target == null)
            return;
        if (lockCursorOnStart && !HandleCursorLock())
            return;

        Vector2 mouseDelta = GameInput.Look;
        float sensitivity = GameSettingsService.Applied.cameraSensitivity;
        yaw += mouseDelta.x * horizontalMouseSensitivity * sensitivity;
        float verticalSign = invertVerticalInput ^
            GameSettingsService.Applied.invertCameraY ? 1f : -1f;
        pitch = Mathf.Clamp(
            pitch + mouseDelta.y * verticalMouseSensitivity * sensitivity * verticalSign,
            minimumPitch,
            maximumPitch
        );
    }

    private bool HandleCursorLock()
    {
        if (GameInput.UnlockCursorPressed)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return false;
        }
        if (Cursor.lockState == CursorLockMode.Locked)
            return true;
        if (GameInput.LightPressed)
            LockCursor();
        return Cursor.lockState == CursorLockMode.Locked;
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
