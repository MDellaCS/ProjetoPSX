using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adaptador opcional entre PlayerAnimationPresenter e Unity Animator.
/// Ele recebe somente intencoes visuais; Animator, clips e root motion nunca
/// definem movimento, combate ou transicoes de gameplay.
/// </summary>
[DisallowMultipleComponent]
public class PlayerAnimatorPlayback : MonoBehaviour, IPlayerAnimationPlayback
{
    [Serializable]
    public struct StateBinding
    {
        public PlayerVisualState visualState;

        [Tooltip("Caminho completo do estado no Animator. Ex.: Base Layer.Idle")]
        public string animatorStatePath;
    }

    [Header("Optional Animator")]
    [SerializeField] private Animator animator;
    [Min(0)] [SerializeField] private int layerIndex;
    [Min(0f)] [SerializeField] private float transitionDuration = .08f;
    [Tooltip("Mantem a simulacao como autoridade sobre a posicao e rotacao.")]
    [SerializeField] private bool disableRootMotion = true;

    [Header("Visual State Bindings")]
    [SerializeField] private StateBinding[] stateBindings =
    {
        new StateBinding { visualState = PlayerVisualState.Idle, animatorStatePath = "Base Layer.Idle" },
        new StateBinding { visualState = PlayerVisualState.Move, animatorStatePath = "Base Layer.Move" },
        new StateBinding { visualState = PlayerVisualState.Crouch, animatorStatePath = "Base Layer.Crouch" },
        new StateBinding { visualState = PlayerVisualState.Jump, animatorStatePath = "Base Layer.Jump" },
        new StateBinding { visualState = PlayerVisualState.Fall, animatorStatePath = "Base Layer.Fall" },
        new StateBinding { visualState = PlayerVisualState.Dash, animatorStatePath = "Base Layer.Dash" },
        new StateBinding { visualState = PlayerVisualState.Slide, animatorStatePath = "Base Layer.Slide" },
        new StateBinding { visualState = PlayerVisualState.Attack, animatorStatePath = "Base Layer.Attack" },
        new StateBinding { visualState = PlayerVisualState.Stunned, animatorStatePath = "Base Layer.Stunned" },
        new StateBinding { visualState = PlayerVisualState.Dead, animatorStatePath = "Base Layer.Dead" }
    };

    private readonly Dictionary<PlayerVisualState, int> stateHashes = new();
    private bool hasPresentedState;
    private PlayerVisualState lastPresentedState;

    private void Awake()
    {
        ResolveAnimator();
        RebuildBindings();
        DisableRootMotion();
    }

    private void OnValidate()
    {
        ResolveAnimator();
        RebuildBindings();
        DisableRootMotion();
    }

    private void OnEnable()
    {
        // Caso o VisualRoot seja reativado durante uma acao, reaplica somente
        // a intencao atual. Isso continua sendo apresentacao, nao gameplay.
        if (!hasPresentedState)
            return;

        PlayerVisualState currentState = lastPresentedState;
        hasPresentedState = false;
        Present(currentState);
    }

    public void Present(PlayerVisualState visualState)
    {
        if (hasPresentedState && lastPresentedState == visualState)
            return;

        hasPresentedState = true;
        lastPresentedState = visualState;

        if (animator == null || !animator.isActiveAndEnabled ||
            !stateHashes.TryGetValue(visualState, out int stateHash) ||
            !animator.HasState(layerIndex, stateHash))
        {
            return;
        }

        animator.CrossFadeInFixedTime(
            stateHash,
            transitionDuration,
            layerIndex
        );
    }

    /// <summary>
    /// Reconstroi o mapa apos editar os bindings no Inspector. Seguro chamar
    /// mesmo sem Animator ou sem Animation Controller.
    /// </summary>
    public void RebuildBindings()
    {
        stateHashes.Clear();

        if (stateBindings == null)
            return;

        foreach (StateBinding binding in stateBindings)
        {
            if (string.IsNullOrWhiteSpace(binding.animatorStatePath))
                continue;

            stateHashes[binding.visualState] = Animator.StringToHash(
                binding.animatorStatePath
            );
        }
    }

    private void ResolveAnimator()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void DisableRootMotion()
    {
        if (animator != null && disableRootMotion)
            animator.applyRootMotion = false;
    }
}
