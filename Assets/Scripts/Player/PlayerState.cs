using UnityEngine;

public class PlayerState : MonoBehaviour
{
    public enum State
    {
        Normal,
        Attacking,
        Dodging,
        Stagger,
        Dead
    }

    [SerializeField]
    private State currentState = State.Normal;

    public State CurrentState => currentState;

    public bool IsNormal()
    {
        return currentState == State.Normal;
    }

    public bool IsAttacking()
    {
        return currentState == State.Attacking;
    }

    public bool IsDodging()
    {
        return currentState == State.Dodging;
    }

    public bool IsStaggered()
    {
        return currentState == State.Stagger;
    }

    public bool IsDead()
    {
        return currentState == State.Dead;
    }

    public void ChangeState(State newState)
    {
        // Morte é um estado terminal
        if (currentState == State.Dead)
            return;

        // Evita transições redundantes
        if (currentState == newState)
            return;

        currentState = newState;
    }
}