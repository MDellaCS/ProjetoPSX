using UnityEngine;

/// <summary>Limita particulas visuais ao chao e paredes configurados.</summary>
[RequireComponent(typeof(Collider))]
public sealed class CombatParticleCollisionFilter : MonoBehaviour
{
    private Collider ownCollider;
    private void Awake() => ownCollider = GetComponent<Collider>();

    private void OnEnable()
    {
        ownCollider ??= GetComponent<Collider>();
        CombatParticleSystem system = CombatParticleSystem.Instance;

        foreach (Collider other in FindObjectsByType<Collider>(FindObjectsInactive.Exclude))
        {
            if (other == null || other == ownCollider) continue;
            Physics.IgnoreCollision(ownCollider, other,
                !system.CanParticleCollideWith(other));
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider other = collision != null ? collision.collider : null;
        if (other == null ||
            CombatParticleSystem.Instance.CanParticleCollideWith(other)) return;
        Physics.IgnoreCollision(ownCollider, other, true);
    }
}
