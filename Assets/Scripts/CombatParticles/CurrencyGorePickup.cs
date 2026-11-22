using UnityEngine;

/// <summary>Transforma uma particula visual de gore em uma coleta fisica de currency.</summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class CurrencyGorePickup : MonoBehaviour
{
    private Rigidbody body;
    private int value;
    private bool vitalOil;
    private float collectableAt;
    private float attractionElapsed;
    private PlayerCurrencyWallet wallet;
    public bool HasCurrency => value > 0;

    public void Initialize(int newValue, bool isVitalOil = false)
    {
        value = Mathf.Max(0, newValue);
        vitalOil = isVitalOil;
        attractionElapsed = 0f;
        wallet = value > 0 ? PlayerCurrencyWallet.FindOrCreate() : null;
        collectableAt = Time.time + (wallet != null ? wallet.AttractionDelay : 0f);
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (value <= 0 || Time.time < collectableAt)
            return;

        if (wallet == null)
            wallet = PlayerCurrencyWallet.FindOrCreate();
        if (wallet == null)
            return;

        Vector3 target = wallet.transform.position + Vector3.up *
            wallet.PlayerCurrencyCenterHeightOffset;
        Vector3 toPlayer = target - transform.position;
        float distance = toPlayer.magnitude;
        if (distance <= wallet.CollectRadius)
        {
            if (vitalOil)
            {
                PlayerHealth health = wallet.GetComponent<PlayerHealth>();
                PlayerMovesetController moveset = wallet.GetComponent<PlayerMovesetController>();
                if (health != null && (moveset == null || moveset.UsesHeat))
                    health.RestoreVitalOil(value);
            }
            else wallet.Add(value);
            value = 0;
            // O pickup deixa de existir visualmente no mesmo frame e o
            // CombatParticleSystem o devolve ao pool no Update seguinte.
            if (TryGetComponent(out CombatVisualParticle visual))
                visual.ExpireNow();
            gameObject.SetActive(false);
            return;
        }

        if (distance <= .0001f)
            return;

        if (distance > wallet.AttractionRadius)
            return;

        if (body == null)
            body = GetComponent<Rigidbody>();
        attractionElapsed += Time.deltaTime;
        float acceleration = wallet.EvaluateAttractionAcceleration(
            attractionElapsed);
        body.AddForce(toPlayer / distance * acceleration,
            ForceMode.Acceleration);
        if (body.linearVelocity.magnitude > wallet.MaximumAttractionSpeed)
            body.linearVelocity = body.linearVelocity.normalized *
                wallet.MaximumAttractionSpeed;
    }
}
