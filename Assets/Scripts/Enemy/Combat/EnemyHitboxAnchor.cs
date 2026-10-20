using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyHitboxAnchor : MonoBehaviour
{
    [Tooltip("ID usado por Hitbox Tracks em modo Follow Anchors.")]
    [SerializeField] private string anchorId = "Weapon";
    public string AnchorId => anchorId;
}
