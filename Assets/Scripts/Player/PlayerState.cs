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

    public State currentState = State.Normal;

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
        currentState = newState;
    }
}