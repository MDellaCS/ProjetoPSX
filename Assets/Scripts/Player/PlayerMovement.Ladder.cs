using UnityEngine;

/// <summary>
/// Escalada de escadas de mao. O estado e o motor ficam no PlayerMovement; a
/// geometria (zona de entrada, saidas, normal) vem da <see cref="HandLadder"/>.
/// Controles: Cima/Baixo sobem e descem; Pular salta na direcao da normal da
/// escada; Agachar solta; sair pelas pontas leva as saidas calculadas. Da agua,
/// pode agarrar e subir para fora; ao descer e entrar em agua funda, solta e
/// retorna ao movimento de natacao.
/// </summary>
public partial class PlayerMovement
{
    private HandLadder activeLadder;
    private HandLadderGrip ladderGrip;
    private int ladderMountFrame = -1;
    // Subida em degraus: o jogador so para nos vaos entre degraus.
    private int ladderSlot;
    private float ladderStepFrom;
    private float ladderStepTo;
    private float ladderStepElapsed;
    private float ladderStepDuration;
    // Movimento pre-feito (encaixe na escada ou saida): vai de A a B em um tempo
    // fixo, a partir de onde o jogador estiver (parado, no ar ou em alta velocidade).
    private bool ladderMoveActive;
    private bool ladderMoveIsExit;
    private float ladderMoveVerticalLead;
    private Vector3 ladderMoveFrom;
    private Vector3 ladderMoveTo;
    private Quaternion ladderMoveFromRotation;
    private Quaternion ladderMoveToRotation;
    private float ladderMoveElapsed;
    private float ladderMoveDuration;

    /// <summary>Verdadeiro enquanto agarrado a uma escada.</summary>
    public bool IsOnLadder => activeLadder != null && playerState != null &&
        playerState.IsClimbingLadder();

    /// <summary>O jogador esta em condicoes de agarrar uma escada agora.</summary>
    public bool CanBeginLadderClimb => controller != null && playerState != null &&
        playerState.IsNormal() && !isCrouching &&
        !playerState.IsClimbingLadder() && !playerState.IsAttacking() &&
        !playerState.IsDashing() && !playerState.IsSliding();

    public static bool ShouldReleaseLadderInDeepWater(bool isOverhead,
        bool isInDeepWater, float verticalInput, bool descendingStepInProgress) =>
        !isOverhead && isInDeepWater &&
        (verticalInput < -.05f || descendingStepInProgress);

    public bool TryBeginLadderClimb(HandLadder ladder, HandLadderGrip grip,
        float pullSpeed = 0f)
    {
        if (ladder == null || !ladder.isActiveAndEnabled || !ladder.IsClimbable ||
            !CanBeginLadderClimb)
            return false;

        activeLadder = ladder;
        ladder.SetPlayerClimbing(true);
        ladderGrip = grip;
        // O Interagir que agarrou nao pode soltar/saltar no mesmo frame.
        ladderMountFrame = Time.frameCount;
        playerState.ChangeState(PlayerState.State.ClimbingLadder);
        ClearTraversalInputBuffer();
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        verticalDisplacementThisFrame = 0f;
        ladderSlot = ladder.NearestSlot(grip.progress);
        if (ladder.RungSpacing > 0f)
            ladderGrip.progress = ladder.SlotHeight(ladderSlot);
        ladderStepElapsed = ladderStepDuration = 0f;
        // Encaixe: o jogador e levado ao trilho por um movimento pre-feito, de
        // qualquer posicao ou velocidade em que agarrou.
        Vector3 target = ladder.GetGripPosition(ladderGrip);
        // Gancho (pullSpeed > 0): o jogador e puxado de longe, mais rapido e mais longo.
        float distance = Vector3.Distance(transform.position, target);
        float duration = pullSpeed > 0f
            ? Mathf.Clamp(distance / pullSpeed, ladder.MountMinDuration, 1.2f)
            : Mathf.Clamp(distance / ladder.MountSpeed, ladder.MountMinDuration, .45f);
        BeginLadderMove(target, ladder.GetGripFacing(ladderGrip.side, ladderGrip.progress), duration, 0f, false);
        return true;
    }

    private bool HandleLadderClimb()
    {
        HandLadder ladder = activeLadder;
        if (ladder == null || !ladder.isActiveAndEnabled)
        {
            FinishLadderClimb();
            return true;
        }

        if (Time.frameCount != ladderMountFrame && !ladderMoveIsExit)
        {
            if (GameInput.JumpPressed)
            {
                JumpOffLadder(ladder);
                return true;
            }

            if (GameInput.CrouchPressed)
            {
                DropFromLadder(ladder);
                return true;
            }
        }

        if (inputBuffer != null)
            inputBuffer.ClearBufferedInputs();

        if (ladderMoveActive)
        {
            AdvanceLadderMove();
            return true;
        }

        Vector2 move = GameInput.Move;
        if (ladder.IsOverhead)
            move = ReadOverheadInput(ladder);

        bool descendingStepInProgress = ladder.RungSpacing > 0f &&
            ladderStepElapsed < ladderStepDuration &&
            ladderStepTo < ladderStepFrom;
        if (ShouldReleaseLadderInDeepWater(ladder.IsOverhead,
                waterSensor != null && waterSensor.IsSwimming, move.y,
                descendingStepInProgress))
        {
            DropFromLadder(ladder);
            return true;
        }

        float deltaTime = Time.deltaTime;
        if (ladder.RungSpacing > 0f)
        {
            if (!UpdateSteppedClimb(ladder, move.y, deltaTime))
                return true;
        }
        else
        {
            bool reachedTop = move.y > .05f && ladderGrip.progress >= ladder.MaxProgress - .001f;
            bool reachedBottom = move.y < -.05f && ladderGrip.progress <= .001f;
            if (reachedTop || reachedBottom)
            {
                ExitLadder(ladder, reachedTop);
                return true;
            }

            ladderGrip.progress = Mathf.Clamp(
                ladderGrip.progress + move.y * ladder.ClimbSpeed * deltaTime, 0f, ladder.MaxProgress);
        }
        if (ladder.LateralClimbSpeed > 0f)
        {
            // Virado para a escada, a direita do jogador e -side * largura.
            float limit = ladder.LateralLimit;
            ladderGrip.lateral = Mathf.Clamp(
                ladderGrip.lateral - ladderGrip.side * move.x * ladder.LateralClimbSpeed * deltaTime,
                -limit, limit);
        }

        // O jogador e puxado ao trilho; na subida normal o alvo anda mais devagar
        // que a velocidade de encaixe, entao ele fica sempre colado.
        Vector3 target = ladder.GetGripPosition(ladderGrip);
        QueueLadderPosition(
            Vector3.MoveTowards(transform.position, target, ladder.MountSpeed * deltaTime),
            ladder.GetGripFacing(ladderGrip.side, ladderGrip.progress));
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        verticalDisplacementThisFrame = 0f;
        return true;
    }

    // Escada de teto: o jogador anda ao longo das barras para onde apontar o
    // movimento (relativo a camera) e fica virado para esse lado.
    private Vector2 ReadOverheadInput(HandLadder ladder)
    {
        float along = Vector3.Dot(DesiredDirection, ladder.ClimbDirection);
        if (Mathf.Abs(along) > .05f)
            ladderGrip.side = along > 0f ? 1 : -1;
        return new Vector2(0f, Mathf.Abs(along) > .05f ? along : 0f);
    }

    // Degraus: cada impulso leva o jogador ao vao seguinte, com aceleracao e
    // desaceleracao (nao linear). Segurar a direcao encadeia os degraus. Retorna
    // falso se saiu da escada neste frame.
    private bool UpdateSteppedClimb(HandLadder ladder, float input, float deltaTime)
    {
        if (ladderStepElapsed >= ladderStepDuration)
        {
            int direction = input > .05f ? 1 : input < -.05f ? -1 : 0;
            if ((direction > 0 && ladderSlot >= ladder.SlotCount - 1) ||
                (direction < 0 && ladderSlot <= 0))
            {
                ExitLadder(ladder, direction > 0);
                return false;
            }

            if (direction != 0)
            {
                ladderStepFrom = ladderGrip.progress;
                ladderSlot += direction;
                ladderStepTo = ladder.SlotHeight(ladderSlot);
                ladderStepDuration = Mathf.Max(.05f,
                    Mathf.Abs(ladderStepTo - ladderStepFrom) / ladder.ClimbSpeed);
                ladderStepElapsed = 0f;
            }
        }

        if (ladderStepElapsed < ladderStepDuration)
        {
            ladderStepElapsed = Mathf.Min(ladderStepDuration, ladderStepElapsed + deltaTime);
            float t = ladderStepElapsed / ladderStepDuration;
            float eased = Mathf.Lerp(t, Mathf.SmoothStep(0f, 1f, t), .7f);
            ladderGrip.progress = Mathf.Lerp(ladderStepFrom, ladderStepTo, eased);
        }

        return true;
    }

    // Pular: salta para longe, na direcao da normal da escada.
    private void JumpOffLadder(HandLadder ladder)
    {
        // Escada de teto: a normal e para baixo, entao o salto vai para a frente.
        Vector3 direction = ladder.IsOverhead
            ? Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized
            : ladder.GetJumpDirection(ladderGrip.side, ladderGrip.progress);
        FinishLadderClimb();
        transform.rotation = Quaternion.LookRotation(direction);
        horizontalVelocity = direction * ladder.JumpOffSpeed;
        BeginJumpAscent(GetJumpHeight() * ladder.JumpOffHeightMultiplier);
        isStandardJump = true;
        isDashJump = false;
        isLaunched = false;
        jumpsUsed = 1;
        wasAirborne = true;
    }

    // Agachar: solta a escada e cai, com um leve empurrao para fora dela.
    private void DropFromLadder(HandLadder ladder)
    {
        if (ladder.IsOverhead)
        {
            // Escada de teto: so solta e cai, mantendo a direcao.
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            FinishLadderClimb();
            if (forward.sqrMagnitude > .0001f)
                transform.rotation = Quaternion.LookRotation(forward);
            return;
        }

        Vector3 direction = ladder.GetJumpDirection(ladderGrip.side, ladderGrip.progress);
        FinishLadderClimb();
        transform.rotation = Quaternion.LookRotation(-direction);
        horizontalVelocity = direction * ladder.DropAwaySpeed;
    }

    // Saida pre-feita: no topo sobe primeiro e passa por cima da borda; na base
    // afasta-se primeiro e desce. O controle so volta ao chegar.
    private void ExitLadder(HandLadder ladder, bool top)
    {
        // Nas pontas da escada de teto o jogador simplesmente se solta.
        if (ladder.IsOverhead)
        {
            DropFromLadder(ladder);
            return;
        }

        BeginLadderMove(ladder.GetExitPosition(ladderGrip, top),
            ladder.GetExitFacing(ladderGrip, top), ladder.ExitDuration,
            top ? 1f : -1f, true);
    }

    private void BeginLadderMove(Vector3 target, Quaternion facing, float duration,
        float verticalLead, bool isExit)
    {
        ladderMoveActive = true;
        ladderMoveIsExit = isExit;
        ladderMoveVerticalLead = verticalLead;
        ladderMoveFrom = transform.position;
        ladderMoveTo = target;
        ladderMoveFromRotation = transform.rotation;
        ladderMoveToRotation = facing;
        ladderMoveElapsed = 0f;
        ladderMoveDuration = Mathf.Max(.05f, duration);
    }

    private void AdvanceLadderMove()
    {
        ladderMoveElapsed = Mathf.Min(ladderMoveDuration, ladderMoveElapsed + Time.deltaTime);
        float t = ladderMoveElapsed / ladderMoveDuration;
        // Lead > 0: sobe antes de avancar; < 0: avanca antes de descer.
        float vertical = t;
        float horizontal = t;
        if (ladderMoveVerticalLead > 0f)
        {
            vertical = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / .6f));
            horizontal = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - .4f) / .6f));
        }
        else if (ladderMoveVerticalLead < 0f)
        {
            horizontal = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / .6f));
            vertical = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - .4f) / .6f));
        }
        else
        {
            vertical = horizontal = Mathf.SmoothStep(0f, 1f, t);
        }

        Vector3 position = new(
            Mathf.Lerp(ladderMoveFrom.x, ladderMoveTo.x, horizontal),
            Mathf.Lerp(ladderMoveFrom.y, ladderMoveTo.y, vertical),
            Mathf.Lerp(ladderMoveFrom.z, ladderMoveTo.z, horizontal));
        QueueLadderPosition(position, Quaternion.Slerp(ladderMoveFromRotation,
            ladderMoveToRotation, Mathf.SmoothStep(0f, 1f, t)));
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        verticalDisplacementThisFrame = 0f;
        if (ladderMoveElapsed < ladderMoveDuration)
            return;

        bool isExit = ladderMoveIsExit;
        ladderMoveActive = false;
        ladderMoveIsExit = false;
        if (isExit)
            FinishLadderClimb();
    }

    private void QueueLadderPosition(Vector3 position, Quaternion facing)
    {
        transform.rotation = facing;
        pendingExternalDisplacement += position - transform.position;
        hasPendingMovementCommit = true;
    }

    private void FinishLadderClimb()
    {
        ResetLadderState();
        ladderStepElapsed = ladderStepDuration = 0f;
        if (playerState != null && playerState.IsClimbingLadder())
            playerState.ChangeState(PlayerState.State.Normal);
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        verticalDisplacementThisFrame = 0f;
        ClearTraversalInputBuffer();
    }

    private void ResetLadderState()
    {
        if (activeLadder != null)
            activeLadder.SetPlayerClimbing(false);
        activeLadder = null;
        ladderGrip = default;
        ladderMoveActive = false;
        ladderMoveIsExit = false;
    }
}
