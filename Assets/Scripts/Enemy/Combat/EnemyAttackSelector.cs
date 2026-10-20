using System.Collections.Generic;
using UnityEngine;

/// <summary>Avalia contexto e faz sorteios ponderados sem executar ataques.</summary>
public sealed class EnemyAttackSelector
{
    private readonly List<EnemyMoveTransition> validTransitions = new();
    private readonly EnemyBase owner;
    private readonly EnemyBrain brain;
    private readonly EnemyMovement movement;
    private readonly EnemyMovesetDefinition moveset;

    public EnemyAttackSelector(EnemyBase owner, EnemyBrain brain,
        EnemyMovement movement, EnemyMovesetDefinition moveset)
    {
        this.owner = owner;
        this.brain = brain;
        this.movement = movement;
        this.moveset = moveset;
    }

    public bool HasOpening() => SelectOpening(false, out _, out _);

    public bool TrySelectOpening(out EnemyMoveNode node,
        out bool requiresDirectorAuthorization) =>
        SelectOpening(true, out node, out requiresDirectorAuthorization);

    private bool SelectOpening(bool roll, out EnemyMoveNode selected,
        out bool requiresDirectorAuthorization)
    {
        selected = null;
        requiresDirectorAuthorization = false;
        if (moveset == null || moveset.openings == null)
            return false;

        float total = 0f;
        foreach (EnemyMovesetOpening opening in moveset.openings)
        {
            EnemyMoveNode node = opening == null ? null :
                moveset.FindNode(opening.targetNodeId);
            if (node == null || opening.selectionWeight <= 0f ||
                !IsValid(node.attack))
                continue;
            total += GetContextualWeight(opening.selectionWeight, node.attack);
            if (!roll)
                return true;
        }
        if (total <= 0f)
            return false;

        float random = Random.value * total;
        foreach (EnemyMovesetOpening opening in moveset.openings)
        {
            EnemyMoveNode node = opening == null ? null :
                moveset.FindNode(opening.targetNodeId);
            if (node == null || opening.selectionWeight <= 0f ||
                !IsValid(node.attack))
                continue;
            random -= GetContextualWeight(opening.selectionWeight, node.attack);
            if (random > 0f)
                continue;
            selected = node;
            requiresDirectorAuthorization = opening.requiresDirectorAuthorization;
            return true;
        }
        return false;
    }

    public bool TrySelectContinuation(EnemyAttackRuntime runtime,
        out EnemyMoveNode selectedNode, out float delayAfterActive)
    {
        selectedNode = null;
        delayAfterActive = 0f;
        if (runtime?.Node?.transitions == null)
            return false;
        validTransitions.Clear();
        float total = 0f;
        foreach (EnemyMoveTransition transition in runtime.Node.transitions)
            if (IsValidContinuation(transition, runtime) &&
                Random.value * 100f <= transition.continuationChance)
            {
                validTransitions.Add(transition);
                total += transition.selectionWeight;
            }
        if (total <= 0f)
            return false;

        float random = Random.value * total;
        foreach (EnemyMoveTransition transition in validTransitions)
        {
            random -= transition.selectionWeight;
            if (random <= 0f)
            {
                selectedNode = moveset.FindNode(transition.targetNodeId);
                delayAfterActive = transition.minimumDelayAfterActive;
                return selectedNode != null;
            }
        }
        return false;
    }

    public bool IsValid(EnemyAttackDefinition attack)
    {
        if (attack == null || brain.Target == null)
            return false;
        EnemyAttackRequirements rules = attack.requirements;
        float distance = brain.DistanceToTarget();
        if (rules == null || distance < rules.minimumRange ||
            distance > rules.maximumRange ||
            brain.EffectiveAggressionPercent < rules.minimumAggressionPercent ||
            brain.EffectiveAggressionPercent > rules.maximumAggressionPercent ||
            !MatchesContext(attack.context, movement.IsGrounded) ||
            !MatchesContext(rules.selfContext, movement.IsGrounded) ||
            !MatchesContext(rules.targetContext, IsTargetGrounded()))
            return false;

        Vector3 direction = brain.Target.position - owner.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > .001f && Vector3.Angle(
            owner.transform.forward, direction) > rules.maximumTargetAngle)
            return false;
        if (rules.requiresLineOfSight && Physics.Linecast(owner.AttackOrigin,
            GetTargetCenter(), out RaycastHit hit,
            rules.lineOfSightBlockingLayers, QueryTriggerInteraction.Ignore) &&
            hit.collider.GetComponentInParent<PlayerHealth>() == null)
            return false;
        return owner.CanSelectAttack(attack);
    }

    private bool IsValidContinuation(EnemyMoveTransition transition,
        EnemyAttackRuntime runtime)
    {
        if (transition == null || transition.selectionWeight <= 0f ||
            transition.continuationWindow > 0f &&
                transition.minimumDelayAfterActive > transition.continuationWindow ||
            transition.requiresHitConfirm && !runtime.HitConfirmed)
            return false;
        EnemyMoveNode target = moveset.FindNode(transition.targetNodeId);
        return target != null && IsValid(target.attack);
    }

    private float GetContextualWeight(float baseWeight,
        EnemyAttackDefinition attack)
    {
        float preferred = Mathf.Max(.01f, attack.requirements.preferredRange);
        float distanceDelta = Mathf.Abs(brain.DistanceToTarget() - preferred);
        return baseWeight / (1f + distanceDelta * .25f);
    }

    private bool IsTargetGrounded()
    {
        PlayerMovement player = brain.Target != null
            ? brain.Target.GetComponent<PlayerMovement>() : null;
        return player == null || player.IsGrounded;
    }

    private Vector3 GetTargetCenter()
    {
        CharacterController targetController = brain.Target != null
            ? brain.Target.GetComponent<CharacterController>() : null;
        return targetController != null
            ? brain.Target.TransformPoint(targetController.center)
            : brain.Target.position;
    }

    private static bool MatchesContext(EnemyAttackContext context,
        bool grounded) => context == EnemyAttackContext.Any ||
        context == EnemyAttackContext.Ground && grounded ||
        context == EnemyAttackContext.Air && !grounded;
}
