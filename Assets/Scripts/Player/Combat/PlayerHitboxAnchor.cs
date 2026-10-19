using UnityEngine;

/// <summary>
/// Socket opcional que permite uma Hitbox Track acompanhar uma arma ou parte
/// do modelo. O ataque continua temporizado pelo gameplay, nunca pela animacao.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerHitboxAnchor : MonoBehaviour
{
    [Tooltip("ID usado pelas Hitbox Tracks para encontrar este socket.")]
    [SerializeField] private string anchorId = "Anchor";

    public string AnchorId => anchorId;
}
