using UnityEngine;

public enum ShopDestructionMode
{
    AccumulateWithinWindow,
    SingleHitOnly
}

/// <summary>Terminal destrutivel; todos os terminais acessam o mesmo catalogo e progresso do Player.</summary>
[RequireComponent(typeof(Collider), typeof(EnemyGore), typeof(WorldLockOnTarget))]
[DisallowMultipleComponent]
public sealed class CurrencyShopTerminal : MonoBehaviour, IPlayerAttackTarget
{
    [Header("Destruicao")]
    [Tooltip("Acumula dano dentro da janela de tempo ou exige que um unico golpe alcance todo o dano necessario.")]
    [SerializeField] private ShopDestructionMode destructionMode;
    [InspectorName("Required Damage")]
    [Tooltip("Dano necessario para destruir este terminal. Usado como limite acumulado ou minimo de um unico golpe, conforme o modo.")]
    [Min(1)] [SerializeField] private int maximumHealth = 100;
    [Tooltip("Intervalo maximo entre golpes para manter o dano acumulado. Apos esse tempo, o proximo golpe inicia uma nova soma. Ignorado no modo Single Hit Only.")]
    [Min(0f)] [SerializeField] private float damageWindow = 1f;
    [Tooltip("Filho que contem apenas o visual da loja, separado dos colliders.")]
    [SerializeField] private GameObject visualRoot;
    private int accumulatedDamage;
    private float lastDamageTime = float.NegativeInfinity;
    private EnemyGore gore;
    public bool IsDestroyed { get; private set; }
    private void Awake()
    {
        gore = GetComponent<EnemyGore>();
    }
    public bool OpenShop(Transform player)
    {
        return !IsDestroyed && player != null && CurrencyShopHUD.Open(this, player);
    }
    public bool ReceivePlayerAttack(int damage, Vector3 impactPosition, Vector3 impactDirection)
    {
        if (IsDestroyed || damage <= 0) return false;
        int threshold = Mathf.Max(1, maximumHealth);
        if (destructionMode == ShopDestructionMode.SingleHitOnly)
        {
            if (damage < threshold) return true;
        }
        else
        {
            if (Time.time - lastDamageTime > Mathf.Max(0f, damageWindow))
                accumulatedDamage = 0;
            accumulatedDamage = (int)System.Math.Min(threshold, (long)accumulatedDamage + damage);
            lastDamageTime = Time.time;
            if (accumulatedDamage < threshold) return true;
        }
        IsDestroyed = true;
        CurrencyShopHUD.CloseFor(this);
        gore.EmitCurrencyDeath(damage, maximumHealth, impactPosition, impactDirection);
        if (visualRoot != null) visualRoot.SetActive(false);
        foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        gameObject.SetActive(false);
        return true;
    }
}
