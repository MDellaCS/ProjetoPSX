using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Area separada do collider fisico; so o terminal mais proximo responde ao input E.</summary>
[RequireComponent(typeof(Collider))]
public sealed class CurrencyShopInteractionArea : MonoBehaviour
{
    private static readonly HashSet<CurrencyShopInteractionArea> active = new();
    private static int nextOrder;
    private int order;
    [Tooltip("Terminal fisico que esta area abre. Vazio usa o terminal nos pais.")]
    [SerializeField] private CurrencyShopTerminal terminal;
    [Tooltip("Mensagem apresentada pelo sistema de prompts enquanto o Player esta na area.")]
    [SerializeField] private string interactionMessage = "Abrir loja";
    [Tooltip("Icone opcional do input E apresentado pelo sistema de prompts.")]
    [SerializeField] private Sprite interactionIcon;
    private Transform player;
    private Collider areaCollider;
    private TemporaryWorldPromptHUD promptHUD;
    private void Awake()
    {
        terminal ??= GetComponentInParent<CurrencyShopTerminal>();
        areaCollider = GetComponent<Collider>();
        areaCollider.isTrigger = true;
    }
    private void OnEnable()
    {
        order = nextOrder++;
        active.Add(this);
    }
    private void OnDisable()
    {
        active.Remove(this);
        promptHUD?.HideContextualPrompt(this);
        player = null;
    }
    private void Reset() => GetComponent<Collider>().isTrigger = true;
    private bool ContainsPlayer(Transform target)
    {
        if (target == null || !isActiveAndEnabled) return false;
        areaCollider ??= GetComponent<Collider>();
        if (areaCollider == null || !areaCollider.enabled) return false;
        CharacterController controller = target.GetComponent<CharacterController>();
        Vector3 center = controller != null ? controller.bounds.center : target.position;
        // ClosestPoint respeita o formato, transform e escala do trigger.
        // A verificacao funciona mesmo se o Player ja nasceu dentro da area
        // ou se a matriz de layers nao entrega callbacks de trigger.
        if (areaCollider is BoxCollider || areaCollider is SphereCollider ||
            areaCollider is CapsuleCollider ||
            areaCollider is MeshCollider mesh && mesh.convex)
            return (areaCollider.ClosestPoint(center) - center).sqrMagnitude <= .0001f;
        return areaCollider.bounds.Contains(center);
    }
    private void ResolvePlayer()
    {
        if (player != null && player.gameObject.activeInHierarchy) return;
        GameObject found = GameObject.FindGameObjectWithTag("Player");
        player = found != null ? found.transform : null;
    }
    private bool IsNearest()
    {
        float distance = (transform.position - player.position).sqrMagnitude;
        foreach (CurrencyShopInteractionArea area in active)
            if (area != this && area != null && area.ContainsPlayer(player) &&
                area.terminal != null && !area.terminal.IsDestroyed)
            {
                float other = (area.transform.position - player.position).sqrMagnitude;
                if (other < distance || (Mathf.Approximately(other, distance) && area.order < order))
                    return false;
            }
        return true;
    }
    private void Update()
    {
        ResolvePlayer();
        if (terminal == null) terminal = GetComponentInParent<CurrencyShopTerminal>();
        if (player == null || terminal == null || terminal.IsDestroyed || !ContainsPlayer(player))
        { promptHUD?.HideContextualPrompt(this); return; }
        if (GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked || !IsNearest())
        { promptHUD?.HideContextualPrompt(this); return; }
        if (promptHUD == null) promptHUD = FindAnyObjectByType<TemporaryWorldPromptHUD>();
        promptHUD?.ShowContextualPrompt(this, "E", interactionMessage, interactionIcon);
        if (Keyboard.current?.eKey.wasPressedThisFrame == true && terminal.OpenShop(player))
            promptHUD?.HideContextualPrompt(this);
    }
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !PlayerDebugToolkitHUD.IsVisible) return;
        Collider area = GetComponent<Collider>();
        if (area == null) return;
        Gizmos.color = new Color(1f, .7f, .15f);
        Gizmos.DrawWireCube(area.bounds.center, area.bounds.size);
    }
}
