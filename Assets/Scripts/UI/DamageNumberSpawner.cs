using UnityEngine;

/// <summary>Exibe numeros usando a configuracao global da entidade atingida.</summary>
public static class DamageNumberSpawner
{
    public static void ShowPlayerDamage(Transform target, int damage) =>
        Show(target, damage, DamageNumberSettings.Instance.Player);

    public static void ShowEnemyDamage(Transform target, int damage) =>
        Show(target, damage, DamageNumberSettings.Instance.Enemy);

    private static void Show(Transform target, int damage, DamageNumberStyle style)
    {
        if (style == null || !style.enabled || !PlayerDebugToolkitHUD.IsVisible ||
            target == null || damage <= 0) return;
        DamageNumber.Show(target, damage, style.spawnOffset, style.color,
            Mathf.Max(0f, style.horizontalScatter), Mathf.Max(0f, style.verticalScatter),
            style.lifetime, style.riseSpeed, style.fontSize, style.shadowColor,
            style.shadowOffsetPixels);
    }
}
