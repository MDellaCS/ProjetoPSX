using UnityEngine;

[CreateAssetMenu(fileName = "CorpseRunSettings", menuName = "Projeto PSX/Progression/Corpse Run Settings")]
public sealed class CorpseRunSettings : ScriptableObject
{
    [Tooltip("Prefab do inimigo que guarda o dinheiro perdido. Configure nele o moveset, visual e combate da copia do jogador.")]
    public GameObject corpseEnemyPrefab;

    [Header("Multiplicadores dos golpes")]
    [Tooltip("Multiplicador da duracao de Windup dos golpes do inimigo CorpseRun.")]
    [Min(0f)] public float windupMultiplier = 1f;
    [Tooltip("Multiplicador da duracao de Active dos golpes do inimigo CorpseRun.")]
    [Min(0f)] public float activeMultiplier = 1f;
    [Tooltip("Multiplicador da duracao de Recovery dos golpes do inimigo CorpseRun.")]
    [Min(0f)] public float recoveryMultiplier = 1f;

    private static CorpseRunSettings instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    public static CorpseRunSettings Load()
    {
        if (instance == null)
            instance = Resources.Load<CorpseRunSettings>("CorpseRun/CorpseRunSettings");
        return instance;
    }
}
