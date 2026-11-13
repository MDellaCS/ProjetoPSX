using UnityEngine;

/// <summary>Adapta o Heat do jogador aos valores de um emissor local, sem acoplar combate a iluminacao.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Projeto PSX/Player/Player Heat Light Driver")]
public sealed class PlayerHeatLightDriver : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Emissor retro controlado pelo Heat. Se vazio, procura um emissor neste objeto.")]
    [SerializeField] private LocalLightEmitter emitter;
    [Tooltip("Fonte do Heat. Se vazio, procura PlayerHeat neste objeto.")]
    [SerializeField] private PlayerHeat heat;

    [Header("Resposta ao Heat")]
    [Tooltip("Multiplicador de intensidade com Heat zero; permite manter um brilho residual.")]
    [Min(0f)] [SerializeField] private float minimumIntensityMultiplier = .65f;
    [Tooltip("Multiplicador adicional de intensidade ao atingir o Heat maximo.")]
    [Min(0f)] [SerializeField] private float maximumIntensityMultiplier = 1.5f;
    [Tooltip("Alcance relativo da luz com Heat zero.")]
    [Min(0f)] [SerializeField] private float minimumRangeMultiplier = .75f;
    [Tooltip("Alcance relativo da luz ao atingir o Heat maximo.")]
    [Min(0f)] [SerializeField] private float maximumRangeMultiplier = 1.25f;
    [Tooltip("Tempo em segundos para a luz acompanhar mudancas de Heat. Zero aplica imediatamente.")]
    [Min(0f)] [SerializeField] private float responseTime = .2f;
    [Tooltip("Quanto a intensidade pulsa no Heat maximo. Zero desativa a pulsacao.")]
    [Range(0f, .5f)] [SerializeField] private float maximumHeatPulse = .06f;
    [Tooltip("Velocidade da pulsacao em ciclos por segundo.")]
    [Min(0f)] [SerializeField] private float pulseFrequency = 1.5f;

    private float intensityVelocity;
    private float rangeVelocity;
    private float currentIntensityMultiplier;
    private float currentRangeMultiplier;
    private PlayerMovesetController movesetController;
    private bool heatModifiersApplied;

    private void Awake() => ResolveReferences();

    private void OnEnable()
    {
        ResolveReferences();
        currentIntensityMultiplier = minimumIntensityMultiplier;
        currentRangeMultiplier = minimumRangeMultiplier;
        intensityVelocity = 0f;
        rangeVelocity = 0f;
        ResetModifiers();
    }

    private void OnDisable()
    {
        ResetModifiers();
    }

    private void Update()
    {
        ResolveReferences();
        if (emitter == null)
            return;

        if (heat == null || movesetController == null || movesetController.Moveset == null ||
            !heat.IsHeatModeActive)
        {
            if (heatModifiersApplied)
            {
                ResetModifiers();
                currentIntensityMultiplier = minimumIntensityMultiplier;
                currentRangeMultiplier = minimumRangeMultiplier;
                intensityVelocity = rangeVelocity = 0f;
            }
            return;
        }

        float normalizedHeat = Mathf.Clamp01(heat.CurrentHeat / Mathf.Max(1f, heat.MaxHeat));
        float targetIntensity = Mathf.Lerp(minimumIntensityMultiplier, maximumIntensityMultiplier, normalizedHeat);
        float targetRange = Mathf.Lerp(minimumRangeMultiplier, maximumRangeMultiplier, normalizedHeat);

        if (responseTime <= 0f)
        {
            currentIntensityMultiplier = targetIntensity;
            currentRangeMultiplier = targetRange;
        }
        else
        {
            currentIntensityMultiplier = Mathf.SmoothDamp(currentIntensityMultiplier, targetIntensity,
                ref intensityVelocity, responseTime, Mathf.Infinity, Time.deltaTime);
            currentRangeMultiplier = Mathf.SmoothDamp(currentRangeMultiplier, targetRange,
                ref rangeVelocity, responseTime, Mathf.Infinity, Time.deltaTime);
        }

        float pulse = normalizedHeat * maximumHeatPulse * Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f);
        Apply(currentIntensityMultiplier * (1f + pulse));
        heatModifiersApplied = true;
    }

    private void ResetModifiers()
    {
        if (emitter != null)
        {
            emitter.DynamicIntensityMultiplier = 1f;
            emitter.SetRangeMultiplier(1f);
        }
        heatModifiersApplied = false;
    }

    private void Apply(float intensityMultiplier)
    {
        if (emitter == null)
            return;
        emitter.DynamicIntensityMultiplier = Mathf.Max(0f, intensityMultiplier);
        emitter.SetRangeMultiplier(currentRangeMultiplier);
    }

    private void ResolveReferences()
    {
        if (emitter == null)
            emitter = GetComponent<LocalLightEmitter>();
        if (heat == null)
            heat = GetComponent<PlayerHeat>();
        if (movesetController == null)
            movesetController = GetComponent<PlayerMovesetController>();
    }
}
