using UnityEngine;

/// <summary>
/// Mostra um prompt contextual perto do jogador e abre uma sequencia de
/// dialogo quando a acao reconfiguravel Interact e pressionada.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class DialogueInteraction : MonoBehaviour
{
    [Header("Dialogue")]
    [Tooltip("Sequencia de falas iniciada ao interagir com este NPC.")]
    [SerializeField] private DialogueSequence sequence;
    [Tooltip("Impede interacoes futuras depois que a sequencia for aceita uma vez.")]
    [SerializeField] private bool interactOnce = true;
    [Tooltip("Ativa a Leave Area mesmo quando Leave Early Lines estiver vazia; nesse caso, sair da area encerra o dialogo atual.")]
    [SerializeField] private bool useLeaveArea;
    [Tooltip("Tamanho da Leave Area como porcentagem das dimensoes da Interaction Area. 100% corresponde ao mesmo tamanho; valores menores sao tratados como 100%.")]
    [Min(100f)] [SerializeField] private float leaveAreaPercent = 150f;

    [Header("Prompt")]
    [Tooltip("Texto mostrado ao lado do input de interacao enquanto o jogador esta no alcance.")]
    [SerializeField] private string promptMessage = "Falar";
    [Tooltip("Icone opcional para o input Interact no prompt. Vazio mostra o nome atual da tecla ou botao.")]
    [SerializeField] private Sprite promptIcon;

    private Collider interactionCollider;
    private TemporaryWorldPromptHUD promptHUD;
    private bool playerInside;
    private bool hasInteracted;
    private bool dialogueStartedByInteraction;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void Awake()
    {
        interactionCollider = GetComponent<Collider>();
        if (interactionCollider != null)
            interactionCollider.isTrigger = true;

        if (sequence != null && (useLeaveArea || sequence.HasLeaveEarlyLines) &&
            interactionCollider != null)
            CreateLeaveAreaTrigger();
    }

    private void OnDisable()
    {
        playerInside = false;
        dialogueStartedByInteraction = false;
        promptHUD?.HideContextualPrompt(this);
    }

    private void Update()
    {
        if (dialogueStartedByInteraction &&
            (DialogueController.Instance == null ||
             !DialogueController.Instance.IsShowing(sequence)))
            dialogueStartedByInteraction = false;

        if (interactOnce && hasInteracted)
        {
            promptHUD?.HideContextualPrompt(this);
            return;
        }

        if (!playerInside || GamePauseController.IsPaused ||
            CutsceneController.IsPlayerControlLocked ||
            DialogueController.IsDisplaying)
        {
            promptHUD?.HideContextualPrompt(this);
            return;
        }

        if (promptHUD == null)
            promptHUD = FindAnyObjectByType<TemporaryWorldPromptHUD>();

        promptHUD?.ShowContextualPrompt(this,
            GameInputBindings.GetDisplayString("Interact"),
            promptMessage, promptIcon);

        if (!GameInput.InteractPressed || sequence == null ||
            DialogueController.Instance == null ||
            !DialogueController.Instance.TryShow(sequence))
            return;

        hasInteracted = true;
        dialogueStartedByInteraction = true;
        promptHUD?.HideContextualPrompt(this);
    }

    private void CreateLeaveAreaTrigger()
    {
        GameObject areaObject = new GameObject("Leave Area (Runtime)");
        areaObject.layer = gameObject.layer;
        areaObject.transform.SetParent(transform, false);
        areaObject.transform.localScale = Vector3.one *
            Mathf.Max(1f, leaveAreaPercent / 100f);

        Collider copiedCollider = CopyCollider(interactionCollider, areaObject);
        if (copiedCollider == null)
        {
            Destroy(areaObject);
            return;
        }

        copiedCollider.isTrigger = true;
        copiedCollider.sharedMaterial = interactionCollider.sharedMaterial;
        copiedCollider.enabled = interactionCollider.enabled;
        areaObject.AddComponent<DialogueLeaveAreaTrigger>().Initialize(this);
    }

    private static Collider CopyCollider(Collider source, GameObject target)
    {
        if (source is BoxCollider box)
        {
            BoxCollider copy = target.AddComponent<BoxCollider>();
            copy.center = box.center;
            copy.size = box.size;
            return copy;
        }
        if (source is SphereCollider sphere)
        {
            SphereCollider copy = target.AddComponent<SphereCollider>();
            copy.center = sphere.center;
            copy.radius = sphere.radius;
            return copy;
        }
        if (source is CapsuleCollider capsule)
        {
            CapsuleCollider copy = target.AddComponent<CapsuleCollider>();
            copy.center = capsule.center;
            copy.radius = capsule.radius;
            copy.height = capsule.height;
            copy.direction = capsule.direction;
            return copy;
        }
        if (source is MeshCollider mesh)
        {
            MeshCollider copy = target.AddComponent<MeshCollider>();
            copy.sharedMesh = mesh.sharedMesh;
            copy.convex = mesh.convex;
            return copy;
        }

        return null;
    }

    internal void OnLeaveAreaExited()
    {
        if (!dialogueStartedByInteraction)
            return;

        dialogueStartedByInteraction = false;
        DialogueController controller = DialogueController.Instance;
        if (controller != null && controller.IsShowing(sequence))
            controller.HandleLeaveArea(sequence);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
            playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerInside = false;
        promptHUD?.HideContextualPrompt(this);
    }

    private void OnDrawGizmosSelected()
    {
        if (sequence == null || (!useLeaveArea &&
            !sequence.HasLeaveEarlyLines))
            return;

        Collider trigger = GetComponent<Collider>();
        if (trigger == null)
            return;

        float scale = Mathf.Max(1f, leaveAreaPercent / 100f);
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, .55f, .12f, .9f);

        if (trigger is BoxCollider box)
        {
            Gizmos.DrawWireCube(box.center, box.size * scale);
        }
        else if (trigger is SphereCollider sphere)
        {
            Gizmos.DrawWireSphere(sphere.center, sphere.radius * scale);
        }
        else if (trigger is CapsuleCollider capsule)
        {
            DrawCapsuleGizmo(capsule, scale);
        }
        else if (trigger is MeshCollider mesh && mesh.sharedMesh != null)
        {
            Bounds bounds = mesh.sharedMesh.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size * scale);
        }

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    private static void DrawCapsuleGizmo(CapsuleCollider capsule, float scale)
    {
        Vector3 axis = capsule.direction == 0 ? Vector3.right :
            capsule.direction == 2 ? Vector3.forward : Vector3.up;
        float radius = capsule.radius * scale;
        float halfSegment = Mathf.Max(0f,
            capsule.height * .5f - capsule.radius) * scale;
        Vector3 first = capsule.center - axis * halfSegment;
        Vector3 second = capsule.center + axis * halfSegment;

        Gizmos.DrawWireSphere(first, radius);
        Gizmos.DrawWireSphere(second, radius);

        Vector3 sideA = capsule.direction == 0 ? Vector3.up : Vector3.right;
        Vector3 sideB = Vector3.Cross(axis, sideA).normalized;
        for (int sign = -1; sign <= 1; sign += 2)
        {
            Vector3 offsetA = sideA * radius * sign;
            Vector3 offsetB = sideB * radius * sign;
            Gizmos.DrawLine(first + offsetA, second + offsetA);
            Gizmos.DrawLine(first + offsetB, second + offsetB);
        }
    }

    private static bool IsPlayer(Collider other) => other != null &&
        (other.GetComponentInParent<PlayerMovement>() != null ||
         other.CompareTag("Player"));
}
