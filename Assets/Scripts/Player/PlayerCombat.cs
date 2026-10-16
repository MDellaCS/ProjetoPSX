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
    private PlayerHeat playerHeat;
    private bool currentAttackIsHeavy;

    private float attackTimer;
    private float windupElapsed;
    private int currentDamage;
    private float currentHeavyPoiseMultiplier = 1f;
    private float currentHeavyHeatConsumed;
    private float currentAttackRadius;
    private bool aerialHeavyCancelQueued;
    private bool recoveryCancelQueued;
    private bool hasLaunchedPlayer;
    private bool diveHasLanded;
    private bool hasPlayedCameraShakeForCurrentAttack;

    private System.Action attackFinishedCallback;
    private System.Action<PlayerAttackData> attackHitCallback;

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
    [Tooltip("Velocidade vertical da queda do ataque Dive.")]
    [Min(0f)] public float aerialDiveFallSpeed = 28f;

    [Header("Launch Up")]
    [Tooltip("Gravidade aplicada apos o apice de Launch Up e Self Launch Up. Menor valor deixa a queda mais sustentada.")]
    [Range(0f, 1f)] public float launchUpLowGravityMultiplier = .2f;
    [Tooltip("Por quanto tempo a gravidade reduzida de Launch Up permanece ativa.")]
    [Min(0f)] public float launchUpLowGravityDuration = .55f;

    [Header("Launch")]
    [Tooltip("Velocidade horizontal de Launch gerada por cada ponto de Poise Damage final.")]
    [Min(0f)] public float launchForcePerPoiseDamage = .42f;
    [Tooltip("Tempo maximo que o inimigo permanece sendo arremessado caso nao bata em uma parede.")]
    [Min(0f)] public float launchDuration = .65f;

    [Header("Combat Status")]
    [Min(0f)]
    [FormerlySerializedAs("outOfCombatDamageDelay")]
    [SerializeField] private float combatTimerDuration = 5f;

    [SerializeField, HideInInspector]
    private int combatTimerSettingsVersion;

    private PlayerMovement playerMovement;
    private PlayerLockOn playerLockOn;
    private CharacterController characterController;
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
        EnsureCombatTimerDefaults();
        // O player nasce fora de combate. O timer so e preenchido por aggro ou dano real.
        combatTimer = 0f;
        PlayerInputBuffer.GetOrAdd(gameObject);
        PlayerComboController.GetOrAdd(gameObject);
        playerLockOn = PlayerLockOn.GetOrAdd(gameObject);
    }

    private void OnValidate()
    {
        EnsureCombatTimerDefaults();
    }

    private void EnsureCombatTimerDefaults()
    {
        if (combatTimerSettingsVersion >= 1)
            return;

        // A versao anterior usava 10 segundos. Esta atualizacao de design
        // reduz o valor existente para 5 sem afetar valores ja customizados.
        if (Mathf.Approximately(combatTimerDuration, 10f))
            combatTimerDuration = 5f;

        combatTimerSettingsVersion = 1;
    }

    void Start()
    {
        playerState = GetComponent<PlayerState>();
        playerMovement = GetComponent<PlayerMovement>();
        characterController = GetComponent<CharacterController>();
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
        System.Action<PlayerAttackData> onAttackHit,
        int damage,
        float attackRadius,
        bool isHeavy = false
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
            isHeavy
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

                if (currentAttack.isDive)
                {
                    // A hitbox do Dive fica ativa durante toda a queda. Ao
                    // pousar, comeca entao a janela Active configurada.
                    if (playerMovement != null && playerMovement.IsAerialDiving)
                        break;

                    if (!diveHasLanded)
                    {
                        diveHasLanded = true;
                        attackTimer = currentAttack.activeDuration;
                    }
                }

                attackTimer -= Time.deltaTime;

                if (attackTimer <= 0f)
                {
                    if (aerialHeavyCancelQueued || recoveryCancelQueued)
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
        currentDamage = 0;
        currentAttackRadius = 0f;
        aerialHeavyCancelQueued = false;
        recoveryCancelQueued = false;
        hasLaunchedPlayer = false;
        diveHasLanded = false;
        hasPlayedCameraShakeForCurrentAttack = false;

        enemiesHit.Clear();

        attackFinishedCallback = null;
        attackHitCallback = null;
        currentAttackIsHeavy = false;
        currentHeavyHeatConsumed = 0f;
        playerMovement?.StopAerialAttackMovement();
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

        if (attack.isDive)
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
            GetAttackOrigin() +
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
                    finalPoiseDamage = playerHeat.CalculateHeavyPoiseDamage(
                        finalPoiseDamage,
                        currentHeavyPoiseMultiplier
                    );
                }

                enemy.TakeDamage(
                    currentDamage,
                    transform.position,
                    finalPoiseDamage,
                    true
                );

                CombatImpactSystem.ReportPlayerAttackImpact(
                    transform,
                    enemy.transform,
                    currentAttackIsHeavy,
                    playerHeat != null && playerHeat.MaxHeat > 0f
                        ? currentHeavyHeatConsumed / playerHeat.MaxHeat
                        : 0f,
                    !hasPlayedCameraShakeForCurrentAttack
                );
                hasPlayedCameraShakeForCurrentAttack = true;

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
                    float launchForce = finalPoiseDamage *
                        launchForcePerPoiseDamage;
                    enemy.ApplyLaunch(
                        transform.position,
                        launchForce,
                        launchDuration
                    );
                }

                if (currentAttack.isDive)
                    enemy.ApplyAerialDive(aerialDiveFallSpeed);
                else
                    ApplyJuggleToAirborneEnemy(enemy);

                RegisterDamageDealtToEnemy();

                enemiesHit.Add(enemy);

                attackHitCallback?.Invoke(currentAttack);
            }
        }
    }

    public void CancelAttack()
    {
        if (!IsAttacking)
            return;

        ResetAttack();
    }

    public bool QueueAerialLightHeavyCancel()
    {
        if (currentAttack == null ||
            !currentAttack.isAerial ||
            currentAttack.isHeavy)
        {
            return false;
        }

        aerialHeavyCancelQueued = true;

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

    private void BeginAttack(
        PlayerAttackData attack,
        int damage,
        float attackRadius,
        bool isHeavy)
    {
        currentAttackIsHeavy = isHeavy;
        currentAttack = attack;
        currentHeavyPoiseMultiplier = 1f;
        currentHeavyHeatConsumed = 0f;

        if (isHeavy && playerHeat != null)
        {
            PlayerHeat.HeavyAttackCharge charge =
                playerHeat.ConsumeHeatForHeavyAttack();
            currentDamage = playerHeat.CalculateHeavyDamage(
                damage,
                charge.DamageMultiplier
            );
            currentHeavyPoiseMultiplier = charge.PoiseMultiplier;
            currentHeavyHeatConsumed = charge.HeatConsumed;
        }
        else
        {
            currentDamage = damage;
        }

        currentAttackRadius = attackRadius;
        hasLaunchedPlayer = false;
        diveHasLanded = false;
        hasPlayedCameraShakeForCurrentAttack = false;
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

        // O pesado aereo atual e um Launch horizontal: ele nao adiciona o
        // lift de juggle depois de arremessar o alvo.
        if (currentAttack.isAerial && currentAttack.isLaunch)
            return;

        // Juggle pertence ao estado do inimigo, nao ao estado do player:
        // leves e pesados no chao podem manter no ar um alvo ja lancado.
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

    /// <summary>
    /// Aplica o Launch Up global do player sem causar dano, Heat ou Poise.
    /// Usado por mecanicas de movimento, como o impacto do Slide.
    /// </summary>
    public void ApplyNonDamagingLaunchUp(
        BaseEnemy enemy,
        Vector3 horizontalDirection,
        float horizontalImpulse)
    {
        if (enemy == null || !enemy.CanBeLaunched)
            return;

        enemy.ApplyLaunchUp(
            GetLaunchUpInitialSpeed(),
            GetLaunchUpGravity(),
            launchUpLowGravityMultiplier,
            launchUpLowGravityDuration
        );
        enemy.ApplyLaunchUpHorizontalImpulse(
            horizontalDirection,
            horizontalImpulse
        );

        // Mesmo sem dano, acertar um inimigo com o Slide e uma interacao de
        // combate explicita e deve manter o player em combate.
        RegisterDamageDealtToEnemy();
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

    private Vector3 GetAttackOrigin()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        // O Transform do Player fica no chao. A origem deve acompanhar o
        // Center atual da capsula, inclusive em crouch/slide, para que a
        // hitbox corresponda ao corpo/arma visual do personagem.
        return characterController != null
            ? transform.TransformPoint(characterController.center)
            : transform.position;
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
            GetAttackOrigin() +
            transform.forward *
            currentAttack.attackDistance;

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            attackPosition,
            currentAttackRadius
        );
    }
}
