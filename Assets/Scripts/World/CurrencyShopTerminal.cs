using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Area de mapa que abre uma loja de currency para melhorar Carga.</summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public sealed class CurrencyShopTerminal : MonoBehaviour
{
    [Header("Loja")]
    [Tooltip("Nome apresentado no topo da loja.")]
    [SerializeField] private string shopTitle = "FORJA DE CARGA";
    [Tooltip("Texto auxiliar exibido abaixo do titulo da loja.")]
    [TextArea] [SerializeField] private string shopDescription = "Aumenta sua capacidade maxima de Heat.";

    [Header("Carga")]
    [Tooltip("Nivel maximo que o jogador pode comprar para Carga nesta configuracao.")]
    [Min(0)] [SerializeField] private int maximumChargeLevel = 10;
    [Tooltip("Curva de preco da proxima Carga. X e o nivel que sera comprado; Y e a currency necessaria.")]
    [SerializeField] private AnimationCurve currencyCostByNextLevel = AnimationCurve.Linear(1f, 10f, 10f, 100f);
    [Tooltip("Curva do bonus de Max Heat. X e o nivel atual de Carga; Y e o Heat adicional total nesse nivel.")]
    [SerializeField] private AnimationCurve maxHeatBonusByChargeLevel = AnimationCurve.Linear(0f, 0f, 10f, 100f);

    private Transform playerInside;

    public string ShopTitle => shopTitle;
    public string ShopDescription => shopDescription;
    public int MaximumChargeLevel => maximumChargeLevel;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnValidate()
    {
        maximumChargeLevel = Mathf.Max(0, maximumChargeLevel);
        Collider area = GetComponent<Collider>();
        if (area != null) area.isTrigger = true;
    }

    private void Update()
    {
        if (playerInside == null || CurrencyShopHUD.IsOpen ||
            GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked)
            return;
        if (Keyboard.current?.eKey.wasPressedThisFrame == true)
            OpenShop();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHeat heat = other.GetComponentInParent<PlayerHeat>();
        if (heat != null) playerInside = heat.transform;
    }

    private void OnTriggerExit(Collider other)
    {
        if (playerInside != null && other.GetComponentInParent<PlayerHeat>()?.transform == playerInside)
            playerInside = null;
    }

    public int GetCostForNextLevel(int currentLevel)
    {
        return Mathf.Max(0, Mathf.RoundToInt(currencyCostByNextLevel.Evaluate(currentLevel + 1)));
    }

    public float GetMaxHeatBonusForLevel(int level) => Mathf.Max(0f,
        maxHeatBonusByChargeLevel.Evaluate(level));

    public void ConfigureCharge(PlayerChargeUpgrade charge) =>
        charge?.Configure(maximumChargeLevel, maxHeatBonusByChargeLevel);

    private void OpenShop()
    {
        PlayerChargeUpgrade charge = PlayerChargeUpgrade.FindOrCreate(playerInside);
        ConfigureCharge(charge);
        CurrencyShopHUD.Open(this, playerInside);
    }

    private void OnDrawGizmos()
    {
        Collider area = GetComponent<Collider>();
        if (area == null) return;
        Gizmos.color = new Color(.95f, .7f, .12f, .8f);
        Gizmos.DrawWireCube(area.bounds.center, area.bounds.size);
    }
}
