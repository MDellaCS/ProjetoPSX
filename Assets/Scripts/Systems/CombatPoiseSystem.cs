using UnityEngine;

public enum CombatReactionType
{
    [InspectorName("Default (Stagger)")]
    DefaultStagger = 0,
    Juggle = 2,
    LaunchUp = 3,
    LaunchDown = 4,
    LaunchForwards = 5,
    // Mantido no fim para não alterar os valores serializados das reações antigas.
    LaunchUpForwards = 6,
    [InspectorName("Launch Backwards")]
    LaunchBackwards = 7
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
    public readonly float LaunchHeight;
    public readonly float LaunchDuration;
    public readonly AnimationCurve LaunchAscentCurve;

    public CombatReactionRequest(CombatReactionType type, Vector3 sourcePosition,
        float launchSpeed, float gravity, float launchDownSpeed,
        float launchForwardsForce, float launchForwardsDeceleration,
        float juggleLiftSpeed, float juggleGravityMultiplier,
        float juggleDuration, float launchHeight = 0f,
        float launchDuration = 0f, AnimationCurve launchAscentCurve = null)
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
        LaunchHeight = launchHeight;
        LaunchDuration = launchDuration;
        LaunchAscentCurve = launchAscentCurve;
    }
}

/// <summary>Resultado fisico simplificado de uma colisao entre duas massas.</summary>
public readonly struct CombatMassImpact
{
    public readonly float SourcePostImpactSpeed;
    public readonly float TargetPostImpactSpeed;
    public readonly float SourceImpactIntensity;
    public readonly float TargetImpactIntensity;

    public CombatMassImpact(float sourcePostImpactSpeed,
        float targetPostImpactSpeed, float sourceImpactIntensity,
        float targetImpactIntensity)
    {
        SourcePostImpactSpeed = sourcePostImpactSpeed;
        TargetPostImpactSpeed = targetPostImpactSpeed;
        SourceImpactIntensity = sourceImpactIntensity;
        TargetImpactIntensity = targetImpactIntensity;
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
    /// <summary>Massa virtual de paredes e chao: superficies imoveis.</summary>
    public const float StaticSurfaceMass = 999999f;
    private static CombatPoiseSystem instance;

    [Header("Global Poise Regeneration")]
    [Tooltip("Tempo sem receber qualquer golpe antes de o Poise poder comecar a regenerar.")]
    [Min(0f)] [SerializeField] private float poiseRegenDelay = 3f;
    [Tooltip("Porcentagem do Poise maximo regenerada por segundo. Ex.: 0.05 regenera 5% por segundo.")]
    [Min(0f)] [SerializeField] private float poiseRegenMaxPercentPerSecond = .05f;
    [Tooltip("Quantidade fixa de Poise regenerada por segundo, somada a parcela percentual.")]
    [Min(0f)] [SerializeField] private float poiseRegenFlatPerSecond = 5f;

    [Header("Global Launch Forwards e Down")]
    [Tooltip("Forca de Launch Forwards e Launch Down gerada por cada ponto de dano realmente causado, antes dos limites minimo e maximo.")]
    [Min(0f)] [SerializeField] private float launchForwardsForcePerDamageDealt = .42f;
    [Tooltip("Menor forca permitida para Launch Forwards e Launch Down. Tambem e usada por Self Launch Down, que nao depende de dano.")]
    [Min(0f)] [SerializeField] private float minimumLaunchForce = 8f;
    [Tooltip("Maior forca permitida para Launch Forwards e Launch Down, impedindo que golpes de dano extremo lancem longe demais.")]
    [Min(0f)] [SerializeField] private float maximumLaunchForce = 35f;
    [Tooltip("Velocidade horizontal perdida por segundo durante Launch Forwards.")]
    [Min(.01f)] [SerializeField] private float launchForwardsDeceleration = 20f;
    [Tooltip("Desvio angular maximo, em graus, que a assistencia global pode aplicar ao Launch Forwards inicial do player para alinhar o inimigo arremessado a outro inimigo.")]
    [Range(0f, 90f)] [SerializeField] private float playerLaunchBodyImpactAssistAngle = 20f;
    [Tooltip("Distancia maxima em que a assistencia global de Launch Forwards do player procura outro inimigo dentro do cone angular.")]
    [Min(0f)] [SerializeField] private float playerLaunchBodyImpactAssistRange = 12f;

    [Header("Impactos e Splats Globais")]
    [Tooltip("Velocidade relativa minima para uma colisao causar dano, Poise e Stagger. Vale para parede, chao e inimigos arremessados.")]
    [Min(0f)] [SerializeField] private float minimumImpactSpeed = 10f;
    [Tooltip("Dano causado por cada unidade de intensidade de impacto calculada pela massa e velocidade relativa.")]
    [Min(0f)] [SerializeField] private float impactDamagePerIntensity = 15f;
    [Tooltip("Poise causado por cada unidade de intensidade de impacto calculada pela massa e velocidade relativa.")]
    [Min(0f)] [SerializeField] private float impactPoisePerIntensity = 20f;
    [Tooltip("Tempo de Stagger aplicado apos um Wall Splat ou Floor Splat. Colisoes entre inimigos usam a reacao de Poise normal.")]
    [Min(0f)] [SerializeField] private float staticImpactStunDuration = 3f;
    [Tooltip("Camadas que contam como superficies estaticas para Wall Splat. O chao usa a mesma fisica ao pousar apos um Launch.")]
    [SerializeField] private LayerMask staticImpactLayers = ~0;

    [Header("Global Aerial Reactions")]
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
    public float PlayerLaunchBodyImpactAssistAngle =>
        playerLaunchBodyImpactAssistAngle;
    public float PlayerLaunchBodyImpactAssistRange =>
        playerLaunchBodyImpactAssistRange;
    public float MinimumImpactSpeed => minimumImpactSpeed;
    public float StaticImpactStunDuration => staticImpactStunDuration;
    public LayerMask StaticImpactLayers => staticImpactLayers;
    public float MinimumLaunchForce => Mathf.Min(minimumLaunchForce, maximumLaunchForce);
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

    /// <summary>Calcula a forca compartilhada por Launch Forwards e Launch Down.</summary>
    public float CalculateLaunchForce(int damageDealt)
    {
        float minimum = MinimumLaunchForce;
        float maximum = Mathf.Max(minimum, maximumLaunchForce);
        float damageForce = Mathf.Max(0, damageDealt) *
            launchForwardsForcePerDamageDealt;
        return Mathf.Clamp(damageForce, minimum, maximum);
    }

    /// <summary>
    /// Resolve uma colisao elastica 1D. A intensidade recebida e o impulso
    /// dividido pela massa de quem recebeu.
    /// </summary>
    public CombatMassImpact ResolveMassImpact(float sourceMass,
        float targetMass, float sourceSpeed, float targetSpeed = 0f)
    {
        float safeSourceMass = Mathf.Max(.01f, sourceMass);
        float safeTargetMass = Mathf.Max(.01f, targetMass);
        float sourceVelocity = Mathf.Max(0f, sourceSpeed);
        // O alvo pode estar vindo na direcao contraria; nesse caso a
        // velocidade assinada aumenta corretamente a colisao frontal.
        float targetVelocity = targetSpeed;
        float totalMass = safeSourceMass + safeTargetMass;
        float relativeSpeed = Mathf.Max(0f, sourceVelocity - targetVelocity);
        float impulse = relativeSpeed * safeSourceMass * safeTargetMass /
            totalMass;
        return new CombatMassImpact(
            ((safeSourceMass - safeTargetMass) * sourceVelocity +
             2f * safeTargetMass * targetVelocity) / totalMass,
            (2f * safeSourceMass * sourceVelocity +
             (safeTargetMass - safeSourceMass) * targetVelocity) / totalMass,
            impulse / safeSourceMass, impulse / safeTargetMass);
    }

    /// <summary>Converte intensidade de impacto em dano e Poise globais.</summary>
    public void CalculateImpactDamage(float intensity, out int damage,
        out int poiseDamage)
    {
        float safeIntensity = Mathf.Max(0f, intensity);
        damage = Mathf.Max(0, Mathf.RoundToInt(safeIntensity *
            impactDamagePerIntensity));
        poiseDamage = Mathf.Max(0, Mathf.RoundToInt(safeIntensity *
            impactPoisePerIntensity));
    }

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
