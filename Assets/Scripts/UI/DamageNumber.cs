using System.Collections.Generic;
using UnityEngine;

/// <summary>Historico e total de dano de um alvo, exibidos enquanto o F3 esta aberto.</summary>
public sealed class DamageNumber : MonoBehaviour
{
    private struct Hit
    {
        public string text;
        public float expiresAt;
    }

    private static readonly Dictionary<Transform, DamageNumber> active = new();
    private readonly List<Hit> hits = new();
    private Transform target;
    private Vector3 lastWorldPosition;
    private DamageNumberStyle style;
    private long total;
    private string totalText;
    private float totalExpiresAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => active.Clear();

    public static void Show(Transform target, int damage, DamageNumberStyle style)
    {
        if (!DebugGizmoSettings.Enabled(DebugGizmoGroup.DamageNumbers) ||
            target == null || damage <= 0 || style == null || !style.enabled)
            return;

        if (!active.TryGetValue(target, out DamageNumber display) || display == null)
        {
            display = new GameObject("Damage Numbers").AddComponent<DamageNumber>();
            display.target = target;
            display.lastWorldPosition = target.position + style.spawnOffset;
            active[target] = display;
        }
        display.AddHit(damage, style);
    }

    private void AddHit(int damage, DamageNumberStyle currentStyle)
    {
        style = currentStyle;
        float now = Time.time;
        if (now >= totalExpiresAt)
            total = 0;
        total += damage;
        totalText = total.ToString();
        totalExpiresAt = now + Mathf.Max(.01f, style.lifetime);
        hits.Add(new Hit { text = damage.ToString(), expiresAt = totalExpiresAt });
    }

    private void Update()
    {
        if (target != null)
            lastWorldPosition = target.position + style.spawnOffset;
        float now = Time.time;
        for (int i = hits.Count - 1; i >= 0; i--)
            if (hits[i].expiresAt <= now) hits.RemoveAt(i);
        if (now >= totalExpiresAt)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (!ReferenceEquals(target, null) && active.TryGetValue(target, out DamageNumber current) && current == this)
            active.Remove(target);
    }

    private void OnGUI()
    {
        if (!DebugGizmoSettings.Enabled(DebugGizmoGroup.DamageNumbers) || style == null)
            return;
        Camera gameCamera = Camera.main;
        if (gameCamera == null) return;
        Vector3 screen = gameCamera.WorldToScreenPoint(lastWorldPosition);
        if (screen.z <= 0f) return;

        GUIStyle textStyle = new(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            font = style.font,
            fontStyle = FontStyle.Bold,
            fontSize = Mathf.Max(1, style.fontSize)
        };
        float lifetime = Mathf.Max(.01f, style.lifetime);
        float width = Mathf.Max(100f, style.fontSize * 6f);
        float height = Mathf.Max(32f, style.fontSize * 1.3f);
        float centerY = Screen.height - screen.y;
        float spacing = Mathf.Max(0f, style.stackSpacingPixels);
        float halfColumns = Mathf.Max(0f, style.columnSpacingPixels) * .5f;
        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;
        GUI.depth = -100;
        for (int i = hits.Count - 1; i >= 0; i--)
        {
            int row = hits.Count - 1 - i;
            float fade = Mathf.Clamp01((hits[i].expiresAt - Time.time) / lifetime);
            DrawText(new Rect(screen.x - halfColumns - width * .5f,
                centerY - height * .5f - row * spacing, width, height), hits[i].text, fade, textStyle);
        }
        float totalFade = Mathf.Clamp01((totalExpiresAt - Time.time) / lifetime);
        DrawText(new Rect(screen.x + halfColumns - width * .5f,
            centerY - height * .5f, width, height), totalText, totalFade, textStyle);
        GUI.depth = previousDepth;
        GUI.color = previousColor;
    }

    private void DrawText(Rect rect, string text, float fade, GUIStyle textStyle)
    {
        Color shadow = style.shadowColor;
        GUI.color = new Color(shadow.r, shadow.g, shadow.b, shadow.a * fade);
        float offset = Mathf.Max(0f, style.shadowOffsetPixels);
        GUI.Label(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), text, textStyle);
        Color color = style.color;
        GUI.color = new Color(color.r, color.g, color.b, color.a * fade);
        GUI.Label(rect, text, textStyle);
    }
}
