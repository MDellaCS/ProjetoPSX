using System.Collections.Generic;
using UnityEngine;

/// <summary>Medidor temporario de dano causado pelo player, visivel apenas no F3.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Projeto PSX/Debug/Player DPS Meter")]
public sealed class PlayerDpsMeter : MonoBehaviour
{
    private readonly struct DamageSample
    {
        public readonly float Time;
        public readonly int Damage;
        public DamageSample(float time, int damage) { Time = time; Damage = damage; }
    }

    [Header("Measurement")]
    [Tooltip("Janela de tempo, em segundos, usada para calcular o DPS mostrado.")]
    [Min(.1f)] [SerializeField] private float measurementWindow = 3f;

    [Header("Display")]
    [Tooltip("Posicao do canto superior esquerdo do medidor em pixels.")]
    [SerializeField] private Vector2 screenPosition = new(180f, 12f);
    [Tooltip("Tamanho da fonte do medidor.")]
    [Min(1)] [SerializeField] private int fontSize = 18;
    [Tooltip("Cor do texto do medidor.")]
    [SerializeField] private Color textColor = Color.white;
    [Tooltip("Cor do fundo do medidor.")]
    [SerializeField] private Color backgroundColor = new(0f, 0f, 0f, .65f);

    private readonly Queue<DamageSample> samples = new();
    private int windowDamage;
    private int lastHitDamage;

    public void ReportDamage(int damage)
    {
        if (!PlayerDebugToolkitHUD.IsVisible || damage <= 0) return;
        samples.Enqueue(new DamageSample(Time.time, damage));
        windowDamage += damage;
        lastHitDamage = damage;
        TrimSamples();
    }

    private void Update()
    {
        if (!PlayerDebugToolkitHUD.IsVisible)
        {
            samples.Clear();
            windowDamage = 0;
            lastHitDamage = 0;
            return;
        }

        TrimSamples();
    }

    private void OnGUI()
    {
        if (!PlayerDebugToolkitHUD.IsVisible)
            return;

        float dps = windowDamage / Mathf.Max(.1f, measurementWindow);
        GUIStyle style = new(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = fontSize,
            alignment = TextAnchor.UpperLeft
        };
        string content = $"DPS ({measurementWindow:0.##}s): {dps:0.0}\n" +
            $"WINDOW DAMAGE: {windowDamage}\nLAST HIT: {lastHitDamage}";
        Vector2 size = style.CalcSize(new GUIContent(content));
        Rect rect = new(screenPosition.x, screenPosition.y,
            Mathf.Max(170f, size.x + 14f), size.y + 14f);
        Color previous = GUI.color;
        GUI.color = backgroundColor;
        GUI.Box(rect, GUIContent.none);
        GUI.color = textColor;
        GUI.Label(new Rect(rect.x + 7f, rect.y + 7f,
            rect.width - 14f, rect.height - 14f), content, style);
        GUI.color = previous;
    }

    private void TrimSamples()
    {
        float cutoff = Time.time - measurementWindow;
        while (samples.Count > 0 && samples.Peek().Time < cutoff)
            windowDamage -= samples.Dequeue().Damage;
    }
}
