using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAttackDebug : MonoBehaviour
{
    [Tooltip("Altura adicional do nome do ataque acima do centro real do player.")]
    [SerializeField] private float labelHeightOffset = 1.25f;
    [Tooltip("Tamanho da fonte usada para mostrar o ataque atual com F3 ativo.")]
    [Min(1)] [SerializeField] private int fontSize = 24;
    private PlayerAttackController attacks;
    private PlayerCharacterController controller;
    private GUIStyle style;

    private void Awake()
    {
        attacks = GetComponent<PlayerAttackController>();
        controller = GetComponent<PlayerCharacterController>();
    }

    private void OnGUI()
    {
        if (!PlayerDebugToolkitHUD.IsVisible || attacks == null || !attacks.IsAttacking || Camera.main == null) return;
        Vector3 center = transform.position + (controller != null ? transform.rotation * controller.center : Vector3.zero);
        Vector3 screen = Camera.main.WorldToScreenPoint(center + Vector3.up * labelHeightOffset);
        if (screen.z <= 0f) return;
        style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        style.fontSize = fontSize; style.normal.textColor = Color.black;
        GUI.Label(new Rect(screen.x - 150f, Screen.height - screen.y - 20f, 300f, 40f), attacks.CurrentAttackId, style);
    }
}
