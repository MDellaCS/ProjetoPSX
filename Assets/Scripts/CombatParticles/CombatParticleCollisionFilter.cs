using UnityEngine;

/// <summary>Limita particulas visuais ao chao e paredes configurados.</summary>
[RequireComponent(typeof(Collider))]
public sealed class CombatParticleCollisionFilter : MonoBehaviour
{
    private Collider ownCollider;
    private void Awake() => ownCollider = GetComponent<Collider>();

    private void OnEnable()
    {
        if (ownCollider == null)
            ownCollider = GetComponent<Collider>();
        if (ownCollider != null)
            ownCollider.gameObject.layer = LayerMask.NameToLayer("CombatParticle");
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider other = collision != null ? collision.collider : null;
        if (other == null || CombatParticleSystem.Instance == null ||
            CombatParticleSystem.Instance.CanParticleCollideWith(other)) return;
        Physics.IgnoreCollision(ownCollider, other, true);
    }
}
