using UnityEngine;

/// <summary>
/// Numero de dano desenhado como overlay de tela. Como nao e um texto no
/// mundo, paredes, inimigos e outros meshes nunca conseguem esconde-lo.
/// </summary>
public class DamageNumber : MonoBehaviour
{
    [Min(.01f)] [SerializeField] private float lifetime = .8f;
    [Min(0f)] [SerializeField] private float riseSpeed = 1.5f;
    [Min(1)] [SerializeField] private int fontSize = 22;

    private static GUIStyle textStyle;
    private static GUIStyle shadowStyle;

    private Transform target;
    private Vector3 headOffset;
    private Vector3 lastWorldPosition;
    private string damageText;
    private Color damageColor;
    private float timer;

    public static void Show(Transform target, int damage, Vector3 headOffset,
        Color color)
    {
        if (target == null || damage <= 0)
            return;

        GameObject numberObject = new GameObject("Damage Number");
        DamageNumber number = numberObject.AddComponent<DamageNumber>();
        number.Initialize(target, damage, headOffset, color);
    }

    public void Initialize(Transform damageTarget, int damage,
        Vector3 offset, Color color)
    {
        target = damageTarget;
        headOffset = offset;
        lastWorldPosition = target.position + headOffset;
        damageText = damage.ToString();
        damageColor = color;
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (target != null)
            lastWorldPosition = target.position + headOffset;

        if (timer >= lifetime)
            Destroy(gameObject);
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(damageText))
            return;

        Camera gameCamera = Camera.main;
        if (gameCamera == null)
            return;

        Vector3 worldPosition = lastWorldPosition +
            Vector3.up * (riseSpeed * timer);
        Vector3 screenPosition = gameCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z <= 0f)
            return;

        EnsureStyles();

        float fade = 1f - Mathf.Clamp01(timer / lifetime);
        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;
        GUI.depth = -100;

        Rect rect = new Rect(
            screenPosition.x - 50f,
            Screen.height - screenPosition.y - 16f,
            100f,
            32f
        );

        GUI.color = new Color(0f, 0f, 0f, fade);
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height),
            damageText, shadowStyle);
        GUI.color = new Color(damageColor.r, damageColor.g, damageColor.b, fade);
        GUI.Label(rect, damageText, textStyle);

        GUI.depth = previousDepth;
        GUI.color = previousColor;
    }

    private void EnsureStyles()
    {
        if (textStyle != null)
            return;

        textStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = fontSize
        };
        shadowStyle = new GUIStyle(textStyle);
    }
}
