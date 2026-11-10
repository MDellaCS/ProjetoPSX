using UnityEngine;
using System.Collections.Generic;

/// <summary>Coleta item-chave por interacao contextual e mostra a modal reutilizavel.</summary>
[AddComponentMenu("Projeto PSX/Items/Collectable Item")]
[RequireComponent(typeof(InteractionArea))]
public class CollectableItem : InteractableBase
{
    private static readonly HashSet<CollectableItem> knownCollectables = new();
    private static Material sharedGlintMaterial;
    private static Mesh sharedGlintMesh;
    [Header("Item")]
    [Tooltip("Item-chave adicionado ao inventario quando o jogador interage com este coletavel.")]
    [SerializeField] private KeyItemDefinition item;
    [Header("Visual")]
    [Tooltip("Visual generico de brilho do coletavel. Pode ser um sprite, malha ou prefab filho.")]
    [SerializeField] private GameObject glintVisual;
    [Tooltip("Deslocamento local do brilho generico em relacao ao coletavel.")]
    [SerializeField] private Vector3 glintOffset = new(0f, .6f, 0f);

    private bool collected;
    private InteractionArea interactionArea;

    private void OnValidate()
    {
        interactionArea = GetComponent<InteractionArea>();
    }

    private void Reset()
    {
        interactionArea = GetComponent<InteractionArea>();
        if (interactionArea != null)
        {
            interactionArea.enabled = false;
            return;
        }

        interactionArea = gameObject.AddComponent<InteractionArea>();
        interactionArea.enabled = false;
    }

    protected override void OnEnable()
    {
        knownCollectables.Add(this);
        interactionArea = GetComponent<InteractionArea>();
        if (interactionArea == null)
            return;
        InteractionSystem.Register(this);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeVisuals()
    {
        knownCollectables.Clear();
        if (sharedGlintMaterial != null)
            Destroy(sharedGlintMaterial);
        if (sharedGlintMesh != null)
            Destroy(sharedGlintMesh);
        sharedGlintMaterial = null;
        sharedGlintMesh = null;
    }

    public override string InteractionMessage => "Pegar";

    private void Awake()
    {
        knownCollectables.Add(this);
        collected = KeyItemInventory.Contains(item);
        if (collected)
        {
            gameObject.SetActive(false);
            return;
        }

        if (glintVisual == null)
            CreateGenericGlint();
        else
            glintVisual.transform.localPosition = glintOffset;
        if (glintVisual != null)
            glintVisual.SetActive(true);
    }

    private void Update()
    {
        if (glintVisual == null || !glintVisual.activeSelf)
            return;
        glintVisual.transform.Rotate(0f, 45f * Time.deltaTime, 0f, Space.Self);
    }

    private void CreateGenericGlint()
    {
        GameObject glint = new("Generic Key Item Glint");
        glint.name = "Generic Key Item Glint";
        glint.transform.SetParent(transform, false);
        glint.transform.localPosition = glintOffset;
        glint.transform.localScale = Vector3.one * .2f;
        MeshFilter meshFilter = glint.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = GetSharedGlintMesh();
        MeshRenderer renderer = glint.AddComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = GetSharedGlintMaterial();
        glintVisual = glint;
    }

    private static Mesh GetSharedGlintMesh()
    {
        if (sharedGlintMesh != null)
            return sharedGlintMesh;

        sharedGlintMesh = new Mesh { name = "Key Item Glint Diamond (Runtime)" };
        sharedGlintMesh.vertices = new[]
        {
            Vector3.up, Vector3.down, Vector3.right,
            Vector3.left, Vector3.forward, Vector3.back
        };
        sharedGlintMesh.triangles = new[]
        {
            0, 4, 2, 0, 3, 4, 0, 5, 3, 0, 2, 5,
            1, 2, 4, 1, 4, 3, 1, 3, 5, 1, 5, 2
        };
        sharedGlintMesh.RecalculateNormals();
        sharedGlintMesh.RecalculateBounds();
        return sharedGlintMesh;
    }

    private static Material GetSharedGlintMaterial()
    {
        if (sharedGlintMaterial != null)
            return sharedGlintMaterial;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            return null;
        sharedGlintMaterial = new Material(shader) { name = "Key Item Glint (Runtime)" };
        Color color = new(1f, .78f, .18f, 1f);
        if (sharedGlintMaterial.HasProperty("_BaseColor"))
            sharedGlintMaterial.SetColor("_BaseColor", color);
        if (sharedGlintMaterial.HasProperty("_Color"))
            sharedGlintMaterial.SetColor("_Color", color);
        return sharedGlintMaterial;
    }

    public override bool CanInteract(PlayerMovement player) =>
        !collected && item != null && !KeyItemInventory.Contains(item) &&
        base.CanInteract(player);

    public override bool Interact(PlayerMovement player)
    {
        if (!CanInteract(player) || !KeyItemInventory.Grant(item))
            return false;
        collected = true;
        if (glintVisual != null)
            glintVisual.SetActive(false);
        gameObject.SetActive(false);
        return true;
    }

    /// <summary>Restaura coletaveis desativados ao iniciar uma campanha nova.</summary>
    public static void ResetAllForNewGame()
    {
        List<CollectableItem> snapshot = new(knownCollectables);
        for (int i = 0; i < snapshot.Count; i++)
        {
            CollectableItem collectable = snapshot[i];
            if (collectable != null)
                collectable.ResetForNewGame();
        }
    }

    private void ResetForNewGame()
    {
        collected = false;
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (glintVisual == null)
            CreateGenericGlint();
        else
            glintVisual.SetActive(true);
    }

    public override bool ContainsPlayer(PlayerMovement player) =>
        interactionArea != null && interactionArea.ContainsPlayer(player);

    public override float DistanceSqrTo(Vector3 position) => interactionArea != null
        ? interactionArea.DistanceSqrTo(position)
        : base.DistanceSqrTo(position);

    protected override void OnDisable()
    {
        InteractionSystem.Unregister(this);
    }

    private void OnDestroy() => knownCollectables.Remove(this);

    protected override void OnDrawGizmos()
    {
        if (!InteractionGizmos.Visible)
            return;
        Gizmos.color = new Color(1f, .85f, .25f, .9f);
        Gizmos.DrawWireSphere(transform.TransformPoint(glintOffset), .16f);
    }
}
