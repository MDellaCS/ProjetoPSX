using UnityEngine;

/// <summary>Valores globais das unidades usadas na vida e nos ataques.</summary>
[CreateAssetMenu(menuName = "Projeto PSX/Combat/Unit Settings", fileName = "Combat/CombatUnitSettings")]
public sealed class CombatUnitSettings : ScriptableObject
{
    private static CombatUnitSettings instance;

    [Tooltip("Quantidade de vida que cada unidade de vida de um inimigo representa. Aplicada quando o inimigo e inicializado.")]
    [Min(1)] [SerializeField] private int enemyHealthUnitValue = 1;
    [Tooltip("Quantidade de dano que cada unidade de dano de um ataque inimigo representa.")]
    [Min(1)] [SerializeField] private int enemyDamageUnitValue = 1;
    [Tooltip("Quantidade de dano que cada unidade de dano de um ataque do jogador representa.")]
    [Min(1)] [SerializeField] private int playerDamageUnitValue = 1;

    private static CombatUnitSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<CombatUnitSettings>("Combat/CombatUnitSettings");
                if (instance == null)
                {
                    instance = CreateInstance<CombatUnitSettings>();
                    instance.hideFlags = HideFlags.HideAndDontSave;
                }
            }
            return instance;
        }
    }

    public static int EnemyHealth(int units) => Convert(units, Instance.enemyHealthUnitValue, 1);
    public static int EnemyDamage(int units) => Convert(units, Instance.enemyDamageUnitValue, 0);
    public static int PlayerDamage(int units) => Convert(units, Instance.playerDamageUnitValue, 0);

    private static int Convert(int units, int unitValue, int minimum)
    {
        long value = (long)Mathf.Max(minimum, units) * Mathf.Max(1, unitValue);
        return (int)System.Math.Min(int.MaxValue, value);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;
}
