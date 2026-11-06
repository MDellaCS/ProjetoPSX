using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Luzes locais retro (Point/Spot) em jogadores, inimigos e objetos. Nao cria
/// componentes Light: registra as luzes do perfil no RetroLightSystem, lidas pelo
/// shader "Projeto PSX/Retro Lit". Funciona tambem no Editor, sem salvar nada na cena.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Projeto PSX/World/Local Light Emitter")]
[ExecuteAlways]
public sealed class LocalLightEmitter : MonoBehaviour
{
    [Tooltip("Perfil com as luzes deste emissor. Sem perfil, nenhuma luz e criada.")]
    [SerializeField] private LocalLightProfile profile;
    [Tooltip("Deslocamento local aplicado a todas as luzes do perfil, em metros.")]
    [SerializeField] private Vector3 positionOffset;
    [Tooltip("Rotacao local em graus aplicada ao conjunto de luzes do perfil.")]
    [SerializeField] private Vector3 rotationOffset;
    [Tooltip("Multiplica a intensidade de todas as luzes deste emissor (inclusive min/max do flicker). Use para variar objetos que compartilham o mesmo perfil.")]
    [Min(0f)] [SerializeField] private float intensityMultiplier = 1f;
    [Tooltip("Mostra o flicker tambem fora do Play, no Editor.")]
    [SerializeField] private bool previewFlickerInEditor;

    private readonly List<RetroLight> lights = new();
    private LocalLightProfile appliedProfile;
    private int appliedRevision = -1;
    private bool settingsDirty = true;
    private bool hasFlicker;
    private float phase;

    public LocalLightProfile Profile
    {
        get => profile;
        set
        {
            profile = value;
            settingsDirty = true;
        }
    }

    public float IntensityMultiplier
    {
        get => intensityMultiplier;
        set
        {
            intensityMultiplier = Mathf.Max(0f, value);
            settingsDirty = true;
        }
    }

    private void OnEnable()
    {
        phase = (Mathf.Abs(GetHashCode()) % 997) * .731f;
        Rebuild();
        RegisterLights();
    }

    private void OnDisable()
    {
        for (int i = 0; i < lights.Count; i++)
            RetroLightSystem.Unregister(lights[i]);
    }

    // OnValidate pode vir da thread de importacao: a reconstrucao fica para o Update.
    private void OnValidate() => settingsDirty = true;

    internal void RegisterLights()
    {
        if (!isActiveAndEnabled)
            return;
        for (int i = 0; i < lights.Count; i++)
            RetroLightSystem.Register(lights[i]);
    }

    private void Update()
    {
        if (settingsDirty || appliedProfile != profile ||
            (profile != null && appliedRevision != profile.Revision))
        {
            Rebuild();
            RegisterLights();
        }

        if (!hasFlicker || (!Application.isPlaying && !previewFlickerInEditor))
            return;

        float time = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartupAsDouble;
        for (int i = 0; i < lights.Count; i++)
        {
            LocalLightDefinition definition = profile.lights[i];
            if (definition == null || !definition.enabled || !definition.flicker.enabled)
                continue;
            ApplyFlicker(lights[i], definition, time, phase + i * 3.17f);
        }
        if (!Application.isPlaying)
            UnityEditorRepaint();
    }

    private void ApplyFlicker(RetroLight light, LocalLightDefinition definition, float time, float lightPhase)
    {
        LocalLightFlicker flicker = definition.flicker;
        float t = flicker.Evaluate(time, lightPhase);
        light.intensity = Mathf.Lerp(flicker.minimumIntensity, flicker.maximumIntensity, t) * intensityMultiplier;
        light.range = definition.range * (1f - flicker.rangeVariation * (1f - t));
    }

    private void Rebuild()
    {
        settingsDirty = false;
        appliedProfile = profile;
        appliedRevision = profile != null ? profile.Revision : -1;
        hasFlicker = false;

        int count = profile != null && profile.lights != null ? profile.lights.Count : 0;
        while (lights.Count > count)
        {
            RetroLightSystem.Unregister(lights[^1]);
            lights.RemoveAt(lights.Count - 1);
        }
        while (lights.Count < count)
            lights.Add(new RetroLight());

        Quaternion offsetRotation = Quaternion.Euler(rotationOffset);
        for (int i = 0; i < count; i++)
        {
            LocalLightDefinition definition = profile.lights[i];
            RetroLight light = lights[i];
            light.owner = transform;
            if (definition == null)
            {
                light.enabled = false;
                continue;
            }

            definition.Validate();
            light.enabled = definition.enabled;
            light.spot = definition.shape == LocalLightShape.Spot;
            light.localPosition = positionOffset + offsetRotation * definition.localPosition;
            light.localRotation = offsetRotation * Quaternion.Euler(definition.localEulerAngles);
            light.color = definition.color;
            light.intensity = definition.intensity * intensityMultiplier;
            light.range = definition.range;
            light.coreRadius = definition.coreRadius;
            light.falloffExponent = definition.falloffExponent;
            light.bands = definition.bands;
            light.wrap = definition.wrap;
            light.spotAngle = definition.spotAngle;
            light.innerSpotAngle = definition.innerSpotAngle;

            if (definition.enabled && definition.flicker.enabled)
            {
                hasFlicker = true;
                // Comeca dentro da faixa min-max mesmo antes do primeiro Update.
                ApplyFlicker(light, definition, 0f, phase + i * 3.17f);
            }
        }
    }

    private static void UnityEditorRepaint()
    {
#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !InteractionGizmos.Visible)
            return;
        DrawLightGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
            return;
        DrawLightGizmos();
    }

    private void DrawLightGizmos()
    {
        if (profile == null || profile.lights == null)
            return;

        Color previousColor = Gizmos.color;
        Quaternion offsetRotation = Quaternion.Euler(rotationOffset);
        for (int i = 0; i < profile.lights.Count; i++)
        {
            LocalLightDefinition definition = profile.lights[i];
            if (definition == null || !definition.enabled)
                continue;

            Vector3 position = transform.TransformPoint(positionOffset + offsetRotation * definition.localPosition);
            Quaternion rotation = transform.rotation * offsetRotation * Quaternion.Euler(definition.localEulerAngles);
            Color lightColor = definition.color;
            float range = i < lights.Count ? lights[i].range : definition.range;

            Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, .9f);
            Gizmos.DrawSphere(position, .08f);
            if (definition.shape == LocalLightShape.Spot)
            {
                DrawSpot(position, rotation, range, definition.spotAngle);
                Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, .35f);
                DrawSpot(position, rotation, range, definition.innerSpotAngle);
            }
            else
            {
                Gizmos.DrawWireSphere(position, range);
                // Aneis das faixas (ou do nucleo, sem faixas) para ver onde a luz muda.
                Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, .35f);
                if (definition.bands > 0)
                    for (int band = 1; band < definition.bands; band++)
                        Gizmos.DrawWireSphere(position, range * RetroLightMath.NormalizedDistanceFor(
                            band / (float)definition.bands, definition.coreRadius, definition.falloffExponent));
                else if (definition.coreRadius > 0f)
                    Gizmos.DrawWireSphere(position, range * definition.coreRadius);
            }
        }
        Gizmos.color = previousColor;
    }

    private static void DrawSpot(Vector3 origin, Quaternion rotation, float range, float angle)
    {
        Vector3 forward = rotation * Vector3.forward;
        Vector3 right = rotation * Vector3.right;
        Vector3 up = rotation * Vector3.up;
        float halfAngle = Mathf.Clamp(angle, 1f, 179f) * .5f * Mathf.Deg2Rad;
        float radius = Mathf.Sin(halfAngle) * range;
        Vector3 end = origin + forward * (Mathf.Cos(halfAngle) * range);
        const int segments = 24;
        Vector3 previous = end + right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float radians = i * (Mathf.PI * 2f / segments);
            Vector3 next = end + (right * Mathf.Cos(radians) + up * Mathf.Sin(radians)) * radius;
            Gizmos.DrawLine(previous, next);
            if (i % 6 == 0)
                Gizmos.DrawLine(origin, next);
            previous = next;
        }
    }
}
