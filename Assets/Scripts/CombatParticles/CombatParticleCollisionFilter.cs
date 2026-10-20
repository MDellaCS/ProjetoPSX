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
        foreach (Collider other in FindObjectsByType<Collider>(FindObjectsInactive.Exclude))
        {
            if (other == null || other == ownCollider) continue;
            Physics.IgnoreCollision(ownCollider, other, !CombatParticleSystem.Instance.IsWorldCollision(other));
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider other = collision != null ? collision.collider : null;
        if (other == null || CombatParticleSystem.Instance.IsWorldCollision(other)) return;
        Physics.IgnoreCollision(ownCollider, other, true);
    }
}
