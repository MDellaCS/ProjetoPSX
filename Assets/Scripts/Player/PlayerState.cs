using UnityEngine;

public class PlayerState : MonoBehaviour
{
    public enum State
    {
        Normal,
        Attacking,
        Dodging,
        Stagger,
        Dead,
        Blocking,
        GuardBroken
    }

    [Header("Debug - State Colors")]
    [SerializeField] private Renderer playerRenderer;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color blockingColor = Color.blue;
    [SerializeField] private Color guardBrokenColor = Color.red;
    [SerializeField] private Color attackingColor = Color.yellow;
    [SerializeField] private Color dodgingColor = Color.cyan;
    [SerializeField] private Color staggerColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color deadColor = Color.gray;

    private Material debugMaterial;

    [SerializeField]
    private State currentState = State.Normal;

    public State CurrentState => currentState;

    public bool IsNormal() => currentState == State.Normal;
    public bool IsAttacking() => currentState == State.Attacking;
    public bool IsDodging() => currentState == State.Dodging;
    public bool IsStaggered() => currentState == State.Stagger;
    public bool IsDead() => currentState == State.Dead;
    public bool IsBlocking() => currentState == State.Blocking;
    public bool IsGuardBroken() => currentState == State.GuardBroken;

    public void ChangeState(State newState)
    {
        if (currentState == State.Dead)
            return;

        // Durante Guard Break, só permite voltar ao normal ou morrer. A liberação é controlada pelo PlayerGuard.
        if (currentState == State.GuardBroken &&
            newState != State.Normal &&
            newState != State.Dead)
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

            case State.Blocking:
                debugMaterial.color = blockingColor;
                break;

            case State.GuardBroken:
                debugMaterial.color = guardBrokenColor;
                break;

            case State.Attacking:
                debugMaterial.color = attackingColor;
                break;

            case State.Dodging:
                debugMaterial.color = dodgingColor;
                break;

            case State.Stagger:
                debugMaterial.color = staggerColor;
                break;

            case State.Dead:
                debugMaterial.color = deadColor;
                break;
        }
    }
}