using UnityEngine;

public enum InteractionActivationMode
{
    [InspectorName("Toque unico")]
    Press,
    [InspectorName("Segurar")]
    Hold
}

/// <summary>Comportamento acionado pelo arbitrador global de interacoes.</summary>
public abstract class InteractableBase : MonoBehaviour
{
    private readonly RaycastHit[] visibilityHits = new RaycastHit[16];
    private bool interactionUsed;
    private float nextInteractionTime;

    public InteractionProfile EffectiveInteractionProfile => ResolveProfile();
    protected InteractionProfile InteractionSettings => EffectiveInteractionProfile;
    public int ResolvedInteractionPriority => EffectiveInteractionProfile.Priority;
    public InteractionActivationMode ActivationMode => EffectiveInteractionProfile.ActivationMode;
    public float HoldDuration => EffectiveInteractionProfile.HoldDuration;
    public string ResolvedInteractionMessage =>
        !string.IsNullOrWhiteSpace(EffectiveInteractionProfile.PromptMessage)
            ? EffectiveInteractionProfile.PromptMessage : InteractionMessage;
    public Sprite ResolvedInteractionIcon => EffectiveInteractionProfile.PromptIcon != null
        ? EffectiveInteractionProfile.PromptIcon : InteractionIcon;

    public abstract string InteractionMessage { get; }
    public virtual Sprite InteractionIcon => null;

    public virtual bool ContainsPlayer(PlayerMovement player)
    {
        return InteractionArea.ContainsPlayer(InteractionVolumeTransform(),
            EffectiveInteractionProfile, player);
    }

    public virtual bool CanInteract(PlayerMovement player)
    {
        InteractionProfile profile = EffectiveInteractionProfile;
        if (player == null || (profile.RequireGrounded && !player.IsGrounded) ||
            (profile.InteractOnce && interactionUsed) || Time.unscaledTime < nextInteractionTime)
            return false;
        return !profile.BlockByGeometry || HasClearPath(player);
    }

    public bool IsInsideInteractionArea(PlayerMovement player) => ContainsPlayer(player);
    public float InteractionDistanceSqrTo(Vector3 position) => DistanceSqrTo(position);

    public virtual float DistanceSqrTo(Vector3 position) =>
        InteractionArea.DistanceSqrTo(InteractionVolumeTransform(), EffectiveInteractionProfile, position);

    protected Transform InteractionVolumeTransform()
    {
        InteractionArea area = GetComponent<InteractionArea>();
        return area != null && area != this ? area.transform : transform;
    }

    public virtual void SetHoldProgress(float progress) { }
    public abstract bool Interact(PlayerMovement player);

    public bool TryActivate(PlayerMovement player)
    {
        if (!CanInteract(player) || !Interact(player))
            return false;
        interactionUsed = true;
        nextInteractionTime = Time.unscaledTime + EffectiveInteractionProfile.Cooldown;
        return true;
    }

    internal void ResetInteractionState()
    {
        interactionUsed = false;
        nextInteractionTime = 0f;
    }

    private InteractionProfile ResolveProfile()
    {
        if (this is InteractionArea ownArea && ownArea.AssignedProfile != null)
            return ownArea.AssignedProfile;
        InteractionArea sibling = GetComponent<InteractionArea>();
        if (sibling != null && sibling != this)
            return sibling.AssignedProfile;
        return InteractionProfile.DefaultFor(this);
    }

    private bool HasClearPath(PlayerMovement player)
    {
        Vector3 origin = PlayerCenter(player);
        Vector3 target = InteractionArea.GetWorldCenter(InteractionVolumeTransform(),
            EffectiveInteractionProfile);
        Vector3 delta = target - origin;
        float distance = delta.magnitude;
        if (distance <= .05f)
            return true;
        int count = Physics.RaycastNonAlloc(origin, delta / distance, visibilityHits,
            distance, EffectiveInteractionProfile.BlockingLayers, QueryTriggerInteraction.Ignore);
        if (count >= visibilityHits.Length)
            return false;
        for (int i = 0; i < count; i++)
        {
            Collider hit = visibilityHits[i].collider;
            if (hit == null || IsChildOf(hit.transform, player.transform) ||
                IsChildOf(hit.transform, transform))
                continue;
            return false;
        }
        return true;
    }

    private static bool IsChildOf(Transform candidate, Transform parent) =>
        candidate == parent || candidate.IsChildOf(parent);

    protected virtual void OnEnable()
    {
        InteractionArea area = GetComponent<InteractionArea>();
        if (area != null && area != this)
        {
            InteractionSystem.Register(area.GetInteractionHandler(this));
            return;
        }
        InteractionSystem.Register(this);
    }

    protected virtual void OnDisable() => InteractionSystem.Unregister(this);

    protected virtual void OnDrawGizmos()
    {
        if (!InteractionGizmos.Visible || GetComponent<InteractionArea>() != null)
            return;
        InteractionArea.DrawProfileGizmo(InteractionVolumeTransform(), EffectiveInteractionProfile);
    }

    public static Vector3 PlayerCenter(PlayerMovement player)
    {
        if (player == null)
            return Vector3.zero;
        PlayerCharacterController motor = player.EntityCharacterController;
        return motor != null ? motor.bounds.center : player.transform.position;
    }
}
