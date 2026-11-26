using UnityEngine;

/// <summary>Configuracao de deslocamento compartilhada entre ataques do jogador.</summary>
[CreateAssetMenu(menuName = "Projeto PSX/Player/Attack Movement Profile", fileName = "PlayerAttackMovementProfile")]
public sealed class PlayerAttackMovementProfile : ScriptableObject
{
    [Tooltip("Distancia, janela e curva do deslocamento aplicado aos ataques que usam este perfil.")]
    public PlayerAttackMovementDefinition movement = new();
}
