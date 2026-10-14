using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class PlayerCombat : MonoBehaviour
{
    private enum DebugCombatOverride
    {
        Neutral,
        ForceInCombat,
        ForceOutOfCombat
    }

    public enum AttackPhase
    {
        None,
        Windup,
        Active,
        Recovery
    }

    private AttackPhase attackPhase = AttackPhase.None;

    private HashSet<BaseEnemy> enemiesHit = new HashSet<BaseEnemy>();

    private PlayerState playerState;
    private PlayerAttackData currentAttack;
    private PlayerAttackSequence currentSequence;
    private PlayerHeat playerHeat;
    private bool currentAttackIsHeavy;

    private float attackTimer;
    private float windupElapsed;
    private int currentDamage;
    private int currentHeatGenerated;
    private float currentAttackRadius;
    private int sequenceStrikeIndex;
    private bool aerialDiveCancelQueued;
    private bool recoveryCancelQueued;
    private bool hasLaunchedPlayer;

    private System.Action attackFinishedCallback;
    private System.Action<PlayerAttackData, int> attackHitCallback;

    [Header("Attack Rotation")]
    [FormerlySerializedAs("windupRotationSpeed")]
    [SerializeField] private float attackingRotationSpeed = 300f;

    [Header("Aerial Attack And Enemy Juggle Movement")]
    [Tooltip("Impulso vertical compartilhado pelos leves aereos do player e pelo juggle de inimigos no ar.")]
    [FormerlySerializedAs("aerialLightLiftSpeed")]
    [Min(0f)] public float juggleLiftSpeed = 4f;
    [Tooltip("Multiplicador de gravidade compartilhado pelos leves aereos do player e pelo juggle de inimigos no ar.")]
    [FormerlySerializedAs("aerialLightGravityMultiplier")]
    [Range(0f, 1f)] public float juggleGravityMultiplier = .3f;
    [Tooltip("Velocidade vertical da queda de qualquer pesado aereo.")]
    [Min(0f)] public float aerialDiveFallSpeed = 28f;
    [Tooltip("Velocidade da queda do Bounce. Limitada pela velocidade terminal do inimigo.")]
    [Min(0f)] public float aerialBounceFallSpeed = 30f;
    [Tooltip("Impulso vertical que o inimigo recebe ao tocar o chao durante um Bounce.")]
    [Min(0f)] public float aerialBounceLiftSpeed = 14f;

    [Header("Launch Up")]
    [Tooltip("Gravidade aplicada apos o apice de Launch Up e Self Launch Up. Menor valor deixa a queda mais sustentada.")]
    [Range(0f, 1f)] public float launchUpLowGravityMultiplier = .2f;
    [Tooltip("Por quanto tempo a gravidade reduzida de Launch Up permanece ativa.")]
    [Min(0f)] public float launchUpLowGravityDuration = .55f;

    [Header("Launch And Wall Splat")]
    [Tooltip("Velocidade horizontal aplicada por ataques marcados como Is Launch.")]
    [Min(0f)] public float launchForce = 16f;
    [Tooltip("Tempo maximo que o inimigo permanece sendo arremessado caso nao bata em uma parede.")]
    [Min(0f)] public float launchDuration = .65f;
    [Min(0)] public int wallSplatDamage = 20;
    [Min(0f)] public float wallSplatStunDuration = 1f;
    [Tooltip("Camadas do cenario que contam como parede para Wall Splat.")]
    [SerializeField] private LayerMask wallSplatLayers = ~0;

    [Header("Combat Status")]
    [Min(0f)]
    [FormerlySerializedAs("outOfCombatDamageDelay")]
    [SerializeField] private float combatTimerDuration = 10f;

    private PlayerMovement playerMovement;
    private PlayerLockOn playerLockOn;
    private Vector3 attackFacingDirection;
    private bool hasAggroedEnemies;
    private float combatTimer;
    private DebugCombatOverride debugCombatOverride;

    public bool IsAttacking
    {
        get
        {
            return attackPhase != AttackPhase.None;
        }
    }

    public bool IsExecutingAttackSequence => currentSequence != null;
    public string CurrentAttackId => currentAttack != null
        ? currentAttack.attackName
        : string.Empty;
    public AttackPhase CurrentAttackPhase => attackPhase;
    public float CurrentAttackPhaseProgress => GetCurrentAttackPhaseProgress();
    public bool HasAggroedEnemies => hasAggroedEnemies;
    public bool IsOutOfCombat =>
        debugCombatOverride switch
        {
            DebugCombatOverride.ForceInCombat => false,
            DebugCombatOverride.ForceOutOfCombat => true,
            _ => !hasAggroedEnemies && combatTimer <= 0f
        };
    public float CombatTimer => combatTimer;
    public bool IsCombatStateForced =>
        debugCombatOverride != DebugCombatOverride.Neutral;
    public bool IsForcedInCombat =>
        debugCombatOverride == DebugCombatOverride.ForceInCombat;

    private float GetCurrentAttackPhaseProgress()
    {
        if (currentAttack == null || attackPhase == AttackPhase.None)
            return 0f;

        float duration = attackPhase switch
        {
            AttackPhase.Windup => currentAttack.windupDuration,
            AttackPhase.Active => currentAttack.activeDuration,
            AttackPhase.Recovery => currentAttack.recoveryDuration,
            _ => 0f
        };

        return duration <= 0f
            ? 1f
            : Mathf.Clamp01(1f - attackTimer / duration);
    }

    void Awake()
    {
        // O player nasce fora de combate. O timer so e preenchido por aggro ou dano real.
        combatTimer = 0f;
        PlayerInputBuffer.GetOrAdd(gameObject);
        PlayerComboController.GetOrAdd(gameObject);
        playerLockOn = PlayerLockOn.GetOrAdd(gameObject);
    }

    void Start()
    {
        playerState = GetComponent<PlayerState>();
        playerMovement = GetComponent<PlayerMovement>();
        if (playerLockOn == null)
            playerLockOn = GetComponent<PlayerLockOn>();
        playerHeat = GetComponent<PlayerHeat>();
    }

    void Update()
    {
        UpdateDebugCombatOverride();
        UpdateCombatStatus();

        if (IsAttacking)
        {
            UpdateAttack();
        }
    }

    private void UpdateDebugCombatOverride()
    {
        if (Keyboard.current == null ||
            !Keyboard.current.f6Key.wasPressedThisFrame)
        {
            return;
        }

        debugCombatOverride = debugCombatOverride switch
        {
            DebugCombatOverride.Neutral => DebugCombatOverride.ForceInCombat,
            DebugCombatOverride.ForceInCombat => DebugCombatOverride.ForceOutOfCombat,
            _ => DebugCombatOverride.Neutral
        };
    }

    private void UpdateCombatStatus()
    {
        hasAggroedEnemies = BaseEnemy.IsAnyEnemyAggroedOn(transform);

        if (hasAggroedEnemies ||
            EnemyEncounter.IsPlayerInActiveEncounter(transform))
        {
            ResetCombatTimer();
            return;
        }

        combatTimer = Mathf.Max(
            0f,
            combatTimer - Time.deltaTime
        );
    }

    public void RegisterDamageDealtToEnemy()
    {
        ResetCombatTimer();
    }

    public void RegisterDamageTakenFromEnemy()
    {
        ResetCombatTimer();
    }

    private void ResetCombatTimer()
    {
        combatTimer = combatTimerDuration;
    }

    public bool StartAttack(
        PlayerAttackData attack,
        System.Action onAttackFinished,
        System.Action<PlayerAttackData, int> onAttackHit,
        int damage,
        float attackRadius,
        bool isHeavy = false,
        int generatedHeat = 0
    )
    {
        bool canStartAttack = playerState != null &&
            (playerState.IsNormal() ||
             (attack != null &&
              attack.isDashAttack &&
              playerState.IsDashing() &&
              playerMovement != null &&
              playerMovement.IsGroundDash));

        if (attack == null ||
            IsAttacking ||
            !canStartAttack)
        {
            return false;
        }

        attackFinishedCallback =
            onAttackFinished;

        attackHitCallback =
            onAttackHit;

        playerState.ChangeState(
            PlayerState.State.Attacking
        );

        BeginAttack(
            attack,
            damage,
            attackRadius,
            isHeavy,
            generatedHeat
        );

        return true;
    }

    void UpdateAttack()
    {
        switch (attackPhase)
        {
            case AttackPhase.Windup:

                RotateDuringWindup();
                ApplyWindupMovement();

                attackTimer -= Time.deltaTime;

                if (attackTimer <= 0f)
                {
                    ChangeAttackPhase(
                        AttackPhase.Active,
                        currentAttack.activeDuration
                    );
                }

                break;

            case AttackPhase.Active:

                RotateDuringActive();
                Attack();
                ApplySelfLaunchUpIfNeeded();

                if (currentAttack.isAerial && currentAttack.isHeavy)
                {
                    // Durante a queda, Active nao consome tempo: a hitbox fica
                    // ativa ate tocar o chao. Ao aterrissar, o mesmo timer de
                    // Active passa a contar normalmente antes do Recovery.
                    if (playerMovement != null && playerMovement.IsAerialDiving)
                        break;

                    attackTimer -= Time.deltaTime;

                    if (attackTimer <= 0f)
                    {
                        if (recoveryCancelQueued)
                            FinishAttack();
                        else
                            ChangeAttackPhase(
                                AttackPhase.Recovery,
                                currentAttack.recoveryDuration
                            );
                    }

                    break;
                }

                attackTimer -= Time.deltaTime;

                if (attackTimer <= 0f)
                {
                    if (aerialDiveCancelQueued || recoveryCancelQueued)
                    {
                        FinishAttack();
                        break;
                    }

                    ChangeAttackPhase(
                        AttackPhase.Recovery,
                        currentAttack.recoveryDuration
                    );
                }

                break;

            case AttackPhase.Recovery:

                if (recoveryCancelQueued)
                {
                    FinishAttack();
                    break;
                }

                attackTimer -= Time.deltaTime;

                if (attackTimer <= 0f)
                {
                    FinishAttack();
                }

                break;
        }
    }

    private void FinishAttack()
    {
        if (TryAdvanceSequence())
            return;

        // Guarda o callback antes de limpar os dados.
        System.Action callback = attackFinishedCallback;

        ResetAttack();

        // Só libera o estado se ainda estiver atacando.
        if (playerState.IsAttacking())
        {
            playerState.ChangeState(
                PlayerState.State.Normal
            );
        }

        callback?.Invoke();
    }

    private void ResetAttack()
    {
        attackPhase = AttackPhase.None;
        attackTimer = 0f;
        windupElapsed = 0f;

        currentAttack = null;
        currentSequence = null;
        currentDamage = 0;
        currentHeatGenerated = 0;
        currentAttackRadius = 0f;
        sequenceStrikeIndex = 0;
        aerialDiveCancelQueued = false;
        recoveryCancelQueued = false;
        hasLaunchedPlayer = false;

        enemiesHit.Clear();

        attackFinishedCallback = null;
        attackHitCallback = null;
        currentAttackIsHeavy = false;
        playerMovement?.StopAerialAttackMovement();
    }

    private bool TryAdvanceSequence()
    {
        if (currentSequence == null)
            return false;

        sequenceStrikeIndex++;
        if (sequenceStrikeIndex >= currentSequence.strikes.Length)
            return false;

        BeginSequenceStrike();
        return true;
    }

    void ChangeAttackPhase(AttackPhase newPhase, float duration)
    {
        attackPhase = newPhase;

        attackTimer = duration;

        if (newPhase == AttackPhase.Windup)
            windupElapsed = 0f;
    }

    private void ApplyAerialAttackMovement(PlayerAttackData attack)
    {
        if (playerMovement == null || attack == null)
            return;

        if (!attack.isAerial)
            return;

        if (attack.isHeavy)
        {
            playerMovement.BeginAerialDive(aerialDiveFallSpeed);
            return;
        }

        playerMovement.BeginAerialLight(
            juggleLiftSpeed,
            juggleGravityMultiplier,
            attack.windupDuration + attack.activeDuration
        );
    }

    private void ApplyWindupMovement()
    {
        if (currentAttack == null ||
            playerMovement == null ||
            currentAttack.windupMoveDistance <= 0f ||
            currentAttack.windupDuration <= 0f)
        {
            return;
        }

        float moveStart = Mathf.Min(
            currentAttack.windupMoveStart,
            currentAttack.windupMoveEnd
        );
        float moveEnd = Mathf.Max(
            currentAttack.windupMoveStart,
            currentAttack.windupMoveEnd
        );

        if (moveEnd <= moveStart)
            return;

        float previousTime = windupElapsed / currentAttack.windupDuration;
        windupElapsed = Mathf.Min(
            currentAttack.windupDuration,
            windupElapsed + Time.deltaTime
        );
        float currentTime = windupElapsed / currentAttack.windupDuration;

        float previousProgress = GetWindupMoveProgress(
            previousTime,
            moveStart,
            moveEnd
        );
        float currentProgress = GetWindupMoveProgress(
            currentTime,
            moveStart,
            moveEnd
        );

        float distance = currentAttack.windupMoveDistance *
            (currentProgress - previousProgress);
        // O avanco segue a frente atual. O giro durante o Windup permite
        // steer sem alterar a distancia total planejada para o ataque.
        playerMovement.ApplyAttackMovement(transform.forward * distance);
    }

    private float GetWindupMoveProgress(
        float windupTime,
        float moveStart,
        float moveEnd)
    {
        float normalizedTime = Mathf.InverseLerp(
            moveStart,
            moveEnd,
            windupTime
        );
        AnimationCurve curve = currentAttack.windupMoveCurve;
        return curve != null
            ? Mathf.Clamp01(curve.Evaluate(normalizedTime))
            : normalizedTime;
    }

    void Attack()
    {
        if (currentAttack == null)
            return;

        Vector3 attackPosition =
            transform.position +
            transform.forward *
            currentAttack.attackDistance;

        Collider[] hits = Physics.OverlapSphere(
            attackPosition,
            currentAttackRadius
        );

        foreach (Collider hit in hits)
        {
            BaseEnemy enemy = hit.GetComponentInParent<BaseEnemy>();

            if (enemy != null && !enemiesHit.Contains(enemy))
            {
                int finalPoiseDamage = currentAttack.staggerDamage;

                if (currentAttackIsHeavy && playerHeat != null)
                {
                    finalPoiseDamage = playerHeat.CalculateHeavyPoiseDamage(finalPoiseDamage);
                }

                enemy.TakeDamage(
                    currentDamage,
                    transform.position,
                    finalPoiseDamage
                );

                if (currentAttack.isLaunchUp)
                {
                    enemy.ApplyLaunchUp(
                        GetLaunchUpInitialSpeed(),
                        GetLaunchUpGravity(),
                        launchUpLowGravityMultiplier,
                        launchUpLowGravityDuration
                    );
                }

                if (currentAttack.isLaunch)
                {
                    enemy.ApplyLaunch(
                        transform.position,
                        launchForce,
                        launchDuration,
                        wallSplatDamage,
                        wallSplatStunDuration,
                        wallSplatLayers
                    );
                }

                ApplyJuggleToAirborneEnemy(enemy);

                RegisterDamageDealtToEnemy();

                enemiesHit.Add(enemy);

                attackHitCallback?.Invoke(currentAttack, currentHeatGenerated);
            }
        }
    }

    public void CancelAttack()
    {
        if (!IsAttacking)
            return;

        ResetAttack();
    }

    public bool QueueAerialLightDiveCancel()
    {
        if (currentAttack == null ||
            !currentAttack.isAerial ||
            currentAttack.isHeavy)
        {
            return false;
        }

        aerialDiveCancelQueued = true;

        // Durante Recovery, o cancelamento e imediato. Durante Windup ou
        // Active, ele aguarda o fim do Active para nao cortar o golpe no meio.
        if (attackPhase == AttackPhase.Recovery)
            FinishAttack();

        return true;
    }

    private void ApplySelfLaunchUpIfNeeded()
    {
        if (currentAttack == null ||
            !currentAttack.launchesPlayer ||
            hasLaunchedPlayer)
        {
            return;
        }

        // Acontece depois da hitbox: o Dash H1 conecta no alvo no chao,
        // mas tambem lanca o player quando o golpe erra.
        playerMovement?.ApplySelfLaunchUp(
            GetLaunchUpInitialSpeed(),
            GetLaunchUpGravity(),
            launchUpLowGravityMultiplier,
            launchUpLowGravityDuration
        );
        hasLaunchedPlayer = true;
    }

    private float GetLaunchUpInitialSpeed()
    {
        return playerMovement != null
            ? playerMovement.GetMaximumJumpLaunchSpeed()
            : 18f;
    }

    private float GetLaunchUpGravity()
    {
        return playerMovement != null
            ? playerMovement.gravity
            : -60f;
    }

    public bool StartAttackSequence(
        PlayerAttackSequence sequence,
        System.Action onAttackFinished,
        System.Action<PlayerAttackData, int> onAttackHit)
    {
        if (sequence == null ||
            !sequence.HasStrikes ||
            IsAttacking ||
            playerState == null)
        {
            return false;
        }

        bool canStartSequence = playerState.IsNormal() ||
            (sequence.isDashAttack &&
             playerState.IsDashing() &&
             playerMovement != null &&
             playerMovement.IsGroundDash);

        if (!canStartSequence)
            return false;

        currentSequence = sequence;
        sequenceStrikeIndex = 0;
        attackFinishedCallback = onAttackFinished;
        attackHitCallback = onAttackHit;

        playerState.ChangeState(PlayerState.State.Attacking);
        BeginSequenceStrike();
        return true;
    }

    private void BeginSequenceStrike()
    {
        PlayerAttackData attack = currentSequence.strikes[sequenceStrikeIndex];

        if (attack == null)
        {
            FinishAttack();
            return;
        }

        int damage = attack.isHeavy && playerHeat != null
            ? playerHeat.CalculateHeavyDamage(attack.damage)
            : attack.damage;
        int generatedHeat = attack.isHeavy && playerHeat != null
            ? playerHeat.CalculateHeavyHeatGeneration(attack.heatGenerated)
            : 0;

        BeginAttack(
            attack,
            damage,
            attack.attackRadius,
            attack.isHeavy,
            generatedHeat
        );
    }

    private void BeginAttack(
        PlayerAttackData attack,
        int damage,
        float attackRadius,
        bool isHeavy,
        int generatedHeat)
    {
        currentAttackIsHeavy = isHeavy;
        currentAttack = attack;
        currentDamage = damage;
        currentHeatGenerated = generatedHeat;
        currentAttackRadius = attackRadius;
        hasLaunchedPlayer = false;
        enemiesHit.Clear();

        ApplyAerialAttackMovement(attack);

        attackFacingDirection = GetLockOnDirectionOrZero();
        if (attackFacingDirection.sqrMagnitude <= 0.0001f &&
            playerMovement != null &&
            playerMovement.DesiredDirection.sqrMagnitude > 0.0001f)
        {
            attackFacingDirection = playerMovement.DesiredDirection.normalized;
        }

        ChangeAttackPhase(AttackPhase.Windup, currentAttack.windupDuration);
    }

    private void ApplyJuggleToAirborneEnemy(BaseEnemy enemy)
    {
        if (enemy == null || currentAttack == null)
        {
            return;
        }

        // O proprio golpe de Launch Up inicia a trajetoria usando os valores
        // do pulo. Ele nao pode ser sobrescrito pelo impulso menor de juggle
        // no mesmo acerto; juggle vale apenas para golpes seguintes.
        if (currentAttack.isLaunchUp)
            return;

        // Juggle pertence ao estado do inimigo, nao ao estado do player:
        // leves e pesados no chao podem manter no ar um alvo ja lancado.
        // O pesado aereo e o unico caso especial: ele inicia o Dive.
        if (currentAttack.isAerialBounce)
        {
            enemy.ApplyAerialJuggleBounce(
                aerialBounceFallSpeed,
                aerialBounceLiftSpeed,
                GetLaunchUpGravity()
            );
            return;
        }

        if (currentAttack.isHeavy && currentAttack.isAerial)
        {
            enemy.ApplyAerialJuggleDive(aerialDiveFallSpeed);
            return;
        }

        float launchGravity = playerMovement != null
            ? playerMovement.gravity
            : -60f;

        enemy.ApplyAerialJuggleLight(
            juggleLiftSpeed,
            launchGravity,
            juggleGravityMultiplier,
            currentAttack.windupDuration + currentAttack.activeDuration
        );
    }

    public bool QueueRecoveryCancel()
    {
        if (!IsAttacking ||
            (attackPhase != AttackPhase.Active &&
             attackPhase != AttackPhase.Recovery))
        {
            return false;
        }

        recoveryCancelQueued = true;
        return true;
    }

    private void RotateDuringWindup()
    {
        if (playerMovement == null)
            return;

        Vector3 direction = GetLockOnDirectionOrZero();
        if (direction.sqrMagnitude <= 0.0001f)
            direction = playerMovement.DesiredDirection;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            attackFacingDirection = direction.normalized;
        }

        RotateTowards(attackFacingDirection);
    }

    private void RotateDuringActive()
    {
        if (playerMovement == null)
        {
            return;
        }

        Vector3 direction = GetLockOnDirectionOrZero();
        if (direction.sqrMagnitude <= 0.0001f)
            direction = playerMovement.DesiredDirection;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        RotateTowards(direction);
    }

    private void RotateTowards(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            attackingRotationSpeed * Time.deltaTime
        );
    }

    private Vector3 GetLockOnDirectionOrZero()
    {
        if (playerLockOn == null)
            playerLockOn = GetComponent<PlayerLockOn>();

        return playerLockOn != null
            ? playerLockOn.GetDirectionToLockedTarget()
            : Vector3.zero;
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying && !PlayerDebugToolkitHUD.IsVisible)
            return;

        if (attackPhase != AttackPhase.Active)
            return;

        if (currentAttack == null)
            return;

        Vector3 attackPosition =
            transform.position +
            transform.forward *
            currentAttack.attackDistance;

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            attackPosition,
            currentAttackRadius
        );
    }
}
