using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Converte estados do gameplay em camadas da camera. Nao calcula Follow,
/// orbita, colisao ou shake.
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraGameplayEffects : MonoBehaviour
{
    private const string CombatModifierId = "Gameplay.Combat";
    private const string HeatModifierId = "Gameplay.Heat";

    [Header("Combat")]
    [Tooltip("Metros somados a distancia base da camera enquanto o player esta em combate. Use valor negativo para aproximar.")]
    [SerializeField] private float combatDistanceOffset = 1.15f;
    [Tooltip("Graus somados ao Pitch da camera enquanto o player esta em combate. Valores positivos mostram o player mais de cima.")]
    [SerializeField] private float combatPitchOffset = 10f;
    [Tooltip("Tempo de transicao para entrar e sair do enquadramento de combate.")]
    [Min(0f)] [SerializeField] private float combatBlendTime = .75f;

    [Header("Positive Heat")]
    [Tooltip("Metros somados a distancia base quando o Heat positivo chega ao maximo. Heat zero ou negativo nao altera a camera.")]
    [SerializeField] private float maximumHeatDistanceOffset = -2.85f;
    [Tooltip("Graus somados ao Pitch quando o Heat positivo chega ao maximo. Heat zero ou negativo nao altera a camera.")]
    [SerializeField] private float maximumHeatPitchOffset;
    [Tooltip("Graus somados ao campo de visao quando o Heat positivo chega ao maximo. Valores positivos ampliam a visao; negativos aproximam visualmente.")]
    [SerializeField] private float maximumHeatFieldOfViewOffset;
    [Tooltip("Forca maxima do blur periférico quando o Heat positivo chega ao maximo. Zero desliga o efeito; 1 ja e visivel e valores maiores tornam os cantos muito desfocados.")]
    [Range(0f, 8f)] [SerializeField] private float maximumHeatPeripheralBlur = 1f;
    [Tooltip("Curva compartilhada pelos efeitos visuais de Heat. X representa Heat positivo normalizado (0 a 1); Y representa a parcela aplicada a distancia, Pitch, FOV e blur.")]
    [SerializeField] private AnimationCurve positiveHeatEffectsCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("Tempo de suavizacao da influencia do Heat na camera.")]
    [Min(0f)] [SerializeField] private float heatBlendTime = .2f;

    [Header("Thermal Color Grading")]
    [Tooltip("Global Volume que recebera uma copia runtime do Profile para o grading termico. Se vazio, encontra o primeiro Volume global ativo.")]
    [SerializeField] private Volume thermalVolume;
    [Tooltip("Temperature adicional no Heat maximo. Valores positivos deixam a imagem mais quente.")]
    [Range(-100f, 100f)] [SerializeField] private float maximumHeatTemperature = 35f;
    [Tooltip("Saturation adicional no Heat maximo.")]
    [Range(-100f, 100f)] [SerializeField] private float maximumHeatSaturation = 8f;
    [Tooltip("Contrast adicional no Heat maximo.")]
    [Range(-100f, 100f)] [SerializeField] private float maximumHeatContrast = 8f;
    private static readonly int HeatPeripheralBlurId =
        Shader.PropertyToID("_HeatPeripheralBlur");
    private Transform player;
    private CameraEffectStack effects;
    private PlayerCombatStatus combatStatus;
    private PlayerHeat playerHeat;
    private float combatWeight;
    private float combatVelocity;
    private float heatWeight;
    private float heatVelocity;
    private WhiteBalance whiteBalance;
    private ColorAdjustments colorAdjustments;
    private float baseTemperature;
    private float baseSaturation;
    private float baseContrast;
    private bool thermalProfileInitialized;
    private static Volume cachedGlobalVolume;
    private static bool hasSearchedGlobalVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetGlobalVolumeCache()
    {
        cachedGlobalVolume = null;
        hasSearchedGlobalVolume = false;
    }

    internal void Initialize(Transform playerTarget, CameraEffectStack stack)
    {
        player = playerTarget;
        FogRuntime.Player = playerTarget;
        effects = stack;
        if (!TryGetComponent(out CameraRain rain))
            rain = gameObject.AddComponent<CameraRain>();
        rain.enabled = true;
        rain.Initialize();
        // Uma troca de cena destroi o Volume global antigo; sem isso a busca em
        // cache nunca seria refeita e o grading termico pararia silenciosamente.
        if (cachedGlobalVolume == null)
            hasSearchedGlobalVolume = false;
        ResolvePlayerComponents();
        EnsureThermalProfile();
    }

    private void Update()
    {
        if (player == null || effects == null)
            return;
        ResolvePlayerComponents();
        EnsureThermalProfile();
        UpdateCombatEffect();
        UpdateHeatEffect();
    }

    private void OnDisable()
    {
        if (TryGetComponent(out CameraRain rain))
            rain.enabled = false;
        if (FogRuntime.Player == player)
            FogRuntime.Player = null;
        if (effects != null)
        {
            effects.RemoveModifier(CombatModifierId);
            effects.RemoveModifier(HeatModifierId);
        }
        Shader.SetGlobalFloat(HeatPeripheralBlurId, 0f);
    }

    private void OnValidate()
    {
        if (positiveHeatEffectsCurve == null ||
            positiveHeatEffectsCurve.length == 0)
        {
            positiveHeatEffectsCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
    }

    private void ResolvePlayerComponents()
    {
        if (combatStatus == null)
            combatStatus = player.GetComponent<PlayerCombatStatus>();
        if (playerHeat == null)
            playerHeat = player.GetComponent<PlayerHeat>();
    }

    private void UpdateCombatEffect()
    {
        float targetWeight = combatStatus != null &&
            !combatStatus.IsOutOfCombat ? 1f : 0f;
        combatWeight = Damp(combatWeight, targetWeight,
            ref combatVelocity, combatBlendTime);
        CameraModifier modifier = CameraModifier.Identity;
        modifier.distanceOffset = combatDistanceOffset * combatWeight;
        modifier.pitchOffset = combatPitchOffset * combatWeight;
        effects.SetModifier(CombatModifierId, modifier);
    }

    private void UpdateHeatEffect()
    {
        float normalizedHeat = playerHeat != null && playerHeat.IsHeatModeActive && playerHeat.InitialMaxHeat > 0f
            ? Mathf.Clamp01(playerHeat.CurrentHeat / playerHeat.InitialMaxHeat) : 0f;
        float targetWeight = Mathf.Clamp01(positiveHeatEffectsCurve != null
            ? positiveHeatEffectsCurve.Evaluate(normalizedHeat)
            : normalizedHeat);
        heatWeight = Damp(heatWeight, targetWeight,
            ref heatVelocity, heatBlendTime);
        CameraModifier modifier = CameraModifier.Identity;
        modifier.distanceOffset = maximumHeatDistanceOffset * heatWeight;
        modifier.pitchOffset = maximumHeatPitchOffset * heatWeight;
        modifier.fieldOfViewOffset =
            maximumHeatFieldOfViewOffset * heatWeight;
        effects.SetModifier(HeatModifierId, modifier);
        Shader.SetGlobalFloat(HeatPeripheralBlurId,
            maximumHeatPeripheralBlur * heatWeight);

        ApplyThermalColorGrading();
    }

    private void ApplyThermalColorGrading()
    {
        if (whiteBalance == null || colorAdjustments == null)
            return;

        // Os pesos ja foram calculados pelas mesmas curvas e pelos mesmos
        // tempos de blend usados para distancia, FOV e Roll da camera.
        whiteBalance.temperature.value = baseTemperature +
            maximumHeatTemperature * heatWeight;
        colorAdjustments.saturation.value = baseSaturation +
            maximumHeatSaturation * heatWeight;
        colorAdjustments.contrast.value = baseContrast +
            maximumHeatContrast * heatWeight;
    }

    private void EnsureThermalProfile()
    {
        if (thermalProfileInitialized)
        {
            // Volume/profile destruidos (troca de cena): descarta o estado e
            // procura o novo Volume global.
            if (thermalVolume != null && whiteBalance != null &&
                colorAdjustments != null)
                return;

            thermalProfileInitialized = false;
            whiteBalance = null;
            colorAdjustments = null;
            if (cachedGlobalVolume == null)
                hasSearchedGlobalVolume = false;
        }

        if (thermalVolume == null)
        {
            if (cachedGlobalVolume == null && !hasSearchedGlobalVolume)
            {
                hasSearchedGlobalVolume = true;
                foreach (Volume volume in FindObjectsByType<Volume>(
                             FindObjectsInactive.Exclude))
                {
                    if (volume != null && volume.isGlobal)
                    {
                        cachedGlobalVolume = volume;
                        break;
                    }
                }
            }
            thermalVolume = cachedGlobalVolume;
        }
        if (thermalVolume == null)
            return;

        // Volume.profile instancia uma copia do sharedProfile em runtime;
        // portanto o Profile asset original nunca recebe estas alteracoes.
        VolumeProfile runtimeProfile = thermalVolume.profile;
        if (!runtimeProfile.TryGet(out whiteBalance))
            whiteBalance = runtimeProfile.Add<WhiteBalance>(true);
        if (!runtimeProfile.TryGet(out colorAdjustments))
            colorAdjustments = runtimeProfile.Add<ColorAdjustments>(true);

        baseTemperature = whiteBalance.temperature.value;
        baseSaturation = colorAdjustments.saturation.value;
        baseContrast = colorAdjustments.contrast.value;
        whiteBalance.temperature.overrideState = true;
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.contrast.overrideState = true;
        thermalProfileInitialized = true;
    }

    private static float Damp(float current, float target,
        ref float velocity, float smoothTime)
    {
        return smoothTime <= 0f ? target : Mathf.SmoothDamp(
            current, target, ref velocity, smoothTime,
            Mathf.Infinity, Time.unscaledDeltaTime
        );
    }
}
