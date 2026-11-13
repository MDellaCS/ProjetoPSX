using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Area espacial compartilhada por qualquer interação. Volume e regras ficam
/// exclusivamente no InteractionProfile associado ao objeto.
/// </summary>
[AddComponentMenu("Projeto PSX/Interaction/Interaction Area")]
[DisallowMultipleComponent]
public class InteractionArea : InteractableBase
{
    [Header("Evento")]
    [Tooltip("Chamado quando esta InteractionArea executa uma acao generica. Recebe o GameObject do jogador.")]
    [SerializeField] private UnityEvent<GameObject> onInteract = new();

    [Header("Perfil de interacao")]
    [Tooltip("Perfil que define formato e dimensoes do volume, prompt e regras. Componentes de comportamento no mesmo objeto usam este perfil.")]
    [SerializeField] private InteractionProfile areaProfile;

    internal InteractionProfile AssignedProfile => areaProfile;
    public InteractionProfile ConfiguredProfile => areaProfile;
    public bool HasInteractionHandler => HasSiblingInteractable();

    internal bool IsProviderForSibling(InteractableBase candidate)
    {
        if (candidate == null || candidate == this)
            return false;
        InteractableBase[] siblings = GetComponents<InteractableBase>();
        for (int i = 0; i < siblings.Length; i++)
            if (siblings[i] == candidate)
                return true;
        return false;
    }

    internal InteractableBase GetInteractionHandler(InteractableBase fallback = null) =>
        GetSiblingInteractionHandler() ?? fallback;

    protected override void OnEnable()
    {
        InteractableBase handler = GetSiblingInteractionHandler();
        if (handler != null)
            InteractionSystem.Register(handler);
        else
            base.OnEnable();
    }

    protected override void OnDisable()
    {
        if (!HasInteractionHandler)
            base.OnDisable();
    }

    protected override void OnDrawGizmos()
    {
        if (!InteractionGizmos.Visible)
            return;

        if (GetSiblingInteractionHandler() is HandLadder ladder && !ladder.IsClimbable)
            return;

        InteractionProfile profile = areaProfile != null
            ? areaProfile
            : EffectiveInteractionProfile;
        DrawProfileGizmo(transform, profile);
    }

    public override string InteractionMessage => "Interagir";

    public override bool ContainsPlayer(PlayerMovement player) =>
        ContainsPlayer(transform, EffectiveInteractionProfile, player);

    public override float DistanceSqrTo(Vector3 position) =>
        DistanceSqrTo(transform, EffectiveInteractionProfile, position);

    public override bool Interact(PlayerMovement player)
    {
        if (!OnInteracted(player))
            return false;
        onInteract?.Invoke(player.gameObject);
        return true;
    }

    protected virtual bool OnInteracted(PlayerMovement player) => true;

    private bool HasSiblingInteractable()
    {
        return GetSiblingInteractionHandler() != null;
    }

    private InteractableBase GetSiblingInteractionHandler()
    {
        InteractableBase[] siblings = GetComponents<InteractableBase>();
        for (int i = 0; i < siblings.Length; i++)
            if (siblings[i] != null && siblings[i] != this)
                return siblings[i];
        return null;
    }

    public static bool ContainsPlayer(Transform owner, InteractionProfile profile,
        PlayerMovement player, float volumeScale = 1f)
    {
        if (owner == null || profile == null || player == null)
            return false;

        Vector3 point = PlayerCenter(player);
        Vector3 center = GetWorldCenter(owner, profile);
        Vector3 scale = owner.lossyScale;
        float safeScale = Mathf.Max(1f, volumeScale);
        switch (profile.AreaShape)
        {
            case InteractionAreaShape.Box:
            {
                Vector3 local = owner.InverseTransformPoint(point) - profile.AreaCenter;
                Vector3 half = profile.BoxSize * (safeScale * .5f);
                return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y &&
                    Mathf.Abs(local.z) <= half.z;
            }
            case InteractionAreaShape.Capsule:
                return ContainsCapsule(owner, profile, point, center, scale, safeScale);
            case InteractionAreaShape.LadderEntry:
            {
                HandLadder ladder = owner.GetComponent<HandLadder>();
                return ladder != null && ladder.TryGetEntry(player, out _);
            }
            default:
                float radius = profile.AreaRadius * MaxAbsScale(scale) * safeScale;
                return (point - center).sqrMagnitude <= radius * radius;
        }
    }

    public static float DistanceSqrTo(Transform owner, InteractionProfile profile,
        Vector3 position)
    {
        if (owner == null || profile == null)
            return float.PositiveInfinity;
        if (profile.AreaShape == InteractionAreaShape.LadderEntry)
        {
            HandLadder ladder = owner.GetComponent<HandLadder>();
            if (ladder != null)
                return ladder.GetEntryDistanceSqr(position);
        }
        return (GetWorldCenter(owner, profile) - position).sqrMagnitude;
    }

    public static Vector3 GetWorldCenter(Transform owner, InteractionProfile profile) =>
        owner != null && profile != null ? owner.TransformPoint(profile.AreaCenter) :
            owner != null ? owner.position : Vector3.zero;

    public static void DrawProfileGizmo(Transform owner, InteractionProfile profile,
        float volumeScale = 1f)
    {
        if (owner == null || profile == null || !InteractionGizmos.Visible)
            return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Vector3 center = profile.AreaCenter;
        Vector3 scale = owner.lossyScale;
        float safeScale = Mathf.Max(1f, volumeScale);
        Gizmos.color = InteractionGizmos.ZoneFill;
        switch (profile.AreaShape)
        {
            case InteractionAreaShape.Box:
                Gizmos.matrix = owner.localToWorldMatrix;
                Gizmos.DrawCube(center, profile.BoxSize * safeScale);
                Gizmos.color = InteractionGizmos.ZoneLine;
                Gizmos.DrawWireCube(center, profile.BoxSize * safeScale);
                break;
            case InteractionAreaShape.Capsule:
                DrawCapsuleGizmo(owner, profile, safeScale);
                break;
            case InteractionAreaShape.LadderEntry:
            {
                HandLadder ladder = owner.GetComponent<HandLadder>();
                if (ladder != null)
                    ladder.DrawEntryAreaGizmo();
                break;
            }
            default:
                Gizmos.matrix = Matrix4x4.identity;
                Vector3 worldCenter = owner.TransformPoint(center);
                float radius = profile.AreaRadius * MaxAbsScale(scale) * safeScale;
                Gizmos.DrawSphere(worldCenter, radius);
                Gizmos.color = InteractionGizmos.ZoneLine;
                Gizmos.DrawWireSphere(worldCenter, radius);
                break;
        }
        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    private static bool ContainsCapsule(Transform owner, InteractionProfile profile,
        Vector3 point, Vector3 center, Vector3 scale, float volumeScale)
    {
        int axisIndex = profile.CapsuleDirection;
        Vector3 axis = Axis(axisIndex);
        float axisScale = Mathf.Abs(scale[axisIndex]);
        float radiusScale = PerpendicularScale(scale, axisIndex);
        float radius = profile.AreaRadius * radiusScale * volumeScale;
        float halfSegment = Mathf.Max(0f,
            profile.CapsuleHeight * .5f - profile.AreaRadius) * axisScale * volumeScale;
        Vector3 worldAxis = owner.TransformDirection(axis).normalized;
        Vector3 a = center - worldAxis * halfSegment;
        Vector3 b = center + worldAxis * halfSegment;
        Vector3 segment = b - a;
        float lengthSqr = segment.sqrMagnitude;
        float t = lengthSqr > .0001f
            ? Mathf.Clamp01(Vector3.Dot(point - a, segment) / lengthSqr) : 0f;
        Vector3 closest = a + segment * t;
        return (point - closest).sqrMagnitude <= radius * radius;
    }

    private static void DrawCapsuleGizmo(Transform owner, InteractionProfile profile,
        float volumeScale)
    {
        int axisIndex = profile.CapsuleDirection;
        Vector3 scale = owner.lossyScale;
        float radius = profile.AreaRadius * PerpendicularScale(scale, axisIndex) * volumeScale;
        float halfSegment = Mathf.Max(0f,
            profile.CapsuleHeight * .5f - profile.AreaRadius) * Mathf.Abs(scale[axisIndex]) *
            volumeScale;
        Vector3 center = GetWorldCenter(owner, profile);
        Vector3 worldAxis = owner.TransformDirection(Axis(axisIndex)).normalized;
        Vector3 a = center - worldAxis * halfSegment;
        Vector3 b = center + worldAxis * halfSegment;
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawWireSphere(a, radius);
        Gizmos.DrawWireSphere(b, radius);
        Vector3 side = Vector3.Cross(worldAxis, Vector3.up);
        if (side.sqrMagnitude < .001f)
            side = Vector3.Cross(worldAxis, Vector3.right);
        side = side.normalized * radius;
        Vector3 other = Vector3.Cross(worldAxis, side).normalized * radius;
        Gizmos.DrawLine(a + side, b + side);
        Gizmos.DrawLine(a - side, b - side);
        Gizmos.DrawLine(a + other, b + other);
        Gizmos.DrawLine(a - other, b - other);
        Gizmos.color = InteractionGizmos.ZoneLine;
        Gizmos.DrawWireSphere(a, radius);
        Gizmos.DrawWireSphere(b, radius);
    }

    private static Vector3 Axis(int axisIndex) => axisIndex == 0 ? Vector3.right :
        axisIndex == 2 ? Vector3.forward : Vector3.up;

    private static float PerpendicularScale(Vector3 scale, int axisIndex) => axisIndex == 0
        ? Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z))
        : axisIndex == 1
            ? Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z))
            : Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));

    private static float MaxAbsScale(Vector3 scale) =>
        Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
}
