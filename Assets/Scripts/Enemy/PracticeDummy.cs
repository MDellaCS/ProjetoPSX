using System.Collections.Generic;
using UnityEngine;

/// <summary>Opcoes de teste para manter um inimigo como boneco de treino.</summary>
[DisallowMultipleComponent]
public sealed class PracticeDummy : MonoBehaviour
{
    [Header("Comportamento")]
    [Tooltip("Impede locomocao voluntaria e movimento configurado pelos ataques. Reacoes fisicas a golpes continuam funcionando.")]
    [SerializeField] private bool preventSelfMovement = true;
    [Tooltip("Permite que a IA inicie ataques.")]
    [SerializeField] private bool allowAttacks;
    [Tooltip("Permite que o inimigo esquive de golpes do jogador.")]
    [SerializeField] private bool allowDodging;
    [Tooltip("Permite que a IA levante a guarda ou bloqueie golpes.")]
    [SerializeField] private bool allowBlocking;

    [Header("Retorno a Posicao Inicial")]
    [Tooltip("Teleporta o inimigo de volta a posicao e rotacao iniciais em intervalos regulares.")]
    [SerializeField] private bool returnToStart = true;
    [Tooltip("Intervalo, em segundos, entre retornos a posicao inicial.")]
    [Min(.1f)] [SerializeField] private float returnInterval = 5f;

    [Header("Renascimento")]
    [Tooltip("Cria uma nova instancia do inimigo depois que ele morrer.")]
    [SerializeField] private bool respawnWhenKilled = true;
    [Tooltip("Tempo, em segundos, entre a morte e o renascimento do boneco.")]
    [Min(0f)] [SerializeField] private float respawnDelay = 3f;

    private EnemyBase enemy;
    private EnemyMovement movement;
    private EnemyAttackController attacks;
    private EnemyGuard guard;
    private EnemyDodge dodge;
    private CharacterController characterController;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float returnTimer;
    private bool waitingToRespawn;
    private float respawnTimer;
    private readonly List<Renderer> hiddenRenderers = new();

    public bool AllowAttacks => allowAttacks;
    public bool AllowDodging => allowDodging;
    public bool AllowBlocking => allowBlocking;

    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
        movement = GetComponent<EnemyMovement>();
        attacks = GetComponent<EnemyAttackController>();
        guard = GetComponent<EnemyGuard>();
        dodge = GetComponent<EnemyDodge>();
        characterController = GetComponent<CharacterController>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        returnTimer = Mathf.Max(.1f, returnInterval);
        movement?.SetAuthoredMovementSuppressed(preventSelfMovement);
    }

    private void OnValidate()
    {
        returnInterval = Mathf.Max(.1f, returnInterval);
        respawnDelay = Mathf.Max(0f, respawnDelay);
        if (Application.isPlaying)
            movement?.SetAuthoredMovementSuppressed(preventSelfMovement);
    }

    private void Update()
    {
        if (GamePauseController.IsPaused || CutsceneController.AreEnemiesFrozen ||
            CombatImpactSystem.IsGameplayFrozen(this))
        {
            return;
        }

        if (waitingToRespawn)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
                FinishRespawn();
            return;
        }

        if (!returnToStart || enemy == null || enemy.IsDead)
            return;

        returnTimer -= Time.deltaTime;
        if (returnTimer > 0f)
            return;

        returnTimer = Mathf.Max(.1f, returnInterval);
        ReturnToStart();
    }

    /// <summary>Cria a instancia de respawn antes que a morte destrua este inimigo.</summary>
    internal void CreateRespawnInstance()
    {
        if (!respawnWhenKilled || !Application.isPlaying)
            return;

        PracticeDummy copy = Instantiate(this, startPosition, startRotation);
        copy.BeginRespawnWait(respawnDelay);
    }

    private void BeginRespawnWait(float delay)
    {
        waitingToRespawn = true;
        respawnTimer = Mathf.Max(0f, delay);
        enemy?.SetSpawning(true);
        enemy?.SetTemporarilyUntargetable(true);
        if (characterController != null)
            characterController.enabled = false;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null) continue;
            hiddenRenderers.Add(renderer);
            renderer.enabled = false;
        }

        if (respawnTimer <= 0f)
            FinishRespawn();
    }

    private void FinishRespawn()
    {
        waitingToRespawn = false;
        transform.SetPositionAndRotation(startPosition, startRotation);
        if (characterController != null)
            characterController.enabled = true;
        foreach (Renderer renderer in hiddenRenderers)
            if (renderer != null) renderer.enabled = true;
        hiddenRenderers.Clear();
        enemy?.SetTemporarilyUntargetable(false);
        enemy?.SetSpawning(false);
        returnTimer = Mathf.Max(.1f, returnInterval);
    }

    private void ReturnToStart()
    {
        attacks?.CancelAttack();
        guard?.CancelGuardForPracticeDummy();
        dodge?.CancelDodge();
        movement?.ResetForPracticeDummy(startPosition, startRotation);
        transform.SetPositionAndRotation(startPosition, startRotation);
        enemy?.ResetPracticeDummyState();
    }
}
