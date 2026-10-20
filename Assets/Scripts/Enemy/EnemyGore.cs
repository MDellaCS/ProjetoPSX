using UnityEngine;

/// <summary>Configuracao opcional de particulas para um inimigo.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Projeto PSX/Enemy Gore")]
public sealed class EnemyGore : MonoBehaviour
{
    [Header("Particle Profiles")]
    [Tooltip("Perfil emitido quando este inimigo recebe dano. Vazio usa o perfil global de impacto.")]
    [SerializeField] private CombatParticleProfile damageProfile;
    [Tooltip("Perfil fisico emitido quando este inimigo morre. Vazio usa o perfil global de gore mecanico.")]
    [SerializeField] private CombatParticleProfile deathProfile;

    public CombatParticleProfile DamageProfile => damageProfile;
    public CombatParticleProfile DeathProfile => deathProfile;

    /// <summary>Usado apenas pelo Enemy Death Loop Test.</summary>
    public void SpawnTestGore(int lethalDamage, Vector3 impactDirection)
    {
        Renderer source = GetComponentInChildren<Renderer>(true);
        Vector3 position = source != null ? source.bounds.center : transform.position;
        CombatParticleSystem.Instance.EmitTestDeath(deathProfile, position,
            impactDirection, lethalDamage, source);
    }
}
