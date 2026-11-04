using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Define uma area que troca temporariamente a camera de gameplay por um
/// enquadramento Cinemachine fixo. A sobreposicao e testada diretamente contra
/// a capsula do jogador para nao depender de eventos de trigger/Rigidbody.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class FixedCameraZone : MonoBehaviour
{
    private enum ZoneShape
    {
        Box,
        Sphere
    }

    [Header("Enquadramento")]
    [Tooltip("CinemachineCamera posicionada e rotacionada para este enquadramento. Deixe-a ativa na cena e sem Follow/LookAt para manter o angulo fixo.")]
    [SerializeField] private CinemachineCamera fixedCamera;
    [Tooltip("Prioridade desta zona. Deve ser maior que a camera de gameplay. Em sobreposicao vence a prioridade maior; em empate, a zona que o jogador entrou mais recentemente vence.")]
    [SerializeField] private int cameraPriority = 20;

    [Header("Volume")]
    [Tooltip("Collider principal da zona. No formato Box define o centro e o tamanho legado; no formato Sphere seu centro e usado para posicionar a esfera.")]
    [SerializeField] private BoxCollider activationVolume;
    [Tooltip("Formato da area. Box usa dimensoes X/Y/Z; Sphere usa o raio configurado abaixo.")]
    [SerializeField] private ZoneShape zoneShape = ZoneShape.Box;
    [Tooltip("Dimensoes X/Y/Z da area no formato Box. Zero mantem o tamanho configurado no BoxCollider.")]
    [SerializeField] private Vector3 areaSize;
    [Tooltip("Raio da area quando Sphere estiver selecionado.")]
    [Min(.01f)] [SerializeField] private float sphereRadius = 5f;
    [Tooltip("Colliders Box ou Sphere adicionais que pertencem a esta mesma camera. As areas devem se tocar ou sobrepor para atravessar entre elas sem sair da camera.")]
    [SerializeField] private List<Collider> additionalActivationVolumes = new();
    [Tooltip("Distancia fixa em unidades do mundo adicionada para sair da zona depois de entrar. Aplica-se em cada face da Box ou ao raio da Sphere; a entrada usa a area original.")]
    [Min(0f)] [SerializeField] private float exitPaddingDistance = .5f;

    private CameraFollow registeredCameraFollow;
    private SphereCollider sphereActivationVolume;
    private readonly List<ExitVolumePair> expandedExitVolumes = new();
    private bool playerWasInside;
    private int lastEnteredFrame = int.MinValue;

    internal int CameraPriority => cameraPriority;
    internal int LastEnteredFrame => lastEnteredFrame;
    internal float WorldVolume => GetCombinedWorldVolume();

    private sealed class ExitVolumePair
    {
        public Collider source;
        public Collider expanded;
    }

    private void Awake()
    {
        ResolveReferences();
        ConfigureActivationVolumes();
        CreateExpandedExitVolumes();
        SetCameraSelected(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        SetCameraSelected(false);
        RegisterWithCameraFollow();
    }

    private void Update()
    {
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

    private void OnDisable()
    {
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
        ResolveReferences();
        exitPaddingDistance = Mathf.Max(0f, exitPaddingDistance);
        sphereRadius = Mathf.Max(.01f, sphereRadius);
        if (areaSize != Vector3.zero)
            areaSize = new Vector3(Mathf.Max(.01f, areaSize.x),
                Mathf.Max(.01f, areaSize.y), Mathf.Max(.01f, areaSize.z));
        if (additionalActivationVolumes == null)
            additionalActivationVolumes = new List<Collider>();
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
            Vector3 exitSize = ExpandBoxSize(size,
                box.transform.lossyScale, exitPaddingDistance);
            DrawFilledBox(box.center, size, exitSize);
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
            DrawFilledSphere(center, radius,
                radius + exitPaddingDistance);
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
            Vector3 expandedSize = ExpandBoxSize(box.size,
                box.transform.lossyScale, exitPaddingDistance);
            DrawFilledBox(box.center, box.size, expandedSize);
        }
        else if (volume is SphereCollider sphere)
        {
            Vector3 center = sphere.transform.TransformPoint(sphere.center);
            float radius = sphere.radius *
                MaxAbsComponent(sphere.transform.lossyScale);
            Gizmos.matrix = Matrix4x4.identity;
            DrawFilledSphere(center, radius,
                radius + exitPaddingDistance);
        }
    }

    private static void DrawFilledBox(Vector3 center, Vector3 size,
        Vector3 exitSize)
    {
        Gizmos.color = new Color(1f, .42f, .05f, .08f);
        Gizmos.DrawCube(center, exitSize);
        Gizmos.color = new Color(1f, .48f, .08f, .95f);
        Gizmos.DrawWireCube(center, exitSize);
        Gizmos.color = new Color(.2f, .65f, 1f, .24f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = new Color(.25f, .75f, 1f, .95f);
        Gizmos.DrawWireCube(center, size);
    }

    private static void DrawFilledSphere(Vector3 center, float radius,
        float exitRadius)
    {
        Gizmos.color = new Color(1f, .42f, .05f, .08f);
        Gizmos.DrawSphere(center, exitRadius);
        Gizmos.color = new Color(1f, .48f, .08f, .95f);
        Gizmos.DrawWireSphere(center, exitRadius);
        Gizmos.color = new Color(.2f, .65f, 1f, .24f);
        Gizmos.DrawSphere(center, radius);
        Gizmos.color = new Color(.25f, .75f, 1f, .95f);
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
            playerWasInside);

        if (isInside && !playerWasInside)
            lastEnteredFrame = Time.frameCount;
        playerWasInside = isInside;
        return isInside;
    }

    private void CreateExpandedExitVolumes()
    {
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
                exitSphere.radius = baseRadius +
                    exitPaddingDistance / worldScale;
            }
            else if (pair.source is BoxCollider sourceBox &&
                pair.expanded is BoxCollider exitBox)
            {
                exitBox.center = sourceBox.center;
                Vector3 baseSize = pair.source == activationVolume
                    ? GetBoxAreaSize(sourceBox) : sourceBox.size;
                exitBox.size = ExpandBoxSize(baseSize,
                    sourceBox.transform.lossyScale, exitPaddingDistance);
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
                additionalVolume.isTrigger = true;
        }
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
        if (fixedCamera == null)
            return;

        fixedCamera.Follow = null;
        fixedCamera.LookAt = null;
        fixedCamera.PreviousStateIsValid = false;
        if (!selected)
        {
            fixedCamera.Priority = 0;
            return;
        }

        int minimumPriority = registeredCameraFollow != null
            ? registeredCameraFollow.GameplayCameraPriority + 2
            : 12;
        fixedCamera.Priority = Mathf.Max(cameraPriority, minimumPriority);
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
