using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerLockOn : MonoBehaviour
{
    [Tooltip("Distancia maxima para adquirir um inimigo visivel, em metros.")]
    [Min(0f)] [SerializeField] private float acquisitionRange = 20f;
    [Tooltip("Distancia de perda do alvo, maior que a distancia de aquisicao, em metros.")]
    [Min(0f)] [SerializeField] private float lossRange = 25f;
    [Tooltip("Deslocamento minimo do analogico direito, em qualquer direcao, para trocar de alvo.")]
    [Range(0f, 1f)] [SerializeField] private float stickDeadZone = .6f;
    [Tooltip("Intervalo minimo entre trocas de alvo, em segundos.")]
    [Min(0f)] [SerializeField] private float switchInterval = .3f;
    [Tooltip("Distancia acumulada do mouse para trocar de alvo, em pixels, em qualquer direcao.")]
    [Min(1f)] [SerializeField] private float mouseThreshold = 50f;
    [Tooltip("Tempo, em segundos, para verificar o progresso do mouse na direcao inicial. Se o progresso for insuficiente, o acumulado volta a zero.")]
    [Min(.01f)] [SerializeField] private float mouseProgressInterval = .3f;
    [Tooltip("Progresso minimo em pixels na direcao inicial durante cada intervalo. Zero desativa a exigencia de progresso.")]
    [Min(0f)] [SerializeField] private float mouseMinimumProgress = 5f;
    [Tooltip("Atraso antes de procurar outro alvo apos uma morte, em segundos.")]
    [Min(0f)] [SerializeField] private float deathDelay = .2f;
    [Tooltip("Velocidade maxima de giro para o alvo durante a preparacao do ataque, em graus por segundo.")]
    [Min(0f)] [SerializeField] private float magnetismSpeed = 540f;
    [Tooltip("Angulo maximo usado na consulta de magnetismo por direcao. Ataques com lock-on ignoram esse limite.")]
    [Range(0f, 180f)] [SerializeField] private float inputAngleLimit = 90f;

    [Tooltip("Quanto a camera desloca horizontalmente o enquadramento do jogador em direcao ao alvo travado: 0 mantem o foco no jogador, 0,5 fica no meio e 1 acompanha a posicao horizontal do alvo.")]
    [Range(0f, 1f)] [SerializeField] private float cameraTargetWeight = .6f;

    [Header("Sons do Lock-on")]
    [Tooltip("Som reproduzido apenas quando o lock-on e iniciado e encontra um alvo.")]
    [SerializeField] private AudioClip lockOnStartSound;
    [Tooltip("Volume do som de inicio do lock-on, antes do volume global de efeitos sonoros.")]
    [Range(0f, 1f)] [SerializeField] private float lockOnStartVolume = 1f;
    [Tooltip("Som reproduzido quando o lock-on termina, inclusive se nao encontrar outro alvo apos uma morte.")]
    [SerializeField] private AudioClip lockOnEndSound;
    [Tooltip("Volume do som de fim do lock-on, antes do volume global de efeitos sonoros.")]
    [Range(0f, 1f)] [SerializeField] private float lockOnEndVolume = 1f;
    [Tooltip("Som reproduzido ao trocar de alvo com o mouse ou analogico direito.")]
    [SerializeField] private AudioClip lockOnSwitchSound;
    [Tooltip("Volume do som de troca de alvo, antes do volume global de efeitos sonoros.")]
    [Range(0f, 1f)] [SerializeField] private float lockOnSwitchVolume = 1f;

    private Camera view;
    private float nextSwitch, deathTimer;
    private Vector2 accumulatedMouse, mouseStartDirection;
    private float mouseProgressCheckpoint, mouseProgressDeadline;
    private bool waitingForDeath, stickReleased = true;
    private bool isQuitting;
    private bool soundLockActive;
    private EnemyBase target;
    private Vector2 debugStick;
    public EnemyBase Target
    {
        get => target;
        private set
        {
            if (ReferenceEquals(target, value)) return;
            target = value;
            if (target != null && !soundLockActive)
            {
                soundLockActive = true;
                if (!isQuitting) SfxAudio.Play(lockOnStartSound, lockOnStartVolume);
            }
            else if (ReferenceEquals(target, null) && !waitingForDeath)
                FinishLockSound();
        }
    }
    public bool IsActive => Target != null || waitingForDeath || soundLockActive;
    public float MagnetismSpeed => magnetismSpeed;
    public float CameraTargetWeight => Mathf.Clamp01(cameraTargetWeight);
    internal Vector2 MouseSwitchProgress => accumulatedMouse;
    internal float MouseSwitchThreshold => Mathf.Max(1f, mouseThreshold);
    internal float MouseProgressTimeRemaining => accumulatedMouse.sqrMagnitude > 0f
        ? Mathf.Max(0f, mouseProgressDeadline - Time.time) : 0f;
    internal Vector2 StickSwitchInput => debugStick;
    internal float StickSwitchDeadZone => Mathf.Clamp01(stickDeadZone);
    internal bool SwitchOnCooldown => Time.time < nextSwitch;

    private void OnValidate()
    {
        lossRange = Mathf.Max(acquisitionRange + .01f, lossRange);
        mouseProgressInterval = Mathf.Max(.01f, mouseProgressInterval);
        mouseMinimumProgress = Mathf.Max(0f, mouseMinimumProgress);
    }
    private void OnEnable() { EnemyBase.EnemyDied += HandleDeath; }
    private void OnApplicationQuit() => isQuitting = true;
    private void OnDisable()
    {
        EnemyBase.EnemyDied -= HandleDeath;
        waitingForDeath = false;
        ResetMouseProgress();
        debugStick = Vector2.zero;
        Target = null;
        FinishLockSound();
    }
    private void FinishLockSound()
    {
        if (!soundLockActive) return;
        soundLockActive = false;
        if (!isQuitting) SfxAudio.Play(lockOnEndSound, lockOnEndVolume);
    }
    private void HandleDeath(EnemyBase enemy)
    {
        if (enemy != Target) return;
        ResetMouseProgress();
        debugStick = Vector2.zero;
        waitingForDeath = true; Target = null; deathTimer = deathDelay;
    }

    private void Update()
    {
        if (GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked) return;
        if (view == null || !view.isActiveAndEnabled) view = Camera.main;
        if (GameInput.LockOnPressed)
        {
            if (IsActive) { waitingForDeath = false; Target = null; FinishLockSound(); }
            else Target = FindBest();
            ResetMouseProgress();
            debugStick = Vector2.zero;
            nextSwitch = 0f;
            stickReleased = true;
            return;
        }
        if (waitingForDeath)
        {
            deathTimer -= Time.deltaTime;
            if (deathTimer <= 0f)
            {
                waitingForDeath = false;
                Target = FindBest();
                if (Target == null) FinishLockSound();
            }
            return;
        }
        if (Target == null)
        {
            ResetMouseProgress();
            debugStick = Vector2.zero;
            // Um objeto destruido compara igual a null na Unity, mas ainda
            // precisa finalizar o alvo para reproduzir o som uma unica vez.
            if (!ReferenceEquals(target, null)) Target = null;
            return;
        }
        if (Target.IsDead)
        {
            ResetMouseProgress();
            waitingForDeath = true; Target = null; deathTimer = deathDelay; return;
        }
        if ((!Target.CanBeCombatTarget || Target.IsSpawning) || !PlayerLockOnSelection.WithinLossRange((Target.LockOnAimPoint - transform.position).sqrMagnitude, acquisitionRange, lossRange))
        { ResetMouseProgress(); debugStick = Vector2.zero; Target = null; return; }
        CheckMouseProgress();
        Vector2 look = GameInput.Look;
        if (GameInput.LookIsMouseDelta)
        {
            debugStick = Vector2.zero;
            if (Time.time < nextSwitch) { ResetMouseProgress(); return; }
            AccumulateMouse(look);
            float threshold = MouseSwitchThreshold;
            if (accumulatedMouse.sqrMagnitude >= threshold * threshold)
            {
                Switch(accumulatedMouse);
                ResetMouseProgress();
                nextSwitch = Time.time + switchInterval;
            }
        }
        else
        {
            debugStick = look;
            if (look.sqrMagnitude > .0001f) ResetMouseProgress();
            float deadZone = Mathf.Clamp01(stickDeadZone);
            if (look.magnitude < Mathf.Max(.01f, deadZone * .5f)) stickReleased = true;
            if (stickReleased && look.sqrMagnitude >= deadZone * deadZone &&
                look.sqrMagnitude > .0001f && Time.time >= nextSwitch)
            {
                Switch(look);
                stickReleased = false;
                nextSwitch = Time.time + switchInterval;
            }
        }
    }

    private void ResetMouseProgress()
    {
        accumulatedMouse = Vector2.zero;
        mouseStartDirection = Vector2.zero;
        mouseProgressCheckpoint = 0f;
        mouseProgressDeadline = 0f;
    }

    private void CheckMouseProgress()
    {
        if (accumulatedMouse.sqrMagnitude <= 0f || Time.time < mouseProgressDeadline)
            return;
        float progress = Vector2.Dot(accumulatedMouse, mouseStartDirection);
        if (PlayerLockOnSelection.ShouldResetMouseProgress(progress,
            mouseProgressCheckpoint, mouseMinimumProgress))
        {
            ResetMouseProgress();
            return;
        }
        mouseProgressCheckpoint = progress;
        mouseProgressDeadline = Time.time + Mathf.Max(.01f, mouseProgressInterval);
    }

    private void AccumulateMouse(Vector2 delta)
    {
        if (delta.sqrMagnitude <= .0001f) return;
        if (accumulatedMouse.sqrMagnitude <= 0f)
        {
            mouseStartDirection = delta.normalized;
            mouseProgressCheckpoint = 0f;
            mouseProgressDeadline = Time.time + Mathf.Max(.01f, mouseProgressInterval);
        }
        accumulatedMouse += delta;
        if (Vector2.Dot(accumulatedMouse, mouseStartDirection) <= 0f)
            ResetMouseProgress();
    }

    private bool Candidate(EnemyBase enemy, out Vector3 viewport, out float distance)
    {
        viewport = default; distance = 0f;
        if (view == null || enemy == null || !enemy.CanBeCombatTarget || enemy.IsSpawning) return false;
        distance = (enemy.LockOnAimPoint - transform.position).sqrMagnitude;
        if (distance > acquisitionRange * acquisitionRange) return false;
        viewport = view.WorldToViewportPoint(enemy.LockOnAimPoint);
        return viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
    }
    private EnemyBase FindBest()
    {
        EnemyBase best = null; float score = float.PositiveInfinity, bestDistance = float.PositiveInfinity;
        foreach (EnemyBase enemy in EnemyBase.ActiveEnemies)
        {
            if (!Candidate(enemy, out Vector3 p, out float distance)) continue;
            float s = (p.x - .5f) * (p.x - .5f) + (p.y - .5f) * (p.y - .5f);
            if (PlayerLockOnSelection.IsBetter(s, distance, score, bestDistance))
            { best = enemy; score = s; bestDistance = distance; }
        }
        return best;
    }
    private void Switch(Vector2 input)
    {
        if (view == null || Target == null) return;
        Vector3 current = view.WorldToViewportPoint(Target.LockOnAimPoint);
        Vector2 direction = PlayerLockOnSelection.QuantizeDirection(input);
        EnemyBase best = null;
        float bestAlignment = .7071067f, bestDistance = float.PositiveInfinity;
        foreach (EnemyBase enemy in EnemyBase.ActiveEnemies)
        {
            if (enemy == Target || !Candidate(enemy, out Vector3 p, out _)) continue;
            Vector2 offset = new((p.x - current.x) * view.pixelWidth,
                (p.y - current.y) * view.pixelHeight);
            if (!PlayerLockOnSelection.IsBetterDirectionalCandidate(offset, direction,
                bestAlignment, bestDistance, out float alignment)) continue;
            best = enemy; bestAlignment = alignment; bestDistance = offset.sqrMagnitude;
        }
        if (best != null)
        {
            Target = best;
            SfxAudio.Play(lockOnSwitchSound, lockOnSwitchVolume);
        }
    }
    public bool AllowsMagnetism(Vector3 input)
    {
        if (Target == null) return false;
        Vector3 direction = Vector3.ProjectOnPlane(Target.LockOnAimPoint - transform.position, Vector3.up);
        return input.sqrMagnitude < .0001f || direction.sqrMagnitude < .0001f || Vector3.Angle(input, direction) <= inputAngleLimit;
    }
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !DebugGizmoSettings.Enabled(DebugGizmoGroup.PlayerLockOn)) return;
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, acquisitionRange);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, lossRange);
        if (Target == null) return;
        Vector3 d = Vector3.ProjectOnPlane(Target.LockOnAimPoint - transform.position, Vector3.up).normalized;
        Gizmos.color = Color.red; Gizmos.DrawLine(transform.position, Target.LockOnAimPoint);
        Gizmos.DrawRay(transform.position, Quaternion.AngleAxis(inputAngleLimit, Vector3.up) * d * 3f);
        Gizmos.DrawRay(transform.position, Quaternion.AngleAxis(-inputAngleLimit, Vector3.up) * d * 3f);
    }
}

// Regras sem dependencias da Unity para selecao e histerese.
public static class PlayerLockOnSelection
{
    internal static bool ShouldResetMouseProgress(float progress, float checkpoint,
        float minimumProgress) => progress - checkpoint < Mathf.Max(0f, minimumProgress);

    internal static bool IsBetterDirectionalCandidate(Vector2 offset, Vector2 direction,
        float bestAlignment, float bestDistance, out float alignment)
    {
        alignment = 0f;
        if (offset.sqrMagnitude <= .0001f) return false;
        alignment = Vector2.Dot(offset.normalized, direction);
        return alignment >= bestAlignment &&
            (!Mathf.Approximately(alignment, bestAlignment) || offset.sqrMagnitude < bestDistance);
    }

    public static Vector2 QuantizeDirection(Vector2 input)
    {
        if (input.sqrMagnitude <= .0001f) return Vector2.zero;
        float angle = Mathf.Atan2(input.y, input.x);
        float step = Mathf.PI * .25f;
        angle = Mathf.Round(angle / step) * step;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    public static bool IsBetter(float score, float distance, float bestScore, float bestDistance) =>
        score < bestScore || (score == bestScore && distance < bestDistance);

    public static bool WithinLossRange(float distanceSquared, float acquisitionRange, float lossRange)
    {
        float effectiveLossRange = System.Math.Max(lossRange, acquisitionRange + .01f);
        return distanceSquared <= effectiveLossRange * effectiveLossRange;
    }
}
