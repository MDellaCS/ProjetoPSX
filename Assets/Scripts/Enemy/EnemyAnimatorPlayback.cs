using System;
using UnityEngine;

/// <summary>Apresentacao opcional do inimigo; o combate continua independente do Animator.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyBase))]
public sealed class EnemyAnimatorPlayback : MonoBehaviour
{
    [Serializable]
    private struct StateBinding
    {
        [Tooltip("Estado de gameplay do inimigo que seleciona esta animacao.")]
        public EnemyBase.State state;
        [Tooltip("Caminho completo do estado no Animator, por exemplo Base Layer.Idle. Vazio nao altera o Animator.")]
        public string animatorStatePath;
    }

    [Serializable]
    private struct AttackBinding
    {
        [Tooltip("Ataque do moveset ao qual esta animacao pertence.")]
        public EnemyAttackDefinition attack;
        [Tooltip("Caminho completo do estado no Animator para este ataque, por exemplo Base Layer.Attack1.")]
        public string animatorStatePath;
    }

    [Header("Animator")]
    [Tooltip("Animator visual do inimigo. Se vazio, procura no proprio objeto e nos filhos.")]
    [SerializeField] private Animator animator;
    [Tooltip("Indice da camada do Animator em que os estados serao reproduzidos.")]
    [Min(0)] [SerializeField] private int layerIndex;
    [Tooltip("Duracao em segundos da transicao entre animacoes visuais.")]
    [Min(0f)] [SerializeField] private float transitionDuration = .08f;
    [Tooltip("Impede que o Animator mova o inimigo, mantendo a movimentacao sob controle do gameplay.")]
    [SerializeField] private bool disableRootMotion = true;

    [Header("Estados")]
    [Tooltip("Animacao opcional para cada estado de gameplay. Entradas vazias preservam o comportamento atual do Animator.")]
    [SerializeField] private StateBinding[] stateBindings =
    {
        new StateBinding { state = EnemyBase.State.Idle },
        new StateBinding { state = EnemyBase.State.Combat },
        new StateBinding { state = EnemyBase.State.Windup },
        new StateBinding { state = EnemyBase.State.Active },
        new StateBinding { state = EnemyBase.State.Recovery },
        new StateBinding { state = EnemyBase.State.Guarding },
        new StateBinding { state = EnemyBase.State.Dodging },
        new StateBinding { state = EnemyBase.State.Taunting },
        new StateBinding { state = EnemyBase.State.Staggered },
        new StateBinding { state = EnemyBase.State.TacticalIdle },
        new StateBinding { state = EnemyBase.State.SpecialAttackQueue }
    };
    [Tooltip("Caminho completo do estado de morte no Animator. Vazio nao altera o Animator ao morrer.")]
    [SerializeField] private string deathStatePath;
    [Tooltip("Estado do Animator usado enquanto o inimigo se move em Combat. Vazio usa a animacao normal de Combat.")]
    [SerializeField] private string combatMovementStatePath;
    [Tooltip("Velocidade horizontal minima para mostrar a animacao de movimento em Combat.")]
    [Min(0f)] [SerializeField] private float movementSpeedThreshold = .1f;

    [Header("Ataques")]
    [Tooltip("Animacoes por ataque. Uma entrada substitui Animation State Id do asset de ataque; sem entrada, esse identificador e usado.")]
    [SerializeField] private AttackBinding[] attackBindings = Array.Empty<AttackBinding>();

    private EnemyBase owner;
    private EnemyAttackController attacks;
    private CharacterController characterController;
    private EnemyAttackRuntime lastAttack;
    private EnemyBase.State lastState;
    private bool lastDead;
    private bool hasPresented;
    private bool suspended;
    private bool lastMoving;

    private void Awake()
    {
        owner = GetComponent<EnemyBase>();
        attacks = GetComponent<EnemyAttackController>();
        characterController = GetComponent<CharacterController>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (animator != null && disableRootMotion)
            animator.applyRootMotion = false;
    }

    private void OnEnable() => hasPresented = false;

    private void Update()
    {
        if (animator == null || !animator.isActiveAndEnabled || owner == null)
        {
            hasPresented = false;
            return;
        }

        if (CutsceneController.AreEnemiesFrozen)
        {
            suspended = true;
            return;
        }
        if (CombatImpactSystem.IsPresentationFrozen(animator))
            return;

        if (suspended)
        {
            suspended = false;
            hasPresented = false;
        }

        bool dead = owner.IsDead;
        EnemyBase.State state = owner.CurrentState;
        Vector3 velocity = characterController != null ? characterController.velocity : Vector3.zero;
        bool moving = state == EnemyBase.State.Combat &&
            velocity.x * velocity.x + velocity.z * velocity.z >
            movementSpeedThreshold * movementSpeedThreshold;
        EnemyAttackRuntime attack = !dead && owner.IsInAttackSequence && attacks != null
            ? attacks.CurrentAttack : null;
        if (hasPresented && dead == lastDead && state == lastState && attack == lastAttack && moving == lastMoving)
            return;

        bool sameAttack = hasPresented && attack != null && attack == lastAttack && !dead;
        hasPresented = true;
        lastDead = dead;
        lastState = state;
        lastAttack = attack;
        lastMoving = moving;

        string path = dead ? deathStatePath : ResolveAttackPath(attack);
        if (string.IsNullOrWhiteSpace(path) && !dead)
            path = moving && !string.IsNullOrWhiteSpace(combatMovementStatePath)
                ? combatMovementStatePath : ResolveStatePath(state);
        else if (sameAttack)
            return; // A troca de fase nao reinicia a animacao do mesmo ataque.

        if (string.IsNullOrWhiteSpace(path) || layerIndex < 0 ||
            layerIndex >= animator.layerCount)
            return;
        int hash = Animator.StringToHash(path);
        if (animator.HasState(layerIndex, hash))
            animator.CrossFadeInFixedTime(hash, Mathf.Max(0f, transitionDuration), layerIndex);
    }

    private string ResolveAttackPath(EnemyAttackRuntime attack)
    {
        if (attack == null || attack.Definition == null)
            return null;
        if (attackBindings != null)
            foreach (AttackBinding binding in attackBindings)
                if (binding.attack == attack.Definition &&
                    !string.IsNullOrWhiteSpace(binding.animatorStatePath))
                    return binding.animatorStatePath;
        return attack.Definition.animationStateId;
    }

    private string ResolveStatePath(EnemyBase.State state)
    {
        if (stateBindings != null)
            foreach (StateBinding binding in stateBindings)
                if (binding.state == state)
                    return binding.animatorStatePath;
        return null;
    }
}
