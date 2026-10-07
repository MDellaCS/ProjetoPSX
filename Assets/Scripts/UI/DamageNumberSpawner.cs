using UnityEngine;

public class DamageNumberSpawner : MonoBehaviour
{
    [SerializeField] private DamageNumber damageNumberPrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 2f, 0f);

    public void ShowDamage(int damage)
    {
        if (damageNumberPrefab == null)
            return;

        Vector3 position = transform.position + spawnOffset;

        DamageNumber number = Instantiate(
            damageNumberPrefab,
            position,
            Quaternion.identity
        );

        number.Initialize(damage);
    }
}