using UnityEngine;

/// <summary>
/// Provocacao opcional escolhida em combate quando a agressividade esta alta.
/// A acao prende o inimigo no lugar e pode sinalizar um Animator visual.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyTaunt : MonoBehaviour
{
    [Header("Decision")]
    [Tooltip("Agressividade efetiva minima para considerar um taunt, incluindo o modificador do EnemyDirector.")]
    [Range(0f, 100f)] [SerializeField] private float minimumAggressionPercent = 70f;
    [Tooltip("Chance de iniciar um taunt sempre que o inimigo toma uma decisao de combate e a agressividade e suficiente.")]
    [Range(0f, 100f)] [SerializeField] private float tauntChancePerDecision = 10f;
    [Tooltip("Tempo minimo em que o inimigo fica parado provocando.")]
    [Min(0f)] [SerializeField] private float minimumTauntDuration = .8f;
    [Tooltip("Tempo maximo sorteado para a provocacao. Use o mesmo valor do minimo para uma duracao fixa.")]
    [Min(0f)] [SerializeField] private float maximumTauntDuration = 1.2f;
    [Tooltip("Tempo apos terminar ou interromper um taunt antes de poder iniciar outro.")]
    [Min(0f)] [SerializeField] private float tauntCooldown = 6f;

    [Header("Visual")]
    [Tooltip("Animator do modelo visual. Se vazio, procura um Animator nos filhos do inimigo.")]
    [SerializeField] private Animator visualAnimator;
    [Tooltip("Nome de um parametro Bool no Animator. Ele fica verdadeiro durante o taunt; vazio desliga essa integracao.")]
    [SerializeField] private string tauntingBoolParameter = "Taunting";

    private EnemyBase owner;
    private EnemyBrain brain;
    private EnemyMovement movement;
    private float tauntTimeRemaining;
    private float cooldownRemaining;
    private int tauntingBoolHash;
    private bool hasTauntingBool;
    private bool isTauntActive;

    internal void Initialize(EnemyBase enemy, EnemyBrain enemyBrain,
        EnemyMovement enemyMovement)
    {
        owner = enemy;
        brain = enemyBrain;
        movement = enemyMovement;
        if (visualAnimator == null)
            visualAnimator = GetComponentInChildren<Animator>(true);

        hasTauntingBool = false;
        if (visualAnimator == null || string.IsNullOrEmpty(tauntingBoolParameter))
            return;

        tauntingBoolHash = Animator.StringToHash(tauntingBoolParameter);
        foreach (AnimatorControllerParameter parameter in visualAnimator.parameters)
        {
            if (parameter.nameHash != tauntingBoolHash ||
                parameter.type != AnimatorControllerParameterType.Bool)
                continue;
            hasTauntingBool = true;
            break;
        }
    }

    private void OnDisable() => EndTaunt();

    internal void TickCooldown()
    {
        cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
        if (isTauntActive && owner != null &&
            owner.CurrentState != EnemyBase.State.Taunting)
            EndTaunt();
    }

    internal bool TryStartFromDecision()
    {
        if (!isActiveAndEnabled || owner == null || brain == null ||
            movement == null || movement.IsLaunched || movement.IsPushed ||
            owner.IsDead || owner.CurrentState != EnemyBase.State.Combat ||
            brain.Target == null || cooldownRemaining > 0f ||
            brain.EffectiveAggressionPercent < minimumAggressionPercent ||
            Random.value * 100f >= tauntChancePerDecision)
            return false;

        tauntTimeRemaining = Random.Range(
            Mathf.Min(minimumTauntDuration, maximumTauntDuration),
            Mathf.Max(minimumTauntDuration, maximumTauntDuration));
        isTauntActive = true;
        brain.CancelReposition();
        movement.StopForTaunt();
        owner.CurrentState = EnemyBase.State.Taunting;
        if (hasTauntingBool && visualAnimator != null)
            visualAnimator.SetBool(tauntingBoolHash, true);
        return true;
    }

    internal void TickTaunt()
    {
        if (!isTauntActive || owner == null ||
            owner.CurrentState != EnemyBase.State.Taunting)
            return;

        tauntTimeRemaining -= Time.deltaTime;
        if (tauntTimeRemaining <= 0f)
            EndTaunt();
    }

    private void EndTaunt()
    {
        if (!isTauntActive)
            return;

        isTauntActive = false;
        tauntTimeRemaining = 0f;
        cooldownRemaining = tauntCooldown;
        if (hasTauntingBool && visualAnimator != null)
            visualAnimator.SetBool(tauntingBoolHash, false);
        if (owner != null && owner.CurrentState == EnemyBase.State.Taunting)
        {
            owner.CurrentState = EnemyBase.State.Combat;
            brain?.ScheduleNextDecision();
        }
    }

    internal void CancelTaunt() => EndTaunt();
}
