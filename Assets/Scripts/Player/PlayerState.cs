using UnityEngine;

public class PlayerState : MonoBehaviour
{
    public enum State
    {
        Normal = 0,
        Attacking = 1,
        Dashing = 2,
        Stagger = 3,
        Dead = 4,
        // Mantem o valor serializado do slide apos remover os estados defensivos.
        Sliding = 7,
        BurnStunned = 8,
        Burning = 9,
        BurnPreExplosion = 10
    }

    [Header("Debug - State Colors")]
    [SerializeField] private Renderer playerRenderer;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color attackingColor = Color.yellow;
    [SerializeField] private Color dashingColor = Color.cyan;
    [SerializeField] private Color slidingColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color staggerColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color burnStunnedColor = new Color(1f, 0.15f, 0f);
    [SerializeField] private Color burningColor = new Color(0.85f, 0.08f, 0f);
    [Tooltip("Cor de debug durante o periodo de Pre-Explosion do Burn.")]
    [SerializeField] private Color burnPreExplosionColor = new Color(1f, 0.45f, 0.05f);
    [SerializeField] private Color deadColor = Color.gray;

    private Material debugMaterial;

    [SerializeField]
    private State currentState = State.Normal;

    public State CurrentState => currentState;

    public bool IsNormal() => currentState == State.Normal;
    public bool IsAttacking() => currentState == State.Attacking;
    public bool IsDashing() => currentState == State.Dashing;
    public bool IsSliding() => currentState == State.Sliding;
    public bool IsStaggered() => currentState == State.Stagger;
    public bool IsBurnStunned() => currentState == State.BurnStunned;
    public bool IsBurning() => currentState == State.Burning;
    public bool IsBurnPreExplosion() => currentState == State.BurnPreExplosion;
    public bool IsBurnWalking() => IsBurnPreExplosion() || IsBurning();
    public bool IsDead() => currentState == State.Dead;

    public void ChangeState(State newState)
    {
        if (currentState == State.Dead)
            return;

        if (currentState == newState)
            return;

        currentState = newState;
        UpdateStateColor();
    }

    void Awake()
    {
        if (playerRenderer != null)
            debugMaterial = playerRenderer.material;

        UpdateStateColor();
    }

    private void UpdateStateColor()
    {
        if (debugMaterial == null)
            return;

        switch (currentState)
        {
            case State.Normal:
                debugMaterial.color = normalColor;
                break;

            case State.Attacking:
                debugMaterial.color = attackingColor;
                break;

            case State.Dashing:
                debugMaterial.color = dashingColor;
                break;

            case State.Sliding:
                debugMaterial.color = slidingColor;
                break;

            case State.Stagger:
                debugMaterial.color = staggerColor;
                break;

            case State.BurnStunned:
                debugMaterial.color = burnStunnedColor;
                break;

            case State.Burning:
                debugMaterial.color = burningColor;
                break;

            case State.BurnPreExplosion:
                debugMaterial.color = burnPreExplosionColor;
                break;

            case State.Dead:
                debugMaterial.color = deadColor;
                break;
        }
    }
}
