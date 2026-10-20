using UnityEngine;

public enum CombatReactionType
{
    None,
    Stagger,
    Juggle,
    LaunchUp,
    LaunchUpForwards,
    LaunchDown,
    LaunchForwards
}

public readonly struct CombatPoiseHitResult
{
    public readonly bool CanReact;
    public readonly bool JustBroke;
    public readonly bool IsSuperStagger;

    public CombatPoiseHitResult(bool canReact, bool justBroke,
        bool isSuperStagger)
    {
        CanReact = canReact;
        JustBroke = justBroke;
        IsSuperStagger = isSuperStagger;
    }
}

public readonly struct CombatReactionRequest
{
    public readonly CombatReactionType Type;
    public readonly Vector3 SourcePosition;
    public readonly float LaunchSpeed;
    public readonly float Gravity;
    public readonly float LaunchDownSpeed;
    public readonly float LaunchForwardsForce;
    public readonly float LaunchForwardsDeceleration;
    public readonly float JuggleLiftSpeed;
    public readonly float JuggleGravityMultiplier;
    public readonly float JuggleDuration;

    public CombatReactionRequest(CombatReactionType type, Vector3 sourcePosition,
        float launchSpeed, float gravity, float launchDownSpeed,
        float launchForwardsForce, float launchForwardsDeceleration,
        float juggleLiftSpeed, float juggleGravityMultiplier,
        float juggleDuration)
    {
        Type = type;
        SourcePosition = sourcePosition;
        LaunchSpeed = launchSpeed;
        Gravity = gravity;
        LaunchDownSpeed = launchDownSpeed;
        LaunchForwardsForce = launchForwardsForce;
        LaunchForwardsDeceleration = launchForwardsDeceleration;
        JuggleLiftSpeed = juggleLiftSpeed;
        JuggleGravityMultiplier = juggleGravityMultiplier;
        JuggleDuration = juggleDuration;
    }
}

/// <summary>Estado reutilizavel de Poise para Player e inimigos.</summary>
public sealed class CombatPoiseState
{
    private float current;
    private float timeSinceLastHit;

    public float Current => current;

    public void Initialize(float maxPoise)
    {
        current = Mathf.Max(1f, maxPoise);
        timeSinceLastHit = 0f;
    }

    public CombatPoiseHitResult ReceiveHit(int poiseDamage, float maxPoise)
    {
        float safeMax = Mathf.Max(1f, maxPoise);
        float before = current;
        float rawAfterHit = before - Mathf.Max(0, poiseDamage);
        timeSinceLastHit = 0f;

        // Poise e um recurso de 0 ate Max Poise: a quebra nunca cria uma
        // "divida" negativa que precisaria ser regenerada depois. Ainda
        // usamos o resultado bruto deste golpe para reconhecer um
        // Superstagger, antes de limitar o valor armazenado.
        current = Mathf.Clamp(rawAfterHit, 0f, safeMax);
        bool justBroke = before > 0f && rawAfterHit <= 0f;
        return new CombatPoiseHitResult(
            rawAfterHit <= 0f,
            justBroke,
            justBroke && rawAfterHit <= -safeMax
        );
    }

    public void Tick(float maxPoise, bool canRegenerate)
    {
        float safeMax = Mathf.Max(1f, maxPoise);

        // Tambem protege a regeneracao de qualquer estado invalido criado
        // por alteracoes futuras: ela sempre comeca, no minimo, em zero.
        current = Mathf.Clamp(current, 0f, safeMax);
        if (current >= safeMax)
        {
            current = safeMax;
            return;
        }

        timeSinceLastHit += Time.deltaTime;
        CombatPoiseSystem rules = CombatPoiseSystem.Instance;
        if (!canRegenerate || timeSinceLastHit < rules.RegenDelay)
            return;

        current = Mathf.Min(safeMax, current +
            (safeMax * rules.RegenMaxPercentPerSecond +
             rules.RegenFlatPerSecond) * Time.deltaTime);
    }
}

/// <summary>Regras globais compartilhadas por todo o sistema de Poise.</summary>
[DisallowMultipleComponent]
public sealed class CombatPoiseSystem : MonoBehaviour
{
    private static CombatPoiseSystem instance;

    [Header("Global Poise Regeneration")]
    [Tooltip("Tempo sem receber qualquer golpe antes de o Poise poder comecar a regenerar.")]
    [Min(0f)] [SerializeField] private float poiseRegenDelay = 3f;
    [Tooltip("Porcentagem do Poise maximo regenerada por segundo. Ex.: 0.05 regenera 5% por segundo.")]
    [Min(0f)] [SerializeField] private float poiseRegenMaxPercentPerSecond = .05f;
    [Tooltip("Quantidade fixa de Poise regenerada por segundo, somada a parcela percentual.")]
    [Min(0f)] [SerializeField] private float poiseRegenFlatPerSecond = 5f;

    [Header("Global Launch Forwards")]
    [Tooltip("Velocidade de Launch Forwards gerada por cada ponto de dano realmente causado.")]
    [Min(0f)] [SerializeField] private float launchForwardsForcePerDamageDealt = .42f;
    [Tooltip("Velocidade horizontal perdida por segundo durante Launch Forwards.")]
    [Min(.01f)] [SerializeField] private float launchForwardsDeceleration = 20f;

    [Header("Global Aerial Reactions")]
    [Tooltip("Velocidade vertical para baixo aplicada por Launch Down.")]
    [Min(0f)] [SerializeField] private float launchDownFallSpeed = 35f;
    [Tooltip("Velocidade vertical aplicada ao manter um alvo em Juggle.")]
    [Min(0f)] [SerializeField] private float juggleLiftSpeed = 2f;
    [Tooltip("Multiplicador temporario de gravidade usado durante Juggle.")]
    [Range(0f, 1f)] [SerializeField] private float juggleGravityMultiplier = .3f;
    [Tooltip("Duracao da gravidade modificada aplicada por cada Juggle.")]
    [Min(0f)] [SerializeField] private float juggleDuration = .2f;

    public float RegenDelay => poiseRegenDelay;
    public float RegenMaxPercentPerSecond => poiseRegenMaxPercentPerSecond;
    public float RegenFlatPerSecond => poiseRegenFlatPerSecond;
    public float LaunchForwardsDeceleration => launchForwardsDeceleration;
    public float LaunchDownFallSpeed => launchDownFallSpeed;
    public float JuggleLiftSpeed => juggleLiftSpeed;
    public float JuggleGravityMultiplier => juggleGravityMultiplier;
    public float JuggleDuration => juggleDuration;

    public static CombatPoiseSystem Instance
    {
        get
        {
            if (instance != null) return instance;
            instance = FindAnyObjectByType<CombatPoiseSystem>();
            if (instance != null) return instance;
            GameObject systemObject = new("CombatPoiseSystem");
            instance = systemObject.AddComponent<CombatPoiseSystem>();
            DontDestroyOnLoad(systemObject);
            return instance;
        }
    }

    public float CalculateLaunchForwardsForce(int damageDealt) =>
        Mathf.Max(0, damageDealt) * launchForwardsForcePerDamageDealt;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
}
