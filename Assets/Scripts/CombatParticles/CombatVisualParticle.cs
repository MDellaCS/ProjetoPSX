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

    public void Initialize(CombatParticleProfile newProfile, Vector3 position,
        Vector3 initialVelocity, float size, Material material, int particleLayer)
    {
        profile = newProfile;
        gameObject.layer = particleLayer;
        transform.SetPositionAndRotation(position, Random.rotation);
        transform.localScale = Vector3.one * size;
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
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
        expiresAt = Time.time + profile.lifetime;
        noiseSeed = Random.value * 1000f;
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
