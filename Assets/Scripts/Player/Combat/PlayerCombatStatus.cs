using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerCombatStatus : MonoBehaviour
{
    private enum DebugOverride { Neutral, ForceInCombat, ForceOutOfCombat }

    [Tooltip("Tempo em segundos que o player permanece em combate depois do ultimo aggro, dano causado ou dano recebido.")]
    [Min(0f)] [SerializeField] private float combatTimerDuration = 5f;
    private float combatTimer;
    private bool hasAggroedEnemies;
    private DebugOverride debugOverride;

    public bool HasAggroedEnemies => hasAggroedEnemies;
    public float CombatTimer => combatTimer;
    public bool IsCombatStateForced => debugOverride != DebugOverride.Neutral;
    public bool IsForcedInCombat => debugOverride == DebugOverride.ForceInCombat;
    public bool IsOutOfCombat => debugOverride switch
    {
        DebugOverride.ForceInCombat => false,
        DebugOverride.ForceOutOfCombat => true,
        _ => !hasAggroedEnemies && combatTimer <= 0f
    };

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame)
            debugOverride = debugOverride switch
            {
                DebugOverride.Neutral => DebugOverride.ForceInCombat,
                DebugOverride.ForceInCombat => DebugOverride.ForceOutOfCombat,
                _ => DebugOverride.Neutral
            };
        hasAggroedEnemies = EnemyBase.IsAnyEnemyAggroedOn(transform);
        if (hasAggroedEnemies || EnemyEncounter.IsPlayerInActiveEncounter(transform))
            combatTimer = combatTimerDuration;
        else
            combatTimer = Mathf.Max(0f, combatTimer - Time.deltaTime);
    }

    public void RegisterDamageDealtToEnemy() => combatTimer = combatTimerDuration;
    public void RegisterDamageTakenFromEnemy() => combatTimer = combatTimerDuration;
}
