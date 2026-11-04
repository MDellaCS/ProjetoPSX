using UnityEngine;

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
    /// <summary>Em sobreposicao vence a maior prioridade; em empate, o mais proximo.</summary>
    public virtual int InteractionPriority => 0;

    /// <summary>Texto mostrado no prompt contextual.</summary>
    public abstract string InteractionMessage { get; }

    /// <summary>Icone opcional no lugar do nome do botao.</summary>
    public virtual Sprite InteractionIcon => null;

    /// <summary>Verdadeiro se o jogador esta na zona de acionamento (geometria).</summary>
    public abstract bool ContainsPlayer(PlayerMovement player);

    /// <summary>Regras extras (estado do jogador, "uma vez so", cooldown...). Falso esconde o prompt.</summary>
    public virtual bool CanInteract(PlayerMovement player) => true;

    /// <summary>Distancia ao quadrado usada para escolher o mais proximo entre candidatos.</summary>
    public virtual float DistanceSqrTo(Vector3 position) =>
        (transform.position - position).sqrMagnitude;

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
