using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Exibe a identidade e a vida do chefe atualmente ativo.
/// Fica no Canvas; o BossController recebe a referencia pelo Inspector.
/// </summary>
public class BossHealthHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private Image healthFill;

    private BossController currentBoss;
    private EnemyHealth currentHealth;

    private void Awake()
    {
        if (hudRoot == null)
            hudRoot = gameObject;

        SetVisible(false);
    }

    private void OnDestroy()
    {
        UnsubscribeFromHealth();
    }

    public void Show(BossController boss)
    {
        if (boss == null)
            return;

        if (currentBoss != boss)
        {
            UnsubscribeFromHealth();
            currentBoss = boss;
            currentHealth = boss.GetComponent<EnemyHealth>();

            if (currentHealth != null)
                currentHealth.HealthChanged += UpdateHealth;
        }

        if (bossNameText != null)
            bossNameText.text = boss.BossName;

        if (currentHealth != null)
            UpdateHealth(
                currentHealth,
                currentHealth.CurrentHealth,
                currentHealth.MaxHealth
            );

        SetVisible(true);
    }

    public void Hide(BossController boss)
    {
        if (boss != null && boss != currentBoss)
            return;

        UnsubscribeFromHealth();
        currentBoss = null;
        currentHealth = null;
        SetVisible(false);
    }

    private void UpdateHealth(EnemyHealth _, int current, int maximum)
    {
        if (healthFill != null)
            healthFill.fillAmount = maximum > 0
                ? Mathf.Clamp01((float)current / maximum)
                : 0f;
    }

    private void UnsubscribeFromHealth()
    {
        if (currentHealth != null)
            currentHealth.HealthChanged -= UpdateHealth;
    }

    private void SetVisible(bool value)
    {
        if (hudRoot != null)
            hudRoot.SetActive(value);
    }
}
