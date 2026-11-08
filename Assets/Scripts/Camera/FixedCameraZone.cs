using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>Comportamento da camera selecionada por um trigger.</summary>
public enum CameraZoneMode
{
    /// <summary>Posicao e rotacao fixas, exatamente como autoradas na CinemachineCamera.</summary>
    Fixed,
    /// <summary>Posicao fixa; a camera gira para manter o player no centro da tela, com o atraso da camera de follow.</summary>
    StaticLookAtTarget,
    /// <summary>A camera desliza sobre um spline acompanhando o player, sem girar, e para ao chegar nas pontas do spline.</summary>
    OnRails,
    /// <summary>A camera de gameplay continua seguindo o jogador com ajustes proprios de posicao e lente.</summary>
    FollowWithOverrides
}

/// <summary>Define se a rotacao da camera On Rails vem do spline ou do Transform autorado.</summary>
public enum CameraRailRotationMode
{
    [Tooltip("Mantem a rotacao definida no Transform da CinemachineCamera.")]
    Fixed,
    [Tooltip("Acompanha a orientacao do spline em cada ponto do trilho.")]
    FollowSpline,
    [Tooltip("Gira a camera para mirar o jogador, usando o Look At Offset configurado abaixo.")]
    LookAtPlayer
}

/// <summary>Como a camera escolhe sua posicao longitudinal no trilho.</summary>
public enum CameraRailDollyMode
{
    [Tooltip("A camera acompanha a posicao mais proxima do alvo no spline.")]
    FollowTarget,
    [Tooltip("A camera fica em uma distancia autorada no spline, ideal para enquadramentos cinematograficos controlados.")]
    ManualPosition
}

/// <summary>
/// Ao receber o jogador, seleciona uma camera Cinemachine ate outro trigger
/// selecionar uma camera diferente ou devolver o controle ao CameraFollow.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
[AddComponentMenu("Projeto PSX/Camera/Camera Trigger")]
public sealed class FixedCameraZone : MonoBehaviour
{
    private enum ZoneShape
    {
        Box,
        Sphere
    }

    [Header("Enquadramento")]
    [Tooltip("CinemachineCamera posicionada e rotacionada para este enquadramento. Deixe-a ativa na cena; Follow/LookAt sao configurados por esta zona conforme o modo.")]
    [SerializeField] private CinemachineCamera fixedCamera;
    [Tooltip("Ative neste trigger para devolver a camera ao CameraFollow (camera livre). Nao precisa de uma CinemachineCamera.")]
    [SerializeField] private bool returnToFreeCamera;
    [Tooltip("Ative para fazer blend da camera atual para a selecionada ao entrar neste trigger.")]
    [SerializeField] private bool useCameraTransition;
    [Tooltip("Curva e duracao do blend entre a camera anterior e esta. Usado quando Use Camera Transition esta ativo.")]
    [SerializeField] private CinemachineBlendDefinition cameraTransition =
        new(CinemachineBlendDefinition.Styles.EaseInOut, .5f);
    [Tooltip("Fixed: camera parada. Static Look At Target: camera parada que mira o jogador. On Rails: camera percorre um spline com ajustes proprios. Follow With Overrides: camera de gameplay segue o jogador com os ajustes abaixo.")]
    [SerializeField] private CameraZoneMode cameraMode = CameraZoneMode.Fixed;
    [Header("Follow With Overrides")]
    [Tooltip("Deslocamento local em Follow With Overrides. Em On Rails soma-se ao offset da camera; cada eixo pode usar o mundo ou acompanhar a orientacao do spline pelos controles abaixo.")]
    [SerializeField] private Vector3 followPositionOffset;
    [Tooltip("Distancia da camera ao jogador em Follow With Overrides. Em On Rails, define quanto a camera recua pelo spline a partir do ponto mais proximo do jogador.")]
    [Min(.1f)] [SerializeField] private float followDistance = 5.7f;
    [Tooltip("Direcao horizontal fixa da camera em graus no mundo. Ajuste este angulo para girar a camera ao redor do jogador; 180 graus coloca a camera no lado oposto ao valor zero.")]
    [SerializeField] private float followYaw = 180f;
    [Tooltip("Inclinacao vertical fixa da camera em graus. Valores positivos olham de cima para baixo.")]
    [Range(-89f, 89f)] [SerializeField] private float followPitch = 18f;
    [Tooltip("Tempo de suavizacao do acompanhamento neste modo. Zero acompanha instantaneamente.")]
    [Min(0f)] [SerializeField] private float followSmoothTime = .08f;
    [Tooltip("Campo de visao usado pela camera de gameplay neste modo.")]
    [Range(1f, 179f)] [SerializeField] private float followFieldOfView = 60f;
    [Header("On Rails")]
    [Tooltip("Spline percorrido pela camera. Deve ser ABERTO: em um spline fechado a camera nao trava no fim.")]
    [SerializeField] private SplineContainer railSpline;
    [Tooltip("Deslocamento mundial XYZ da camera em metros, independente da rotacao do spline. Follow Position Offset soma-se a este valor.")]
    [SerializeField] private Vector3 railOffset;
    [Tooltip("No modo On Rails, mantenha a rotacao autorada, acompanhe o spline ou mire o jogador.")]
    [SerializeField] private CameraRailRotationMode railRotationMode = CameraRailRotationMode.Fixed;
    [Tooltip("Rotacao da camera em graus: X = pitch, Y = yaw, Z = roll. Zero deixa a camera alinhada ao mundo; os controles abaixo podem fazer cada eixo acompanhar a orientacao do spline.")]
    [SerializeField] private Vector3 railWorldRotationOffset;
    [Tooltip("Faz o pitch do Rail World Rotation Offset acompanhar o eixo local de pitch do spline. Tambem orienta a camera pela curva.")]
    [SerializeField] private bool railPitchFollowSpline;
    [Tooltip("Faz o yaw do Rail World Rotation Offset acompanhar o eixo local de yaw do spline. Tambem orienta a camera pela curva.")]
    [SerializeField] private bool railYawFollowSpline;
    [Tooltip("Faz o roll do Rail World Rotation Offset acompanhar o eixo local de roll do spline. Tambem orienta a camera pela curva.")]
    [SerializeField] private bool railRollFollowSpline;
    [Tooltip("Follow Target busca a posicao mais proxima do alvo no trilho. Manual Position usa uma distancia fixa no caminho, util para tomadas autoradas.")]
    [SerializeField] private CameraRailDollyMode railDollyMode = CameraRailDollyMode.FollowTarget;
    [Tooltip("Alvo que move a camera ao longo do trilho. Vazio usa o jogador.")]
    [SerializeField] private Transform railDollyTarget;
    [Tooltip("Distancia ao longo do spline usada em Manual Position. O valor e limitado ao comprimento do trilho aberto.")]
    [Min(0f)] [SerializeField] private float railManualPosition;
    [Tooltip("Resolucao da busca do ponto mais proximo do alvo no spline. Valores maiores aumentam a precisao e o custo; use o minimo necessario.")]
    [Range(1, 32)] [SerializeField] private int railSearchResolution = 4;
    [Tooltip("Iteracoes de refinamento da busca do ponto mais proximo. Valores maiores aumentam a precisao e o custo.")]
    [Range(1, 12)] [SerializeField] private int railSearchIterations = 2;
    [Tooltip("Ajuste assinado em metros ao longo do spline. Soma-se a Follow Distance: negativo recua mais; positivo avanca pelo trilho.")]
    [SerializeField] private float railDistanceOffset;
    [Tooltip("Substitui o campo de visao da CinemachineCamera enquanto este trigger On Rails estiver ativo.")]
    [SerializeField] private bool overrideRailFieldOfView = true;
    [Tooltip("Campo de visao aplicado enquanto este trigger On Rails estiver ativo.")]
    [Range(1f, 179f)] [SerializeField] private float railFieldOfView = 60f;
    [Tooltip("Deslocamento do ponto para o qual a camera mira no modo Look At Player.")]
    [SerializeField] private Vector3 railLookAtOffset;
    [Tooltip("Alvo que a camera enquadra em Look At Player. Vazio usa o jogador. O offset e aplicado em eixos do mundo.")]
    [SerializeField] private Transform railLookAtTarget;
    [Tooltip("Posicao horizontal e vertical do alvo na tela. (0, 0) centraliza; valores negativos ou positivos deslocam o enquadramento.")]
    [SerializeField] private Vector2 railScreenPosition;
    [Tooltip("A camera so corrige o enquadramento quando o alvo sai desta zona morta normalizada da tela.")]
    [SerializeField] private Vector2 railDeadZoneSize;
    [Tooltip("Ativa a zona morta para reduzir pequenos movimentos de enquadramento.")]
    [SerializeField] private bool railDeadZoneEnabled;
    [Tooltip("Impede que o alvo saia desta area normalizada da tela, mantendo o personagem enquadrado em tomadas de movimento intenso.")]
    [SerializeField] private Vector2 railHardLimitsSize = new(.9f, .9f);
    [Tooltip("Ativa os limites que mantem o alvo dentro da area definida na tela.")]
    [SerializeField] private bool railHardLimitsEnabled;
    [Tooltip("Desloca os limites de enquadramento horizontal e verticalmente em relacao a posicao do alvo na tela.")]
    [SerializeField] private Vector2 railHardLimitsOffset;
    [Tooltip("Tempo de suavizacao horizontal e vertical da mira. Valores maiores deixam a camera mais pesada.")]
    [Min(0f)] [SerializeField] private Vector2 railAimDamping = new(.2f, .2f);
    [Tooltip("Prevê a posicao futura do alvo para antecipar o enquadramento. Valores altos podem amplificar tremores da animacao.")]
    [SerializeField] private bool railLookaheadEnabled;
    [Tooltip("Segundos de previsao do movimento do alvo para antecipar a camera.")]
    [Range(0f, 1f)] [SerializeField] private float railLookaheadTime = .15f;
    [Tooltip("Suavizacao da previsao do alvo; valores altos reduzem tremores, mas aumentam o atraso.")]
    [Range(0f, 30f)] [SerializeField] private float railLookaheadSmoothing = 10f;
    [Tooltip("Ignora o movimento vertical do alvo ao prever a posicao de enquadramento.")]
    [SerializeField] private bool railLookaheadIgnoreY;
    [Tooltip("Atraso de acompanhamento desta camera. Desmarcado usa o mesmo atraso (Follow Smooth Time) da camera de gameplay.")]
    [SerializeField] private bool overrideFollowDelay;
    [Tooltip("Atraso, em segundos, usado quando 'Override Follow Delay' esta marcado. Zero acompanha instantaneamente.")]
    [Min(0f)] [SerializeField] private float followDelaySeconds = .08f;
    [Tooltip("Prioridade desta zona. Deve ser maior que a camera de gameplay. Em sobreposicao vence a prioridade maior; em empate, a zona que o jogador entrou mais recentemente vence.")]
    [SerializeField] private int cameraPriority = 20;
    [Tooltip("Enquanto marcado, esta camera so permanece selecionada enquanto o jogador estiver na zona. Ao sair, volta para a camera livre. Desmarcado, a selecao fica ativa ate outro trigger trocar de camera.")]
    [SerializeField] private bool activeOnlyInsideZone;

    [Header("Trigger")]
    [Tooltip("Collider trigger que ativa a troca ao ser atravessado pelo jogador. O componente configura este collider automaticamente.")]
    [SerializeField] private BoxCollider activationVolume;
    [Tooltip("Formato da area. Box usa dimensoes X/Y/Z; Sphere usa o raio configurado abaixo.")]
    [SerializeField] private ZoneShape zoneShape = ZoneShape.Box;
    [Tooltip("Dimensoes X/Y/Z da area no formato Box. Zero mantem o tamanho configurado no BoxCollider.")]
    [SerializeField] private Vector3 areaSize;
    [Tooltip("Raio da area quando Sphere estiver selecionado.")]
    [Min(.01f)] [SerializeField] private float sphereRadius = 5f;
    [Tooltip("Colliders Box ou Sphere adicionais que pertencem a esta mesma camera. As areas devem se tocar ou sobrepor para atravessar entre elas sem sair da camera.")]
    [SerializeField] private List<Collider> additionalActivationVolumes = new();
    [Tooltip("Depois de entrar, expande a area usada para detectar a saida. Aplica-se em cada face da Box ou ao raio da Sphere; a entrada continua usando a area original.")]
    [Min(0f)] [SerializeField] private float exitPaddingDistance = .5f;

    private CameraFollow registeredCameraFollow;
    private CinemachineRotationComposer rotationComposer;
    private CinemachineSplineDolly splineDolly;
    private CameraRailWorldRotation railWorldRotation;
    private bool warnedInvalidRail;
    private SphereCollider sphereActivationVolume;
    private readonly List<ExitVolumePair> expandedExitVolumes = new();
    private readonly List<Collider> registeredAdditionalVolumes = new();
    private float previousRailFieldOfView;
    private bool hasSavedRailFieldOfView;
    private readonly List<RailBodyComponentState> railBodyComponents = new();
    private bool playerWasInside;
    private bool cameraSelected;
    private bool activationVolumesDirty = true;
    private bool cameraSettingsDirty;
    private bool runtimeInitialized;
    private int lastEnteredFrame = int.MinValue;

    internal int CameraPriority => cameraPriority;
    internal int LastEnteredFrame => lastEnteredFrame;
    internal float WorldVolume => GetCombinedWorldVolume();
    internal CameraZoneMode CameraMode => cameraMode;
    internal Vector3 FollowPositionOffset => followPositionOffset;
    internal float FollowDistance => Mathf.Max(.1f, followDistance);
    internal float FollowYaw => followYaw;
    internal float FollowPitch => followPitch;
    internal float FollowSmoothTime => Mathf.Max(0f, followSmoothTime);
    internal float FollowFieldOfView => Mathf.Clamp(followFieldOfView, 1f, 179f);
    internal bool UseCameraTransition => useCameraTransition;
    internal CinemachineBlendDefinition CameraTransition => cameraTransition;

    private sealed class ExitVolumePair
    {
        public Collider source;
        public Collider expanded;
    }

    private sealed class RailBodyComponentState
    {
        public CinemachineComponentBase component;
        public bool wasEnabled;
    }

    private void Awake()
    {
        ResolveReferences();
        SetCameraSelected(false);
    }

    private void Start()
    {
        if (!Application.isPlaying)
            return;
        runtimeInitialized = true;
        ProcessActivationVolumes();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SetCameraSelected(false);
        activationVolumesDirty = true;
        if (Application.isPlaying && runtimeInitialized)
            ProcessActivationVolumes();
        RegisterWithCameraFollow();
    }

    private void Update()
    {
        if (activationVolumesDirty && Application.isPlaying)
            ProcessActivationVolumes();

        if (Application.isPlaying && activeOnlyInsideZone && cameraSelected &&
            registeredCameraFollow != null)
        {
            Transform player = registeredCameraFollow.Target;
            PlayerCharacterController controller = player != null
                ? player.GetComponent<PlayerCharacterController>() : null;
            if (controller == null && player != null)
                controller = player.GetComponentInParent<PlayerCharacterController>();
            Collider playerCollider = controller != null
                ? controller.Collider
                : player != null ? player.GetComponent<Collider>() : null;
            if (!IsPlayerInside(playerCollider))
                registeredCameraFollow.DeactivateCameraTrigger(this);
        }

        if (cameraSettingsDirty && Application.isPlaying)
        {
            cameraSettingsDirty = false;
            if (cameraSelected)
            {
                if (!returnToFreeCamera &&
                    cameraMode == CameraZoneMode.OnRails &&
                    splineDolly != null && fixedCamera != null &&
                    splineDolly.gameObject == fixedCamera.gameObject)
                    ApplyRailSettingsInPlace();
                else
                    SetCameraSelected(true);
            }
        }

        if (cameraSelected && cameraMode == CameraZoneMode.OnRails &&
            railRotationMode == CameraRailRotationMode.LookAtPlayer)
            RefreshRailAimWorldOffset();

        if (registeredCameraFollow == null ||
            registeredCameraFollow != CameraFollow.Instance)
        {
            if (registeredCameraFollow != null)
                registeredCameraFollow.UnregisterFixedCameraZone(this);

            registeredCameraFollow = CameraFollow.Instance;
            if (registeredCameraFollow != null)
            registeredCameraFollow.RegisterFixedCameraZone(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleTriggerEnter(other);
    }

    internal void HandleTriggerEnter(Collider other)
    {
        if (!isActiveAndEnabled || registeredCameraFollow == null || !IsPlayerCollider(other))
            return;
        registeredCameraFollow.ActivateCameraTrigger(this);
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null || registeredCameraFollow == null || registeredCameraFollow.Target == null)
            return false;
        Transform player = registeredCameraFollow.Target;
        if (other.transform == player || other.transform.IsChildOf(player))
            return true;

        PlayerCharacterController controller = other.GetComponent<PlayerCharacterController>();
        if (controller == null)
            controller = other.GetComponentInParent<PlayerCharacterController>();
        if (controller == null)
            return false;

        return controller.Collider == other || controller.transform == player ||
            controller.transform.IsChildOf(player) || player.IsChildOf(controller.transform);
    }

    internal bool ReturnsToFreeCamera => returnToFreeCamera;

    private void OnDisable()
    {
        UnregisterAdditionalVolumes();
        if (registeredCameraFollow != null)
            registeredCameraFollow.UnregisterFixedCameraZone(this);

        registeredCameraFollow = null;
        playerWasInside = false;
        SetCameraSelected(false);
    }

    private void OnDestroy()
    {
        foreach (ExitVolumePair pair in expandedExitVolumes)
        {
            if (pair.expanded != null)
                Destroy(pair.expanded.gameObject);
        }
    }

    private void OnValidate()
    {
        cameraPriority = Mathf.Max(1, cameraPriority);
        followDistance = Mathf.Max(.1f, followDistance);
        followSmoothTime = Mathf.Max(0f, followSmoothTime);
        followFieldOfView = Mathf.Clamp(followFieldOfView, 1f, 179f);
        followPitch = Mathf.Clamp(followPitch, -89f, 89f);
        railFieldOfView = Mathf.Clamp(railFieldOfView, 1f, 179f);
        railManualPosition = Mathf.Max(0f, railManualPosition);
        railSearchResolution = Mathf.Clamp(railSearchResolution, 1, 32);
        railSearchIterations = Mathf.Clamp(railSearchIterations, 1, 12);
        railAimDamping.x = Mathf.Max(0f, railAimDamping.x);
        railAimDamping.y = Mathf.Max(0f, railAimDamping.y);
        railLookaheadTime = Mathf.Clamp01(railLookaheadTime);
        railLookaheadSmoothing = Mathf.Max(0f, railLookaheadSmoothing);
        cameraTransition.Time = Mathf.Max(0f, cameraTransition.Time);
        ResolveReferences();
        exitPaddingDistance = Mathf.Max(0f, exitPaddingDistance);
        sphereRadius = Mathf.Max(.01f, sphereRadius);
        if (areaSize != Vector3.zero)
            areaSize = new Vector3(Mathf.Max(.01f, areaSize.x),
                Mathf.Max(.01f, areaSize.y), Mathf.Max(.01f, areaSize.z));
        if (additionalActivationVolumes == null)
            additionalActivationVolumes = new List<Collider>();
        if (Application.isPlaying)
        {
            activationVolumesDirty = true;
            cameraSettingsDirty = true;
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && !PlayerDebugToolkitHUD.IsVisible)
            return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        if (zoneShape == ZoneShape.Box)
        {
            BoxCollider box = activationVolume != null
                ? activationVolume : GetComponent<BoxCollider>();
            if (box == null)
                return;
            Gizmos.matrix = box.transform.localToWorldMatrix;
            Vector3 size = GetBoxAreaSize(box);
            if (activeOnlyInsideZone)
                DrawFilledBox(box.center, size, ExpandBoxSize(size,
                    box.transform.lossyScale, exitPaddingDistance));
            else
                DrawSingleBox(box.center, size);
        }
        else
        {
            Transform volumeTransform = activationVolume != null
                ? activationVolume.transform : transform;
            Vector3 localCenter = activationVolume != null
                ? activationVolume.center : Vector3.zero;
            Vector3 center = volumeTransform.TransformPoint(localCenter);
            float radius = GetSphereAreaRadius() *
                MaxAbsComponent(volumeTransform.lossyScale);
            Gizmos.matrix = Matrix4x4.identity;
            if (activeOnlyInsideZone)
                DrawFilledSphere(center, radius, radius + exitPaddingDistance);
            else
                DrawSingleSphere(center, radius);
        }

        if (additionalActivationVolumes != null)
        {
            foreach (Collider additionalVolume in additionalActivationVolumes)
            {
                if (additionalVolume == null ||
                    additionalVolume == GetSelectedVolume())
                    continue;
                DrawAdditionalVolume(additionalVolume);
            }
        }

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    private void DrawAdditionalVolume(Collider volume)
    {
        if (volume is BoxCollider box)
        {
            Gizmos.matrix = box.transform.localToWorldMatrix;
            if (activeOnlyInsideZone)
                DrawFilledBox(box.center, box.size, ExpandBoxSize(box.size,
                    box.transform.lossyScale, exitPaddingDistance));
            else
                DrawSingleBox(box.center, box.size);
        }
        else if (volume is SphereCollider sphere)
        {
            Vector3 center = sphere.transform.TransformPoint(sphere.center);
            float radius = sphere.radius *
                MaxAbsComponent(sphere.transform.lossyScale);
            Gizmos.matrix = Matrix4x4.identity;
            if (activeOnlyInsideZone)
                DrawFilledSphere(center, radius, radius + exitPaddingDistance);
            else
                DrawSingleSphere(center, radius);
        }
    }

    private static void DrawSingleBox(Vector3 center, Vector3 size)
    {
        Gizmos.color = new Color(.04f, .2f, .07f, .2f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = new Color(.06f, .25f, .09f, .95f);
        Gizmos.DrawWireCube(center, size);
    }

    private static void DrawSingleSphere(Vector3 center, float radius)
    {
        Gizmos.color = new Color(.04f, .2f, .07f, .2f);
        Gizmos.DrawSphere(center, radius);
        Gizmos.color = new Color(.06f, .25f, .09f, .95f);
        Gizmos.DrawWireSphere(center, radius);
    }

    private static void DrawFilledBox(Vector3 center, Vector3 size,
        Vector3 exitSize)
    {
        Gizmos.color = new Color(.025f, .09f, .035f, .06f);
        Gizmos.DrawCube(center, exitSize);
        Gizmos.color = new Color(.04f, .16f, .06f, .8f);
        Gizmos.DrawWireCube(center, exitSize);
        Gizmos.color = new Color(.04f, .2f, .07f, .2f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = new Color(.06f, .25f, .09f, .95f);
        Gizmos.DrawWireCube(center, size);
    }

    private static void DrawFilledSphere(Vector3 center, float radius,
        float exitRadius)
    {
        Gizmos.color = new Color(.025f, .09f, .035f, .06f);
        Gizmos.DrawSphere(center, exitRadius);
        Gizmos.color = new Color(.04f, .16f, .06f, .8f);
        Gizmos.DrawWireSphere(center, exitRadius);
        Gizmos.color = new Color(.04f, .2f, .07f, .2f);
        Gizmos.DrawSphere(center, radius);
        Gizmos.color = new Color(.06f, .25f, .09f, .95f);
        Gizmos.DrawWireSphere(center, radius);
    }

    internal bool IsPlayerInside(Collider playerCollider)
    {
        UpdateExpandedExitVolumes();
        bool canCheck = isActiveAndEnabled &&
            fixedCamera != null && fixedCamera.isActiveAndEnabled &&
            playerCollider != null && playerCollider.enabled &&
            playerCollider.gameObject.activeInHierarchy;
        bool isInside = canCheck && HasPlayerOverlap(playerCollider,
            activeOnlyInsideZone && playerWasInside);

        if (isInside && !playerWasInside)
            lastEnteredFrame = Time.frameCount;
        playerWasInside = isInside;
        return isInside;
    }

    private void CreateExpandedExitVolumes()
    {
        foreach (ExitVolumePair pair in expandedExitVolumes)
        {
            if (pair.expanded != null)
                Destroy(pair.expanded.gameObject);
        }
        expandedExitVolumes.Clear();
        AddExpandedExitVolume(GetSelectedVolume());
        if (additionalActivationVolumes == null)
            return;

        foreach (Collider volume in additionalActivationVolumes)
            AddExpandedExitVolume(volume);
    }

    private void AddExpandedExitVolume(Collider volume)
    {
        if (volume == null || !(volume is BoxCollider ||
            volume is SphereCollider) || HasExpandedPair(volume))
            return;

        GameObject exitObject = new("Fixed Camera Exit Margin (Runtime)");
        exitObject.hideFlags = HideFlags.HideAndDontSave;
        exitObject.transform.SetParent(volume.transform, false);
        Collider expanded = volume is SphereCollider
            ? exitObject.AddComponent<SphereCollider>()
            : exitObject.AddComponent<BoxCollider>();
        expanded.isTrigger = true;
        expandedExitVolumes.Add(new ExitVolumePair
        {
            source = volume,
            expanded = expanded
        });
    }

    private bool HasExpandedPair(Collider source)
    {
        foreach (ExitVolumePair pair in expandedExitVolumes)
        {
            if (pair.source == source)
                return true;
        }
        return false;
    }

    private void UpdateExpandedExitVolumes()
    {
        foreach (ExitVolumePair pair in expandedExitVolumes)
        {
            if (pair.source == null || pair.expanded == null)
                continue;

            pair.expanded.enabled = pair.source.enabled &&
                pair.source.gameObject.activeInHierarchy;
            if (pair.source is SphereCollider sourceSphere &&
                pair.expanded is SphereCollider exitSphere)
            {
                exitSphere.center = sourceSphere.center;
                float baseRadius = pair.source == sphereActivationVolume
                    ? GetSphereAreaRadius() : sourceSphere.radius;
                float worldScale = Mathf.Max(.0001f,
                    MaxAbsComponent(sourceSphere.transform.lossyScale));
                float padding = activeOnlyInsideZone ? exitPaddingDistance : 0f;
                exitSphere.radius = baseRadius + padding / worldScale;
            }
            else if (pair.source is BoxCollider sourceBox &&
                pair.expanded is BoxCollider exitBox)
            {
                exitBox.center = sourceBox.center;
                Vector3 baseSize = pair.source == activationVolume
                    ? GetBoxAreaSize(sourceBox) : sourceBox.size;
                float padding = activeOnlyInsideZone ? exitPaddingDistance : 0f;
                exitBox.size = ExpandBoxSize(baseSize,
                    sourceBox.transform.lossyScale, padding);
            }
        }
    }

    private bool HasPlayerOverlap(Collider playerCollider, bool useExpandedVolumes)
    {
        foreach (ExitVolumePair pair in expandedExitVolumes)
        {
            Collider source = pair.source;
            Collider candidate = useExpandedVolumes ? pair.expanded : source;
            if (source == null || candidate == null || !source.enabled ||
                !source.gameObject.activeInHierarchy || !candidate.enabled)
                continue;

            if (Physics.ComputePenetration(candidate,
                candidate.transform.position, candidate.transform.rotation,
                playerCollider, playerCollider.transform.position,
                playerCollider.transform.rotation, out _, out _))
            {
                return true;
            }
        }

        return false;
    }

    private float GetCombinedWorldVolume()
    {
        float totalVolume = 0f;
        foreach (ExitVolumePair pair in expandedExitVolumes)
        {
            if (pair.source == null)
                continue;
            Vector3 size = pair.source.bounds.size;
            totalVolume += size.x * size.y * size.z;
        }

        if (totalVolume > 0f)
            return totalVolume;
        return activationVolume != null
            ? activationVolume.bounds.size.x * activationVolume.bounds.size.y *
              activationVolume.bounds.size.z
            : float.PositiveInfinity;
    }

    private void ConfigureActivationVolumes()
    {
        if (!TryGetComponent(out Rigidbody triggerBody))
            triggerBody = gameObject.AddComponent<Rigidbody>();
        triggerBody.isKinematic = true;
        triggerBody.useGravity = false;
        triggerBody.constraints = RigidbodyConstraints.FreezeAll;

        if (activationVolume == null)
            activationVolume = GetComponent<BoxCollider>();
        if (activationVolume == null)
            activationVolume = gameObject.AddComponent<BoxCollider>();

        activationVolume.isTrigger = true;
        activationVolume.enabled = zoneShape == ZoneShape.Box;
        if (zoneShape == ZoneShape.Box)
        {
            activationVolume.size = GetBoxAreaSize(activationVolume);
        }
        else
        {
            sphereActivationVolume = GetComponent<SphereCollider>();
            if (sphereActivationVolume == null)
                sphereActivationVolume = gameObject.AddComponent<SphereCollider>();
            sphereActivationVolume.center = activationVolume.center;
            sphereActivationVolume.radius = GetSphereAreaRadius();
            sphereActivationVolume.isTrigger = true;
            sphereActivationVolume.enabled = true;
        }

        if (additionalActivationVolumes == null)
            return;
        foreach (Collider additionalVolume in additionalActivationVolumes)
        {
            if (additionalVolume != null)
            {
                additionalVolume.isTrigger = true;
                EnsureTriggerBody(additionalVolume);
            }
        }
    }

    private void ProcessActivationVolumes()
    {
        activationVolumesDirty = false;
        ConfigureActivationVolumes();
        RegisterAdditionalVolumes();
        CreateExpandedExitVolumes();
    }

    private void RegisterAdditionalVolumes()
    {
        UnregisterAdditionalVolumes();
        if (additionalActivationVolumes == null)
            return;

        foreach (Collider volume in additionalActivationVolumes)
        {
            if (volume == null || volume.gameObject == gameObject)
                continue;
            CameraTriggerVolumeRelay relay = volume.GetComponent<CameraTriggerVolumeRelay>();
            if (relay == null)
                relay = volume.gameObject.AddComponent<CameraTriggerVolumeRelay>();
            relay.Register(this);
            registeredAdditionalVolumes.Add(volume);
        }
    }

    private void UnregisterAdditionalVolumes()
    {
        foreach (Collider volume in registeredAdditionalVolumes)
        {
            if (volume == null)
                continue;
            CameraTriggerVolumeRelay relay = volume.GetComponent<CameraTriggerVolumeRelay>();
            if (relay != null)
                relay.Unregister(this);
        }
        registeredAdditionalVolumes.Clear();
    }

    private static void EnsureTriggerBody(Collider volume)
    {
        if (volume == null || volume.attachedRigidbody != null)
            return;
        Rigidbody body = volume.gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeAll;
    }

    private Collider GetSelectedVolume()
    {
        if (zoneShape == ZoneShape.Sphere)
        {
            if (sphereActivationVolume == null)
                sphereActivationVolume = GetComponent<SphereCollider>();
            return sphereActivationVolume;
        }

        if (activationVolume == null)
            activationVolume = GetComponent<BoxCollider>();
        return activationVolume;
    }

    private Vector3 GetBoxAreaSize(BoxCollider box)
    {
        if (areaSize == Vector3.zero)
            return box.size;
        return new Vector3(Mathf.Max(.01f, areaSize.x),
            Mathf.Max(.01f, areaSize.y), Mathf.Max(.01f, areaSize.z));
    }

    private static Vector3 ExpandBoxSize(Vector3 size, Vector3 lossyScale,
        float worldPadding)
    {
        Vector3 safeScale = new(
            Mathf.Max(.0001f, Mathf.Abs(lossyScale.x)),
            Mathf.Max(.0001f, Mathf.Abs(lossyScale.y)),
            Mathf.Max(.0001f, Mathf.Abs(lossyScale.z)));
        Vector3 localPadding = new(worldPadding / safeScale.x,
            worldPadding / safeScale.y, worldPadding / safeScale.z);
        return size + localPadding * 2f;
    }

    private float GetSphereAreaRadius() => Mathf.Max(.01f, sphereRadius);

    private static float MaxAbsComponent(Vector3 value) =>
        Mathf.Max(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

    internal void SetCameraSelected(bool selected)
    {
        cameraSelected = selected;
        cameraSettingsDirty = false;
        if (!selected)
        {
            RestoreRailFieldOfView();
            RestoreRailBodyComponents();
        }

        if (returnToFreeCamera || cameraMode == CameraZoneMode.FollowWithOverrides)
        {
            if (fixedCamera != null)
            {
                fixedCamera.Follow = null;
                fixedCamera.LookAt = null;
                fixedCamera.PreviousStateIsValid = false;
                SetModeComponentsEnabled(false, false);
                fixedCamera.Priority = 0;
            }
            return;
        }
        if (fixedCamera == null)
            return;

        fixedCamera.Follow = null;
        fixedCamera.LookAt = null;
        fixedCamera.PreviousStateIsValid = false;
        if (!selected)
        {
            SetModeComponentsEnabled(false, false);
            fixedCamera.Priority = 0;
            return;
        }

        ApplyCameraMode();
        ApplyRailFieldOfView();

        int minimumPriority = registeredCameraFollow != null
            ? registeredCameraFollow.GameplayCameraPriority + 2
            : 12;
        fixedCamera.Priority = Mathf.Max(cameraPriority, minimumPriority);
    }

    private void ApplyRailFieldOfView()
    {
        if (cameraMode != CameraZoneMode.OnRails || !overrideRailFieldOfView)
        {
            RestoreRailFieldOfView();
            return;
        }

        if (fixedCamera == null || registeredCameraFollow == null ||
            registeredCameraFollow.TrackingTarget == null || !IsRailSplineUsable())
        {
            RestoreRailFieldOfView();
            return;
        }

        if (!hasSavedRailFieldOfView)
        {
            previousRailFieldOfView = fixedCamera.Lens.FieldOfView;
            hasSavedRailFieldOfView = true;
        }
        fixedCamera.Lens.FieldOfView = railFieldOfView;
    }

    private void ApplyRailSettingsInPlace()
    {
        Transform tracking = registeredCameraFollow != null
            ? registeredCameraFollow.TrackingTarget : null;
        if (fixedCamera == null || tracking == null ||
            !IsRailSplineUsable())
        {
            SetCameraSelected(true);
            return;
        }

        // While the camera is live, update its existing pipeline components
        // without toggling them or invalidating Cinemachine's pipeline cache.
        if (splineDolly.Spline != railSpline)
            splineDolly.Spline = railSpline;
        if (!splineDolly.enabled)
            splineDolly.enabled = true;
        splineDolly.PositionUnits = PathIndexUnit.Distance;
        splineDolly.SplineOffset = Vector3.zero;
        MakeSplineDollyTheOnlyBodyComponent();
        splineDolly.CameraRotation = CinemachineSplineDolly.RotationMode.Default;
        ApplyRailWorldRotation();
        float delay = ResolveFollowDelay();
        ConfigureRailDolly(tracking, delay);
        ConfigureRailAim(tracking, delay);

        ApplyRailFieldOfView();
        int minimumPriority = registeredCameraFollow != null
            ? registeredCameraFollow.GameplayCameraPriority + 2
            : 12;
        fixedCamera.Priority = Mathf.Max(cameraPriority, minimumPriority);
    }

    private void RestoreRailFieldOfView()
    {
        if (!hasSavedRailFieldOfView)
            return;
        if (fixedCamera != null)
            fixedCamera.Lens.FieldOfView = previousRailFieldOfView;
        hasSavedRailFieldOfView = false;
    }

    /// <summary>Atraso aplicado aos modos que acompanham o player.</summary>
    private float ResolveFollowDelay()
    {
        if (overrideFollowDelay || registeredCameraFollow == null)
            return Mathf.Max(0f, followDelaySeconds);
        return registeredCameraFollow.FollowSmoothTime;
    }

    private void ApplyCameraMode()
    {
        Transform tracking = registeredCameraFollow != null
            ? registeredCameraFollow.TrackingTarget : null;
        CameraZoneMode mode = cameraMode;
        if (mode != CameraZoneMode.Fixed && tracking == null)
            mode = CameraZoneMode.Fixed;

        if (mode == CameraZoneMode.OnRails && !IsRailSplineUsable())
            mode = CameraZoneMode.Fixed;

        if (mode != CameraZoneMode.OnRails)
            RestoreRailBodyComponents();

        bool railLooksAtPlayer = mode == CameraZoneMode.OnRails &&
            railRotationMode == CameraRailRotationMode.LookAtPlayer;
        SetModeComponentsEnabled(mode == CameraZoneMode.StaticLookAtTarget ||
            railLooksAtPlayer, mode == CameraZoneMode.OnRails,
            mode == CameraZoneMode.OnRails);
        float delay = ResolveFollowDelay();

        if (mode == CameraZoneMode.StaticLookAtTarget)
        {
            if (rotationComposer == null &&
                !fixedCamera.TryGetComponent(out rotationComposer))
                rotationComposer = fixedCamera.gameObject
                    .AddComponent<CinemachineRotationComposer>();

            rotationComposer.enabled = true;
            rotationComposer.Composition.ScreenPosition = Vector2.zero;
            rotationComposer.Composition.DeadZone.Enabled = false;
            rotationComposer.Composition.HardLimits.Enabled = false;
            rotationComposer.TargetOffset = Vector3.zero;
            rotationComposer.CenterOnActivate = true;
            rotationComposer.Damping = new Vector2(delay, delay);
            fixedCamera.LookAt = tracking;
        }
        else if (mode == CameraZoneMode.OnRails)
        {
            if (splineDolly == null &&
                !fixedCamera.TryGetComponent(out splineDolly))
                splineDolly = fixedCamera.gameObject
                    .AddComponent<CinemachineSplineDolly>();

            splineDolly.enabled = true;
            splineDolly.Spline = railSpline;
            splineDolly.PositionUnits = PathIndexUnit.Distance;
            splineDolly.SplineOffset = Vector3.zero;
            MakeSplineDollyTheOnlyBodyComponent();
            splineDolly.CameraRotation = CinemachineSplineDolly.RotationMode.Default;
            ApplyRailWorldRotation();
            ConfigureRailDolly(tracking, delay);
            ConfigureRailAim(tracking, delay);
        }
    }

    private void ConfigureRailDolly(Transform player, float delay)
    {
        bool followsTarget = railDollyMode == CameraRailDollyMode.FollowTarget;
        Transform dollyTarget = railDollyTarget != null
            ? railDollyTarget : player;

        SplineAutoDolly autoDolly = splineDolly.AutomaticDolly;
        autoDolly.Enabled = followsTarget;
        if (followsTarget)
        {
            SplineAutoDolly.NearestPointToTarget nearest =
                autoDolly.Method as SplineAutoDolly.NearestPointToTarget;
            if (nearest == null)
                nearest = new SplineAutoDolly.NearestPointToTarget();
            nearest.PositionOffset = railDistanceOffset - followDistance;
            nearest.SearchResolution = Mathf.Max(1, railSearchResolution);
            nearest.SearchIteration = Mathf.Max(1, railSearchIterations);
            autoDolly.Method = nearest;
            if (fixedCamera.Follow != dollyTarget)
                fixedCamera.Follow = dollyTarget;
        }
        else
        {
            autoDolly.Method = null;
            float maxPosition = railSpline != null && railSpline.Spline != null
                ? railSpline.Spline.GetLength() : 0f;
            splineDolly.CameraPosition = Mathf.Clamp(railManualPosition,
                0f, maxPosition);
            if (fixedCamera.Follow != null)
                fixedCamera.Follow = null;
        }
        splineDolly.AutomaticDolly = autoDolly;

        // So o eixo longitudinal e suavizado: amortecer os eixos locais X/Y
        // faria a camera sair da curva e contrariaria o offset mundial autorado.
        CinemachineSplineDolly.DampingSettings damping = splineDolly.Damping;
        damping.Enabled = followsTarget && delay > 0f;
        damping.Position = new Vector3(0f, 0f, followsTarget ? delay : 0f);
        damping.Angular = 0f;
        splineDolly.Damping = damping;
    }

    private void ConfigureRailAim(Transform player, float delay)
    {
        bool looksAtTarget = railRotationMode ==
            CameraRailRotationMode.LookAtPlayer;
        if (looksAtTarget && rotationComposer == null &&
            !fixedCamera.TryGetComponent(out rotationComposer))
            rotationComposer = fixedCamera.gameObject
                .AddComponent<CinemachineRotationComposer>();

        if (rotationComposer != null)
            rotationComposer.enabled = looksAtTarget;

        Transform lookAt = looksAtTarget
            ? (railLookAtTarget != null ? railLookAtTarget : player)
            : null;
        if (fixedCamera.LookAt != lookAt)
            fixedCamera.LookAt = lookAt;
        if (!looksAtTarget)
            return;

        ScreenComposerSettings composition = rotationComposer.Composition;
        composition.ScreenPosition = railScreenPosition;
        composition.DeadZone.Enabled = railDeadZoneEnabled;
        composition.DeadZone.Size = railDeadZoneSize;
        composition.HardLimits.Enabled = railHardLimitsEnabled;
        composition.HardLimits.Size = railHardLimitsSize;
        composition.HardLimits.Offset = railHardLimitsOffset;
        composition.Validate();
        rotationComposer.Composition = composition;
        SetRailAimWorldOffset(lookAt);
        rotationComposer.CenterOnActivate = true;
        rotationComposer.Damping = railAimDamping;
        rotationComposer.Lookahead.Enabled = railLookaheadEnabled;
        rotationComposer.Lookahead.Time = railLookaheadTime;
        rotationComposer.Lookahead.Smoothing = railLookaheadSmoothing;
        rotationComposer.Lookahead.IgnoreY = railLookaheadIgnoreY;
    }

    private void RefreshRailAimWorldOffset()
    {
        if (rotationComposer == null || fixedCamera == null)
            return;
        Transform lookAt = railLookAtTarget != null
            ? railLookAtTarget
            : registeredCameraFollow != null
                ? registeredCameraFollow.TrackingTarget : null;
        SetRailAimWorldOffset(lookAt);
    }

    private void SetRailAimWorldOffset(Transform lookAt)
    {
        if (rotationComposer == null)
            return;
        rotationComposer.TargetOffset = lookAt != null
            ? Quaternion.Inverse(lookAt.rotation) * railLookAtOffset
            : railLookAtOffset;
    }

    private void SetModeComponentsEnabled(bool composer, bool dolly,
        bool applyRailWorldOffsets = false)
    {
        if (rotationComposer == null ||
            rotationComposer.gameObject != fixedCamera.gameObject)
            fixedCamera.TryGetComponent(out rotationComposer);
        if (splineDolly == null ||
            splineDolly.gameObject != fixedCamera.gameObject)
            fixedCamera.TryGetComponent(out splineDolly);
        if (rotationComposer != null)
            rotationComposer.enabled = composer;
        if (splineDolly != null)
            splineDolly.enabled = dolly;
        if (railWorldRotation == null ||
            railWorldRotation.gameObject != fixedCamera.gameObject)
            fixedCamera.TryGetComponent(out railWorldRotation);
        if (applyRailWorldOffsets && railWorldRotation == null)
            railWorldRotation = fixedCamera.gameObject
                .AddComponent<CameraRailWorldRotation>();
        if (railWorldRotation != null)
        {
            ConfigureRailWorldRotation(applyRailWorldOffsets);
        }
    }

    private void ApplyRailWorldRotation()
    {
        bool shouldApply = cameraMode == CameraZoneMode.OnRails;
        if (railWorldRotation == null && fixedCamera != null)
            fixedCamera.TryGetComponent(out railWorldRotation);
        if (shouldApply && railWorldRotation == null && fixedCamera != null)
            railWorldRotation = fixedCamera.gameObject
                .AddComponent<CameraRailWorldRotation>();
        if (railWorldRotation != null)
        {
            ConfigureRailWorldRotation(shouldApply);
        }
    }

    private void ConfigureRailWorldRotation(bool enabled)
    {
        if (railWorldRotation == null)
            return;

        railWorldRotation.WorldPositionOffset = railOffset + followPositionOffset;
        railWorldRotation.PitchFollowSpline = railPitchFollowSpline;
        railWorldRotation.YawFollowSpline = railYawFollowSpline;
        railWorldRotation.RollFollowSpline = railRollFollowSpline;
        railWorldRotation.Spline = railSpline;
        railWorldRotation.SplineDolly = splineDolly;
        CinemachineSplineRoll splineRoll = null;
        if (fixedCamera != null &&
            !fixedCamera.TryGetComponent(out splineRoll) && railSpline != null)
            railSpline.TryGetComponent(out splineRoll);
        railWorldRotation.SplineRoll = splineRoll;
        railWorldRotation.WorldEulerOffset = railWorldRotationOffset;
        railWorldRotation.OverrideWorldOrientation = railRotationMode ==
            CameraRailRotationMode.FollowSpline;
        railWorldRotation.enabled = enabled;
    }

    private void MakeSplineDollyTheOnlyBodyComponent()
    {
        // CinemachineCamera keeps the first component found for each pipeline
        // stage, so a Body that was already present would otherwise win over
        // the Dolly added later.
        CinemachineComponentBase[] components =
            fixedCamera.GetComponents<CinemachineComponentBase>();
        foreach (CinemachineComponentBase component in components)
        {
            if (component == null || component == splineDolly ||
                component.Stage != CinemachineCore.Stage.Body)
                continue;

            bool alreadyTracked = false;
            foreach (RailBodyComponentState state in railBodyComponents)
            {
                if (state.component == component)
                {
                    alreadyTracked = true;
                    break;
                }
            }
            if (alreadyTracked)
                continue;

            railBodyComponents.Add(new RailBodyComponentState
            {
                component = component,
                wasEnabled = component.enabled
            });
            component.enabled = false;
        }
    }

    private void RestoreRailBodyComponents()
    {
        foreach (RailBodyComponentState state in railBodyComponents)
        {
            if (state.component != null)
                state.component.enabled = state.wasEnabled;
        }
        railBodyComponents.Clear();
    }

    private bool IsRailSplineUsable()
    {
        if (railSpline != null && railSpline.Splines != null &&
            railSpline.Splines.Count > 0 && railSpline.Spline != null &&
            railSpline.Spline.Count >= 2 && !railSpline.Spline.Closed)
            return true;

        if (!warnedInvalidRail)
        {
            warnedInvalidRail = true;
            Debug.LogWarning($"FixedCameraZone '{name}': o modo On Rails precisa de um SplineContainer com um spline ABERTO de pelo menos 2 pontos. A camera ficara fixa.", this);
        }
        return false;
    }

    private void RegisterWithCameraFollow()
    {
        if (registeredCameraFollow == CameraFollow.Instance)
            return;

        if (registeredCameraFollow != null)
            registeredCameraFollow.UnregisterFixedCameraZone(this);

        registeredCameraFollow = CameraFollow.Instance;
        if (registeredCameraFollow != null)
            registeredCameraFollow.RegisterFixedCameraZone(this);
    }

    private void ResolveReferences()
    {
        if (activationVolume == null)
            activationVolume = GetComponent<BoxCollider>();
        if (fixedCamera == null)
            fixedCamera = GetComponentInChildren<CinemachineCamera>(true);
    }
}
