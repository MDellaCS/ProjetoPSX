using UnityEngine;

/// <summary>Assistencia de mira, impacto e puxao do ataque especial de grapple.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAttackController), typeof(PlayerMovement),
    typeof(PlayerState))]
public sealed class PlayerGrappleAttackController : MonoBehaviour
{
    private readonly struct CapsuleSeparation
    {
        public readonly float SurfaceDistance;
        public readonly Vector3 PlayerToEnemyDirection;

        public CapsuleSeparation(float surfaceDistance,
            Vector3 playerToEnemyDirection)
        {
            SurfaceDistance = surfaceDistance;
            PlayerToEnemyDirection = playerToEnemyDirection;
        }
    }

    [Header("Attack")]
    [Tooltip("Definicao normal de ataque usada pelo grapple. Configure seus tempos, strikes e hitboxes como qualquer outro ataque do jogador.")]
    [SerializeField] private PlayerAttackDefinition attackDefinition;

    [HideInInspector, SerializeField] private float targetAssistRange = 16f;
    [HideInInspector, SerializeField] private float targetAssistAngle = 55f;
    [HideInInspector, SerializeField] private float targetAssistNearAngle = 90f;
    [HideInInspector, SerializeField] private LayerMask targetObstructionMask = 243;
    [HideInInspector, SerializeField] private bool useTargetAssist = true;

    // Alcance, velocidades e assistencia do gancho de combate sao globais:
    // Assets/Resources/GrappleSettings (secao Combat).
    private float pullStopDistance => GrappleSettings.Combat.pullStopDistance;
    private float lighterEnemyPullSpeed => GrappleSettings.Combat.lighterEnemyPullSpeed;
    private float heavierEnemyPullSpeed => GrappleSettings.Combat.heavierEnemyPullSpeed;
    private float postPullStaggerDuration => GrappleSettings.Combat.postPullStaggerDuration;

    private PlayerAttackController attacks;
    private PlayerMovement movement;
    private PlayerCharacterController characterController;
    private PlayerState playerState;
    private PlayerHeat heat;
    private PlayerCombatStatus combatStatus;
    private PlayerMovesetController movesetController;
    private EnemyBase pulledEnemy;
    private EnemyMovement pulledEnemyMovement;
    private bool isPulling;
    private bool playerMovesToEnemy;
    private bool bothEntitiesMove;
    private Vector3 previousPlayerPullPosition;
    private Vector3 previousEnemyPullPosition;
    private int consecutiveStationaryPullFrames;
    private const float PullMovementEpsilonSqr = .000001f;
    private const int StationaryPullFramesBeforeAbort = 3;

    public bool IsPulling => isPulling;
    public bool CanStartGrappleAttack => attackDefinition != null &&
        attacks != null && attacks.CanStartAttack(attackDefinition);

    private void Awake()
    {
        attacks = GetComponent<PlayerAttackController>();
        movement = GetComponent<PlayerMovement>();
        characterController = movement.EntityCharacterController;
        playerState = GetComponent<PlayerState>();
        heat = GetComponent<PlayerHeat>();
        combatStatus = GetComponent<PlayerCombatStatus>();
        movesetController = GetComponent<PlayerMovesetController>();
    }

    /// <summary>Transfere a antiga configuracao do grapple para o assistente global.</summary>
    public void CopyLegacyTargetAssistSettingsTo(PlayerTargetAssist targetAssist)
    {
        if (targetAssist == null)
            return;

        targetAssist.ConfigureFromLegacy(targetAssistRange, targetAssistAngle,
            targetAssistNearAngle, targetObstructionMask, useTargetAssist);
    }

    private void Update()
    {
        if (!isPulling)
            return;

        if (GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked ||
            CombatImpactSystem.IsGameplayFrozen(this))
        {
            return;
        }

        if (playerState == null || !playerState.IsGrapplePulling())
        {
            EndPull();
            return;
        }

        if (pulledEnemy == null || pulledEnemy.IsDead)
        {
            EndPull();
            return;
        }

        if (BothPullEntitiesAreStationary())
        {
            EndPull();
            return;
        }

        // O ciclo do grapple e governado pela distancia, nao pelo
        // temporizador normal de stagger. Reaplica o estado de tracao se outro
        // sistema tiver liberado o stagger antes do fim do puxao.
        if (!pulledEnemy.IsGrapplePulled)
            pulledEnemy.BeginGrappleStagger();

        CapsuleSeparation separation = GetCapsuleSeparation(pulledEnemy);
        if (separation.SurfaceDistance <= pullStopDistance)
        {
            EndPull();
            return;
        }

        float safeDeltaTime = Mathf.Max(.0001f, Time.deltaTime);
        float usefulDistance = separation.SurfaceDistance - pullStopDistance;
        Vector3 playerToEnemy = separation.PlayerToEnemyDirection;
        if (playerToEnemy.sqrMagnitude <= .0001f)
            playerToEnemy = transform.forward;
        if (playerMovesToEnemy)
        {
            Vector3 playerVelocity = playerToEnemy.normalized *
                Mathf.Min(heavierEnemyPullSpeed,
                    usefulDistance / safeDeltaTime);
            movement.MoveDuringGrapplePull(playerVelocity * Time.deltaTime);
        }
        else if (bothEntitiesMove)
        {
            // Com pesos iguais, ambos percorrem a mesma distancia por frame
            // para se encontrarem no meio sem cruzar um pelo outro.
            float sharedSpeed = Mathf.Min(
                Mathf.Min(lighterEnemyPullSpeed, heavierEnemyPullSpeed),
                usefulDistance / (2f * safeDeltaTime));
            Vector3 direction = playerToEnemy.normalized;
            movement.MoveDuringGrapplePull(direction *
                sharedSpeed * Time.deltaTime);
            pulledEnemyMovement.SetGrapplePullVelocity(-direction *
                sharedSpeed);
        }
        else
        {
            Vector3 enemyVelocity = -playerToEnemy.normalized *
                Mathf.Min(lighterEnemyPullSpeed,
                    usefulDistance / safeDeltaTime);
            pulledEnemyMovement.SetGrapplePullVelocity(enemyVelocity);
        }
    }

    private void OnDisable()
    {
        if (isPulling)
            EndPull();
    }

    /// <summary>Dispara o ataque especial, mirando um inimigo elegivel se houver um proximo.</summary>
    public bool TryFire()
    {
        if (attackDefinition == null || attacks == null ||
            attacks.IsAttacking || playerState == null ||
            !playerState.IsNormal() || isPulling)
        {
            return false;
        }

        movesetController?.ResetSequence();
        return attacks.TryStartGrappleAttack(attackDefinition);
    }

    /// <summary>Resolve o impacto da hitbox e inicia a interacao de massa no primeiro alvo valido.</summary>
    internal bool ResolveHit(PlayerAttackRuntime runtime,
        PlayerAttackStrikeDefinition strike, EnemyBase enemy,
        float attackReach, Vector3 impactPosition, Vector3 impactDirection,
        PlayerHitboxTrackDefinition hitboxTrack, Collider impactCollider)
    {
        if (runtime == null || !runtime.IsGrappleAttack || strike == null ||
            enemy == null || enemy.IsDead || isPulling)
        {
            return false;
        }

        float distanceFromPlayer = Vector3.Distance(transform.position,
            impactPosition);
        float falloff = strike.damageFalloff != null
            ? strike.damageFalloff.Evaluate(distanceFromPlayer)
            : 1f;
        int damage = Mathf.RoundToInt(runtime.Damage *
            strike.damageMultiplier * falloff);
        int poiseDamage = Mathf.RoundToInt(runtime.PoiseDamage *
            strike.poiseMultiplier);
        PlayerAttackType type = runtime.Definition.kind == PlayerAttackKind.Heavy
            ? PlayerAttackType.Heavy : PlayerAttackType.Light;

        bool connected = enemy.TakeDamage(damage, transform.position,
            poiseDamage, out int damageDealt, out _, true, type, attackReach,
            impactDirection, impactPosition, true, transform);
        if (!connected)
            return false;

        runtime.HitConfirmed = true;
        PlayerComboSystem.RegisterConfirmedHit(transform);
        GetComponent<PlayerAttackAudioController>()?.NotifyHit(runtime,
            hitboxTrack, impactCollider != null ? impactCollider : enemy,
            impactPosition);
        CombatImpactSystem.ReportPlayerAttackImpact(transform, enemy.transform,
            runtime.Definition.kind == PlayerAttackKind.Heavy, damageDealt, true);

        if (heat != null && heat.IsHeatModeActive &&
            runtime.Definition.kind == PlayerAttackKind.Light &&
            strike.generatesHeatOnHit)
        {
            heat.AddHeat(damageDealt * heat.heatGeneratedPerDamageDealt);
        }
        combatStatus?.RegisterDamageDealtToEnemy();

        if (enemy.IsDead)
        {
            movesetController?.ResetSequence();
            attacks.CompleteGrappleAttack(runtime);
            return true;
        }

        BeginPull(enemy, runtime);
        return true;
    }

    private void BeginPull(EnemyBase enemy, PlayerAttackRuntime runtime)
    {
        if (!movement.BeginGrapplePullMotion())
        {
            enemy.BeginGrappleStagger();
            enemy.EndGrappleStagger(postPullStaggerDuration);
            attacks.CompleteGrappleAttack(runtime);
            return;
        }

        pulledEnemy = enemy;
        pulledEnemyMovement = enemy.GetComponent<EnemyMovement>();
        float enemyWeight = enemy.ImpactWeight;
        float playerWeight = movement.InteractionWeight;
        bothEntitiesMove = Mathf.Approximately(enemyWeight, playerWeight);
        playerMovesToEnemy = enemyWeight > playerWeight && !bothEntitiesMove;
        isPulling = true;
        previousPlayerPullPosition = transform.position;
        previousEnemyPullPosition = enemy.transform.position;
        consecutiveStationaryPullFrames = 0;

        enemy.BeginGrappleStagger();
        if (pulledEnemyMovement == null)
        {
            EndPull();
            attacks.CompleteGrappleAttack(runtime);
            return;
        }

        if (!playerMovesToEnemy && !bothEntitiesMove)
        {
            Vector3 pullDirection = GetPlayerCenter() - enemy.AttackOrigin;
            pullDirection = pullDirection.sqrMagnitude > .0001f
                ? pullDirection.normalized : transform.forward;
            pulledEnemyMovement.SetGrapplePullVelocity(pullDirection *
                lighterEnemyPullSpeed);
        }

        movesetController?.ResetSequence();
        attacks.CompleteGrappleAttack(runtime);
    }

    private Vector3 GetPlayerCenter()
    {
        Vector3 centerOffset = characterController != null
            ? characterController.center
            : Vector3.zero;
        return transform.position + transform.rotation * centerOffset;
    }

    private CapsuleSeparation GetCapsuleSeparation(EnemyBase enemy)
    {
        Collider playerCollider = characterController != null
            ? characterController.Collider
            : null;
        EnemyMovement enemyMovement = enemy != null
            ? enemy.GetComponent<EnemyMovement>()
            : null;
        Collider enemyCollider = enemyMovement != null
            ? enemyMovement.EntityCharacterController
            : null;

        if (playerCollider == null || enemyCollider == null ||
            !playerCollider.enabled || !enemyCollider.enabled)
        {
            Vector3 fallbackOffset = enemy != null
                ? enemy.AttackOrigin - GetPlayerCenter()
                : transform.forward;
            float fallbackDistance = fallbackOffset.magnitude;
            Vector3 fallbackDirection = fallbackDistance > .0001f
                ? fallbackOffset / fallbackDistance
                : transform.forward;
            return new CapsuleSeparation(fallbackDistance,
                fallbackDirection);
        }

        // As entidades usam capsulas verticais. Encontramos os pontos mais
        // proximos dos segmentos centrais; a direcao entre eles tambem e a
        // direcao que reduz a separacao das superficies.
        Bounds first = playerCollider.bounds;
        Bounds second = enemyCollider.bounds;
        float firstRadius = Mathf.Max(first.extents.x, first.extents.z);
        float secondRadius = Mathf.Max(second.extents.x, second.extents.z);
        float firstSegmentHalfHeight = Mathf.Max(0f,
            first.extents.y - firstRadius);
        float secondSegmentHalfHeight = Mathf.Max(0f,
            second.extents.y - secondRadius);

        float firstMinY = first.center.y - firstSegmentHalfHeight;
        float firstMaxY = first.center.y + firstSegmentHalfHeight;
        float secondMinY = second.center.y - secondSegmentHalfHeight;
        float secondMaxY = second.center.y + secondSegmentHalfHeight;
        float firstAxisY;
        float secondAxisY;

        if (firstMaxY < secondMinY)
        {
            firstAxisY = firstMaxY;
            secondAxisY = secondMinY;
        }
        else if (secondMaxY < firstMinY)
        {
            firstAxisY = firstMinY;
            secondAxisY = secondMaxY;
        }
        else
        {
            float sharedY = (Mathf.Max(firstMinY, secondMinY) +
                Mathf.Min(firstMaxY, secondMaxY)) * .5f;
            firstAxisY = sharedY;
            secondAxisY = sharedY;
        }

        Vector3 firstAxisPoint = new(first.center.x, firstAxisY,
            first.center.z);
        Vector3 secondAxisPoint = new(second.center.x, secondAxisY,
            second.center.z);
        Vector3 axisOffset = secondAxisPoint - firstAxisPoint;
        float axisDistance = axisOffset.magnitude;
        Vector3 direction = axisDistance > .0001f
            ? axisOffset / axisDistance
            : Vector3.ProjectOnPlane(second.center - first.center,
                Vector3.up).normalized;
        if (direction.sqrMagnitude <= .0001f)
            direction = transform.forward;

        float surfaceDistance = Mathf.Max(0f,
            axisDistance - firstRadius - secondRadius);
        return new CapsuleSeparation(surfaceDistance, direction);
    }

    private bool BothPullEntitiesAreStationary()
    {
        Vector3 playerPosition = transform.position;
        Vector3 enemyPosition = pulledEnemy.transform.position;
        bool playerMoved = (playerPosition - previousPlayerPullPosition)
            .sqrMagnitude >= PullMovementEpsilonSqr;
        bool enemyMoved = (enemyPosition - previousEnemyPullPosition)
            .sqrMagnitude >= PullMovementEpsilonSqr;

        previousPlayerPullPosition = playerPosition;
        previousEnemyPullPosition = enemyPosition;

        if (playerMoved || enemyMoved)
            consecutiveStationaryPullFrames = 0;
        else
            consecutiveStationaryPullFrames++;

        return consecutiveStationaryPullFrames >=
            StationaryPullFramesBeforeAbort;
    }

    private void EndPull()
    {
        if (!isPulling)
            return;

        EnemyBase enemy = pulledEnemy;
        isPulling = false;
        playerMovesToEnemy = false;
        bothEntitiesMove = false;
        pulledEnemy = null;
        pulledEnemyMovement = null;
        consecutiveStationaryPullFrames = 0;

        if (playerState != null && playerState.IsGrapplePulling())
        {
            if (movement != null)
                movement.EndGrapplePullMotion();
            else
                playerState.ChangeState(PlayerState.State.Normal);
        }

        // A recuperacao do stagger do inimigo comeca depois de liberar o player;
        // ela nunca determina quanto tempo o player permanece em GrapplePulling.
        if (enemy != null)
            enemy.EndGrappleStagger(postPullStaggerDuration);
    }
}
