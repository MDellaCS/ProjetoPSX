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
    private const string ColdModifierId = "Gameplay.Cold";

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

    [Header("Negative Heat / Cold")]
    [Tooltip("Metros somados a distancia base quando o Cold chega ao maximo. Valores positivos afastam a camera.")]
    [SerializeField] private float maximumColdDistanceOffset = 2f;
    [Tooltip("Graus somados ao campo de visao quando o Cold chega ao maximo. Valores negativos estreitam a visao.")]
    [SerializeField] private float maximumColdFieldOfViewOffset = -12f;
    [Tooltip("Graus maximos de Roll quando o Cold chega ao maximo. Simula a camera tombando horizontalmente para um lado.")]
    [SerializeField] private float maximumColdRollOffset = 8f;
    [Tooltip("Curva compartilhada pelos efeitos visuais de Cold. X representa a magnitude de Cold normalizada (0 a 1); Y representa a parcela aplicada a distancia, FOV e Roll.")]
    [SerializeField] private AnimationCurve negativeHeatEffectsCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("Tempo de suavizacao da influencia do Cold na camera.")]
    [Min(0f)] [SerializeField] private float coldBlendTime = .2f;

    [Header("Thermal Color Grading")]
    [Tooltip("Global Volume que recebera uma copia runtime do Profile para o grading termico. Se vazio, encontra o primeiro Volume global ativo.")]
    [SerializeField] private Volume thermalVolume;
    [Tooltip("Temperature adicional no Heat maximo. Valores positivos deixam a imagem mais quente.")]
    [Range(-100f, 100f)] [SerializeField] private float maximumHeatTemperature = 35f;
    [Tooltip("Saturation adicional no Heat maximo.")]
    [Range(-100f, 100f)] [SerializeField] private float maximumHeatSaturation = 8f;
    [Tooltip("Contrast adicional no Heat maximo.")]
    [Range(-100f, 100f)] [SerializeField] private float maximumHeatContrast = 8f;
    [Tooltip("Temperature adicional no Cold maximo. Valores negativos deixam a imagem mais fria.")]
    [Range(-100f, 100f)] [SerializeField] private float maximumColdTemperature = -40f;
    [Tooltip("Saturation adicional no Cold maximo. Valores negativos dessaturam a imagem.")]
    [Range(-100f, 100f)] [SerializeField] private float maximumColdSaturation = -20f;
    [Tooltip("Contrast adicional no Cold maximo.")]
    [Range(-100f, 100f)] [SerializeField] private float maximumColdContrast = 6f;

    [Header("Player Centered Fog")]
    [Tooltip("Ativa a nevoa centrada na posicao mundial do player. Requer o Renderer Feature 'Player Centered Fog' configurado no PC_Renderer.")]
    [SerializeField] private bool playerCenteredFogEnabled = true;
    [Tooltip("Cor final da nevoa que cobre o mundo distante.")]
    [SerializeField] private Color playerCenteredFogColor = new(.34f, .41f, .4f, 1f);
    [Tooltip("Distancia horizontal ao player onde a nevoa comeca a aparecer.")]
    [Min(0f)] [SerializeField] private float playerCenteredFogStartRadius = 10f;
    [Tooltip("Distancia horizontal ao player onde a nevoa chega a intensidade maxima.")]
    [Min(.01f)] [SerializeField] private float playerCenteredFogEndRadius = 28f;
    [Tooltip("Deslocamento vertical da base da nevoa em relacao ao centro do player. Valor negativo aproxima a base do chao.")]
    [SerializeField] private float playerCenteredFogBaseHeightOffset = -1f;
    [Tooltip("Altura, a partir da base, em que a nevoa se dissolve completamente. Valores baixos deixam a nevoa rente ao chao.")]
    [Min(.01f)] [SerializeField] private float playerCenteredFogHeight = 5f;
    [Tooltip("Opacidade maxima da nevoa. 1 cobre totalmente objetos dentro da regiao mais densa.")]
    [Range(0f, 1f)] [SerializeField] private float playerCenteredFogOpacity = .92f;
    [Tooltip("Escala do ruido no mundo. Valores maiores criam variacoes menores e mais frequentes.")]
    [Min(.001f)] [SerializeField] private float playerCenteredFogNoiseScale = .3f;
    [Tooltip("Quanto o ruido altera a borda da nevoa. Use valores pequenos para movimento sutil.")]
    [Range(0f, 1f)] [SerializeField] private float playerCenteredFogNoiseStrength = .08f;
    [Tooltip("Velocidade do movimento do ruido da nevoa.")]
    [Min(0f)] [SerializeField] private float playerCenteredFogNoiseSpeed = .15f;

    private static readonly int HeatPeripheralBlurId =
        Shader.PropertyToID("_HeatPeripheralBlur");
    private static readonly int PlayerCenteredFogEnabledId = Shader.PropertyToID("_PlayerCenteredFogEnabled");
    private static readonly int PlayerCenteredFogColorId = Shader.PropertyToID("_PlayerCenteredFogColor");
    private static readonly int PlayerCenteredFogPlayerPositionId = Shader.PropertyToID("_PlayerCenteredFogPlayerPosition");
    private static readonly int PlayerCenteredFogStartRadiusId = Shader.PropertyToID("_PlayerCenteredFogStartRadius");
    private static readonly int PlayerCenteredFogEndRadiusId = Shader.PropertyToID("_PlayerCenteredFogEndRadius");
    private static readonly int PlayerCenteredFogBaseHeightOffsetId = Shader.PropertyToID("_PlayerCenteredFogBaseHeightOffset");
    private static readonly int PlayerCenteredFogHeightId = Shader.PropertyToID("_PlayerCenteredFogHeight");
    private static readonly int PlayerCenteredFogOpacityId = Shader.PropertyToID("_PlayerCenteredFogOpacity");
    private static readonly int PlayerCenteredFogNoiseScaleId = Shader.PropertyToID("_PlayerCenteredFogNoiseScale");
    private static readonly int PlayerCenteredFogNoiseStrengthId = Shader.PropertyToID("_PlayerCenteredFogNoiseStrength");
    private static readonly int PlayerCenteredFogNoiseSpeedId = Shader.PropertyToID("_PlayerCenteredFogNoiseSpeed");

    private Transform player;
    private CameraEffectStack effects;
    private PlayerCombatStatus combatStatus;
    private PlayerHeat playerHeat;
    private float combatWeight;
    private float combatVelocity;
    private float heatWeight;
    private float heatVelocity;
    private float coldWeight;
    private float coldVelocity;
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
        effects = stack;
        // Origem da nevoa retro no modo Player (RetroEnvironmentProfile).
        RetroLightSystem.FogFocus = playerTarget;
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
        UpdatePlayerCenteredFog();
    }

    private void OnDisable()
    {
        if (effects != null)
        {
            effects.RemoveModifier(CombatModifierId);
            effects.RemoveModifier(HeatModifierId);
            effects.RemoveModifier(ColdModifierId);
        }
        Shader.SetGlobalFloat(HeatPeripheralBlurId, 0f);
        Shader.SetGlobalFloat(PlayerCenteredFogEnabledId, 0f);
    }

    private void OnValidate()
    {
        if (positiveHeatEffectsCurve == null ||
            positiveHeatEffectsCurve.length == 0)
        {
            positiveHeatEffectsCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
        if (negativeHeatEffectsCurve == null ||
            negativeHeatEffectsCurve.length == 0)
        {
            negativeHeatEffectsCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
        playerCenteredFogEndRadius = Mathf.Max(playerCenteredFogStartRadius + .01f,
            playerCenteredFogEndRadius);
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

        float maximumCold = playerHeat != null && playerHeat.IsHeatModeActive
            ? Mathf.Max(0f, -playerHeat.MinHeat) : 0f;
        float normalizedCold = maximumCold > 0f
            ? Mathf.Clamp01(-playerHeat.CurrentHeat / maximumCold) : 0f;
        float coldTargetWeight = Mathf.Clamp01(negativeHeatEffectsCurve != null
            ? negativeHeatEffectsCurve.Evaluate(normalizedCold)
            : normalizedCold);
        coldWeight = Damp(coldWeight, coldTargetWeight,
            ref coldVelocity, coldBlendTime);
        CameraModifier coldModifier = CameraModifier.Identity;
        coldModifier.distanceOffset = maximumColdDistanceOffset * coldWeight;
        coldModifier.fieldOfViewOffset =
            maximumColdFieldOfViewOffset * coldWeight;
        coldModifier.rollOffset = maximumColdRollOffset * coldWeight;
        effects.SetModifier(ColdModifierId, coldModifier);
        ApplyThermalColorGrading();
    }

    private void UpdatePlayerCenteredFog()
    {
        Shader.SetGlobalFloat(PlayerCenteredFogEnabledId,
            playerCenteredFogEnabled ? 1f : 0f);
        Shader.SetGlobalColor(PlayerCenteredFogColorId, playerCenteredFogColor);
        Shader.SetGlobalVector(PlayerCenteredFogPlayerPositionId,
            player.position);
        Shader.SetGlobalFloat(PlayerCenteredFogStartRadiusId,
            Mathf.Max(0f, playerCenteredFogStartRadius));
        Shader.SetGlobalFloat(PlayerCenteredFogEndRadiusId,
            Mathf.Max(playerCenteredFogStartRadius + .001f,
                playerCenteredFogEndRadius));
        Shader.SetGlobalFloat(PlayerCenteredFogBaseHeightOffsetId,
            playerCenteredFogBaseHeightOffset);
        Shader.SetGlobalFloat(PlayerCenteredFogHeightId,
            Mathf.Max(.001f, playerCenteredFogHeight));
        Shader.SetGlobalFloat(PlayerCenteredFogOpacityId,
            Mathf.Clamp01(playerCenteredFogOpacity));
        Shader.SetGlobalFloat(PlayerCenteredFogNoiseScaleId,
            Mathf.Max(.001f, playerCenteredFogNoiseScale));
        Shader.SetGlobalFloat(PlayerCenteredFogNoiseStrengthId,
            Mathf.Clamp01(playerCenteredFogNoiseStrength));
        Shader.SetGlobalFloat(PlayerCenteredFogNoiseSpeedId,
            Mathf.Max(0f, playerCenteredFogNoiseSpeed));
    }

    private void ApplyThermalColorGrading()
    {
        if (whiteBalance == null || colorAdjustments == null)
            return;

        // Os pesos ja foram calculados pelas mesmas curvas e pelos mesmos
        // tempos de blend usados para distancia, FOV e Roll da camera.
        whiteBalance.temperature.value = baseTemperature +
            maximumHeatTemperature * heatWeight +
            maximumColdTemperature * coldWeight;
        colorAdjustments.saturation.value = baseSaturation +
            maximumHeatSaturation * heatWeight +
            maximumColdSaturation * coldWeight;
        colorAdjustments.contrast.value = baseContrast +
            maximumHeatContrast * heatWeight +
            maximumColdContrast * coldWeight;
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
