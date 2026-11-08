using UnityEngine;
using UnityEngine.Serialization;

public enum InteractionActivationMode
{
    [InspectorName("Toque unico")]
    Press,
    [InspectorName("Segurar")]
    Hold
}

/// <summary>
/// Base de tudo que o jogador aciona com o botao Interagir (escadas, lojas,
/// cutscenes, areas genericas). A base so descreve o objeto; quem decide qual
/// objeto responde, mostra o prompt e consome o input e o
/// <see cref="InteractionSystem"/>, que mantem UM candidato por frame.
/// Para criar uma nova interacao herde desta classe e implemente
/// <see cref="ContainsPlayer"/> e <see cref="Interact"/>.
/// </summary>
public abstract class InteractableBase : MonoBehaviour
{
    [Header("Interacao")]
    [Tooltip("Prioridade desta interacao quando varias areas estiverem disponiveis. Valores maiores vencem; em empate, vence a mais proxima.")]
    [FormerlySerializedAs("priority")]
    [SerializeField] private int interactionPriority;
    [Tooltip("Toque unico ativa ao apertar Interagir; Segurar exige manter o botao pelo tempo configurado.")]
    [SerializeField] private InteractionActivationMode activationMode;
    [Tooltip("Tempo que o jogador deve manter Interagir pressionado para ativar esta interacao.")]
    [Min(.1f)] [SerializeField] private float holdDuration = .65f;
    [Tooltip("Exige caminho desobstruido entre jogador e alvo. Colliders nas layers marcadas bloqueiam a interacao.")]
    [SerializeField] private bool blockByGeometry;
    [Tooltip("Layers que podem bloquear a interacao quando Bloquear por Geometria estiver ativo.")]
    [SerializeField] private LayerMask blockingLayers = Physics.DefaultRaycastLayers;
    [Tooltip("Ponto de destino do teste de visibilidade. Vazio usa o ponto da interacao.")]
    [SerializeField] private Transform visibilityTarget;

    private readonly RaycastHit[] visibilityHits = new RaycastHit[16];

    /// <summary>Em sobreposicao vence a maior prioridade; em empate, o mais proximo.</summary>
    public virtual int InteractionPriority => interactionPriority;
    public InteractionActivationMode ActivationMode => activationMode;
    public float HoldDuration => Mathf.Max(.1f, holdDuration);

    /// <summary>Texto mostrado no prompt contextual.</summary>
    public abstract string InteractionMessage { get; }

    /// <summary>Icone opcional no lugar do nome do botao.</summary>
    public virtual Sprite InteractionIcon => null;

    /// <summary>Verdadeiro se o jogador esta na zona de acionamento (geometria).</summary>
    public abstract bool ContainsPlayer(PlayerMovement player);

    /// <summary>Regras extras (estado do jogador, "uma vez so", cooldown...). Falso esconde o prompt.</summary>
    public virtual bool CanInteract(PlayerMovement player) =>
        !blockByGeometry || HasClearPath(player);

    /// <summary>Distancia ao quadrado usada para escolher o mais proximo entre candidatos.</summary>
    public virtual float DistanceSqrTo(Vector3 position) =>
        (transform.position - position).sqrMagnitude;

    /// <summary>Progresso visual do modo Segurar, atualizado pelo driver central.</summary>
    public virtual void SetHoldProgress(float progress) { }

    protected virtual Vector3 InteractionPoint => transform.position;

    private bool HasClearPath(PlayerMovement player)
    {
        if (player == null)
            return false;
        Vector3 origin = PlayerCenter(player);
        Vector3 target = visibilityTarget != null
            ? visibilityTarget.position : InteractionPoint;
        Vector3 delta = target - origin;
        float distance = delta.magnitude;
        if (distance <= .05f)
            return true;

        int hitCount = Physics.RaycastNonAlloc(origin, delta / distance,
            visibilityHits, distance, blockingLayers, QueryTriggerInteraction.Ignore);
        if (hitCount >= visibilityHits.Length)
            return false;
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = visibilityHits[i].collider;
            if (hit == null || IsChildOf(hit.transform, player.transform) ||
                IsChildOf(hit.transform, transform) || visibilityTarget != null &&
                IsChildOf(hit.transform, visibilityTarget))
                continue;
            return false;
        }
        return true;
    }

    private static bool IsChildOf(Transform candidate, Transform parent) =>
        candidate == parent || candidate.IsChildOf(parent);

    /// <summary>Executa a interacao. Retorna verdadeiro se aconteceu (o prompt some).</summary>
    public abstract bool Interact(PlayerMovement player);

    protected virtual void OnEnable() => InteractionSystem.Register(this);
    protected virtual void OnDisable() => InteractionSystem.Unregister(this);

    /// <summary>Centro da capsula do jogador (ou a posicao, sem motor).</summary>
    public static Vector3 PlayerCenter(PlayerMovement player)
    {
        if (player == null)
            return Vector3.zero;
        PlayerCharacterController motor = player.EntityCharacterController;
        return motor != null ? motor.bounds.center : player.transform.position;
    }

    /// <summary>
    /// Centro do jogador dentro do collider da area. Usa ClosestPoint (respeita
    /// forma, escala e rotacao) e funciona mesmo se o jogador nasceu dentro da
    /// area ou se a matriz de layers nao entrega callbacks de trigger.
    /// </summary>
    public static bool ColliderContainsPlayer(Collider area, PlayerMovement player)
    {
        if (area == null || player == null || !area.enabled ||
            !area.gameObject.activeInHierarchy)
            return false;

        Vector3 center = PlayerCenter(player);
        if (area is BoxCollider || area is SphereCollider || area is CapsuleCollider ||
            (area is MeshCollider mesh && mesh.convex))
            return (area.ClosestPoint(center) - center).sqrMagnitude <= .0001f;
        return area.bounds.Contains(center);
    }
}
