using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Escolhe quem pressiona o jogador e distribui vagas de ataque. O Brain
/// continua responsavel por perseguir, defender e escolher cada golpe.
/// </summary>
[DisallowMultipleComponent]
public class EnemyDirector : MonoBehaviour
{
    private static EnemyDirector activeDirector;

    [Header("Lead Budget")]
    [Tooltip("Ativa a escolha de atacantes, a alternancia de turnos e os modificadores de agressividade do Director.")]
    [SerializeField] private bool controlAttackTurns = true;
    [Tooltip("Orcamento maximo inteiro ocupado simultaneamente pelos inimigos Lead. Aceita qualquer valor positivo.")]
    [Min(1)] [SerializeField] private int maximumLeadWeight = 100;

    [Header("Spawn Weight Variation")]
    [Tooltip("Chance global de um inimigo nascer ocupando metade de seu Director Weight. O sorteio acontece uma vez por nascimento.")]
    [Range(0f, 100f)] [SerializeField] private float halfWeightSpawnChance = 10f;
    [Tooltip("Chance global de um inimigo nascer ocupando o dobro de seu Director Weight. O resultado final nunca ultrapassa 100.")]
    [Range(0f, 100f)] [SerializeField] private float doubleWeightSpawnChance = 10f;

    [Header("Focus")]
    [Tooltip("Pontos de agressividade temporarios dados ao atacante principal ou ao proximo escolhido.")]
    [Range(0f, 100f)] [SerializeField] private float leadAggressionBonus = 25f;
    [Tooltip("Pontos de agressividade temporarios retirados dos demais inimigos enquanto existe um atacante principal.")]
    [Range(0f, 100f)] [SerializeField] private float supportAggressionPenalty = 20f;
    [Tooltip("Quanto a proximidade do jogador pesa na escolha do proximo atacante. Cada unidade de distancia reduz esta pontuacao.")]
    [Min(0f)] [SerializeField] private float proximityPriorityPerUnit = 8f;

    [Header("Global Dodge Rules")]
    [Tooltip("Inimigos nao esquivam de um golpe cujo dano bruto seja igual ou maior que esta porcentagem da vida maxima. Use 100 para bloquear apenas golpes capazes de remover uma barra inteira de uma vez.")]
    [Range(0f, 100f)] [SerializeField] private float undodgeableDamagePercentOfMaximumHealth = 50f;
    [Tooltip("Multiplicador global aplicado a chance de esquiva enquanto o inimigo esta no ar. Por exemplo, 0.5 transforma uma chance de 60 por cento em 30 por cento.")]
    [Range(0f, 1f)] [SerializeField] private float airborneDodgeChanceMultiplier = .5f;

    private readonly HashSet<EnemyBase> activeAttackers = new();
    private readonly Dictionary<EnemyBase, float> lastAttackAt = new();
    private readonly Dictionary<EnemyBase, float> runtimeWeights = new();
    private readonly List<EnemyBase> releasedEnemies = new();
    private readonly List<EnemyBase> replacementLeads = new();
    private EnemyBase nextAttacker;

    public float CurrentLeadWeight => GetOccupiedLeadWeight();

    public static bool IsControllingAttackTurns => activeDirector != null &&
        activeDirector.isActiveAndEnabled && activeDirector.controlAttackTurns;

    public static bool CanDodgeIncomingDamage(int damage, int maximumHealth)
    {
        if (damage <= 0 || maximumHealth <= 0)
            return true;

        float blockedPercent = activeDirector != null
            ? activeDirector.undodgeableDamagePercentOfMaximumHealth : 50f;
        float damagePercent = damage * 100f / maximumHealth;
        return damagePercent < blockedPercent;
    }

    public static float GetDodgeChanceMultiplier(bool isAirborne) =>
        isAirborne
            ? activeDirector != null
                ? activeDirector.airborneDodgeChanceMultiplier : .5f
            : 1f;

    private void OnEnable() => activeDirector = this;

    private void OnValidate()
    {
        maximumLeadWeight = Mathf.Max(1, maximumLeadWeight);
        halfWeightSpawnChance = Mathf.Clamp(halfWeightSpawnChance, 0f, 100f);
        doubleWeightSpawnChance = Mathf.Clamp(doubleWeightSpawnChance,
            0f, 100f - halfWeightSpawnChance);
    }

    private void OnDisable()
    {
        if (activeDirector == this)
            activeDirector = null;
        activeAttackers.Clear();
        lastAttackAt.Clear();
        nextAttacker = null;
        runtimeWeights.Clear();
    }

    private void Update()
    {
        if (!controlAttackTurns)
            return;
        RefreshAttackTurns();
        TryReplaceDistantLead();
        SelectNextAttacker();
    }

    public static bool RequestAttackTurn(EnemyBase enemy)
    {
        return !IsControllingAttackTurns ||
            activeDirector.TryReserveAttackTurn(enemy);
    }

    public static void ReleaseEnemy(EnemyBase enemy)
    {
        if (activeDirector != null && enemy != null)
            activeDirector.RemoveEnemy(enemy);
    }

    public static float GetAggressionModifier(EnemyBase enemy)
    {
        if (!IsControllingAttackTurns || enemy == null || enemy.Target == null)
            return 0f;

        if (activeDirector.activeAttackers.Contains(enemy) ||
            enemy == activeDirector.nextAttacker)
            return activeDirector.leadAggressionBonus;

        return activeDirector.HasLeadForTarget(enemy.Target) &&
            enemy.CurrentState != EnemyBase.State.Idle
            ? -activeDirector.supportAggressionPenalty : 0f;
    }

    public static string GetRoleLabel(EnemyBase enemy)
    {
        if (!IsControllingAttackTurns)
            return "OFF";
        if (activeDirector.activeAttackers.Contains(enemy))
            return "LEAD";
        if (enemy == activeDirector.nextAttacker)
            return "NEXT";
        return enemy != null &&
            activeDirector.HasLeadForTarget(enemy.Target) &&
            enemy.CurrentState != EnemyBase.State.Idle
            ? "SUPPORT" : "WAITING";
    }

    public static bool IsLeadEnemy(EnemyBase enemy) =>
        IsControllingAttackTurns && enemy != null &&
        activeDirector.activeAttackers.Contains(enemy);

    public static float GetAssignedWeight(EnemyBase enemy) =>
        activeDirector != null && enemy != null
            ? activeDirector.GetRuntimeWeight(enemy) : 0f;

    public static float GetCurrentLeadWeight() => activeDirector != null
        ? activeDirector.GetOccupiedLeadWeight() : 0f;

    public static float GetMaximumLeadWeight() => activeDirector != null
        ? activeDirector.maximumLeadWeight : 0f;

    private bool TryReserveAttackTurn(EnemyBase enemy)
    {
        if (enemy == null)
            return false;

        RefreshAttackTurns();
        if (activeAttackers.Contains(enemy))
            return true;
        float enemyWeight = GetRuntimeWeight(enemy);
        if (enemyWeight > GetAvailableLeadWeight())
            return false;

        SelectNextAttacker();
        if (enemy != nextAttacker)
            return false;

        activeAttackers.Add(enemy);
        lastAttackAt[enemy] = Time.time;
        nextAttacker = null;
        return true;
    }

    private void SelectNextAttacker()
    {
        float availableWeight = GetAvailableLeadWeight();
        if (availableWeight < 1f)
        {
            nextAttacker = null;
            return;
        }

        if (IsCandidate(nextAttacker) &&
            GetRuntimeWeight(nextAttacker) <= availableWeight)
            return;

        nextAttacker = FindBestCandidate(availableWeight);

        if (nextAttacker != null)
            nextAttacker.GetComponent<EnemyBrain>()?.DecideImmediately();
    }

    private EnemyBase FindBestCandidate(float availableWeight)
    {
        EnemyBase best = null;
        float bestScore = float.NegativeInfinity;
        foreach (EnemyBase enemy in EnemyBase.ActiveEnemies)
        {
            if (!IsCandidate(enemy) ||
                GetRuntimeWeight(enemy) > availableWeight)
                continue;

            float distance = HorizontalDistanceToTarget(enemy);
            float score = enemy.CurrentAggressionPercent * .2f -
                distance * proximityPriorityPerUnit;
            if (lastAttackAt.TryGetValue(enemy, out float lastTime))
                score += Mathf.Min(20f, (Time.time - lastTime) * 5f);
            else
                score += 20f;

            if (score <= bestScore)
                continue;
            best = enemy;
            bestScore = score;
        }
        return best;
    }

    private bool IsCandidate(EnemyBase enemy)
    {
        if (enemy == null || !enemy.isActiveAndEnabled || enemy.IsDead ||
            enemy.CurrentState != EnemyBase.State.Combat ||
            enemy.IsLaunched || enemy.IsPushed ||
            enemy.Target == null || enemy.IsSearchingLastKnownPosition ||
            activeAttackers.Contains(enemy))
            return false;

        return enemy.HasAvailableOpening;
    }

    private static float HorizontalDistanceToTarget(EnemyBase enemy)
    {
        Vector3 offset = enemy.Target.position - enemy.transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private float GetAvailableLeadWeight() =>
        Mathf.Max(0f, maximumLeadWeight - GetOccupiedLeadWeight());

    private float GetOccupiedLeadWeight()
    {
        float occupied = 0f;
        foreach (EnemyBase enemy in activeAttackers)
        {
            if (enemy != null && !enemy.IsDead)
                occupied += GetRuntimeWeight(enemy);
        }
        return Mathf.Clamp(occupied, 0f, maximumLeadWeight);
    }

    private float GetRuntimeWeight(EnemyBase enemy)
    {
        if (enemy == null)
            return maximumLeadWeight;
        if (runtimeWeights.TryGetValue(enemy, out float weight))
            return weight;

        float roll = Random.value * 100f;
        float multiplier = roll < halfWeightSpawnChance ? .5f :
            roll < halfWeightSpawnChance + doubleWeightSpawnChance ? 2f : 1f;
        weight = Mathf.Clamp(enemy.DirectorWeight * multiplier,
            1f, maximumLeadWeight);
        runtimeWeights[enemy] = weight;
        return weight;
    }

    private bool HasLeadForTarget(Transform target)
    {
        if (target == null)
            return false;
        foreach (EnemyBase enemy in activeAttackers)
        {
            if (enemy != null && !enemy.IsDead && enemy.Target == target)
                return true;
        }
        return nextAttacker != null && nextAttacker.Target == target;
    }

    private void TryReplaceDistantLead()
    {
        EnemyBase challenger = FindClosestWaitingCandidate();
        if (challenger == null)
            return;

        float challengerDistance = HorizontalDistanceToTarget(challenger);
        float challengerWeight = GetRuntimeWeight(challenger);
        float availableWeight = GetAvailableLeadWeight();
        if (challengerWeight <= availableWeight)
            return;

        replacementLeads.Clear();
        foreach (EnemyBase lead in activeAttackers)
        {
            if (lead == null || lead.IsDead || lead.IsInAttackSequence ||
                lead.Target != challenger.Target)
            {
                continue;
            }

            float leadDistance = HorizontalDistanceToTarget(lead);
            if (leadDistance > challengerDistance + .05f)
                replacementLeads.Add(lead);
        }

        replacementLeads.Sort((left, right) =>
            HorizontalDistanceToTarget(right).CompareTo(
                HorizontalDistanceToTarget(left)));

        float weightAfterReplacements = availableWeight;
        int replacementCount = 0;
        while (replacementCount < replacementLeads.Count &&
            weightAfterReplacements < challengerWeight)
        {
            weightAfterReplacements +=
                GetRuntimeWeight(replacementLeads[replacementCount]);
            replacementCount++;
        }

        if (weightAfterReplacements < challengerWeight)
            return;

        for (int i = 0; i < replacementCount; i++)
            activeAttackers.Remove(replacementLeads[i]);
        nextAttacker = challenger;
        challenger.GetComponent<EnemyBrain>()?.DecideImmediately();
    }

    private EnemyBase FindClosestWaitingCandidate()
    {
        EnemyBase closest = null;
        float closestDistance = float.PositiveInfinity;
        foreach (EnemyBase enemy in EnemyBase.ActiveEnemies)
        {
            if (!IsCandidate(enemy))
                continue;
            float distance = HorizontalDistanceToTarget(enemy);
            if (distance >= closestDistance)
                continue;
            closest = enemy;
            closestDistance = distance;
        }
        return closest;
    }

    private void RefreshAttackTurns()
    {
        releasedEnemies.Clear();
        foreach (EnemyBase enemy in activeAttackers)
        {
            if (enemy == null || enemy.IsDead || !enemy.CanBeLockOnTarget ||
                enemy.Target == null || enemy.CurrentState == EnemyBase.State.Idle)
            {
                releasedEnemies.Add(enemy);
            }
        }

        foreach (EnemyBase enemy in releasedEnemies)
            activeAttackers.Remove(enemy);
    }

    private void RemoveEnemy(EnemyBase enemy)
    {
        activeAttackers.Remove(enemy);
        lastAttackAt.Remove(enemy);
        runtimeWeights.Remove(enemy);
        if (nextAttacker == enemy)
            nextAttacker = null;
    }
}
