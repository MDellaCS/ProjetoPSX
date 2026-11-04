using System.Collections.Generic;
using UnityEngine;

/// <summary>Area contextual que apresenta e recebe o input generico da escada.</summary>
[RequireComponent(typeof(Collider))]
[AddComponentMenu("Projeto PSX/Level Design/Hand Ladder Interaction Area")]
public sealed class HandLadderInteractionArea : MonoBehaviour
{
    private static readonly HashSet<HandLadderInteractionArea> active = new();
    private static int nextOrder;
    private int order;

    [Tooltip("Escada iniciada por esta area. Vazio usa a HandLadder em um objeto pai.")]
    [SerializeField] private HandLadder ladder;
    [Tooltip("Texto mostrado no prompt generico enquanto o jogador esta na area.")]
    [SerializeField] private string interactionMessage = "Escalar";
    [Tooltip("Icone opcional do input Interagir mostrado no prompt.")]
    [SerializeField] private Sprite interactionIcon;

    private Collider interactionCollider;
    private PlayerMovement playerMovement;
    private Transform playerTransform;
    private TemporaryWorldPromptHUD promptHUD;
    private float nextPlayerSearchTime;
    private float nextPromptSearchTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        active.Clear();
        nextOrder = 0;
    }

    private void Awake()
    {
        if (ladder == null)
            ladder = GetComponentInParent<HandLadder>();
        interactionCollider = GetComponent<Collider>();
        if (interactionCollider != null)
            interactionCollider.isTrigger = true;
    }

    private void OnEnable()
    {
        order = nextOrder++;
        active.Add(this);
    }

    private void OnDisable()
    {
        active.Remove(this);
        if (promptHUD != null)
            promptHUD.HideContextualPrompt(this);
        playerMovement = null;
        playerTransform = null;
    }

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null) trigger.isTrigger = true;
    }

    private void Update()
    {
        ResolvePlayer();
        if (ladder == null)
            ladder = GetComponentInParent<HandLadder>();
        if (playerMovement == null || playerTransform == null || ladder == null ||
            !ladder.isActiveAndEnabled || !ContainsPlayer(playerTransform) ||
            GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked ||
            DialogueController.IsDisplaying || !IsNearest())
        {
            HidePrompt();
            return;
        }

        if (promptHUD == null && Time.unscaledTime >= nextPromptSearchTime)
        {
            promptHUD = FindAnyObjectByType<TemporaryWorldPromptHUD>();
            nextPromptSearchTime = Time.unscaledTime + .5f;
        }

        if (promptHUD != null)
            promptHUD.ShowContextualPrompt(this,
                GameInputBindings.GetDisplayString("Interact"),
                interactionMessage, interactionIcon);

        if (GameInput.InteractPressed &&
            playerMovement.TryBeginLadderClimb(ladder))
            HidePrompt();
    }

    private void ResolvePlayer()
    {
        if (playerMovement != null && playerMovement.isActiveAndEnabled &&
            playerTransform != null && playerTransform.gameObject.activeInHierarchy)
            return;
        if (Time.unscaledTime < nextPlayerSearchTime) return;
        nextPlayerSearchTime = Time.unscaledTime + .5f;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        playerMovement = playerObject != null
            ? playerObject.GetComponent<PlayerMovement>() : null;
        playerTransform = playerMovement != null ? playerMovement.transform : null;
    }

    private bool ContainsPlayer(Transform target)
    {
        if (target == null || !isActiveAndEnabled) return false;
        if (interactionCollider == null)
            interactionCollider = GetComponent<Collider>();
        if (interactionCollider == null || !interactionCollider.enabled) return false;
        PlayerCharacterController controller = target.GetComponent<PlayerCharacterController>();
        Vector3 center = controller != null ? controller.bounds.center : target.position;
        return (interactionCollider.ClosestPoint(center) - center).sqrMagnitude <= .0001f;
    }

    private bool IsNearest()
    {
        float distance = (transform.position - playerTransform.position).sqrMagnitude;
        foreach (HandLadderInteractionArea area in active)
        {
            if (area == null || area == this || area.ladder == null ||
                !area.ContainsPlayer(playerTransform)) continue;
            float otherDistance = (area.transform.position - playerTransform.position).sqrMagnitude;
            if (otherDistance < distance ||
                (Mathf.Approximately(otherDistance, distance) && area.order < order))
                return false;
        }
        return true;
    }

    private void HidePrompt()
    {
        if (promptHUD != null)
            promptHUD.HideContextualPrompt(this);
    }
}
