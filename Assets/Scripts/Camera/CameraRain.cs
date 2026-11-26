using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Emite chuva em espaco mundial num volume ao redor da camera de jogo.</summary>
[DisallowMultipleComponent]
public sealed class CameraRain : MonoBehaviour
{
    private static int revision;
    private ParticleSystem particles;
    private ParticleSystemRenderer particleRenderer;
    private RainProfile profile;
    private RainProfile configured;
    private int configuredRevision = -1;
    private float rate;
    private float rateVelocity;
    private Vector3 previousPosition;
    private bool hasPosition;
    private Material material;

    public static void ProfileChanged() => revision++;

    public void Initialize()
    {
        if (particles != null) return;
        material = Resources.Load<Material>("Weather/Rain");
        if (material == null)
        {
            Debug.LogWarning("CameraRain: material Resources/Weather/Rain nao encontrado.", this);
            enabled = false;
            return;
        }
        var root = new GameObject("Rain Particles");
        root.transform.SetParent(transform, false);
        particles = root.AddComponent<ParticleSystem>();
        particleRenderer = root.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = material;
        particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        particleRenderer.cameraVelocityScale = 0f;
        particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;
        particleRenderer.lightProbeUsage = LightProbeUsage.Off;
        particleRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.gravityModifier = 0f;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        var fade = particles.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .12f), new GradientAlphaKey(1f, .8f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void LateUpdate()
    {
        if (particles == null) return;
        RetroEnvironmentProfile environment = RetroLightSystem.ActiveEnvironment;
        RainProfile next = environment != null ? environment.rainProfile : null;
        if (next != profile)
        {
            profile = next;
            if (profile != null) Configure(profile);
        }
        if (profile != null && (configured != profile || configuredRevision != revision)) Configure(profile);
        float desiredRate = profile != null && profile.enabled ? Mathf.Max(0f, profile.emissionRate) : 0f;
        float duration = profile != null ? Mathf.Max(0f, profile.transitionTime) : configured != null ? Mathf.Max(0f, configured.transitionTime) : 0f;
        rate = duration <= 0f ? desiredRate : Mathf.SmoothDamp(rate, desiredRate, ref rateVelocity, duration, Mathf.Infinity, Time.unscaledDeltaTime);
        if (Mathf.Abs(rate - desiredRate) < .01f) rate = desiredRate;
        var emission = particles.emission;
        emission.rateOverTime = rate;
        RainProfile active = profile != null ? profile : configured;
        if (active == null) return;
        Vector3 cameraPosition = transform.position;
        Vector3 delta = hasPosition ? cameraPosition - previousPosition : Vector3.zero;
        Vector3 lead = Vector3.ClampMagnitude(delta / Mathf.Max(Time.unscaledDeltaTime, .001f) * .1f, Mathf.Max(0f, active.movementLead));
        Vector3 center = cameraPosition + Vector3.up * active.heightOffset + lead;
        particles.transform.SetPositionAndRotation(center, Quaternion.identity);
        if (hasPosition && delta.sqrMagnitude > active.volumeSize.sqrMagnitude)
            particles.Clear(true);
        previousPosition = cameraPosition;
        hasPosition = true;
        if (rate > .01f && !particles.isPlaying) particles.Play();
        else if (rate <= .01f && desiredRate == 0f && particles.isPlaying)
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void Configure(RainProfile value)
    {
        configured = value;
        configuredRevision = revision;
        var main = particles.main;
        main.maxParticles = Mathf.Clamp(value.maxParticles, 1, 10000);
        main.startLifetime = new ParticleSystem.MinMaxCurve(Mathf.Max(.01f, value.lifetime.x), Mathf.Max(value.lifetime.x, value.lifetime.y));
        main.startSize = new ParticleSystem.MinMaxCurve(Mathf.Max(.001f, value.dropWidth.x), Mathf.Max(value.dropWidth.x, value.dropWidth.y));
        main.startColor = value.color;
        var shape = particles.shape;
        shape.scale = new Vector3(Mathf.Max(.1f, value.volumeSize.x), Mathf.Max(.1f, value.volumeSize.y), Mathf.Max(.1f, value.volumeSize.z));
        float inclination = Mathf.Clamp(value.angle, 0f, 80f) * Mathf.Deg2Rad;
        float azimuth = value.direction * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Sin(inclination) * Mathf.Sin(azimuth), -Mathf.Cos(inclination), Mathf.Sin(inclination) * Mathf.Cos(azimuth));
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = direction.x;
        velocity.y = direction.y;
        velocity.z = direction.z;
        velocity.speedModifier = new ParticleSystem.MinMaxCurve(Mathf.Max(.01f, value.speed.x), Mathf.Max(value.speed.x, value.speed.y));
        particleRenderer.lengthScale = Mathf.Max(.01f, value.lengthScale);
        particleRenderer.velocityScale = Mathf.Max(0f, value.velocityStretch);
        particleRenderer.freeformStretching = value.freeformStretching;
    }

    private void OnDisable()
    {
        if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        rate = rateVelocity = 0f;
        hasPosition = false;
    }
}
