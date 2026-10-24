using UnityEngine;

/// <summary>Particula visual com fisica limitada ao mundo configurado.</summary>
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public sealed class CombatVisualParticle : MonoBehaviour
{
    private Rigidbody body;
    private float expiresAt;
    private CombatParticleProfile profile;
    private float noiseSeed;

    public bool HasExpired => Time.time >= expiresAt;

    /// <summary>Encerra a particula para que o pool a recicle no proximo frame.</summary>
    public void ExpireNow()
    {
        expiresAt = 0f;
    }

    public void Initialize(CombatParticleProfile newProfile, Vector3 position,
        Vector3 initialVelocity, float size, Material material, int particleLayer,
        int currencyValue = 0, Material currencyMaterial = null,
        float currencyLifetimeMultiplier = 1f, float minimumCurrencyLifetime = 0f)
    {
        profile = newProfile;
        gameObject.layer = particleLayer;
        transform.SetPositionAndRotation(position, Random.rotation);
        transform.localScale = Vector3.one * size;
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = currencyValue > 0 && currencyMaterial != null
                ? currencyMaterial : material;
        body ??= GetComponent<Rigidbody>();
        body.mass = profile.mass;
        body.linearDamping = profile.linearDamping;
        body.angularDamping = profile.angularDamping;
        body.useGravity = profile.kind != CombatParticleKind.Cotton;
        // A ativacao de um Rigidbody reutilizado pode limpar sua velocidade.
        // Ativa primeiro e aplica o impulso baseado no dano em seguida.
        gameObject.SetActive(true);
        body.linearVelocity = initialVelocity;
        body.angularVelocity = Random.insideUnitSphere * 8f;
        body.WakeUp();
        float lifetime = currencyValue > 0
            ? Mathf.Max(minimumCurrencyLifetime,
                profile.lifetime * currencyLifetimeMultiplier)
            : profile.lifetime;
        expiresAt = Time.time + lifetime;
        noiseSeed = Random.value * 1000f;
        GetComponent<CurrencyGorePickup>()?.Initialize(currencyValue);
    }

    private void FixedUpdate()
    {
        if (profile == null || profile.kind != CombatParticleKind.Cotton) return;
        float time = Time.time;
        Vector3 wind = new(Mathf.PerlinNoise(noiseSeed, time * .35f) * 2f - 1f,
            0f, Mathf.PerlinNoise(noiseSeed + 41f, time * .35f) * 2f - 1f);
        body.AddForce(Physics.gravity * profile.cottonGravityMultiplier +
            wind * profile.cottonWindForce, ForceMode.Acceleration);
    }
}
