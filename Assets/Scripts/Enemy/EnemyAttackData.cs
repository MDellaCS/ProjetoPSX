using UnityEngine;

[System.Serializable]
public class EnemyAttackData
{
    public string attackName;
    public int damage;
    public float minimumRange;
    public float maximumRange;
    public float windupDuration;
    public float activeDuration;
    public float recoveryDuration;
    public float hitboxDistance;
    public float hitboxRadius;
    public float knockbackForce;
    public float knockbackDuration;
    public bool movesForward;
    public float movementSpeed;

    [Header("Projectile")]
    public bool firesProjectile;
    public EnemyProjectile projectilePrefab;
    public Transform projectileSpawnPoint;
    public float projectileSpeed;
    public float projectileHitRadius;
    public float projectileLifetime;
}
