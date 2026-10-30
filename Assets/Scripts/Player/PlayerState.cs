using System.Collections.Generic;
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
        BurnPreExplosion = 10,
        Rage = 11,
        HeavyStagger = 12,
        Swinging = 13,
        GrapplePulling = 14
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

    private Material[] debugMaterials = System.Array.Empty<Material>();
    private PlayerHeat playerHeat;

    [SerializeField]
    private State currentState = State.Normal;

    public State CurrentState => currentState;

    public bool IsNormal() => currentState == State.Normal || currentState == State.Rage;
    public bool IsAttacking() => currentState == State.Attacking;
    public bool IsDashing() => currentState == State.Dashing;
    public bool IsSliding() => currentState == State.Sliding;
    public bool IsStaggered() => currentState == State.Stagger ||
        currentState == State.HeavyStagger;
    public bool IsHeavyStaggered() => currentState == State.HeavyStagger;
    public bool IsSwinging() => currentState == State.Swinging;
    public bool IsGrapplePulling() => currentState == State.GrapplePulling;
    public bool IsBurnStunned() => currentState == State.BurnStunned;
    public bool IsBurning() => currentState == State.Burning;
    public bool IsBurnPreExplosion() => currentState == State.BurnPreExplosion;
    public bool IsBurnWalking() => IsBurnPreExplosion() || IsBurning();
    public bool IsDead() => currentState == State.Dead;

    public void ChangeState(State newState)
    {
        if (currentState == State.Dead)
            return;

        if (newState == State.Normal && playerHeat != null && playerHeat.IsRaging)
            newState = State.Rage;

        if (currentState == newState)
            return;

        currentState = newState;
        UpdateStateColor();
    }

    /// <summary>Forca o estado inicial ao iniciar uma campanha nova, inclusive apos morte permanente.</summary>
    public void ResetForNewGame()
    {
        currentState = State.Normal;
        UpdateStateColor();
    }

    void Awake()
    {
        playerHeat = GetComponent<PlayerHeat>();
        CacheDebugMaterials();

        UpdateStateColor();
    }

    private void CacheDebugMaterials()
    {
        List<Material> materials = new();
        if (playerRenderer != null)
            AddRendererMaterials(playerRenderer, materials);

        foreach (Renderer childRenderer in GetComponentsInChildren<Renderer>(true))
        {
            if (childRenderer != playerRenderer)
                AddRendererMaterials(childRenderer, materials);
        }

        debugMaterials = materials.ToArray();
    }

    private static void AddRendererMaterials(Renderer target,
        List<Material> materials)
    {
        if (target == null)
            return;

        foreach (Material material in target.materials)
        {
            if (material != null)
                materials.Add(material);
        }
    }

    private void UpdateStateColor()
    {
        if (debugMaterials.Length == 0)
            return;

        Color stateColor = normalColor;
        switch (currentState)
        {
            case State.Normal:
            case State.Rage:
            case State.Swinging:
            case State.GrapplePulling:
                break;

            case State.Attacking:
                stateColor = attackingColor;
                break;

            case State.Dashing:
                stateColor = dashingColor;
                break;

            case State.Sliding:
                stateColor = slidingColor;
                break;

            case State.Stagger:
            case State.HeavyStagger:
                stateColor = staggerColor;
                break;

            case State.BurnStunned:
                stateColor = burnStunnedColor;
                break;

            case State.Burning:
                stateColor = burningColor;
                break;

            case State.BurnPreExplosion:
                stateColor = burnPreExplosionColor;
                break;

            case State.Dead:
                stateColor = deadColor;
                break;
        }

        foreach (Material material in debugMaterials)
        {
            if (material != null)
                material.color = stateColor;
        }
    }
}
