using UnityEngine;

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

    internal void Initialize(Transform playerTarget, CameraEffectStack stack)
    {
        player = playerTarget;
        effects = stack;
        ResolvePlayerComponents();
    }

    private void Update()
    {
        if (player == null || effects == null)
            return;
        ResolvePlayerComponents();
        UpdateCombatEffect();
        UpdateHeatEffect();
    }

    private void OnDisable()
    {
        effects?.RemoveModifier(CombatModifierId);
        effects?.RemoveModifier(HeatModifierId);
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
        float normalizedHeat = playerHeat != null && playerHeat.MaxHeat > 0f
            ? Mathf.Clamp01(playerHeat.CurrentHeat / playerHeat.MaxHeat) : 0f;
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
