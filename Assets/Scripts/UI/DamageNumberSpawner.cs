using UnityEngine;

/// <summary>Exibe danos usando a mesma configuracao para todos os alvos.</summary>
public static class DamageNumberSpawner
{
    public static void ShowPlayerDamage(Transform target, int damage) => Show(target, damage);
    public static void ShowEnemyDamage(Transform target, int damage) => Show(target, damage);

    private static void Show(Transform target, int damage)
    {
        if (!DebugGizmoSettings.Enabled(DebugGizmoGroup.DamageNumbers) ||
            target == null || damage <= 0) return;
        DamageNumberStyle style = DamageNumberSettings.Instance.Style;
        if (style == null || !style.enabled) return;
        DamageNumber.Show(target, damage, style);
    }
}
