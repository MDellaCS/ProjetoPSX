using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance;

    public Transform target;
    public Vector3 offset = new Vector3(0f, 7f, -8f);

    public float impactShakeDistance = 0.3f;
    public float impactShakeDuration = 0.08f;

    private Vector3 shakeDirection;
    private float shakeTimer;

    void Awake()
    {
        Instance = this;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 shakeOffset = Vector3.zero;

        if (shakeTimer > 0f)
        {
            float shakeStrength =
                shakeTimer / impactShakeDuration;

            shakeOffset =
                shakeDirection *
                impactShakeDistance *
                shakeStrength;

            shakeTimer -= Time.unscaledDeltaTime;
        }

        transform.position =
            target.position +
            offset +
            shakeOffset;

        transform.LookAt(target);
    }

    public void ImpactShake(Vector3 attackDirection)
    {
        attackDirection.y = 0f;
        attackDirection.Normalize();

        shakeDirection = -attackDirection;
        shakeTimer = impactShakeDuration;
    }
}