using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(PanelRenderer))]
public sealed class BossHealthHUD : MonoBehaviour
{
    [Tooltip("Painel que desenha as barras dos chefes ativos.")]
    [SerializeField] private PanelRenderer panelRenderer;
    [Tooltip("Estrutura visual usada para o nome e a vida de cada chefe.")]
    [SerializeField] private VisualTreeAsset rowTemplate;

    private sealed class Row
    {
        internal EnemyBase boss;
        internal string name;
        internal int health;
        internal int maximum;
        internal VisualElement element;
        internal VisualElement fill;
        internal Label value;

        internal void Paint()
        {
            if (fill == null) return;
            fill.style.width = Length.Percent(maximum > 0 ? 100f * health / maximum : 0f);
            value.text = health + " / " + maximum;
        }
    }

    private readonly List<Row> rows = new();
    private VisualElement stack;
    private VisualElement viewport;

    private void OnEnable()
    {
        if (panelRenderer == null) panelRenderer = GetComponent<PanelRenderer>();
        panelRenderer.RegisterUIReloadCallback(Bind);
    }

    private void OnDisable()
    {
        panelRenderer.UnregisterUIReloadCallback(Bind);
        Unbind();
    }

    private void Unbind()
    {
        viewport?.UnregisterCallback<GeometryChangedEvent>(FitStack);
        stack?.UnregisterCallback<GeometryChangedEvent>(FitStack);
        viewport = null;
        stack = null;
    }

    private void FitStack(GeometryChangedEvent evt)
    {
        if (stack == null || stack.layout.height <= 0f) return;
        float available = Mathf.Max(1f, viewport.layout.height - stack.layout.y - 8f);
        float scale = Mathf.Min(1f, available / stack.layout.height);
        stack.style.scale = new Scale(new Vector3(scale, scale, 1f));
    }

    private void Bind(PanelRenderer _, VisualElement root, int version)
    {
        Unbind();
        viewport = root;
        stack = root?.Q("boss-hud");
        if (stack == null) return;
        viewport.RegisterCallback<GeometryChangedEvent>(FitStack);
        stack.RegisterCallback<GeometryChangedEvent>(FitStack);
        stack.Clear();
        foreach (Row row in rows) Build(row);
    }

    private void Build(Row row)
    {
        if (stack == null || rowTemplate == null) return;
        var tree = rowTemplate.Instantiate();
        row.element = tree.Q(className: "boss-row");
        row.element.Q<Label>("boss-name").text = row.name;
        row.fill = row.element.Q("boss-health-fill");
        row.value = row.element.Q<Label>("boss-health-value");
        stack.Add(row.element);
        row.Paint();
    }

    public void Show(EnemyBase boss, string bossName)
    {
        if (boss == null) return;
        foreach (Row existing in rows)
            if (existing.boss == boss) return;
        var row = new Row { boss = boss, name = bossName, health = boss.CurrentHealth,
            maximum = boss.MaxHealth };
        rows.Add(row);
        Build(row);
    }

    public void SetHealth(EnemyBase boss, int current, int maximum)
    {
        foreach (Row row in rows)
        {
            if (!ReferenceEquals(row.boss, boss)) continue;
            row.health = current;
            row.maximum = maximum;
            row.Paint();
            return;
        }
    }

    public void Hide(EnemyBase boss)
    {
        for (int i = rows.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(rows[i].boss, boss)) continue;
            rows[i].element?.RemoveFromHierarchy();
            rows.RemoveAt(i);
            return;
        }
    }
}
