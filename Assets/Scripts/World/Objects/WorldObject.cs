using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Quando a barra de vida flutuante de um objeto de mundo aparece.</summary>
public enum WorldObjectHealthBarMode
{
    Never,
    Always,
    /// <summary>Aparece ao receber um golpe e some depois de alguns segundos.</summary>
    AfterHit,
    /// <summary>Aparece enquanto o jogador esta com a mira travada no objeto.</summary>
    WhenLockedOn,
    /// <summary>Aparece ao receber um golpe ou enquanto estiver travado.</summary>
    AfterHitOrLockedOn
}

[Serializable]
public sealed class WorldObjectHealthBarSettings
{
    [Tooltip("Quando a barra de vida aparece sobre o objeto.")]
    public WorldObjectHealthBarMode mode = WorldObjectHealthBarMode.AfterHitOrLockedOn;
    [Tooltip("Deslocamento, em unidades do mundo, da barra em relacao ao topo do objeto.")]
    public Vector3 worldOffset = new(0f, .35f, 0f);
    [Tooltip("Segundos que a barra continua visivel depois do ultimo golpe (modos After Hit).")]
    [Min(0f)] public float visibleSecondsAfterHit = 2.5f;
    [Tooltip("Mostra o texto com o valor (ex.: 60 / 100) junto da barra.")]
    public bool showValueText = true;
}

/// <summary>
/// Objeto de mundo atacavel (destrutivel, alavanca, cristal, caixa...).
/// Centraliza dano, estados, reacoes, lock-on, numeros de dano e barra de vida.
/// O comportamento vem dos <see cref="WorldObjectState"/> configurados no
/// Inspector; nao e preciso escrever codigo para criar novos objetos.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
[AddComponentMenu("Projeto PSX/World/World Object")]
public sealed class WorldObject : MonoBehaviour, IPlayerAttackTarget
{
    private const int MaximumStateChainDepth = 8;
    private static readonly List<WorldObject> activeObjects = new();

    [Header("Estados")]
    [Tooltip("Estados de gameplay do objeto. O primeiro e o padrao se 'Estado inicial' estiver vazio.")]
    [SerializeField] private List<WorldObjectState> states = new()
    {
        new WorldObjectState { damageModel = new HealthPoolDamageModel() }
    };
    [Tooltip("Nome do estado em que o objeto comeca. Vazio usa o primeiro da lista.")]
    [SerializeField] private string initialStateId;

    [Header("Apresentacao")]
    [Tooltip("Mostra o numero de dano a cada golpe (usa o Damage Number Spawner deste objeto, configuravel no proprio componente).")]
    [SerializeField] private bool showDamageNumbers = true;
    [Tooltip("Configuracao da barra de vida flutuante.")]
    [SerializeField] private WorldObjectHealthBarSettings healthBar = new();

    [Header("Eventos")]
    [Tooltip("Chamado sempre que o objeto entra em um estado. Recebe o nome do estado.")]
    [SerializeField] private UnityEvent<string> onStateChanged;
    [Tooltip("Chamado quando o objeto e destruido.")]
    [SerializeField] private UnityEvent onDestroyed;

    private readonly Dictionary<string, WorldObjectState> stateById = new();
    private WorldObjectState currentState;
    private Collider hurtbox;
    private Renderer primaryRenderer;
    private WorldLockOnTarget lockOnTarget;
    private DamageNumberSpawner damageNumbers;
    private float lastHitTime = float.NegativeInfinity;
    private bool destroyed;
    private int stateChainDepth;

    /// <summary>Objetos ativos, usados pela barra de vida flutuante.</summary>
    public static IReadOnlyList<WorldObject> Active => activeObjects;

    public event Action<WorldObject, string> StateChanged;
    public event Action<WorldObject> Destroyed;

    public string CurrentStateId => currentState != null ? currentState.id : null;
    public bool IsDestroyed => destroyed;
    public WorldObjectHealthBarSettings HealthBarSettings => healthBar;
    public Renderer PrimaryRenderer => primaryRenderer;
    public Vector3 CenterPosition => hurtbox != null
        ? hurtbox.bounds.center : transform.position;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => activeObjects.Clear();

    private void Reset()
    {
        states = new List<WorldObjectState>
        {
            new WorldObjectState { damageModel = new HealthPoolDamageModel() }
        };
    }

    private void Awake()
    {
        hurtbox = GetComponent<Collider>();
        primaryRenderer = GetComponentInChildren<Renderer>(true);
        if (!TryGetComponent(out lockOnTarget))
            lockOnTarget = gameObject.AddComponent<WorldLockOnTarget>();
        if (showDamageNumbers && !TryGetComponent(out damageNumbers))
            damageNumbers = gameObject.AddComponent<DamageNumberSpawner>();

        stateById.Clear();
        foreach (WorldObjectState state in states)
        {
            if (state == null || string.IsNullOrEmpty(state.id))
                continue;
            if (!stateById.TryAdd(state.id, state))
                Debug.LogWarning($"WorldObject '{name}': estado '{state.id}' repetido; o segundo foi ignorado.", this);
        }

        EnterState(ResolveInitialState());
    }

    private void OnEnable()
    {
        if (!activeObjects.Contains(this))
            activeObjects.Add(this);
        WorldObjectHealthBarHUD.EnsureExists();
    }

    private void OnDisable() => activeObjects.Remove(this);

    /// <summary>Entra em um estado pelo nome. Ignorado se o nome nao existir.</summary>
    public void SetState(string stateId)
    {
        if (destroyed)
            return;
        if (string.IsNullOrEmpty(stateId) ||
            !stateById.TryGetValue(stateId, out WorldObjectState state))
        {
            Debug.LogWarning($"WorldObject '{name}': estado '{stateId}' nao existe.", this);
            return;
        }

        EnterState(state);
    }

    /// <summary>Volta ao estado inicial, restaurando a vida e as reacoes de entrada.</summary>
    [ContextMenu("Reset World Object")]
    public void ResetObject()
    {
        destroyed = false;
        lastHitTime = float.NegativeInfinity;
        EnterState(ResolveInitialState());
    }

    /// <summary>Destroi o objeto, opcionalmente depois de um atraso.</summary>
    public void DestroyObject(float delay = 0f)
    {
        if (destroyed)
            return;

        destroyed = true;
        if (lockOnTarget != null)
            lockOnTarget.enabled = false;
        onDestroyed?.Invoke();
        Destroyed?.Invoke(this);
        Destroy(gameObject, Mathf.Max(0f, delay));
    }

    public bool ReceivePlayerAttack(int damage, Vector3 impactPosition,
        Vector3 impactDirection)
    {
        WorldObjectState state = currentState;
        if (destroyed || state == null || !state.hittable || damage <= 0)
            return false;

        WorldDamageResult result = state.damageModel != null
            ? state.damageModel.ApplyDamage(damage, Time.time)
            : new WorldDamageResult(true, false);
        if (!result.Accepted)
            return false;

        lastHitTime = Time.time;
        if (showDamageNumbers && damageNumbers != null)
            damageNumbers.ShowDamage(damage);

        WorldObjectContext context = new(this, damage, true, impactPosition,
            impactDirection);
        RunReactions(state.onHit, context);

        // Uma reacao de golpe pode ter mudado o estado; so esgota o estado em
        // que o golpe foi aplicado.
        if (result.Depleted && !destroyed && currentState == state)
            Deplete(state, context);
        return true;
    }

    // --- Barra de vida (lida pelo WorldObjectHealthBarHUD) ---------------

    public bool HasHealthBarData => currentState != null &&
        currentState.damageModel != null && !destroyed;

    public float HealthBarFill => HasHealthBarData
        ? currentState.damageModel.RemainingNormalized(Time.time) : 0f;

    public string HealthBarText => HasHealthBarData && healthBar.showValueText
        ? currentState.damageModel.GetDisplayText(Time.time) : string.Empty;

    public Vector3 HealthBarAnchor
    {
        get
        {
            Bounds bounds = hurtbox != null ? hurtbox.bounds
                : new Bounds(transform.position, Vector3.zero);
            return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) +
                healthBar.worldOffset;
        }
    }

    public bool ShouldShowHealthBar(bool lockedOn)
    {
        if (!HasHealthBarData)
            return false;

        bool recentlyHit = Time.time - lastHitTime <=
            healthBar.visibleSecondsAfterHit;
        return healthBar.mode switch
        {
            WorldObjectHealthBarMode.Always => true,
            WorldObjectHealthBarMode.AfterHit => recentlyHit,
            WorldObjectHealthBarMode.WhenLockedOn => lockedOn,
            WorldObjectHealthBarMode.AfterHitOrLockedOn => recentlyHit || lockedOn,
            _ => false
        };
    }

    // --- Internos --------------------------------------------------------

    private WorldObjectState ResolveInitialState()
    {
        if (!string.IsNullOrEmpty(initialStateId) &&
            stateById.TryGetValue(initialStateId, out WorldObjectState state))
            return state;
        foreach (WorldObjectState candidate in states)
            if (candidate != null)
                return candidate;
        return null;
    }

    private void EnterState(WorldObjectState state)
    {
        if (state == null || stateChainDepth >= MaximumStateChainDepth)
        {
            if (state != null)
                Debug.LogWarning($"WorldObject '{name}': cadeia de mudancas de estado muito longa; interrompida em '{state.id}'.", this);
            return;
        }

        stateChainDepth++;
        try
        {
            currentState = state;
            SetObjectsActive(state.deactivateOnEnter, false);
            SetObjectsActive(state.activateOnEnter, true);
            state.damageModel?.Initialize();
            if (lockOnTarget != null)
                lockOnTarget.enabled = state.lockOnEnabled && !destroyed;

            onStateChanged?.Invoke(state.id);
            StateChanged?.Invoke(this, state.id);
            RunReactions(state.onEnter, new WorldObjectContext(this, 0, false,
                CenterPosition, Vector3.up));
        }
        finally
        {
            stateChainDepth--;
        }
    }

    private void Deplete(WorldObjectState state, WorldObjectContext context)
    {
        RunReactions(state.onDepleted, context);
        if (destroyed || currentState != state)
            return;

        switch (state.depletedAction)
        {
            case WorldObjectDepletedAction.GoToState:
                SetState(state.nextStateId);
                break;
            case WorldObjectDepletedAction.DestroyObject:
                DestroyObject();
                break;
            case WorldObjectDepletedAction.DisableObject:
                gameObject.SetActive(false);
                break;
        }
    }

    private static void RunReactions(List<WorldObjectReaction> reactions,
        WorldObjectContext context)
    {
        if (reactions == null)
            return;

        for (int i = 0; i < reactions.Count; i++)
        {
            WorldObjectReaction reaction = reactions[i];
            if (reaction == null)
                continue;
            try
            {
                reaction.Execute(context);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, context.Owner);
            }
        }
    }

    private static void SetObjectsActive(GameObject[] targets, bool active)
    {
        if (targets == null)
            return;
        foreach (GameObject target in targets)
            if (target != null)
                target.SetActive(active);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !PlayerDebugToolkitHUD.IsVisible)
            return;

        Collider collider = hurtbox != null ? hurtbox : GetComponent<Collider>();
        if (collider == null)
            return;

        Gizmos.color = currentState != null && currentState.hittable
            ? new Color(1f, .45f, .1f, .9f) : new Color(.5f, .5f, .5f, .9f);
        Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
    }
}
