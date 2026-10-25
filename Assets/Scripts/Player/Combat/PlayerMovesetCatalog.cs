using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMovesetCatalog", menuName = "Projeto PSX/Player/Moveset Catalog")]
public sealed class PlayerMovesetCatalog : ScriptableObject
{
    [Tooltip("Moveset aplicado ao jogador ao iniciar uma sessão, antes de qualquer troca manual.")]
    public PlayerMovesetDefinition startingMoveset;
    [Tooltip("Movesets disponiveis para a troca de teste pela tecla U. A ordem desta lista define a ordem do ciclo.")]
    public List<PlayerMovesetDefinition> availableMovesets = new();

    public static PlayerMovesetCatalog Load() =>
        Resources.Load<PlayerMovesetCatalog>("PlayerMovesetCatalog");
}
