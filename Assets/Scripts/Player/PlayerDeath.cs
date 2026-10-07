using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Transform visual;

    [Header("Death Animation")]
    [SerializeField] private float fallDuration = 0.7f;
    [SerializeField] private float fallAngle = 85f;

    [Header("Fall Position")]
    [SerializeField] private float fallDrop = 0.5f;

    private Vector3 initialPosition;
    private Vector3 targetPosition;

    private PlayerState playerState;

    private bool deathStarted;
    private float deathTimer;

    private Quaternion initialRotation;
    private Quaternion targetRotation;

    void Awake()
    {
        playerState = GetComponent<PlayerState>();
    }

    void Update()
    {
        if (!playerState.IsDead())
            return;

        if (!deathStarted)
        {
            StartDeath();
        }

        AnimateDeath();
    }

    void StartDeath()
    {
        deathStarted = true;
        deathTimer = 0f;

        if (visual == null)
            return;

        initialRotation = visual.localRotation;

        initialPosition = visual.localPosition;
        targetPosition = initialPosition + Vector3.down * fallDrop;

        targetRotation = initialRotation * Quaternion.Euler(fallAngle, 0f, 0f);
    }

    void AnimateDeath()
    {
        if (visual == null)
            return;

        deathTimer += Time.deltaTime;

        float t = Mathf.Clamp01(
            deathTimer / Mathf.Max(0.01f, fallDuration)
        );

        // Começa rápido e desacelera ao atingir o chão.
        t = 1f - Mathf.Pow(1f - t, 3f);

        visual.localRotation = Quaternion.Slerp(
            initialRotation,
            targetRotation,
            t
        );

        visual.localPosition = Vector3.Lerp(
            initialPosition,
            targetPosition,
            t
        );
    }
}