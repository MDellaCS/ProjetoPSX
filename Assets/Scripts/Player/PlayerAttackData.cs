using UnityEngine;

[System.Serializable]
public class PlayerAttackData
{
    public string attackName;

    public int damage = 25;

    public float windupDuration = 0.1f;
    public float activeDuration = 0.15f;
    public float recoveryDuration = 0.25f;

    public float attackDistance = 1.5f;
    public float attackRadius = 1f;

    public int heatGenerated = 0;
    public int heatRemovedOnHit = 0;
    public int superheatRemovedOnHit = 0;
}