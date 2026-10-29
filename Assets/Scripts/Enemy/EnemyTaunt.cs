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
    [Tooltip("Pontos percentuais somados a chance de taunt deste inimigo quando ele causa dano ao jogador. O bonus acumula ate uma decisao elegivel de taunt.")]
    [Min(0f)] [SerializeField] private float tauntChanceBoostAfterPlayerDamage;
    [Tooltip("Tempo minimo em que o inimigo fica parado provocando.")]
    [Min(0f)] [SerializeField] private float minimumTauntDuration = .8f;
    [Tooltip("Tempo maximo sorteado para a provocacao. Use o mesmo valor do minimo para uma duracao fixa.")]
    [Min(0f)] [SerializeField] private float maximumTauntDuration = 1.2f;
    [Tooltip("Tempo apos terminar ou interromper um taunt antes de poder iniciar outro.")]
    [Min(0f)] [SerializeField] private float tauntCooldown = 6f;
    [Tooltip("Se ativo, o inimigo gira para olhar o jogador durante o taunt. Se desligado, mantem a direcao que tinha ao comecar.")]
    [SerializeField] private bool facePlayerDuringTaunt = true;

    [Header("Audio")]
    [Tooltip("Som ou variacoes tocadas quando este inimigo inicia um taunt. Usa as configuracoes globais de audio de gameplay.")]
    [SerializeField] private GameplayAudioCue tauntSound = new();

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
    private float pendingChanceBoost;
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
            movement == null || movement.IsLaunched || movement.IsLaunchedForwards ||
            owner.IsDead || owner.CurrentState != EnemyBase.State.Combat ||
            brain.Target == null || cooldownRemaining > 0f ||
            brain.EffectiveAggressionPercent < minimumAggressionPercent)
            return false;

        float effectiveTauntChance = Mathf.Clamp(
            tauntChancePerDecision + pendingChanceBoost, 0f, 100f);
        pendingChanceBoost = 0f;
        if (Random.value * 100f >= effectiveTauntChance)
            return false;

        tauntTimeRemaining = Random.Range(
            Mathf.Min(minimumTauntDuration, maximumTauntDuration),
            Mathf.Max(minimumTauntDuration, maximumTauntDuration));
        isTauntActive = true;
        brain.CancelReposition();
        movement.StopForTaunt();
        owner.CurrentState = EnemyBase.State.Taunting;
        GameplayAudioSystem.Play(tauntSound, transform.position);
        if (hasTauntingBool && visualAnimator != null)
            visualAnimator.SetBool(tauntingBoolHash, true);
        return true;
    }

    internal void AddChanceBoost(float percentagePoints)
    {
        if (!isActiveAndEnabled || percentagePoints <= 0f)
            return;

        pendingChanceBoost = Mathf.Min(100f,
            pendingChanceBoost + percentagePoints);
    }

    internal void OnDamageDealtToPlayer() =>
        AddChanceBoost(tauntChanceBoostAfterPlayerDamage);

    internal void TickTaunt()
    {
        if (!isTauntActive || owner == null ||
            owner.CurrentState != EnemyBase.State.Taunting)
            return;

        if (facePlayerDuringTaunt && brain.Target != null)
            movement.FaceTarget();

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
