using UnityEngine;

/// <summary>
/// Projetil inimigo simples: segue em linha reta, sem gravidade ou curva.
/// A colisao e feita por SphereCast para funcionar mesmo em alta velocidade.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    [Min(.01f)] [SerializeField] private float gizmoRadius = .18f;

    private Transform owner;
    private Vector3 velocity;
    private int damage;
    private float knockbackForce;
    private float knockbackDuration;
    private float hitRadius;
    private float remainingLifetime;
    private bool launched;

    public void Launch(
        Transform projectileOwner,
        Vector3 direction,
        int projectileDamage,
        float projectileKnockbackForce,
        float projectileKnockbackDuration,
        float speed,
        float radius,
        float lifetime)
    {
        owner = projectileOwner;
        damage = projectileDamage;
        knockbackForce = projectileKnockbackForce;
        knockbackDuration = projectileKnockbackDuration;
        hitRadius = Mathf.Max(.01f, radius);
        remainingLifetime = Mathf.Max(.01f, lifetime);
        velocity = direction.sqrMagnitude > .001f
            ? direction.normalized * Mathf.Max(0f, speed)
            : transform.forward * Mathf.Max(0f, speed);
        launched = true;
    }

    private void Update()
    {
        if (!launched)
            return;

        float distance = velocity.magnitude * Time.deltaTime;
        if (distance > 0f && TryHit(distance))
            return;

        transform.position += velocity * Time.deltaTime;
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f)
            Destroy(gameObject);
    }

    private bool TryHit(float distance)
    {
        Vector3 direction = velocity.normalized;
        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,
            hitRadius,
            direction,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit hit in hits)
        {
            if (ShouldIgnore(hit.collider))
                continue;

            transform.position = hit.point;
            PlayerHealth playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(
                    damage,
                    owner != null ? owner.position : transform.position,
                    knockbackForce,
                    knockbackDuration,
                    true
                );
            }

            Destroy(gameObject);
            return true;
        }

        return false;
    }

    private bool ShouldIgnore(Collider hit)
    {
        if (hit == null)
            return true;

        Transform hitTransform = hit.transform;
        return hitTransform.IsChildOf(transform) ||
            transform.IsChildOf(hitTransform) ||
            hit.GetComponentInParent<BaseEnemy>() != null ||
            (owner != null &&
             (hitTransform.IsChildOf(owner) || owner.IsChildOf(hitTransform)));
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !PlayerDebugToolkitHUD.IsVisible)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, launched ? hitRadius : gizmoRadius);
    }
}
