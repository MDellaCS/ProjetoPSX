using UnityEngine;

public class DamageNumberSpawner : MonoBehaviour
{
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private Color damageColor = Color.white;

    public void ShowDamage(int damage)
    {
        DamageNumber.Show(transform, damage, spawnOffset, damageColor);
    }
}
