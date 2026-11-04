using UnityEngine;
using UnityEngine.Events;

public enum StrenghLeverActivationMode
{
    AccumulateWithinWindow,
    SingleHitOnly
}

/// <summary>
/// Gatilho de mundo ativado pelo dano dos ataques do player. Pode representar
/// uma alavanca, cristal, botao ou outro objeto destrutivel de uma encounter.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class StrenghLever : MonoBehaviour, IPlayerAttackTarget
{
    [Header("Dano Necessario")]
    [Tooltip("Define se varios golpes somam dano dentro de uma janela ou se um unico golpe precisa atingir o valor exigido.")]
    [SerializeField] private StrenghLeverActivationMode activationMode;
    [Tooltip("Dano necessario para ativar a StrenghLever.")]
    [Min(1)] [SerializeField] private int requiredDamage = 100;
    [Tooltip("Tempo maximo entre golpes acumulados. Ignorado em Single Hit Only.")]
    [Min(0f)] [SerializeField] private float damageWindow = 1f;
    [Tooltip("Permite ativar somente uma vez. Desmarque para poder usar Reset Lever e acionar novamente.")]
    [SerializeField] private bool singleUse = true;

    [Header("Acao Inicial")]
    [Tooltip("Objeto do mapa ocultado ao ativar. Pode ficar vazio se usar somente On Activated.")]
    [SerializeField] private GameObject objectToHide;
    [Tooltip("Acoes extras disparadas quando a StrenghLever ativa. Serve para portas, animacoes, encounters e outros sistemas.")]
    [SerializeField] private UnityEvent onActivated;

    private int accumulatedDamage;
    private float lastDamageTime = float.NegativeInfinity;
    private bool activated;
    private Collider hurtbox;

    public bool IsActivated => activated;
    public int AccumulatedDamage => accumulatedDamage;

    private void Awake() => hurtbox = GetComponent<Collider>();

    /// <summary>
    /// Todo golpe valido confirma impacto para que o ataque produza Heat,
    /// hitstop e tremor de camera, mesmo antes de atingir o dano de ativacao.
    /// </summary>
    public bool ReceivePlayerAttack(int damage, Vector3 impactPosition,
        Vector3 impactDirection)
    {
        if (activated)
            return false;

        int appliedDamage = Mathf.Max(0, damage);
        if (appliedDamage <= 0)
            return false;

        if (activationMode == StrenghLeverActivationMode.SingleHitOnly)
        {
            if (appliedDamage >= requiredDamage)
                Activate();
            return true;
        }

        if (Time.time - lastDamageTime > damageWindow)
            accumulatedDamage = 0;

        accumulatedDamage += appliedDamage;
        lastDamageTime = Time.time;
        if (accumulatedDamage >= requiredDamage)
            Activate();
        return true;
    }

    [ContextMenu("Reset Lever")]
    public void ResetLever()
    {
        activated = false;
        accumulatedDamage = 0;
        lastDamageTime = float.NegativeInfinity;
    }

    private void Activate()
    {
        activated = true;
        accumulatedDamage = requiredDamage;
        if (objectToHide != null)
            objectToHide.SetActive(false);
        onActivated?.Invoke();
        if (!singleUse)
        {
            activated = false;
            accumulatedDamage = 0;
            lastDamageTime = float.NegativeInfinity;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !PlayerDebugToolkitHUD.IsVisible)
            return;

        Collider collider = hurtbox != null ? hurtbox : GetComponent<Collider>();
        if (collider == null)
            return;

        Gizmos.color = activated ? Color.green : new Color(1f, .45f, .1f, .9f);
        Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
    }
}
