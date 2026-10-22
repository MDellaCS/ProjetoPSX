using UnityEngine;

/// <summary>Alvo nao-inimigo que pode receber dano de um ataque do player.</summary>
public interface IPlayerAttackTarget
{
    bool ReceivePlayerAttack(int damage, Vector3 impactPosition,
        Vector3 impactDirection);
}
