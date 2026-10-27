using UnityEngine;

/// <summary>
/// Consulta sobreposicao entre areas de mundo e a capsula do player sem
/// depender de callbacks de trigger ou de um Rigidbody na entidade.
/// </summary>
public static class PlayerAreaOverlap
{
    public static bool ContainsPlayer(Collider area, PlayerMovement player)
    {
        if (area == null || player == null || !area.enabled ||
            !area.gameObject.activeInHierarchy)
        {
            return false;
        }

        PlayerCharacterController motor = player.EntityCharacterController;
        Collider playerCollider = motor != null ? motor.Collider : null;
        if (playerCollider == null || !playerCollider.enabled ||
            !playerCollider.gameObject.activeInHierarchy ||
            !area.bounds.Intersects(playerCollider.bounds))
        {
            return false;
        }

        return Physics.ComputePenetration(
            area,
            area.transform.position,
            area.transform.rotation,
            playerCollider,
            playerCollider.transform.position,
            playerCollider.transform.rotation,
            out _,
            out _
        );
    }
}
