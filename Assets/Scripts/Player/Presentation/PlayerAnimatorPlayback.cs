using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adaptador opcional entre PlayerAnimationPresenter e Unity Animator.
/// Ele recebe somente intencoes visuais; Animator, clips e root motion nunca
/// definem movimento, combate ou transicoes de gameplay.
/// </summary>
[DisallowMultipleComponent]
public class PlayerAnimatorPlayback : MonoBehaviour, IPlayerAnimationPlayback,
    IPlayerAttackAnimationPlayback, IPlayerMovementAnimationPlayback
{
    [Serializable]
    public struct StateBinding
    {
        public PlayerVisualState visualState;

        [Tooltip("Caminho completo do estado no Animator. Ex.: Base Layer.Idle")]
        public string animatorStatePath;
    }

    [Serializable]
    public struct AttackBinding
    {
        [Tooltip("Deve ser igual ao Attack Name configurado no moveset. Ex.: L1")]
        public string attackId;

        [Tooltip("Caminho completo do estado no Animator. Ex.: Base Layer.L1")]
        public string animatorStatePath;

        [Header("Regioes normalizadas do clip")]
        [Range(0f, 1f)] public float windupStart;
        [Range(0f, 1f)] public float windupEnd;
        [Range(0f, 1f)] public float activeStart;
        [Range(0f, 1f)] public float activeEnd;
        [Range(0f, 1f)] public float recoveryStart;
        [Range(0f, 1f)] public float recoveryEnd;

        public float GetClipProgress(PlayerCombat.AttackPhase phase,
            float phaseProgress)
        {
            float start;
            float end;

            switch (phase)
            {
                case PlayerCombat.AttackPhase.Windup:
                    start = windupStart;
                    end = windupEnd;
                    break;
                case PlayerCombat.AttackPhase.Active:
                    start = activeStart;
                    end = activeEnd;
                    break;
                case PlayerCombat.AttackPhase.Recovery:
                    start = recoveryStart;
                    end = recoveryEnd;
                    break;
                default:
                    return 0f;
            }

            return Mathf.Lerp(
                Mathf.Clamp01(start),
                Mathf.Clamp01(end),
                Mathf.Clamp01(phaseProgress)
            );
        }
    }

    [Header("Optional Animator")]
    [SerializeField] private Animator animator;
    [Min(0)] [SerializeField] private int layerIndex;
    [Min(0f)] [SerializeField] private float transitionDuration = .08f;
    [Tooltip("Mantem a simulacao como autoridade sobre a posicao e rotacao.")]
    [SerializeField] private bool disableRootMotion = true;

    [Header("Move Playback Speed")]
    [Tooltip("Velocidade real do player que reproduz o clip Move em 1x.")]
    [Min(.01f)] [SerializeField] private float moveSpeedReference = 7.5f;
    [Tooltip("Evita que o clip pare quase por completo durante pequenas variacoes de velocidade.")]
    [Min(0f)] [SerializeField] private float minimumMoveAnimationSpeed = .2f;
    [Min(.01f)] [SerializeField] private float maximumMoveAnimationSpeed = 2f;

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

    [Header("Attack Bindings")]
    [Tooltip("Dados puramente visuais. Adicione uma entrada para cada Attack Name que possuir clip proprio.")]
    [SerializeField] private AttackBinding[] attackBindings =
    {
        CreateDefaultAttackBinding("L1"),
        CreateDefaultAttackBinding("L2"),
        CreateDefaultAttackBinding("L3")
    };

    private readonly Dictionary<PlayerVisualState, int> stateHashes = new();
    private readonly Dictionary<string, AttackBinding> attackBindingMap =
        new(StringComparer.Ordinal);
    private bool hasPresentedState;
    private PlayerVisualState lastPresentedState;
    private bool hasPendingStateAfterHitstop;
    private PlayerVisualState pendingStateAfterHitstop;

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

    private void Update()
    {
        if (!hasPendingStateAfterHitstop ||
            CombatImpactSystem.IsPresentationFrozen(animator))
        {
            return;
        }

        PlayerVisualState pendingState = pendingStateAfterHitstop;
        hasPendingStateAfterHitstop = false;
        Present(pendingState);
    }

    private void OnDisable()
    {
        if (animator != null)
            animator.speed = 1f;
    }

    public void Present(PlayerVisualState visualState)
    {
        if (CombatImpactSystem.IsPresentationFrozen(animator))
        {
            pendingStateAfterHitstop = visualState;
            hasPendingStateAfterHitstop = true;
            return;
        }

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

    public void PresentAttack(string attackId, PlayerCombat.AttackPhase phase,
        float phaseProgress)
    {
        if (string.IsNullOrEmpty(attackId) || animator == null ||
            !animator.isActiveAndEnabled ||
            CombatImpactSystem.IsPresentationFrozen(animator) ||
            !attackBindingMap.TryGetValue(attackId, out AttackBinding binding))
        {
            return;
        }

        int stateHash = Animator.StringToHash(binding.animatorStatePath);
        if (!animator.HasState(layerIndex, stateHash))
            return;

        // O tempo do PlayerCombat escolhe exatamente a pose do clip. Assim,
        // alterar Windup/Active/Recovery jamais devolve autoridade ao Animator.
        animator.Play(
            stateHash,
            layerIndex,
            binding.GetClipProgress(phase, phaseProgress)
        );
    }

    public void PresentMovement(float horizontalSpeed, bool isMoving)
    {
        if (animator == null || !animator.isActiveAndEnabled)
            return;

        if (CombatImpactSystem.IsPresentationFrozen(animator))
            return;

        if (!isMoving)
        {
            animator.speed = 1f;
            return;
        }

        float reference = Mathf.Max(.01f, moveSpeedReference);
        float maximum = Mathf.Max(minimumMoveAnimationSpeed,
            maximumMoveAnimationSpeed);
        animator.speed = Mathf.Clamp(
            Mathf.Max(0f, horizontalSpeed) / reference,
            minimumMoveAnimationSpeed,
            maximum
        );
    }

    /// <summary>
    /// Reconstroi o mapa apos editar os bindings no Inspector. Seguro chamar
    /// mesmo sem Animator ou sem Animation Controller.
    /// </summary>
    public void RebuildBindings()
    {
        stateHashes.Clear();
        attackBindingMap.Clear();

        if (stateBindings != null)
        {
            foreach (StateBinding binding in stateBindings)
            {
                if (string.IsNullOrWhiteSpace(binding.animatorStatePath))
                    continue;

                stateHashes[binding.visualState] = Animator.StringToHash(
                    binding.animatorStatePath
                );
            }
        }

        if (attackBindings == null)
            return;

        foreach (AttackBinding binding in attackBindings)
        {
            if (string.IsNullOrWhiteSpace(binding.attackId) ||
                string.IsNullOrWhiteSpace(binding.animatorStatePath))
            {
                continue;
            }

            attackBindingMap[binding.attackId] = binding;
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

    private static AttackBinding CreateDefaultAttackBinding(string attackId) =>
        new()
        {
            attackId = attackId,
            animatorStatePath = "Base Layer." + attackId,
            windupStart = 0f,
            windupEnd = .35f,
            activeStart = .35f,
            activeEnd = .6f,
            recoveryStart = .6f,
            recoveryEnd = 1f
        };
}
