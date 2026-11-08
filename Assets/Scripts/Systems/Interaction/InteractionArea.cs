using UnityEngine;
using UnityEngine.Events;

public enum InteractionAreaShape
{
    [InspectorName("Collider existente")]
    ExistingCollider,
    [InspectorName("Caixa")]
    Box,
    [InspectorName("Esfera")]
    Sphere,
    [InspectorName("Capsula")]
    Capsule
}

/// <summary>
/// Area generica com prompt, geometria configuravel e UnityEvent.
/// Use para placas, paineis, pontos de descanso ou qualquer interacao sem
/// escrever codigo. Para logica propria herde desta classe e sobrescreva
/// <see cref="CanInteract"/> e <see cref="OnInteracted"/>.
/// </summary>
[AddComponentMenu("Projeto PSX/Interaction/Interaction Area")]
public class InteractionArea : InteractableBase
{
    [Header("Prompt")]
    [Tooltip("Texto mostrado ao lado do botao de interacao enquanto o jogador esta na area.")]
    [SerializeField] protected string interactionMessage = "Interagir";
    [Tooltip("Icone opcional mostrado no lugar do nome do botao.")]
    [SerializeField] protected Sprite interactionIcon;

    [Header("Area")]
    [Tooltip("Formato da deteccao. Collider existente preserva a area configurada no objeto; Caixa, Esfera e Capsula usam as dimensoes locais abaixo.")]
    [SerializeField] protected InteractionAreaShape areaShape;
    [Tooltip("Centro local das formas Caixa, Esfera e Capsula.")]
    [SerializeField] private Vector3 areaCenter;
    [Tooltip("Dimensoes locais da area quando o formato Caixa estiver selecionado.")]
    [Min(.01f)] [SerializeField] private Vector3 boxSize = new(2f, 2f, 2f);
    [Tooltip("Raio local da area quando o formato Esfera ou Capsula estiver selecionado.")]
    [Min(.01f)] [SerializeField] private float areaRadius = 1f;
    [Tooltip("Altura local da area quando o formato Capsula estiver selecionado.")]
    [Min(.02f)] [SerializeField] private float capsuleHeight = 2f;
    [Tooltip("Eixo local da Capsula: X, Y ou Z.")]
    [SerializeField] private int capsuleDirection = 1;
    [Tooltip("Ponto de destino do teste de visibilidade; vazio usa o centro calculado pelo formato.")]
    [SerializeField] private Transform areaFocusPoint;

    [Header("Regras")]
    [Tooltip("Depois de usada com sucesso a area deixa de responder.")]
    [SerializeField] private bool interactOnce;
    [Tooltip("Segundos, em tempo real, antes de a area poder ser usada de novo.")]
    [Min(0f)] [SerializeField] private float cooldown;
    [Tooltip("So permite interagir com o jogador no chao.")]
    [SerializeField] private bool requireGrounded;

    [Header("Eventos")]
    [Tooltip("Chamado ao interagir. Recebe o GameObject do jogador.")]
    [SerializeField] private UnityEvent<GameObject> onInteract = new();

    private Collider areaCollider;
    private bool used;
    private float nextUseTime;

    public override string InteractionMessage => interactionMessage;
    public override Sprite InteractionIcon => interactionIcon;
    protected override Vector3 InteractionPoint => areaFocusPoint != null
        ? areaFocusPoint.position : GetShapeCenter();

    /// <summary>Permite usar a area de novo (depois de "Interact Once").</summary>
    public void ResetUsed() => used = false;

    protected virtual void Reset()
    {
        if (TryGetComponent(out Collider trigger))
        {
            if (areaShape == InteractionAreaShape.ExistingCollider)
                trigger.isTrigger = true;
        }
        else
            areaShape = InteractionAreaShape.Box;
    }

    protected virtual void Awake()
    {
        areaCollider = GetComponent<Collider>();
        if (areaCollider != null && areaShape == InteractionAreaShape.ExistingCollider)
            areaCollider.isTrigger = true;
    }

    public override bool ContainsPlayer(PlayerMovement player)
    {
        if (player == null)
            return false;
        Vector3 point = PlayerCenter(player);
        switch (areaShape)
        {
            case InteractionAreaShape.Box:
                return IsInsideBox(point);
            case InteractionAreaShape.Sphere:
                float sphereScale = MaxAbsScale(transform.lossyScale);
                return (point - transform.TransformPoint(areaCenter)).sqrMagnitude <=
                    Mathf.Pow(areaRadius * sphereScale, 2f);
            case InteractionAreaShape.Capsule:
                return IsPointInsideCapsule(point);
            default:
                if (areaCollider == null)
                    areaCollider = GetComponent<Collider>();
                return areaCollider != null
                    ? ColliderContainsPlayer(areaCollider, player)
                    : IsInsideBox(point);
        }
    }

    public override bool CanInteract(PlayerMovement player) =>
        !(interactOnce && used) && Time.unscaledTime >= nextUseTime &&
        (!requireGrounded || player.IsGrounded) &&
        base.CanInteract(player);

    public override float DistanceSqrTo(Vector3 position) =>
        (GetShapeCenter() - position).sqrMagnitude;

    public sealed override bool Interact(PlayerMovement player)
    {
        if (!OnInteracted(player))
            return false;

        used = true;
        nextUseTime = Time.unscaledTime + cooldown;
        onInteract?.Invoke(player.gameObject);
        return true;
    }

    /// <summary>Logica da interacao. Retorne falso para recusar (nada e consumido).</summary>
    protected virtual bool OnInteracted(PlayerMovement player) => true;

    private bool IsPointInsideCapsule(Vector3 point)
    {
        Vector3 axis = capsuleDirection == 0 ? Vector3.right :
            capsuleDirection == 2 ? Vector3.forward : Vector3.up;
        float axisScale = Mathf.Abs(transform.lossyScale[capsuleDirection]);
        float radiusScale = capsuleDirection == 0
            ? Mathf.Max(Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z))
            : capsuleDirection == 1
                ? Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z))
                : Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        float radius = areaRadius * radiusScale;
        float halfSegment = Mathf.Max(0f, capsuleHeight * .5f - areaRadius) * axisScale;
        Vector3 center = transform.TransformPoint(areaCenter);
        Vector3 worldAxis = transform.TransformDirection(axis).normalized;
        Vector3 a = center - worldAxis * halfSegment;
        Vector3 b = center + worldAxis * halfSegment;
        Vector3 segment = b - a;
        float segmentLengthSqr = segment.sqrMagnitude;
        float segmentT = segmentLengthSqr > .0001f
            ? Mathf.Clamp01(Vector3.Dot(point - a, segment) / segmentLengthSqr) : 0f;
        Vector3 closest = a + segment * segmentT;
        return (point - closest).sqrMagnitude <= radius * radius;
    }

    private bool IsInsideBox(Vector3 point)
    {
        Vector3 local = transform.InverseTransformPoint(point) - areaCenter;
        Vector3 half = Vector3.Max(boxSize, Vector3.one * .01f) * .5f;
        return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y &&
            Mathf.Abs(local.z) <= half.z;
    }

    private Vector3 GetShapeCenter()
    {
        if (areaShape != InteractionAreaShape.ExistingCollider)
            return transform.TransformPoint(areaCenter);
        if (areaCollider == null)
            areaCollider = GetComponent<Collider>();
        return areaCollider != null ? areaCollider.bounds.center : transform.position;
    }

    private static float MaxAbsScale(Vector3 scale) =>
        Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

    protected virtual void OnDrawGizmos()
    {
        if (!InteractionGizmos.Visible)
            return;
        if (areaShape == InteractionAreaShape.ExistingCollider)
        {
            Collider existing = GetComponent<Collider>();
            if (existing != null)
                InteractionGizmos.DrawCollider(existing,
                    InteractionGizmos.ZoneFill, InteractionGizmos.ZoneLine);
            else
            {
                Matrix4x4 boxPreviousMatrix = Gizmos.matrix;
                Color boxPreviousColor = Gizmos.color;
                Gizmos.matrix = transform.localToWorldMatrix;
                InteractionGizmos.DrawBox(areaCenter, boxSize,
                    InteractionGizmos.ZoneFill, InteractionGizmos.ZoneLine);
                Gizmos.matrix = boxPreviousMatrix;
                Gizmos.color = boxPreviousColor;
            }
            return;
        }

        Matrix4x4 previous = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = InteractionGizmos.ZoneFill;
        switch (areaShape)
        {
            case InteractionAreaShape.Box:
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(areaCenter, boxSize);
                Gizmos.color = InteractionGizmos.ZoneLine;
                Gizmos.DrawWireCube(areaCenter, boxSize);
                break;
            case InteractionAreaShape.Sphere:
                Vector3 center = transform.TransformPoint(areaCenter);
                float radius = areaRadius * MaxAbsScale(transform.lossyScale);
                Gizmos.DrawSphere(center, radius);
                Gizmos.color = InteractionGizmos.ZoneLine;
                Gizmos.DrawWireSphere(center, radius);
                break;
            case InteractionAreaShape.Capsule:
                DrawCapsuleGizmo();
                break;
        }
        Gizmos.matrix = previous;
        Gizmos.color = previousColor;
    }

    private void DrawCapsuleGizmo()
    {
        Vector3 axis = capsuleDirection == 0 ? Vector3.right :
            capsuleDirection == 2 ? Vector3.forward : Vector3.up;
        float axisScale = Mathf.Abs(transform.lossyScale[capsuleDirection]);
        float radiusScale = capsuleDirection == 0
            ? Mathf.Max(Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z))
            : capsuleDirection == 1
                ? Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z))
                : Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        float radius = areaRadius * radiusScale;
        float halfSegment = Mathf.Max(0f, capsuleHeight * .5f - areaRadius) * axisScale;
        Vector3 center = transform.TransformPoint(areaCenter);
        Vector3 worldAxis = transform.TransformDirection(axis).normalized;
        Vector3 a = center - worldAxis * halfSegment;
        Vector3 b = center + worldAxis * halfSegment;
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

    protected virtual void OnValidate()
    {
        boxSize = Vector3.Max(boxSize, Vector3.one * .01f);
        areaRadius = Mathf.Max(.01f, areaRadius);
        capsuleHeight = Mathf.Max(areaRadius * 2f, capsuleHeight);
        capsuleDirection = Mathf.Clamp(capsuleDirection, 0, 2);
    }
}
