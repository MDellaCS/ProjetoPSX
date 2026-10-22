using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHeat : MonoBehaviour
{
    private const float HeatPerNotch = 10f;

    /// <summary>Snapshot da carga usada por um pesado especifico.</summary>
    public readonly struct HeavyAttackCharge
    {
        public readonly float HeatConsumed;
        public readonly float DamageMultiplier;
        public readonly float PoiseMultiplier;

        public HeavyAttackCharge(float heatConsumed, float damageMultiplier, float poiseMultiplier)
        {
            HeatConsumed = heatConsumed;
            DamageMultiplier = damageMultiplier;
            PoiseMultiplier = poiseMultiplier;
        }
    }

    private enum BurnPhase { None, PreExplosion, ExplosionStun, Burning, RecoveryStun }

    [Header("Heat Notches")]
    [Tooltip("Limite positivo de Heat. De 0 ate este valor existem 10 notches; cada notch aumenta o poder dos pesados.")]
    [Min(1f)] public float maxHeat = 100f;
    [Tooltip("Magnitude maxima de Cold. O limite real de Heat e o valor negativo deste campo; alcanca-lo mata o player.")]
    [Min(1f)] public float maxColdMagnitude = 100f;

    [Header("Heavy Attack Power")]
    [Tooltip("Bonus de dano base adicionado por cada ponto do multiplicador de poder antes de multiplicar o pesado. Ex.: dano 130, poder 10x e bonus 5 resulta em (130 + 10 x 5) x 10 = 1800.")]
    [Min(0f)] public float heavyDamageAdditivePowerBonus = 5f;

    [Header("Light Attack Heat")]
    [Tooltip("Heat gerado por cada ponto de dano realmente retirado da vida do inimigo por um ataque leve marcado para gerar Heat.")]
    [Min(0f)] public float heatGeneratedPerDamageDealt = .3f;

    [Header("Superheat Notches")]
    [Tooltip("Capacidade maxima de Superheat. Ao encher, inicia o estado Burn.")]
    [Min(1f)] public float maxSuperheat = 50f;
    [Tooltip("Parte do Heat que excede 100 convertida em Superheat.")]
    [Min(0f)] public float excessHeatToSuperheatMultiplier = .5f;

    [Header("Passive Heat Cooling")]
    [Tooltip("Tempo fora de combate antes de o resfriamento passivo de Heat positivo comecar.")]
    [Min(0f)] public float passiveHeatCoolingStartDelay = 5f;
    [Tooltip("Heat removido por segundo no inicio do resfriamento, apos o Start Delay.")]
    [Min(0f)] public float initialPassiveHeatCoolingPerSecond = .2f;
    [Tooltip("Heat removido por segundo quando a rampa de resfriamento chega ao fim.")]
    [Min(0f)] public float finalPassiveHeatCoolingPerSecond = 18f;
    [Tooltip("Tempo fora de combate para a taxa de resfriamento ir do valor Initial ate o Final.")]
    [Min(0f)] public float passiveHeatCoolingRampDuration = 15f;
    [Tooltip("Formato da aceleracao da rampa. Valores maiores mantem o resfriamento fraco por mais tempo e aceleram perto do final.")]
    [Min(.01f)] public float passiveHeatCoolingRampExponent = 1.4f;

    [Header("Passive Cold Recovery")]
    [Tooltip("Heat recuperado por segundo enquanto o player esta com Heat negativo e fora de combate.")]
    [Min(0f)] public float passiveColdRecoveryPerSecond = 1f;
    [Tooltip("Parte do Cold maximo ate onde a recuperacao passiva pode chegar. Ex.: 0.9 recupera somente ate -90 se Min Heat for -100.")]
    [Range(0f, 1f)] public float coldRecoveryLimitPercent = .7f;

    [Header("Passive Superheat Cooling")]
    [Tooltip("Superheat removido por segundo enquanto o player esta fora de combate.")]
    [Min(0f)] public float passiveSuperheatCoolingPerSecond = 1f;
    [Tooltip("Superheat extra removido por segundo enquanto o Heat atual esta negativo. Acumula com o resfriamento fora de combate.")]
    [Min(0f)] public float coldSuperheatCoolingPerSecond = 4f;

    [Header("Burn")]
    [Tooltip("Tempo de Pre-Explosion em que o player pode apenas andar lentamente antes de explodir.")]
    [Min(0f)] public float burnPreExplosionDuration = 1.5f;
    [Tooltip("Duracao do stun completo causado pela propria explosao de Burn.")]
    [Min(0f)] public float burnInitialStunDuration = 3f;
    [Tooltip("Duracao do Burn depois da explosao. Durante esse tempo o player anda lentamente e perde Heat ate o alvo final.")]
    [Min(0f)] public float burningDuration = 5f;
    [Tooltip("Duracao do stun de Burn Recovery aplicado ao fim do Burn.")]
    [Min(0f)] public float burnFinalStunDuration = 1f;
    [Tooltip("Multiplicador da velocidade de movimento durante Pre-Explosion e Burn.")]
    [Range(0f, 1f)] public float burnMovementSpeedMultiplier = .3f;
    [Tooltip("Porcentagem do Cold maximo alcancada ao terminar Burn. Ex.: 0.7 leva o Heat a -70 quando o Cold maximo e 100.")]
    [Range(0f, 1f)] public float burnFinalColdPercent = .7f;

    [Header("Burn Blast")]
    [Tooltip("Raio da explosao disparada a partir do centro do player quando o primeiro stun de Burn comeca.")]
    [Min(0f)] public float burnBlastRadius = 6f;
    [Tooltip("Tempo em que a hitbox da explosao de Burn permanece ativa. Cada inimigo pode ser atingido uma unica vez nessa janela.")]
    [Min(.01f)] public float burnBlastActiveDuration = .7f;
    [Tooltip("Dano causado pela explosao de Burn em cada inimigo dentro do raio.")]
    [Min(0)] public int burnBlastDamage = 850;
    [Tooltip("Dano de Poise causado pela explosao de Burn. O Launch Forwards so acontece quando este valor quebra o Poise do inimigo.")]
    [Min(0)] public int burnBlastPoiseDamage = 100;
    [Header("Debug")]
    [Tooltip("Permite usar F4 para remover Heat e F5 para adicionar Heat durante testes. Esses atalhos alteram apenas o Heat e nao geram Superheat.")]
    [SerializeField] private bool debugHeatInputEnabled = true;
    [Tooltip("Quantidade de Heat adicionada ou removida por segundo pelos atalhos de debug.")]
    [Min(0f)] [SerializeField] private float debugHeatPerSecond = 35f;

    private float currentHeat;
    private float currentSuperheat;
    private float heatCoolingOutOfCombatElapsed;
    private float heatCoolingElapsed;
    private float debugHeatAccumulator;
    private BurnPhase burnPhase;
    private float burnPhaseTimer;
    private float burnElapsed;
    private float burnStartHeat;
    private float burnBlastActiveTimer;
    private readonly HashSet<EnemyBase> burnBlastAffectedEnemies = new();

    private PlayerState playerState;
    private PlayerCombatStatus combatStatus;
    private PlayerAttackController attackController;
    private PlayerMovement playerMovement;
    private CharacterController characterController;

    public float CurrentHeat => currentHeat;
    public float CurrentSuperheat => currentSuperheat;
    public float MaxHeat => maxHeat;
    public float MinHeat => -maxColdMagnitude;
    public float MaxSuperheat => maxSuperheat;
    // A pequena tolerancia evita que 100 armazenado como 99.99999 por
    // operacoes de ponto flutuante deixe o ultimo quadrado apagado.
    public int CurrentHeatNotch => GetSignedNotch(currentHeat, maxHeat, maxColdMagnitude);
    public int CurrentSuperheatNotch => GetPositiveNotch(currentSuperheat, maxSuperheat, 5);
    public bool IsBurning => burnPhase != BurnPhase.None;
    public float BurnMovementSpeedMultiplier => burnMovementSpeedMultiplier;
    public float HeavyDamageMultiplier => GetHeavyDamageMultiplier();
    public float HeavyPoiseMultiplier => GetHeavyPoiseMultiplier();
    private float BurnFinalHeatTarget => -maxColdMagnitude * burnFinalColdPercent;

    private void Start()
    {
        playerState = GetComponent<PlayerState>();
        combatStatus = GetComponent<PlayerCombatStatus>();
        attackController = GetComponent<PlayerAttackController>();
        playerMovement = GetComponent<PlayerMovement>();
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked ||
            CombatImpactSystem.IsGameplayFrozen(this))
            return;

        if (playerState == null || playerState.IsDead()) return;
        UpdateDebugHeatInput();

        if (IsBurning)
        {
            UpdateBurn();
            UpdateBurnBlast();
            CheckColdDeath();
            return;
        }

        UpdatePassiveHeat();
        UpdatePassiveSuperheat();
        CheckColdDeath();
    }

    private void UpdatePassiveHeat()
    {
        if (combatStatus == null || !combatStatus.IsOutOfCombat)
        {
            ResetHeatCoolingRamp();
            return;
        }
        if (currentHeat < 0f)
        {
            ResetHeatCoolingRamp();
            RecoverColdOutOfCombat();
            return;
        }
        if (currentHeat <= 0f)
        {
            ResetHeatCoolingRamp();
            return;
        }

        heatCoolingOutOfCombatElapsed += Time.deltaTime;
        if (heatCoolingOutOfCombatElapsed < passiveHeatCoolingStartDelay) return;
        heatCoolingElapsed += Time.deltaTime;
        currentHeat = Mathf.Max(0f, currentHeat - GetPassiveHeatCoolingPerSecond() * Time.deltaTime);
    }

    private void ResetHeatCoolingRamp()
    {
        heatCoolingOutOfCombatElapsed = 0f;
        heatCoolingElapsed = 0f;
    }

    private float GetPassiveHeatCoolingPerSecond()
    {
        float initialRate = Mathf.Max(0f, initialPassiveHeatCoolingPerSecond);
        float finalRate = Mathf.Max(initialRate, finalPassiveHeatCoolingPerSecond);
        if (passiveHeatCoolingRampDuration <= 0f) return finalRate;
        float progress = Mathf.Clamp01(heatCoolingElapsed / passiveHeatCoolingRampDuration);
        float exponent = Mathf.Max(.01f, passiveHeatCoolingRampExponent);
        float exponentialProgress = (Mathf.Exp(exponent * progress) - 1f) / (Mathf.Exp(exponent) - 1f);
        return Mathf.Lerp(initialRate, finalRate, exponentialProgress);
    }

    private void RecoverColdOutOfCombat()
    {
        float recoveryLimit = Mathf.Clamp(MinHeat * coldRecoveryLimitPercent, MinHeat, 0f);
        currentHeat = Mathf.Min(recoveryLimit, currentHeat + passiveColdRecoveryPerSecond * Time.deltaTime);
    }

    private void UpdatePassiveSuperheat()
    {
        // Superheat so nasce do Heat que excede o maximo. Permanecer com
        // Heat alto nao o gera passivamente.
        float cooling = 0f;
        if (combatStatus != null && combatStatus.IsOutOfCombat) cooling += passiveSuperheatCoolingPerSecond;
        if (currentHeat < 0f) cooling += coldSuperheatCoolingPerSecond;
        RemoveSuperheat(cooling * Time.deltaTime);
    }

    public void AddHeat(float amount)
    {
        if (amount <= 0f || IsBurning) return;
        float availableHeat = maxHeat - currentHeat;
        if (amount <= availableHeat)
        {
            currentHeat += amount;
            return;
        }
        currentHeat = maxHeat;
        AddSuperheat((amount - availableHeat) * excessHeatToSuperheatMultiplier);
    }

    public void RemoveHeat(float amount)
    {
        if (amount <= 0f) return;
        currentHeat = Mathf.Max(MinHeat, currentHeat - amount);
        CheckColdDeath();
    }

    public void AddSuperheat(float amount)
    {
        if (amount <= 0f || IsBurning) return;
        currentSuperheat = Mathf.Min(maxSuperheat, currentSuperheat + amount);
        if (currentSuperheat >= maxSuperheat) BeginBurn();
    }

    public void RemoveSuperheat(float amount)
    {
        if (amount > 0f) currentSuperheat = Mathf.Max(0f, currentSuperheat - amount);
    }

    /// <summary>
    /// Atalho de teste usado por F8. Encher Superheat dispara Burn da mesma
    /// forma que ocorreria no gameplay normal.
    /// </summary>
    public void FillHeatAndSuperheatForDebug()
    {
        if (IsBurning)
            return;

        currentHeat = maxHeat;
        currentSuperheat = maxSuperheat;
        BeginBurn();
    }

    /// <summary>
    /// Captura a carga do pesado e a gasta imediatamente. O snapshot e usado
    /// pelo golpe mesmo se ele errar ou se o Heat mudar durante a animacao.
    /// </summary>
    public HeavyAttackCharge ConsumeHeatForHeavyAttack()
    {
        float availablePositiveHeat = Mathf.Max(0f, currentHeat);
        int consumedNotch = GetSignedNotch(availablePositiveHeat, maxHeat,
            maxColdMagnitude);
        float damageMultiplier = GetHeavyDamageMultiplier(consumedNotch);
        float poiseMultiplier = GetHeavyPoiseMultiplier(consumedNotch);
        // O poder ainda e discreto por notch, mas o gasto tambem: um pesado
        // consome apenas os blocos completos de 10 Heat que de fato mudaram
        // seu multiplicador. O restante fica disponivel para o proximo golpe.
        float heatConsumed = Mathf.Max(0, consumedNotch) * HeatPerNotch;

        if (heatConsumed <= 0f)
            return new HeavyAttackCharge(0f, damageMultiplier, poiseMultiplier);

        currentHeat = Mathf.Max(0f, currentHeat - heatConsumed);
        RefillHeatFromSuperheat(heatConsumed);
        return new HeavyAttackCharge(heatConsumed, damageMultiplier, poiseMultiplier);
    }

    private void RefillHeatFromSuperheat(float maximumRefill)
    {
        // Superheat recompõe somente a carga que o pesado acabou de gastar.
        // Assim, tanto o resto fracionario quanto a reserva de Superheat
        // permanecem legiveis e o ataque nao "arredonda para cima" o Heat.
        float refill = Mathf.Min(currentSuperheat, maxHeat - currentHeat,
            Mathf.Max(0f, maximumRefill));
        if (refill <= 0f)
            return;

        currentSuperheat -= refill;
        currentHeat += refill;
    }

    public int CalculateHeavyDamage(int baseDamage, float damageMultiplier)
    {
        if (baseDamage <= 0)
            return 0;

        float powerMultiplier = Mathf.Max(0f, damageMultiplier);
        float additiveBonus = powerMultiplier *
            Mathf.Max(0f, heavyDamageAdditivePowerBonus);
        return Mathf.RoundToInt((baseDamage + additiveBonus) *
            powerMultiplier);
    }

    public int CalculateHeavyPoiseDamage(int basePoiseDamage, float poiseMultiplier)
    {
        return basePoiseDamage <= 0 ? 0 : Mathf.RoundToInt(basePoiseDamage * poiseMultiplier);
    }

    private float GetHeavyDamageMultiplier()
    {
        return GetHeavyDamageMultiplier(CurrentHeatNotch);
    }

    private static float GetHeavyDamageMultiplier(int notch)
    {
        return notch >= 0 ? 1f + notch : Mathf.Max(0f, 1f + notch * .1f);
    }

    private float GetHeavyPoiseMultiplier()
    {
        return GetHeavyPoiseMultiplier(CurrentHeatNotch);
    }

    private static float GetHeavyPoiseMultiplier(int notch)
    {
        return notch >= 0 ? 1f + notch * .5f : Mathf.Max(0f, 1f + notch * .05f);
    }

    private static int GetSignedNotch(float value, float positiveMaximum, float negativeMagnitude)
    {
        const float fullNotchTolerance = .01f;

        if (value >= positiveMaximum - fullNotchTolerance)
            return 10;
        if (value <= -negativeMagnitude + fullNotchTolerance)
            return -10;

        return value > 0f
            ? Mathf.FloorToInt(value / HeatPerNotch)
            : Mathf.CeilToInt(value / HeatPerNotch);
    }

    private static int GetPositiveNotch(float value, float maximum, int notchCount)
    {
        const float fullNotchTolerance = .01f;
        if (value >= maximum - fullNotchTolerance)
            return notchCount;

        return Mathf.Clamp(Mathf.FloorToInt(value / HeatPerNotch), 0, notchCount);
    }

    private void BeginBurn()
    {
        burnPhase = BurnPhase.PreExplosion;
        burnPhaseTimer = burnPreExplosionDuration;
        burnElapsed = 0f;
        burnStartHeat = currentHeat;
        attackController?.CancelAttack();
        playerMovement?.InterruptForBurn();
        playerState?.ChangeState(PlayerState.State.BurnPreExplosion);
    }

    private void UpdateBurn()
    {
        if (burnPhase == BurnPhase.Burning)
        {
            burnElapsed += Time.deltaTime;
            float progress = burningDuration <= 0f
                ? 1f
                : Mathf.Clamp01(burnElapsed / burningDuration);
            currentHeat = Mathf.Lerp(burnStartHeat, BurnFinalHeatTarget, progress);
        }

        burnPhaseTimer -= Time.deltaTime;
        if (burnPhaseTimer > 0f) return;

        switch (burnPhase)
        {
            case BurnPhase.PreExplosion:
                currentSuperheat = 0f;
                burnPhase = BurnPhase.ExplosionStun;
                burnPhaseTimer = burnInitialStunDuration;
                playerState.ChangeState(PlayerState.State.BurnStunned);
                BeginBurnBlast();
                break;
            case BurnPhase.ExplosionStun:
                burnPhase = BurnPhase.Burning;
                burnPhaseTimer = burningDuration;
                burnElapsed = 0f;
                burnStartHeat = currentHeat;
                playerState.ChangeState(PlayerState.State.Burning);
                break;
            case BurnPhase.Burning:
                currentHeat = BurnFinalHeatTarget;
                burnPhase = BurnPhase.RecoveryStun;
                burnPhaseTimer = burnFinalStunDuration;
                playerState.ChangeState(PlayerState.State.BurnStunned);
                break;
            case BurnPhase.RecoveryStun:
                currentHeat = BurnFinalHeatTarget;
                currentSuperheat = 0f;
                burnPhase = BurnPhase.None;
                playerState.ChangeState(PlayerState.State.Normal);
                break;
        }
    }

    private void BeginBurnBlast()
    {
        if (burnBlastRadius <= 0f || burnBlastActiveDuration <= 0f)
            return;

        burnBlastAffectedEnemies.Clear();
        burnBlastActiveTimer = burnBlastActiveDuration;
        ApplyBurnBlastHitbox();
    }

    private void UpdateBurnBlast()
    {
        if (burnBlastActiveTimer <= 0f)
            return;

        ApplyBurnBlastHitbox();
        burnBlastActiveTimer = Mathf.Max(
            0f,
            burnBlastActiveTimer - Time.deltaTime
        );
    }

    private void ApplyBurnBlastHitbox()
    {
        Vector3 origin = GetBurnBlastOrigin();

        foreach (Collider hit in Physics.OverlapSphere(origin, burnBlastRadius))
        {
            EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
            if (enemy == null || !burnBlastAffectedEnemies.Add(enemy))
                continue;

            int damageDealt = 0;
            CombatPoiseHitResult poiseHit = default;
            bool connected = burnBlastDamage > 0 &&
                enemy.TakeDamage(
                    burnBlastDamage,
                    origin,
                    burnBlastPoiseDamage,
                    out damageDealt,
                    out poiseHit,
                    true
                );

            if (connected)
            {
                CombatPoiseSystem rules = CombatPoiseSystem.Instance;
                enemy.ApplyPoiseReaction(poiseHit, new CombatReactionRequest(
                    CombatReactionType.LaunchForwards, origin,
                    playerMovement != null
                        ? playerMovement.GetMaximumJumpLaunchSpeed() : 18f,
                    playerMovement != null ? playerMovement.gravity : -60f,
                    rules.MinimumLaunchForce,
                    rules.CalculateLaunchForce(damageDealt),
                    rules.LaunchForwardsDeceleration,
                    rules.JuggleLiftSpeed,
                    rules.JuggleGravityMultiplier,
                    rules.JuggleDuration));
            }
        }
    }

    private Vector3 GetBurnBlastOrigin()
    {
        return characterController != null
            ? characterController.bounds.center
            : transform.position;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying ||
            !PlayerDebugToolkitHUD.IsVisible ||
            burnBlastActiveTimer <= 0f)
        {
            return;
        }

        Gizmos.color = new Color(1f, .45f, 0f, .9f);
        Gizmos.DrawWireSphere(GetBurnBlastOrigin(), burnBlastRadius);
    }

    private void UpdateDebugHeatInput()
    {
        if (!debugHeatInputEnabled || IsBurning) return;
        float direction = 0f;
        if (GameInput.DebugHeatDownHeld) direction -= 1f;
        if (GameInput.DebugHeatUpHeld) direction += 1f;
        if (Mathf.Approximately(direction, 0f))
        {
            debugHeatAccumulator = 0f;
            return;
        }
        debugHeatAccumulator += debugHeatPerSecond * Time.deltaTime;
        float amount = debugHeatAccumulator;
        debugHeatAccumulator = 0f;
        // O controle de Heat isolado nao usa AddHeat: ultrapassar Max Heat
        // por F5 nao deve gerar Superheat nem iniciar Burn. F8 testa esse
        // fluxo completo separadamente.
        currentHeat = Mathf.Clamp(
            currentHeat + amount * direction,
            MinHeat,
            maxHeat
        );
        ResetHeatCoolingRamp();
    }

    private void CheckColdDeath()
    {
        if (playerState != null && !playerState.IsDead() && currentHeat <= MinHeat)
            playerState.ChangeState(PlayerState.State.Dead);
    }
}
