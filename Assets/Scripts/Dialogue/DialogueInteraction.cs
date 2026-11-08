using UnityEngine;

/// <summary>Inicia dialogos pelo arbitrador global de interacoes.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class DialogueInteraction : InteractableBase
{
    [Header("Dialogue")]
    [Tooltip("Sequencia de falas iniciada ao interagir com este NPC.")]
    [SerializeField] private DialogueSequence sequence;
    [Tooltip("Impede interacoes futuras depois que a sequencia for aceita uma vez.")]
    [SerializeField] private bool interactOnce = true;
    [Tooltip("Ativa a Leave Area mesmo quando Leave Early Lines estiver vazia; nesse caso, sair da area encerra o dialogo atual.")]
    [SerializeField] private bool useLeaveArea;
    [Tooltip("Tamanho da Leave Area como porcentagem da Interaction Area. 100% corresponde ao mesmo tamanho; valores menores sao tratados como 100%.")]
    [Min(100f)] [SerializeField] private float leaveAreaPercent = 150f;

    [Header("Prompt")]
    [Tooltip("Texto mostrado ao lado do input de interacao enquanto o jogador esta no alcance.")]
    [SerializeField] private string promptMessage = "Falar";
    [Tooltip("Icone opcional para o input Interact no prompt. Vazio mostra o nome atual da tecla ou botao.")]
    [SerializeField] private Sprite promptIcon;

    private Collider interactionCollider;
    private Collider leaveAreaCollider;
    private PlayerMovement playerMovement;
    private bool hasInteracted;
    private bool dialogueStartedByInteraction;

    public override string InteractionMessage => promptMessage;
    public override Sprite InteractionIcon => promptIcon;

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

    protected override void OnDisable()
    {
        dialogueStartedByInteraction = false;
        base.OnDisable();
    }

    private void Update()
    {
        if (!dialogueStartedByInteraction)
            return;

        if (DialogueController.Instance == null ||
            !DialogueController.Instance.IsShowing(sequence))
            dialogueStartedByInteraction = false;

        if (!dialogueStartedByInteraction || leaveAreaCollider == null)
            return;

        if (playerMovement == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            playerMovement = player != null ? player.GetComponent<PlayerMovement>() : null;
        }
        if (playerMovement != null &&
            !PlayerAreaOverlap.ContainsPlayer(leaveAreaCollider, playerMovement))
            OnLeaveAreaExited();
    }

    public override bool ContainsPlayer(PlayerMovement player)
    {
        if (interactionCollider == null)
            interactionCollider = GetComponent<Collider>();
        return PlayerAreaOverlap.ContainsPlayer(interactionCollider, player);
    }

    public override float DistanceSqrTo(Vector3 position)
    {
        if (interactionCollider == null)
            interactionCollider = GetComponent<Collider>();
        Vector3 center = interactionCollider != null
            ? interactionCollider.bounds.center : transform.position;
        return (center - position).sqrMagnitude;
    }

    public override bool CanInteract(PlayerMovement player) =>
        sequence != null && !(interactOnce && hasInteracted) &&
        !DialogueController.IsInteractionCooldownActive &&
        DialogueController.Instance != null && base.CanInteract(player);

    public override bool Interact(PlayerMovement player)
    {
        if (!CanInteract(player) ||
            !DialogueController.Instance.TryShow(sequence))
            return false;

        hasInteracted = true;
        dialogueStartedByInteraction = true;
        playerMovement = player;
        return true;
    }

    private void CreateLeaveAreaTrigger()
    {
        GameObject areaObject = new("Leave Area (Runtime)");
        areaObject.layer = gameObject.layer;
        areaObject.transform.SetParent(transform, false);
        areaObject.transform.localScale = Vector3.one *
            Mathf.Max(1f, leaveAreaPercent / 100f);

        leaveAreaCollider = CopyCollider(interactionCollider, areaObject);
        if (leaveAreaCollider == null)
        {
            Destroy(areaObject);
            return;
        }

        leaveAreaCollider.isTrigger = true;
        leaveAreaCollider.sharedMaterial = interactionCollider.sharedMaterial;
        leaveAreaCollider.enabled = interactionCollider.enabled;
        areaObject.AddComponent<DialogueLeaveAreaTrigger>().Initialize(this);
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

    private void OnDrawGizmos()
    {
        if (InteractionGizmos.Visible)
            InteractionGizmos.DrawCollider(GetComponent<Collider>(),
                InteractionGizmos.ZoneFill, InteractionGizmos.ZoneLine);
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
            Gizmos.DrawWireCube(box.center, box.size * scale);
        else if (trigger is SphereCollider sphere)
            Gizmos.DrawWireSphere(sphere.center, sphere.radius * scale);
        else if (trigger is CapsuleCollider capsule)
            InteractionGizmos.DrawCollider(capsule, Color.clear,
                new Color(1f, .55f, .12f, .9f));
        else if (trigger is MeshCollider mesh && mesh.sharedMesh != null)
        {
            Bounds bounds = mesh.sharedMesh.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size * scale);
        }

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
}
