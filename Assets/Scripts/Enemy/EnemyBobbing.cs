using UnityEngine;

/// <summary>
/// Perfil reutilizavel de movimento vertical oscilante. O inimigo que usa o
/// componente fornece a velocidade horizontal atual e aplica os resultados.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyBobbing : MonoBehaviour
{
    [Header("Bobbing")]
    [Tooltip("Faixa sorteada uma vez ao nascer para definir a altura da oscilacao vertical desta instancia.")]
    [SerializeField] private EnemyFloatRange bobbingHeight = new() { min = .15f, max = .35f };
    [Tooltip("Faixa de frequencia lenta, em ciclos por segundo, usada em uma das fases alternadas do bobbing.")]
    [SerializeField] private EnemyFloatRange slowFrequency = new() { min = .12f, max = .25f };
    [Tooltip("Faixa de frequencia rapida, em ciclos por segundo, usada na outra fase alternada do bobbing.")]
    [SerializeField] private EnemyFloatRange fastFrequency = new() { min = .3f, max = .55f };
    [Tooltip("Tempo aleatorio entre as trocas das fases lenta e rapida do bobbing.")]
    [SerializeField] private EnemyFloatRange frequencyChangeInterval = new() { min = 2f, max = 4f };
    [Tooltip("Rapidez com que a frequencia atual se aproxima da nova fase, evitando mudancas bruscas.")]
    [Min(.01f)] [SerializeField] private float frequencyTransitionSpeed = .5f;

    [Header("Puxao ao Retomar Movimento")]
    [Tooltip("Velocidade horizontal minima para considerar que o inimigo comecou a se mover.")]
    [Min(0f)] [SerializeField] private float movementThreshold = .1f;
    [Tooltip("Tempo parado necessario antes de um novo movimento gerar o puxao vertical.")]
    [Min(0f)] [SerializeField] private float minimumStillDuration = .2f;
    [Tooltip("Duracao do impulso visual e vertical ao voltar a se mover depois de ficar parado.")]
    [Min(.01f)] [SerializeField] private float movementPullDuration = .3f;
    [Tooltip("Altura adicional maxima atingida pelo bobbing durante o puxao ao retomar movimento.")]
    [Min(0f)] [SerializeField] private float movementPullHeight = .35f;
    [Tooltip("Velocidade vertical inicial adicionada durante o puxao ao retomar movimento.")]
    [Min(0f)] [SerializeField] private float movementPullVerticalSpeed = 4f;

    private float randomizedHeight;
    private float randomizedSlowFrequency;
    private float randomizedFastFrequency;
    private float currentFrequency;
    private float frequencyTimer;
    private float phase;
    private float stillTimer;
    private float movementPullTimer;
    private bool useFastFrequency;
    private bool wasMoving;
    private bool initialized;

    public float CurrentHeightOffset { get; private set; }
    public float CurrentVerticalSpeedBoost { get; private set; }

    private void Awake() => InitializeProfile();

    private void OnValidate()
    {
        bobbingHeight.min = Mathf.Max(0f, bobbingHeight.min);
        bobbingHeight.max = Mathf.Max(bobbingHeight.min, bobbingHeight.max);
        slowFrequency.min = Mathf.Max(0f, slowFrequency.min);
        slowFrequency.max = Mathf.Max(slowFrequency.min, slowFrequency.max);
        fastFrequency.min = Mathf.Max(0f, fastFrequency.min);
        fastFrequency.max = Mathf.Max(fastFrequency.min, fastFrequency.max);
        frequencyChangeInterval.min = Mathf.Max(.1f,
            frequencyChangeInterval.min);
        frequencyChangeInterval.max = Mathf.Max(frequencyChangeInterval.min,
            frequencyChangeInterval.max);
    }

    /// <summary>Atualiza o perfil e expoe offset e impulso vertical deste frame.</summary>
    public void Tick(float horizontalSpeed, float deltaTime)
    {
        if (!initialized)
            InitializeProfile();
        if (deltaTime <= 0f)
            return;

        TickMovementPull(horizontalSpeed, deltaTime);
        TickFrequency(deltaTime);

        phase = Mathf.Repeat(phase + currentFrequency * Mathf.PI * 2f *
            deltaTime, Mathf.PI * 2f);
        CurrentHeightOffset = Mathf.Sin(phase) * randomizedHeight +
            EvaluateMovementPullHeight();
        CurrentVerticalSpeedBoost = EvaluateMovementPullSpeed();
    }

    private void InitializeProfile()
    {
        randomizedHeight = Mathf.Max(0f, bobbingHeight.RandomValue());
        randomizedSlowFrequency = Mathf.Max(0f, slowFrequency.RandomValue());
        randomizedFastFrequency = Mathf.Max(randomizedSlowFrequency,
            fastFrequency.RandomValue());
        useFastFrequency = Random.value >= .5f;
        currentFrequency = useFastFrequency
            ? randomizedFastFrequency : randomizedSlowFrequency;
        phase = Random.Range(0f, Mathf.PI * 2f);
        frequencyTimer = Mathf.Max(.1f, frequencyChangeInterval.RandomValue());
        initialized = true;
    }

    private void TickMovementPull(float horizontalSpeed, float deltaTime)
    {
        bool moving = horizontalSpeed >= movementThreshold;
        if (moving)
        {
            if (!wasMoving && stillTimer >= minimumStillDuration)
                movementPullTimer = movementPullDuration;
            stillTimer = 0f;
        }
        else
        {
            stillTimer += deltaTime;
        }

        wasMoving = moving;
        movementPullTimer = Mathf.Max(0f, movementPullTimer - deltaTime);
    }

    private void TickFrequency(float deltaTime)
    {
        frequencyTimer -= deltaTime;
        if (frequencyTimer <= 0f)
        {
            useFastFrequency = !useFastFrequency;
            if (useFastFrequency)
                randomizedFastFrequency = Mathf.Max(randomizedSlowFrequency,
                    fastFrequency.RandomValue());
            else
                randomizedSlowFrequency = Mathf.Max(0f,
                    slowFrequency.RandomValue());
            frequencyTimer = Mathf.Max(.1f, frequencyChangeInterval.RandomValue());
        }

        float targetFrequency = useFastFrequency
            ? randomizedFastFrequency : randomizedSlowFrequency;
        currentFrequency = Mathf.MoveTowards(currentFrequency, targetFrequency,
            frequencyTransitionSpeed * deltaTime);
    }

    private float GetMovementPullProgress()
    {
        if (movementPullTimer <= 0f || movementPullDuration <= 0f)
            return 1f;
        return 1f - movementPullTimer / movementPullDuration;
    }

    private float EvaluateMovementPullHeight()
    {
        if (movementPullTimer <= 0f)
            return 0f;
        float progress = GetMovementPullProgress();
        return movementPullHeight * Mathf.Sin(progress * Mathf.PI);
    }

    private float EvaluateMovementPullSpeed()
    {
        if (movementPullTimer <= 0f)
            return 0f;
        return movementPullVerticalSpeed *
            (1f - GetMovementPullProgress());
    }
}
