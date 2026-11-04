using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Apresentacao visual do nascimento de um inimigo. Nao controla IA, ondas ou
/// regras de spawn; apenas reserva o inimigo ate a apresentacao terminar.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyBase))]
public sealed class EnemySpawnPresentation : MonoBehaviour
{
    [Header("Ativacao")]
    [Tooltip("Toca a apresentacao sempre que este inimigo entra em uma wave de encounter.")]
    [SerializeField] private bool playOnEncounterSpawn = true;

    [Header("Esfera")]
    [Tooltip("Cor da esfera que anuncia o nascimento do inimigo.")]
    [SerializeField] private Color sphereColor = new(.2f, .9f, 1f, .8f);
    [Tooltip("Escala da esfera no primeiro frame do spawn.")]
    [Min(0f)] [SerializeField] private float initialSphereScale = .05f;
    [Tooltip("Escala maxima alcançada pela esfera antes de desaparecer.")]
    [Min(.01f)] [SerializeField] private float finalSphereScale = 1.5f;
    [Tooltip("Tempo que a esfera leva para crescer.")]
    [Min(0f)] [SerializeField] private float growthDuration = .35f;
    [Tooltip("Tempo em que a esfera permanece no tamanho maximo antes do inimigo surgir.")]
    [Min(0f)] [SerializeField] private float holdDuration = .05f;
    [Tooltip("Curva usada durante o crescimento da esfera.")]
    [SerializeField] private AnimationCurve growthCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private EnemyBase enemy;
    private Coroutine routine;
    private readonly List<RendererState> hiddenRenderers = new();
    private readonly List<ColliderState> disabledColliders = new();
    private CharacterController controller;
    private bool controllerWasEnabled;
    private bool controllerStateCaptured;
    private GameObject sphere;
    private Material sphereMaterial;

    private readonly struct RendererState
    {
        public readonly Renderer Renderer;
        public readonly bool WasEnabled;
        public RendererState(Renderer renderer) { Renderer = renderer; WasEnabled = renderer.enabled; }
    }

    private readonly struct ColliderState
    {
        public readonly Collider Collider;
        public readonly bool WasEnabled;
        public ColliderState(Collider collider) { Collider = collider; WasEnabled = collider.enabled; }
    }

    public bool IsPlaying => routine != null;

    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
        controller = GetComponent<CharacterController>();
    }

    /// <summary>Inicia o efeito. Retorna falso quando ele esta desativado.</summary>
    public bool PlayEncounterSpawn()
    {
        if (!playOnEncounterSpawn)
            return false;

        if (routine != null)
            StopCoroutine(routine);
        DestroySphere();
        RestoreEnemy();
        routine = StartCoroutine(PlayRoutine());
        return true;
    }

    private IEnumerator PlayRoutine()
    {
        if (enemy == null)
            enemy = GetComponent<EnemyBase>();
        if (enemy == null)
            yield break;

        enemy.SetSpawning(true);
        HideEnemy();
        CreateSphere();

        float elapsed = 0f;
        float safeGrowth = Mathf.Max(0f, growthDuration);
        while (elapsed < safeGrowth)
        {
            elapsed += Time.deltaTime;
            float progress = safeGrowth <= 0f ? 1f : Mathf.Clamp01(elapsed / safeGrowth);
            SetSphereScale(Mathf.Lerp(initialSphereScale, finalSphereScale,
                growthCurve.Evaluate(progress)));
            yield return null;
        }
        SetSphereScale(finalSphereScale);

        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        DestroySphere();
        RestoreEnemy();
        enemy.SetSpawning(false);
        routine = null;
    }

    private void HideEnemy()
    {
        hiddenRenderers.Clear();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            hiddenRenderers.Add(new RendererState(renderer));
            renderer.enabled = false;
        }

        // CharacterController tambem deriva de Collider. Capture seu estado
        // antes da lista generica e o exclua dela; antes ele era desativado
        // pelo foreach e o estado ja falso era salvo como se fosse original,
        // deixando o inimigo sem colisao/IA ao terminar o spawn.
        if (controller != null)
        {
            controllerWasEnabled = controller.enabled;
            controllerStateCaptured = true;
            controller.enabled = false;
        }

        disabledColliders.Clear();
        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
        {
            if (collider == controller)
                continue;
            disabledColliders.Add(new ColliderState(collider));
            collider.enabled = false;
        }
    }

    private void RestoreEnemy()
    {
        foreach (RendererState state in hiddenRenderers)
            if (state.Renderer != null) state.Renderer.enabled = state.WasEnabled;
        hiddenRenderers.Clear();
        foreach (ColliderState state in disabledColliders)
            if (state.Collider != null) state.Collider.enabled = state.WasEnabled;
        disabledColliders.Clear();
        if (controller != null && controllerStateCaptured)
        {
            controller.enabled = controllerWasEnabled;
            controllerStateCaptured = false;
        }
    }

    private void CreateSphere()
    {
        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Encounter Spawn Sphere";
        Collider sphereCollider = sphere.GetComponent<Collider>();
        if (sphereCollider != null)
        {
            sphereCollider.enabled = false;
            Destroy(sphereCollider);
        }
        sphere.transform.SetPositionAndRotation(enemy.AttackOrigin, Quaternion.identity);
        sphere.transform.SetParent(transform, true);

        Renderer renderer = sphere.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader != null)
        {
            sphereMaterial = new Material(shader) { color = sphereColor };
            renderer.sharedMaterial = sphereMaterial;
        }
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        SetSphereScale(initialSphereScale);
    }

    private void SetSphereScale(float scale)
    {
        if (sphere != null)
            sphere.transform.localScale = Vector3.one * Mathf.Max(0f, scale);
    }

    private void DestroySphere()
    {
        if (sphere != null) Destroy(sphere);
        if (sphereMaterial != null) Destroy(sphereMaterial);
        sphere = null;
        sphereMaterial = null;
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        DestroySphere();
        RestoreEnemy();
        if (enemy != null) enemy.SetSpawning(false);
    }
}
