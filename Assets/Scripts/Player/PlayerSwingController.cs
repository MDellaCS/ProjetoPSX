using System.Collections.Generic;
using UnityEngine;

/// <summary>Localiza pontos de balanco e aplica um movimento presetado no motor do jogador.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement), typeof(PlayerState),
    typeof(PlayerGrappleAttackController))]
public sealed class PlayerSwingController : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Permite soltar a corda ao pressionar o input normal de pulo.")]
    [SerializeField] private bool releaseOnJump = true;

    [Header("Anchor Visibility")]
    [Tooltip("O ponto precisa estar pelo menos esta altura acima do centro do jogador.")]
    [Min(0f)] [SerializeField] private float minimumAnchorHeight = .75f;
    [Tooltip("Margem interna da tela usada para considerar um ponto de balanco visivel pela camera.")]
    [Range(0f, .4f)] [SerializeField] private float cameraViewportMargin = .03f;
    [Tooltip("Camadas que podem bloquear a linha de alcance ate o ponto.")]
    [SerializeField] private LayerMask obstructionMask = Physics.DefaultRaycastLayers;
    [Tooltip("Distancia final antes da ancora em que um collider pode ser tratado como a superficie onde o ponto foi colocado.")]
    [Min(0f)] [SerializeField] private float anchorSurfaceTolerance = .25f;

    [Header("Swing Motion Preset")]
    [Tooltip("Duracao de um ciclo completo do movimento configurado pela curva.")]
    [Min(.1f)] [SerializeField] private float swingCycleDuration = 2.5f;
    [Tooltip("Angulo maximo para cada lado em torno da posicao inicial do jogador, em graus.")]
    [Range(0f, 85f)] [SerializeField] private float swingAngleAmplitude = 35f;
    [Tooltip("Curva repetida a cada ciclo. O eixo X e o progresso de 0 a 1; o eixo Y vai de -1 a 1 e define o deslocamento entre os lados do arco.")]
    [SerializeField] private AnimationCurve swingMotionCurve = new(
        new Keyframe(0f, 0f, 6.283f, 6.283f),
        new Keyframe(.25f, 1f, 0f, 0f),
        new Keyframe(.5f, 0f, -6.283f, -6.283f),
        new Keyframe(.75f, -1f, 0f, 0f),
        new Keyframe(1f, 0f, 6.283f, 6.283f));
    [Tooltip("Impulso vertical extra somado ao pulo normal ao soltar o balanco com o input de pulo.")]
    [Min(0f)] [SerializeField] private float jumpReleaseUpwardSpeed = 2f;

    [Header("Presentation")]
    [Tooltip("Faz o corpo acompanhar gradualmente a direcao horizontal do balanco.")]
    [SerializeField] private bool faceSwingDirection = true;
    [Tooltip("Rapidez de rotacao do corpo enquanto balanca, em graus por segundo.")]
    [Min(0f)] [SerializeField] private float swingRotationSpeed = 540f;
    [Tooltip("Solta o jogador se o motor detectar colisao com parede, teto ou chao durante o arco.")]
    [SerializeField] private bool detachOnWorldCollision = true;
    [Tooltip("Line Renderer opcional no jogador para desenhar a corda entre a mao e o ponto do mapa.")]
    [SerializeField] private LineRenderer ropeRenderer;
    [Tooltip("Ponto local do jogador onde a corda visual se conecta, normalmente perto da mao.")]
    [SerializeField] private Vector3 ropeAttachOffset = new(0f, 1.5f, 0f);
    [Tooltip("Largura da corda visual em unidades do mundo.")]
    [Min(.001f)] [SerializeField] private float ropeWidth = .035f;
    [Tooltip("Cor aplicada a corda visual durante o balanco.")]
    [SerializeField] private Color ropeColor = Color.white;
    [Tooltip("Camera de gameplay usada para decidir se uma ancora esta visivel. Se vazio, usa Camera.main.")]
    [SerializeField] private Camera gameplayCamera;

    private PlayerMovement movement;
    private PlayerCharacterController characterController;
    private PlayerState playerState;
    private PlayerCombatStatus combatStatus;
    private PlayerAttackController attackController;
    private PlayerMovesetController movesetController;
    private PlayerGrappleAttackController grappleAttack;
    private PlayerTargetAssist targetAssist;
    private IGrappleTargetProvider anchorProvider;
    private IGrappleTargetProvider ladderProvider;
    private PlayerSwingAnchor currentAnchor;
    private Vector3 swingPlaneForward;
    private float ropeLength;
    private float swingAngle;
    private float swingBaseAngle;
    private float swingElapsed;
    private Vector3 lastSwingVelocity;
    private bool isSwinging;
    private PlayerInputBuffer inputBuffer;

    public bool IsSwinging => isSwinging;
    public PlayerSwingAnchor CurrentAnchor => currentAnchor;
    public bool SwingPressedThisFrame => SwingPressed();

    private void OnValidate()
    {
        swingCycleDuration = Mathf.Max(.1f, swingCycleDuration);
        swingAngleAmplitude = Mathf.Clamp(swingAngleAmplitude, 0f, 85f);
        ropeWidth = Mathf.Max(.001f, ropeWidth);
    }

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        characterController = movement.EntityCharacterController;
        playerState = GetComponent<PlayerState>();
        combatStatus = GetComponent<PlayerCombatStatus>();
        attackController = GetComponent<PlayerAttackController>();
        movesetController = GetComponent<PlayerMovesetController>();
        grappleAttack = GetComponent<PlayerGrappleAttackController>();
        inputBuffer = PlayerInputBuffer.GetOrAdd(gameObject);
        targetAssist = GetComponent<PlayerTargetAssist>();
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;
        if (ropeRenderer != null)
        {
            ropeRenderer.useWorldSpace = true;
            ropeRenderer.positionCount = 2;
            ropeRenderer.startWidth = ropeWidth;
            ropeRenderer.endWidth = ropeWidth;
            ropeRenderer.startColor = ropeColor;
            ropeRenderer.endColor = ropeColor;
            ropeRenderer.enabled = false;
        }
    }

    private void Update()
    {
        if (!isSwinging)
        {
            if (!CanReadInput())
                return;

            bool swingPressed = SwingPressed();
            bool bufferedGrapple = playerState != null &&
                playerState.IsNormal() && inputBuffer != null &&
                inputBuffer.TryConsume(PlayerBufferedInput.Grapple);
            if (!swingPressed && !bufferedGrapple)
                return;

            if (attackController != null && attackController.IsAttacking)
            {
                TryCancelAttackForGrapple();
                return;
            }

            if (playerState == null || !playerState.IsNormal())
                return;

            ExecuteGrappleInput();
            return;
        }

        if (playerState == null || !playerState.IsSwinging())
        {
            ClearSwingState();
            return;
        }

        if (GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked ||
            CombatImpactSystem.IsGameplayFrozen(this))
        {
            return;
        }

        if (SwingPressed())
        {
            ReleaseSwing(false);
            return;
        }

        if (releaseOnJump && GameInput.JumpPressed)
        {
            ReleaseSwing(true);
            return;
        }

        TickPendulum(Time.deltaTime);
    }

    private void TryCancelAttackForGrapple()
    {
        if (attackController == null || inputBuffer == null ||
            grappleAttack == null ||
            (!grappleAttack.CanStartGrappleAttack &&
             FindBestAnchor(allowAttacking: true) == null) ||
            !attackController.CanRequestCancel(
                PlayerAttackCancelAction.Grapple) ||
            !inputBuffer.TryQueueGrappleUntilAttackEnds())
        {
            return;
        }

        if (movesetController != null)
            movesetController.ResetSequence();
        if (!attackController.RequestCancel(PlayerAttackCancelAction.Grapple))
        {
            inputBuffer.ClearBufferedInput(PlayerBufferedInput.Grapple);
            return;
        }

        if (!attackController.IsAttacking && playerState != null &&
            playerState.IsNormal() &&
            inputBuffer.TryConsume(PlayerBufferedInput.Grapple))
        {
            ExecuteGrappleInput();
        }
    }

    private void ExecuteGrappleInput()
    {
        PlayerSwingAnchor availableAnchor = FindBestAnchor();
        if (availableAnchor != null)
        {
            TryBeginSwing(availableAnchor);
            return;
        }

        // Sem inimigo para o ataque de grapple, o gancho atraca na escada mirada.
        if (!HasEnemyTarget() &&
            TryFindLadderTarget(out HandLadder ladder, out HandLadderGrip grip, out _) &&
            movement.TryBeginLadderClimb(ladder, grip, GrappleSettings.Movement.pullSpeed))
            return;

        grappleAttack?.TryFire();
    }

    private bool HasEnemyTarget()
    {
        if (targetAssist == null)
            targetAssist = GetComponent<PlayerTargetAssist>();
        return targetAssist != null && targetAssist.FindCombatGrappleTarget() != null;
    }

    // Provedores do indicador global: pontos de balanco e escadas. Mesma ordem de
    // prioridade do input (balanco > inimigo > escada).
    private sealed class AnchorProvider : IGrappleTargetProvider
    {
        private readonly PlayerSwingController owner;
        public AnchorProvider(PlayerSwingController owner) => this.owner = owner;
        public int GrappleTargetPriority => 300;

        public bool TryGetGrappleTarget(out GrappleTarget target)
        {
            target = default;
            if (owner.playerState == null || !owner.playerState.IsNormal() || owner.isSwinging)
                return false;
            PlayerSwingAnchor anchor = owner.FindBestAnchor();
            if (anchor == null)
                return false;
            target = new GrappleTarget
            {
                kind = GrappleTargetKind.SwingAnchor,
                point = anchor.Position,
                source = anchor
            };
            return true;
        }
    }

    private sealed class LadderProvider : IGrappleTargetProvider
    {
        private readonly PlayerSwingController owner;
        public LadderProvider(PlayerSwingController owner) => this.owner = owner;
        public int GrappleTargetPriority => 100;

        public bool TryGetGrappleTarget(out GrappleTarget target)
        {
            target = default;
            if (owner.playerState == null || !owner.playerState.IsNormal() ||
                owner.isSwinging || owner.HasEnemyTarget() ||
                !owner.TryFindLadderTarget(out HandLadder ladder, out _, out Vector3 point))
                return false;
            target = new GrappleTarget
            {
                kind = GrappleTargetKind.Ladder,
                point = point,
                source = ladder
            };
            return true;
        }
    }

    // Degrau mais proximo da mira: raio do centro do jogador na direcao da camera.
    private bool TryFindLadderTarget(out HandLadder best, out HandLadderGrip bestGrip,
        out Vector3 bestPoint)
    {
        best = null;
        bestGrip = default;
        bestPoint = default;
        if (movement == null || !movement.CanBeginLadderClimb ||
            HandLadder.ActiveLadders.Count == 0)
            return false;

        if (gameplayCamera == null)
            gameplayCamera = Camera.main;
        Vector3 aim = gameplayCamera != null
            ? gameplayCamera.transform.forward : transform.forward;
        Vector3 center = GetPlayerCenter();
        float bestAngle = float.MaxValue;
        IReadOnlyList<HandLadder> ladders = HandLadder.ActiveLadders;
        for (int i = 0; i < ladders.Count; i++)
        {
            HandLadder ladder = ladders[i];
            if (ladder == null || !ladder.isActiveAndEnabled ||
                !ladder.TryGetGrappleTarget(center, aim, center,
                    out HandLadderGrip grip, out Vector3 point, out float angle) ||
                angle >= bestAngle || IsBlockedToPoint(center, point, ladder))
                continue;

            best = ladder;
            bestGrip = grip;
            bestPoint = point;
            bestAngle = angle;
        }

        return best != null;
    }

    private bool IsBlockedToPoint(Vector3 from, Vector3 point, HandLadder ladder)
    {
        Vector3 toPoint = point - from;
        float distance = toPoint.magnitude;
        if (distance < .01f || !Physics.Raycast(from, toPoint / distance,
            out RaycastHit hit, distance, obstructionMask, QueryTriggerInteraction.Ignore))
            return false;
        return hit.collider != null &&
            hit.collider.GetComponentInParent<HandLadder>() != ladder &&
            hit.distance < distance - anchorSurfaceTolerance;
    }

    private void OnEnable()
    {
        anchorProvider ??= new AnchorProvider(this);
        ladderProvider ??= new LadderProvider(this);
        GrappleTargetSystem.Register(anchorProvider);
        GrappleTargetSystem.Register(ladderProvider);
    }

    private void OnDisable()
    {
        GrappleTargetSystem.Unregister(anchorProvider);
        GrappleTargetSystem.Unregister(ladderProvider);
        if (isSwinging && movement != null && playerState != null &&
            playerState.IsSwinging())
        {
            movement.EndSwingMotion(lastSwingVelocity);
        }
        ClearSwingState();
    }

    private bool CanReadInput() => !GamePauseController.IsPaused &&
        !CutsceneController.IsPlayerControlLocked &&
        !CombatImpactSystem.IsGameplayFrozen(this);

    private bool SwingPressed()
    {
        return GameInput.SwingPressed;
    }

    private PlayerSwingAnchor FindBestAnchor(bool allowAttacking = false)
    {
        bool validPlayerState = playerState != null &&
            (playerState.IsNormal() ||
             (allowAttacking && playerState.IsAttacking()));
        if (movement == null || playerState == null ||
            !validPlayerState || characterController == null ||
            !characterController.enabled)
        {
            return null;
        }

        bool allowOffscreenAnchors = combatStatus != null &&
            combatStatus.IsOutOfCombat;
        if (!allowOffscreenAnchors && gameplayCamera == null)
            gameplayCamera = Camera.main;
        if (!allowOffscreenAnchors && gameplayCamera == null)
            return null;

        Vector3 playerCenter = GetPlayerCenter();
        Vector3 playerForward = Vector3.ProjectOnPlane(transform.forward,
            Vector3.up).normalized;
        PlayerSwingAnchor bestAnchor = null;
        float bestDistance = float.PositiveInfinity;
        float bestFacingAngle = float.PositiveInfinity;
        float bestCameraAngle = float.PositiveInfinity;
        const float cameraAngleTieTolerance = .5f;
        const float distanceTieTolerance = .05f;

        foreach (PlayerSwingAnchor anchor in PlayerSwingAnchor.ActiveAnchors)
        {
            if (anchor == null || !anchor.CanAttach)
                continue;

            Vector3 toAnchor = anchor.Position - playerCenter;
            float distance = toAnchor.magnitude;
            if (distance > anchor.InteractionRange ||
                anchor.Position.y - playerCenter.y < minimumAnchorHeight ||
                !IsWithinAnchorAimAngle(anchor) ||
                IsBlocked(playerCenter, anchor) ||
                (!allowOffscreenAnchors &&
                    !IsVisibleToCamera(anchor, gameplayCamera)))
            {
                continue;
            }

            Vector3 horizontalDirection = Vector3.ProjectOnPlane(toAnchor,
                Vector3.up);
            float facingAngle = horizontalDirection.sqrMagnitude > .0001f
                ? Vector3.Angle(playerForward, horizontalDirection)
                : 180f;
            float cameraAngle = facingAngle;
            if (gameplayCamera != null)
            {
                Vector3 cameraDirection = anchor.Position -
                    gameplayCamera.transform.position;
                cameraAngle = cameraDirection.sqrMagnitude > .0001f
                    ? Vector3.Angle(gameplayCamera.transform.forward,
                        cameraDirection)
                    : 180f;
            }

            bool isBetterCameraAlignment = cameraAngle <
                bestCameraAngle - cameraAngleTieTolerance;
            bool isCameraAlignmentTie = Mathf.Abs(cameraAngle -
                bestCameraAngle) <= cameraAngleTieTolerance;
            bool isCloser = distance < bestDistance -
                distanceTieTolerance;
            bool isDistanceTie = Mathf.Abs(distance - bestDistance) <=
                distanceTieTolerance;
            bool isBetterBodyAlignment = facingAngle < bestFacingAngle;
            if (!isBetterCameraAlignment &&
                !(isCameraAlignmentTie && (isCloser ||
                    (isDistanceTie && isBetterBodyAlignment))))
            {
                continue;
            }

            bestAnchor = anchor;
            bestDistance = distance;
            bestFacingAngle = facingAngle;
            bestCameraAngle = cameraAngle;
        }

        return bestAnchor;
    }

    /// <summary>Inicia o balanco em um ponto previamente selecionado pelo roteador de grapple.</summary>
    public bool TryBeginSwing(PlayerSwingAnchor anchor)
    {
        if (anchor == null || !anchor.CanAttach || movement == null ||
            playerState == null || !playerState.IsNormal())
        {
            return false;
        }

        Vector3 playerCenter = GetPlayerCenter();
        float distance = Vector3.Distance(playerCenter, anchor.Position);
        if (distance > anchor.InteractionRange ||
            anchor.Position.y - playerCenter.y < minimumAnchorHeight ||
            IsBlocked(playerCenter, anchor))
        {
            return false;
        }

        BeginSwing(anchor, playerCenter);
        return isSwinging;
    }

    // Assistencia de mira do gancho de movimento (GrappleSettings.Movement).
    private bool IsWithinAnchorAimAngle(PlayerSwingAnchor anchor)
    {
        float limit = GrappleSettings.Movement.anchorAimAngle;
        if (limit >= 179f)
            return true;
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;
        if (gameplayCamera == null)
            return true;
        Vector3 toAnchor = anchor.Position - GetPlayerCenter();
        return toAnchor.sqrMagnitude < .0001f ||
            Vector3.Angle(gameplayCamera.transform.forward, toAnchor) <= limit;
    }

    private bool IsVisibleToCamera(PlayerSwingAnchor anchor, Camera camera)
    {
        Vector3 viewport = camera.WorldToViewportPoint(anchor.Position);
        if (viewport.z <= 0f || viewport.x < cameraViewportMargin ||
            viewport.x > 1f - cameraViewportMargin ||
            viewport.y < cameraViewportMargin ||
            viewport.y > 1f - cameraViewportMargin)
        {
            return false;
        }

        Vector3 toAnchor = anchor.Position - camera.transform.position;
        if (!Physics.Raycast(camera.transform.position, toAnchor.normalized,
            out RaycastHit hit, toAnchor.magnitude, obstructionMask,
            QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        if (hit.collider == null ||
            hit.collider.GetComponentInParent<PlayerSwingAnchor>() == anchor)
        {
            return true;
        }

        return hit.distance >= toAnchor.magnitude - anchorSurfaceTolerance;
    }

    private bool IsBlocked(Vector3 playerCenter, PlayerSwingAnchor anchor)
    {
        Vector3 toAnchor = anchor.Position - playerCenter;
        if (!Physics.Raycast(playerCenter, toAnchor.normalized,
            out RaycastHit hit, toAnchor.magnitude, obstructionMask,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (hit.collider == null ||
            hit.collider.GetComponentInParent<PlayerSwingAnchor>() == anchor)
        {
            return false;
        }

        return hit.distance < toAnchor.magnitude - anchorSurfaceTolerance;
    }

    private void BeginSwing(PlayerSwingAnchor anchor, Vector3 playerCenter)
    {
        if (!movement.BeginSwingMotion())
            return;

        currentAnchor = anchor;
        swingPlaneForward = anchor.SwingForward;
        ropeLength = anchor.RopeLength;
        Vector3 relative = playerCenter - anchor.Position;
        swingBaseAngle = Mathf.Atan2(Vector3.Dot(relative, swingPlaneForward),
            -relative.y);
        swingAngle = swingBaseAngle;
        swingElapsed = 0f;
        lastSwingVelocity = Vector3.zero;

        Vector3 targetCenter = GetPendulumPosition();
        movement.MoveDuringSwing(targetCenter - playerCenter);
        isSwinging = true;
        UpdateRopeVisual();
    }

    private void TickPendulum(float deltaTime)
    {
        if (currentAnchor == null || deltaTime <= 0f)
        {
            ReleaseSwing(false);
            return;
        }

        swingElapsed += deltaTime;
        float cycleProgress = Mathf.Repeat(
            swingElapsed / swingCycleDuration, 1f);
        float curveValue = swingMotionCurve != null &&
            swingMotionCurve.length > 0
            ? Mathf.Clamp(swingMotionCurve.Evaluate(cycleProgress), -1f, 1f)
            : 0f;
        swingAngle = swingBaseAngle +
            curveValue * swingAngleAmplitude * Mathf.Deg2Rad;

        Vector3 targetCenter = GetPendulumPosition();
        Vector3 displacement = targetCenter - GetPlayerCenter();
        lastSwingVelocity = displacement / deltaTime;
        movement.MoveDuringSwing(displacement);

        if (faceSwingDirection)
        {
            Vector3 facing = displacement;
            facing.y = 0f;
            if (facing.sqrMagnitude > .0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(facing);
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    targetRotation, swingRotationSpeed * deltaTime);
            }
        }
        UpdateRopeVisual();
    }

    internal void OnMovementCommitted(CollisionFlags collision)
    {
        if (isSwinging && detachOnWorldCollision &&
            collision != CollisionFlags.None)
            ReleaseSwing(false);
    }

    private Vector3 GetPendulumPosition()
    {
        float horizontal = Mathf.Sin(swingAngle) * ropeLength;
        float vertical = Mathf.Cos(swingAngle) * ropeLength;
        return currentAnchor.Position + swingPlaneForward * horizontal -
            Vector3.up * vertical;
    }

    private Vector3 GetPlayerCenter()
    {
        Vector3 centerOffset = characterController != null
            ? characterController.center
            : Vector3.zero;
        return transform.position + transform.rotation * centerOffset;
    }

    private void UpdateRopeVisual()
    {
        if (ropeRenderer == null || currentAnchor == null)
            return;

        ropeRenderer.startWidth = ropeWidth;
        ropeRenderer.endWidth = ropeWidth;
        ropeRenderer.startColor = ropeColor;
        ropeRenderer.endColor = ropeColor;
        ropeRenderer.SetPosition(0, currentAnchor.Position);
        ropeRenderer.SetPosition(1, transform.TransformPoint(ropeAttachOffset));
        ropeRenderer.enabled = true;
    }

    private void ReleaseSwing(bool releasedByJump)
    {
        if (!isSwinging)
            return;

        isSwinging = false;
        currentAnchor = null;
        swingElapsed = 0f;
        if (ropeRenderer != null)
            ropeRenderer.enabled = false;
        movement.EndSwingMotion(lastSwingVelocity, releasedByJump,
            jumpReleaseUpwardSpeed);
    }

    private void ClearSwingState()
    {
        isSwinging = false;
        currentAnchor = null;
        swingElapsed = 0f;
        if (ropeRenderer != null)
            ropeRenderer.enabled = false;
    }

    private void OnDrawGizmos()
    {
        if (!DebugGizmoSettings.Enabled(DebugGizmoGroup.Swing) || !isSwinging ||
            currentAnchor == null)
        {
            return;
        }

        Gizmos.color = new Color(1f, .7f, .1f, .9f);
        Gizmos.DrawLine(currentAnchor.Position, GetPlayerCenter());
        Gizmos.DrawWireSphere(GetPendulumPosition(), .12f);
    }
}
