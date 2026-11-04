using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Area de interacao generica: um Collider (trigger) + prompt + UnityEvent.
/// Use para placas, paineis, pontos de descanso ou qualquer "aperte E aqui" sem
/// escrever codigo. Para logica propria herde desta classe e sobrescreva
/// <see cref="CanInteract"/> e <see cref="OnInteracted"/>.
/// </summary>
[RequireComponent(typeof(Collider))]
[AddComponentMenu("Projeto PSX/Interaction/Interaction Area")]
public class InteractionArea : InteractableBase
{
    [Header("Prompt")]
    [Tooltip("Texto mostrado ao lado do botao de interacao enquanto o jogador esta na area.")]
    [SerializeField] protected string interactionMessage = "Interagir";
    [Tooltip("Icone opcional mostrado no lugar do nome do botao.")]
    [SerializeField] protected Sprite interactionIcon;
    [Tooltip("Em sobreposicao de areas vence a maior prioridade; em empate, a mais proxima do jogador.")]
    [SerializeField] private int priority;

    [Header("Regras")]
    [Tooltip("Depois de usada com sucesso a area deixa de responder.")]
    [SerializeField] private bool interactOnce;
    [Tooltip("Segundos, em tempo real, antes de a area poder ser usada de novo.")]
    [Min(0f)] [SerializeField] private float cooldown;
    [Tooltip("So permite interagir com o jogador no chao.")]
    [SerializeField] private bool requireGrounded;

    [Header("Eventos")]
    [Tooltip("Chamado ao interagir. Recebe o GameObject do jogador.")]
    [SerializeField] private UnityEvent<GameObject> onInteract;

    private Collider areaCollider;
    private bool used;
    private float nextUseTime;

    public override int InteractionPriority => priority;
    public override string InteractionMessage => interactionMessage;
    public override Sprite InteractionIcon => interactionIcon;

    /// <summary>Permite usar a area de novo (depois de "Interact Once").</summary>
    public void ResetUsed() => used = false;

    protected virtual void Reset()
    {
        if (TryGetComponent(out Collider trigger))
            trigger.isTrigger = true;
    }

    protected virtual void Awake()
    {
        areaCollider = GetComponent<Collider>();
        if (areaCollider != null)
            areaCollider.isTrigger = true;
    }

    public override bool ContainsPlayer(PlayerMovement player)
    {
        if (areaCollider == null)
            areaCollider = GetComponent<Collider>();
        return ColliderContainsPlayer(areaCollider, player);
    }

    public override bool CanInteract(PlayerMovement player) =>
        !(interactOnce && used) && Time.unscaledTime >= nextUseTime &&
        (!requireGrounded || player.IsGrounded);

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

    protected virtual void OnDrawGizmos()
    {
        if (!InteractionGizmos.Visible)
            return;
        InteractionGizmos.DrawCollider(GetComponent<Collider>(),
            InteractionGizmos.ZoneFill, InteractionGizmos.ZoneLine);
    }
}
