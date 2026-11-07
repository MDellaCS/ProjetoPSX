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
    public bool LocksCursorOnStart => lockCursorOnStart;

    // Domain Reload desligado: sem este reset a instancia de uma sessao de
    // Play anterior sobreviveria (destruida) na proxima.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

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
    [Tooltip("Velocidade de giro em graus por segundo ao usar o analógico direito.")]
    [Min(0f)] [SerializeField] private float gamepadLookSpeed = 180f;
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
    private CinemachineBrain brain;
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
    private PlayerMovement targetMovement;
    private float yaw;
    private float pitch;
    private FixedCameraZone activeFixedCameraZone;
    private int movementCameraVersion;
    private FollowCameraSettings followTransitionFrom;
    private FollowCameraSettings followTransitionTo;
    private AnimationCurve followTransitionCurve;
    private float followTransitionDuration;
    private float followTransitionElapsed;
    private bool followTransitionActive;

    private struct FollowCameraSettings
    {
        public float yaw;
        public float pitch;
        public float smoothTime;
        public Vector3 positionOffset;
        public float distance;
        public float fieldOfView;
    }

    public Transform Target => target;
    public Vector3 TargetCenter => GetTargetCenter();
    public CinemachineCamera GameplayCamera => gameplayCamera;
    /// <summary>
    /// Ponto que representa o centro do player para outras cameras (zonas de
    /// camera estatica/trilho). Acompanha o mesmo centro usado pela camera de
    /// gameplay, sem a rotacao da orbita.
    /// </summary>
    public Transform TrackingTarget => trackingPivot;
    /// <summary>Atraso de acompanhamento (em segundos) da camera de gameplay.</summary>
    public float FollowSmoothTime => followSmoothTime;
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

        // As configuracoes de colisao deixaram de ser reaplicadas a cada
        // frame; ajustes no Inspector durante o Play entram por aqui.
        if (Application.isPlaying && thirdPersonFollow != null &&
            gameplayCamera != null)
            ConfigureCinemachineComponents();
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
        if (!initialized || target == null || trackingPivot == null ||
            thirdPersonFollow == null || gameplayCamera == null)
            return;

        UpdateTrackingPivot();
        ApplyCameraState();
    }

    internal void RegisterFixedCameraZone(FixedCameraZone zone)
    {
        // Mantido para componentes ja habilitados; a selecao agora acontece
        // somente no evento de entrada do trigger.
    }

    internal void UnregisterFixedCameraZone(FixedCameraZone zone)
    {
        if (zone == null)
            return;

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

    internal void ActivateCameraTrigger(FixedCameraZone trigger)
    {
        if (trigger == null || !trigger.isActiveAndEnabled)
            return;
        FixedCameraZone nextCamera = trigger.ReturnsToFreeCamera ? null : trigger;
        if (nextCamera == activeFixedCameraZone)
            return;

        bool blendSameFollowCamera = trigger.UseCameraTransition &&
            trigger.CameraTransition.BlendTime > 0f &&
            (activeFixedCameraZone == null ||
             activeFixedCameraZone.CameraMode == CameraZoneMode.FollowWithOverrides) &&
            (nextCamera == null ||
             nextCamera.CameraMode == CameraZoneMode.FollowWithOverrides);
        if (blendSameFollowCamera)
            BeginFollowCameraTransition(nextCamera, trigger.CameraTransition);
        else
            followTransitionActive = false;

        movementCameraVersion++;

        if (trigger.UseCameraTransition)
            PrepareCameraSwitch(trigger.CameraTransition);
        else
            PrepareInstantCameraSwitch();

        if (activeFixedCameraZone != null)
            activeFixedCameraZone.SetCameraSelected(false);

        activeFixedCameraZone = nextCamera;
        fixedCameraActive = activeFixedCameraZone != null;
        if (activeFixedCameraZone != null)
            activeFixedCameraZone.SetCameraSelected(true);
        else
            ScheduleGameplayBlendRestore();
    }

    private void EnsureCinemachineRig()
    {
        if (brain == null && !TryGetComponent(out brain))
            brain = gameObject.AddComponent<CinemachineBrain>();

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
        PrepareCameraSwitch(new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Styles.Cut, 0f));
    }

    private void PrepareCameraSwitch(CinemachineBlendDefinition blend)
    {
        if (fixedCameraBlendRestoreRoutine != null)
        {
            StopCoroutine(fixedCameraBlendRestoreRoutine);
            fixedCameraBlendRestoreRoutine = null;
        }

        if (brain == null)
            return;

        if (!hasSavedDefaultBlend)
        {
            gameplayDefaultBlend = brain.DefaultBlend;
            hasSavedDefaultBlend = true;
        }

        brain.DefaultBlend = blend;
    }

    private void ScheduleGameplayBlendRestore()
    {
        if (fixedCameraBlendRestoreRoutine != null)
            StopCoroutine(fixedCameraBlendRestoreRoutine);
        fixedCameraBlendRestoreRoutine =
            StartCoroutine(RestoreGameplayBlendAfterTransition());
    }

    private IEnumerator RestoreGameplayBlendAfterTransition()
    {
        // Aguarda o Brain confirmar que a camera de follow ja foi escolhida.
        // A coroutine retoma em Update, antes de o Brain avaliar LateUpdate;
        // restaurar imediatamente faria a saida ignorar o blend configurado.
        int waitedFrames = 0;
        while (!fixedCameraActive && brain != null && gameplayCamera != null &&
            !System.Object.ReferenceEquals(brain.ActiveVirtualCamera,
                gameplayCamera) && waitedFrames < 4)
        {
            waitedFrames++;
            yield return null;
        }

        // Garante que pelo menos um frame recebeu a configuracao deste trigger
        // antes de devolver ao gameplay o blend padrao.
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

        if (effectStack != null)
            effectStack.Initialize(gameplayCamera);
        if (gameplayEffects != null)
            gameplayEffects.Initialize(target, effectStack);
        if (playerVisibility != null)
            playerVisibility.Initialize(target, GetComponent<Camera>());
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
        FollowCameraSettings settings = EvaluateFollowCameraSettings();

        trackingPivot.rotation = Quaternion.Euler(
            settings.pitch + effects.pitchOffset,
            settings.yaw + effects.yawOffset,
            effects.rollOffset
        );
        thirdPersonFollow.Damping = Vector3.one * settings.smoothTime;
        thirdPersonFollow.ShoulderOffset = effects.cameraLocalOffset + settings.positionOffset;
        thirdPersonFollow.CameraDistance = Mathf.Max(
            .1f,
            settings.distance + effects.distanceOffset
        );
        gameplayCamera.Lens.FieldOfView = Mathf.Clamp(
            settings.fieldOfView + effects.fieldOfViewOffset,
            1f,
            179f
        );
        gameplayCamera.Priority = gameplayCameraPriority;
    }

    private FollowCameraSettings EvaluateFollowCameraSettings()
    {
        FollowCameraSettings settings = activeFixedCameraZone != null &&
            activeFixedCameraZone.CameraMode == CameraZoneMode.FollowWithOverrides
            ? new FollowCameraSettings
            {
                yaw = activeFixedCameraZone.FollowYaw,
                pitch = activeFixedCameraZone.FollowPitch,
                smoothTime = activeFixedCameraZone.FollowSmoothTime,
                positionOffset = activeFixedCameraZone.FollowPositionOffset,
                distance = activeFixedCameraZone.FollowDistance,
                fieldOfView = activeFixedCameraZone.FollowFieldOfView
            }
            : new FollowCameraSettings
            {
                yaw = yaw,
                pitch = pitch,
                smoothTime = followSmoothTime,
                positionOffset = Vector3.zero,
                distance = distance,
                fieldOfView = baseFieldOfView
            };

        if (!followTransitionActive)
            return settings;

        followTransitionElapsed += Time.deltaTime;
        float linearT = Mathf.Clamp01(followTransitionElapsed / followTransitionDuration);
        float t = followTransitionCurve != null
            ? Mathf.Clamp01(followTransitionCurve.Evaluate(linearT))
            : Mathf.SmoothStep(0f, 1f, linearT);
        FollowCameraSettings blended = new()
        {
            yaw = Mathf.LerpAngle(followTransitionFrom.yaw, followTransitionTo.yaw, t),
            pitch = Mathf.Lerp(followTransitionFrom.pitch, followTransitionTo.pitch, t),
            smoothTime = Mathf.Lerp(followTransitionFrom.smoothTime, followTransitionTo.smoothTime, t),
            positionOffset = Vector3.Lerp(followTransitionFrom.positionOffset,
                followTransitionTo.positionOffset, t),
            distance = Mathf.Lerp(followTransitionFrom.distance, followTransitionTo.distance, t),
            fieldOfView = Mathf.Lerp(followTransitionFrom.fieldOfView,
                followTransitionTo.fieldOfView, t)
        };
        if (linearT >= 1f)
            followTransitionActive = false;
        return blended;
    }

    private void BeginFollowCameraTransition(FixedCameraZone nextCamera,
        CinemachineBlendDefinition blend)
    {
        followTransitionFrom = EvaluateFollowCameraSettings();
        followTransitionTo = nextCamera != null
            ? new FollowCameraSettings
            {
                yaw = nextCamera.FollowYaw,
                pitch = nextCamera.FollowPitch,
                smoothTime = nextCamera.FollowSmoothTime,
                positionOffset = nextCamera.FollowPositionOffset,
                distance = nextCamera.FollowDistance,
                fieldOfView = nextCamera.FollowFieldOfView
            }
            : new FollowCameraSettings
            {
                yaw = yaw,
                pitch = pitch,
                smoothTime = followSmoothTime,
                positionOffset = Vector3.zero,
                distance = distance,
                fieldOfView = baseFieldOfView
            };
        followTransitionDuration = blend.BlendTime;
        followTransitionCurve = blend.BlendCurve;
        followTransitionElapsed = 0f;
        followTransitionActive = followTransitionDuration > 0f;
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
            // Agachar e deslizar encolhem a capsula mantendo os pes no chao.
            // A camera deve seguir sempre o centro da capsula EM PE, para nao
            // descer junto com o encolhimento.
            if (targetMovement == null)
                target.TryGetComponent(out targetMovement);
            float standingHeight = bounds.size.y *
                (targetMovement != null ? targetMovement.StandingHeightRatio : 1f);
            Vector3 standingCenter = new(bounds.center.x,
                bounds.min.y + standingHeight * .5f, bounds.center.z);
            float heightOffset = standingHeight *
                (targetHeightOffsetPercent / 100f);
            return standingCenter + Vector3.up * heightOffset;
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
        float lookScale = GameInput.LookIsMouseDelta
            ? horizontalMouseSensitivity
            : gamepadLookSpeed * Time.unscaledDeltaTime;
        yaw += mouseDelta.x * lookScale * sensitivity;
        float verticalSign = invertVerticalInput ^
            GameSettingsService.Applied.invertCameraY ? 1f : -1f;
        pitch = Mathf.Clamp(
            pitch + mouseDelta.y * (GameInput.LookIsMouseDelta
                ? verticalMouseSensitivity
                : gamepadLookSpeed * Time.unscaledDeltaTime) *
                sensitivity * verticalSign,
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
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            LockCursor();
        return Cursor.lockState == CursorLockMode.Locked;
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
