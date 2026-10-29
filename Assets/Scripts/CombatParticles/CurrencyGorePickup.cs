using UnityEngine;

/// <summary>Transforma uma particula visual de gore em uma coleta fisica de currency.</summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class CurrencyGorePickup : MonoBehaviour
{
    private Rigidbody body;
    private int value;
    private float collectableAt;
    private float attractionElapsed;
    private PlayerCurrencyWallet wallet;

    public void Initialize(int newValue)
    {
        value = Mathf.Max(0, newValue);
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
            wallet.Add(value);
            value = 0;
            // O pickup deixa de existir visualmente no mesmo frame e o
            // CombatParticleSystem o devolve ao pool no Update seguinte.
            GetComponent<CombatVisualParticle>()?.ExpireNow();
            gameObject.SetActive(false);
            return;
        }

        if (distance <= .0001f)
            return;

        if (distance > wallet.AttractionRadius)
            return;

        body ??= GetComponent<Rigidbody>();
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
