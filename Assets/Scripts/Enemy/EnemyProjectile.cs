using UnityEngine;

/// <summary>
/// Projetil simples de inimigo. Move por codigo e usa SphereCast para acertar
/// player e cenario sem depender de Rigidbody ou Collider no prefab visual.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    [Header("Gizmo Fallback")]
    [Min(0.01f)] [SerializeField] private float defaultHitRadius = 0.25f;

    private Transform owner;
    private Vector3 direction;
    private int damage;
    private float knockbackForce;
    private float knockbackDuration;
    private float speed;
    private float hitRadius;
    private float lifetime;

    public void Launch(
        Transform projectileOwner,
        Vector3 travelDirection,
        int projectileDamage,
        float projectileKnockbackForce,
        float projectileKnockbackDuration,
        float projectileSpeed,
        float projectileHitRadius,
        float projectileLifetime)
    {
        owner = projectileOwner;
        direction = travelDirection.normalized;
        damage = projectileDamage;
        knockbackForce = projectileKnockbackForce;
        knockbackDuration = projectileKnockbackDuration;
        speed = Mathf.Max(0f, projectileSpeed);
        hitRadius = Mathf.Max(0.01f, projectileHitRadius);
        lifetime = Mathf.Max(0f, projectileLifetime);
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float distance = speed * Time.deltaTime;
        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,
            hitRadius,
            direction,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        RaycastHit? firstValidHit = GetFirstValidHit(hits);
        if (firstValidHit.HasValue)
        {
            Impact(firstValidHit.Value.collider);
            return;
        }

        transform.position += direction * distance;
    }

    private RaycastHit? GetFirstValidHit(RaycastHit[] hits)
    {
        RaycastHit? firstHit = null;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || IsIgnored(hit.collider.transform))
                continue;

            if (!firstHit.HasValue || hit.distance < firstHit.Value.distance)
                firstHit = hit;
        }

        return firstHit;
    }

    private bool IsIgnored(Transform other)
    {
        bool isOwner = owner != null &&
            (other == owner || other.IsChildOf(owner));
        bool isProjectile = other == transform || other.IsChildOf(transform);

        return isOwner || isProjectile;
    }

    private void Impact(Collider hit)
    {
        PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();
        if (player != null)
        {
            Vector3 attackerPosition = owner != null
                ? owner.position
                : transform.position;
            player.TakeDamage(
                damage,
                attackerPosition,
                knockbackForce,
                knockbackDuration,
                true
            );
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, defaultHitRadius);
    }
}
