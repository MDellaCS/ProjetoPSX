using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ferramenta temporaria para visualizar mortes sem destruir o inimigo nem
/// interferir em Encounter, IA ou contagem de inimigos vivos.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Projeto PSX/Tests/Enemy Death Loop Test")]
public sealed class EnemyDeathLoopTest : MonoBehaviour
{
    public enum TestDeathForm
    {
        HideVisual,
        Gore
    }

    [Header("Loop")]
    [Tooltip("Inicia o ciclo automaticamente ao entrar em Play. Desligado permite iniciar pelo menu de contexto do componente.")]
    [SerializeField] private bool playOnStart = true;
    [Tooltip("Tempo que o inimigo permanece visivel antes de executar a morte de teste.")]
    [Min(0f)] [SerializeField] private float aliveDuration = 1.5f;
    [Tooltip("Tempo que o inimigo permanece morto/invisivel antes de reviver.")]
    [Min(0f)] [SerializeField] private float deathDuration = 2f;
    [Tooltip("Forma visual usada na morte de teste.")]
    [SerializeField] private TestDeathForm deathForm = TestDeathForm.Gore;

    [Header("Test Damage")]
    [Tooltip("Dano letal ficticio passado ao gore. Nao altera a vida real do inimigo.")]
    [Min(0)] [SerializeField] private int testDamage = 100;
    [Tooltip("Direcao local do dano. (0, 0, 1) significa um golpe vindo de tras do inimigo e o empurra para a frente.")]
    [SerializeField] private Vector3 localDamageDirection = Vector3.forward;

    private readonly List<Renderer> cachedRenderers = new();
    private readonly List<Collider> cachedColliders = new();
    private readonly List<Behaviour> pausedBehaviours = new();
    private CharacterController characterController;
    private EnemyGore gore;
    private bool isAlive = true;
    private float stateTimer;

    private void Awake()
    {
        CacheComponents();
        SetAliveVisual(true);
    }

    private void Start()
    {
        if (playOnStart)
            RestartLoop();
        else
            enabled = false;
    }

    private void Update()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f)
            return;

        if (isAlive)
            PerformTestDeath();
        else
            ReviveForTest();
    }

    [ContextMenu("Restart Test Loop")]
    public void RestartLoop()
    {
        if (!Debug.isDebugBuild)
        {
            enabled = false;
            return;
        }
        SetAliveVisual(true);
        isAlive = true;
        stateTimer = aliveDuration;
        enabled = true;
    }

    [ContextMenu("Kill Now (Test)")]
    public void PerformTestDeath()
    {
        if (!isAlive)
            return;

        Vector3 worldDamageDirection = transform.TransformDirection(
            localDamageDirection.sqrMagnitude > .0001f
                ? localDamageDirection.normalized : Vector3.forward);
        if (deathForm == TestDeathForm.Gore)
        {
            if (gore == null)
                gore = GetComponent<EnemyGore>();
            if (gore == null)
                gore = gameObject.AddComponent<EnemyGore>();
            gore.SpawnTestGore(testDamage, worldDamageDirection);
            GetComponent<EnemyBodyPartDeathParticles>()?.EmitDeathParts(
                worldDamageDirection, testDamage);
        }

        SetAliveVisual(false);
        isAlive = false;
        stateTimer = deathDuration;
    }

    [ContextMenu("Revive Now (Test)")]
    public void ReviveForTest()
    {
        SetAliveVisual(true);
        isAlive = true;
        stateTimer = aliveDuration;
    }

    private void CacheComponents()
    {
        cachedRenderers.AddRange(GetComponentsInChildren<Renderer>(true));
        cachedColliders.AddRange(GetComponentsInChildren<Collider>(true));
        characterController = GetComponent<CharacterController>();

        // EnemyBase concentra a simulacao dos componentes de combate. Pausa-lo
        // impede ataques e movimento sem desativar este componente de teste.
        EnemyBase enemy = GetComponent<EnemyBase>();
        if (enemy != null)
            pausedBehaviours.Add(enemy);
    }

    private void SetAliveVisual(bool alive)
    {
        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer != null)
                renderer.enabled = alive;
        }

        foreach (Collider collider in cachedColliders)
        {
            if (collider != null)
                collider.enabled = alive;
        }

        if (characterController != null)
            characterController.enabled = alive;

        foreach (Behaviour behaviour in pausedBehaviours)
        {
            if (behaviour != null)
                behaviour.enabled = alive;
        }
    }
}
