using UnityEngine;

/// <summary>Overlay de numero de dano usado somente enquanto o F3 esta aberto.</summary>
public sealed class DamageNumber : MonoBehaviour
{
    private Transform target;
    private Vector3 headOffset;
    private Vector3 lastWorldPosition;
    private string damageText;
    private Color damageColor;
    private Color shadowColor;
    private float lifetime;
    private float riseSpeed;
    private float shadowOffsetPixels;
    private int fontSize;
    private float timer;

    public static void Show(Transform target, int damage, Vector3 offset,
        Color color, float horizontalScatter, float verticalScatter,
        float lifetime, float riseSpeed, int fontSize, Color shadowColor,
        float shadowOffsetPixels)
    {
        if (!PlayerDebugToolkitHUD.IsVisible || target == null || damage <= 0)
            return;

        GameObject numberObject = new("Damage Number");
        DamageNumber number = numberObject.AddComponent<DamageNumber>();
        Vector3 scatter = new(Random.Range(-horizontalScatter, horizontalScatter),
            Random.Range(0f, verticalScatter), 0f);
        number.Initialize(target, damage, offset + scatter, color, lifetime,
            riseSpeed, fontSize, shadowColor, shadowOffsetPixels);
    }

    private void Initialize(Transform damageTarget, int damage, Vector3 offset,
        Color color, float newLifetime, float newRiseSpeed, int newFontSize,
        Color newShadowColor, float newShadowOffsetPixels)
    {
        target = damageTarget;
        headOffset = offset;
        lastWorldPosition = target.position + headOffset;
        damageText = damage.ToString();
        damageColor = color;
        lifetime = Mathf.Max(.01f, newLifetime);
        riseSpeed = Mathf.Max(0f, newRiseSpeed);
        fontSize = Mathf.Max(1, newFontSize);
        shadowColor = newShadowColor;
        shadowOffsetPixels = Mathf.Max(0f, newShadowOffsetPixels);
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
        if (!PlayerDebugToolkitHUD.IsVisible || string.IsNullOrEmpty(damageText))
            return;

        Camera gameCamera = Camera.main;
        if (gameCamera == null)
            return;

        Vector3 worldPosition = lastWorldPosition + Vector3.up * (riseSpeed * timer);
        Vector3 screenPosition = gameCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z <= 0f)
            return;

        float fade = 1f - Mathf.Clamp01(timer / lifetime);
        GUIStyle textStyle = CreateStyle(fontSize);
        Rect rect = new(screenPosition.x - 50f,
            Screen.height - screenPosition.y - 16f, 100f, 32f);
        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;
        GUI.depth = -100;

        GUI.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b,
            shadowColor.a * fade);
        GUI.Label(new Rect(rect.x + shadowOffsetPixels,
            rect.y + shadowOffsetPixels, rect.width, rect.height), damageText, textStyle);
        GUI.color = new Color(damageColor.r, damageColor.g, damageColor.b,
            damageColor.a * fade);
        GUI.Label(rect, damageText, textStyle);

        GUI.depth = previousDepth;
        GUI.color = previousColor;
    }

    private static GUIStyle CreateStyle(int size) => new(GUI.skin.label)
    {
        alignment = TextAnchor.MiddleCenter,
        fontStyle = FontStyle.Bold,
        fontSize = size
    };
}
