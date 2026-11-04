using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerHitboxController : MonoBehaviour
{
    private sealed class TrackState { public Vector3 position; public Quaternion rotation; public float radius; public bool valid; }
    private readonly Dictionary<string, PlayerHitboxAnchor> anchors = new();
    private readonly Dictionary<PlayerHitboxTrackDefinition, TrackState> trackStates = new();
    private readonly Dictionary<(PlayerAttackStrikeDefinition, int), HashSet<EnemyBase>> groupHits = new();
    private readonly Dictionary<(PlayerAttackStrikeDefinition, int), HashSet<IPlayerAttackTarget>> utilityGroupHits = new();
    private readonly Dictionary<(PlayerHitboxTrackDefinition, IPlayerAttackTarget), float> utilityRepeatTimes = new();
    private readonly HashSet<PlayerAttackStrikeDefinition> startedStrikes = new();
    private readonly HashSet<PlayerHitboxTrackDefinition> activeTrackWindows = new();
    private readonly Dictionary<(PlayerHitboxTrackDefinition, EnemyBase), float> repeatTimes = new();
    [Tooltip("Camadas consultadas pelas hitboxes de ataques. Inclua os inimigos e objetos interativos que podem receber golpes.")]
    [SerializeField] private LayerMask hitboxQueryMask = Physics.DefaultRaycastLayers;
    private Collider[] overlapBuffer = new Collider[64];
    private readonly List<MonoBehaviour> componentBuffer = new();
    private readonly List<DebugVolume> debugVolumes = new();
    private PlayerAttackRuntime runtime;
    private PlayerAttackResolver resolver;
    private PlayerAttackAudioController attackAudio;
    private PlayerCharacterController characterController;

    private struct DebugVolume { public PlayerHitboxShape shape; public Vector3 center, a, b, size; public Quaternion rotation; public float radius; }

    private void Awake()
    {
        resolver = GetComponent<PlayerAttackResolver>();
        attackAudio = GetComponent<PlayerAttackAudioController>();
        characterController = GetComponent<PlayerCharacterController>();
        foreach (PlayerHitboxAnchor anchor in GetComponentsInChildren<PlayerHitboxAnchor>(true))
            if (anchor != null && !string.IsNullOrWhiteSpace(anchor.AnchorId)) anchors[anchor.AnchorId] = anchor;
    }

    public void Begin(PlayerAttackRuntime attack)
    {
        runtime = attack; trackStates.Clear(); groupHits.Clear(); utilityGroupHits.Clear(); repeatTimes.Clear(); utilityRepeatTimes.Clear();
        startedStrikes.Clear(); activeTrackWindows.Clear(); debugVolumes.Clear();
    }
    public void End()
    {
        runtime = null;
        activeTrackWindows.Clear();
        debugVolumes.Clear();
        trackStates.Clear();
    }

    public void Tick(float activeProgress)
    {
        if (runtime == null) return;
        PlayerAttackRuntime activeRuntime = runtime;
        debugVolumes.Clear();
        activeTrackWindows.Clear();
        foreach (PlayerAttackStrikeDefinition strike in activeRuntime.Definition.strikes)
        {
            if (strike == null || activeProgress < strike.activeStartNormalized ||
                activeProgress > strike.activeEndNormalized ||
                strike.hitboxes == null)
            {
                continue;
            }

            float strikeProgress = Mathf.InverseLerp(
                strike.activeStartNormalized, strike.activeEndNormalized,
                activeProgress);
            foreach (PlayerHitboxTrackDefinition track in strike.hitboxes)
            {
                if (track != null &&
                    strikeProgress >= track.activeStartNormalized &&
                    strikeProgress <= track.activeEndNormalized)
                {
                    activeTrackWindows.Add(track);
                }
            }
        }

        attackAudio ??= GetComponent<PlayerAttackAudioController>();
        attackAudio?.SyncActiveHitboxTracks(activeRuntime,
            activeTrackWindows);

        foreach (PlayerAttackStrikeDefinition strike in activeRuntime.Definition.strikes)
        {
            if (strike == null || strike.hitboxes == null ||
                activeProgress < strike.activeStartNormalized ||
                activeProgress > strike.activeEndNormalized)
            {
                continue;
            }
            if (startedStrikes.Add(strike))
            {
                if (strike.effects.selfLaunchDown)
                    resolver.ApplySelfLaunchDown();
                if (strike.effects.selfLaunchUp)
                    resolver.ApplySelfLaunchUp();
            }
            float strikeProgress = Mathf.InverseLerp(strike.activeStartNormalized, strike.activeEndNormalized, activeProgress);
            foreach (PlayerHitboxTrackDefinition track in strike.hitboxes)
            {
                EvaluateTrack(strike, track, strikeProgress);
                // Um alvo interativo pode iniciar uma cutscene e cancelar o
                // ataque no meio desta consulta. Nao continue acessando o
                // runtime que acabou de ser encerrado nesse mesmo frame.
                if (runtime != activeRuntime)
                    return;
            }
        }
    }

    private void EvaluateTrack(PlayerAttackStrikeDefinition strike, PlayerHitboxTrackDefinition track, float strikeProgress)
    {
        if (track == null || strikeProgress < track.activeStartNormalized || strikeProgress > track.activeEndNormalized) return;
        PlayerAttackRuntime activeRuntime = runtime;
        if (activeRuntime == null)
            return;
        float progress = Mathf.InverseLerp(track.activeStartNormalized, track.activeEndNormalized, strikeProgress);
        GetPose(track, progress, out Vector3 center, out Quaternion rotation,
            out Vector3 a, out Vector3 b, out float effectiveRadius);
        if (!trackStates.TryGetValue(track, out TrackState state)) { state = new TrackState(); trackStates.Add(track, state); }
        int steps = 1;
        if (track.sweep && state.valid)
            steps = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(state.position, center) / track.sweepStepDistance), 1, 24);
        Vector3 previousSampleCenter = state.valid
            ? state.position
            : GetInitialImpactDirectionReference(track, progress, center, rotation);
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 sampleCenter = state.valid ? Vector3.Lerp(state.position, center, t) : center;
            Quaternion sampleRotation = state.valid ? Quaternion.Slerp(state.rotation, rotation, t) : rotation;
            float sampleRadius = state.valid
                ? Mathf.Lerp(state.radius, effectiveRadius, t)
                : effectiveRadius;
            Vector3 impactDirection = sampleCenter - previousSampleCenter;
            if (impactDirection.sqrMagnitude <= .0001f)
                impactDirection = GetFallbackImpactDirection(track, progress, center, rotation);
            Query(strike, track, sampleCenter, sampleRotation,
                a + (sampleCenter - center), b + (sampleCenter - center),
                sampleRadius, impactDirection);
            if (runtime != activeRuntime)
                return;
            if (activeRuntime.ExtendedArmHitboxRadius > 0f)
                QueryExtendedArm(strike, track, GetPlayerCenter() +
                    transform.up * activeRuntime.ExtendedArmHitboxHeightOffset,
                    sampleCenter, activeRuntime.ExtendedArmHitboxRadius, impactDirection);
            if (runtime != activeRuntime)
                return;
            previousSampleCenter = sampleCenter;
        }
        state.position = center; state.rotation = rotation;
        state.radius = effectiveRadius; state.valid = true;
        debugVolumes.Add(new DebugVolume { shape = track.shape, center = center, rotation = rotation, a = a, b = b, radius = effectiveRadius, size = track.boxSize });
        if (activeRuntime.ExtendedArmHitboxRadius > 0f)
        {
            Vector3 extendedArmOrigin = GetPlayerCenter() +
                transform.up * activeRuntime.ExtendedArmHitboxHeightOffset;
            debugVolumes.Add(new DebugVolume
            {
                shape = PlayerHitboxShape.Capsule,
                a = extendedArmOrigin,
                b = center,
                center = (extendedArmOrigin + center) * .5f,
                radius = activeRuntime.ExtendedArmHitboxRadius
            });
        }
    }

    private void Query(PlayerAttackStrikeDefinition strike, PlayerHitboxTrackDefinition track,
        Vector3 center, Quaternion rotation, Vector3 a, Vector3 b,
        float effectiveRadius, Vector3 impactDirection)
    {
        int count = track.shape switch
        {
            PlayerHitboxShape.Capsule => QueryCapsule(a, b, effectiveRadius),
            PlayerHitboxShape.Box => QueryBox(center, track.boxSize * .5f, rotation),
            _ => QuerySphere(center, effectiveRadius)
        };
        float reach = CalculateHorizontalReach(track, center, rotation, a, b,
            effectiveRadius);
        ResolveHits(strike, track, count, reach, center, impactDirection);
    }

    private void QueryExtendedArm(PlayerAttackStrikeDefinition strike,
        PlayerHitboxTrackDefinition track, Vector3 playerCenter,
        Vector3 hitboxCenter, float radius, Vector3 impactDirection)
    {
        int count = QueryCapsule(playerCenter, hitboxCenter, radius);
        float reach = Vector2.Distance(ToHorizontal(playerCenter),
            ToHorizontal(hitboxCenter)) + radius;
        ResolveHits(strike, track, count, reach, hitboxCenter, impactDirection);
    }

    private void ResolveHits(PlayerAttackStrikeDefinition strike,
        PlayerHitboxTrackDefinition track, int count, float attackReach,
        Vector3 hitboxCenter, Vector3 impactDirection)
    {
        var groupKey = (strike, track.hitGroup);
        if (!groupHits.TryGetValue(groupKey, out HashSet<EnemyBase> hitSet))
        { hitSet = new HashSet<EnemyBase>(); groupHits.Add(groupKey, hitSet); }
        for (int i = 0; i < count; i++)
        {
            if (overlapBuffer[i] != null &&
                overlapBuffer[i].GetComponent<CurrencyShopInteractionArea>() != null)
                continue;
            EnemyBase enemy = overlapBuffer[i] != null ? overlapBuffer[i].GetComponentInParent<EnemyBase>() : null;
            if (enemy == null)
            {
                IPlayerAttackTarget target = FindUtilityTarget(overlapBuffer[i]);
                if (target == null) continue;
                if (!utilityGroupHits.TryGetValue(groupKey,
                    out HashSet<IPlayerAttackTarget> utilityHitSet))
                {
                    utilityHitSet = new HashSet<IPlayerAttackTarget>();
                    utilityGroupHits.Add(groupKey, utilityHitSet);
                }
                if (!track.allowRepeatHits && utilityHitSet.Contains(target)) continue;
                var utilityKey = (track, target);
                if (track.allowRepeatHits &&
                    utilityRepeatTimes.TryGetValue(utilityKey, out float utilityNext) &&
                    Time.time < utilityNext) continue;
                utilityHitSet.Add(target);
                utilityRepeatTimes[utilityKey] = Time.time + track.repeatHitInterval;
                Vector3 utilityImpact = GetSafeImpactPoint(overlapBuffer[i],
                    hitboxCenter);
                resolver.ResolveUtilityTarget(runtime, strike, target,
                    utilityImpact, impactDirection, track, overlapBuffer[i]);
                continue;
            }
            var key = (track, enemy);
            if (!track.allowRepeatHits && hitSet.Contains(enemy)) continue;
            if (track.allowRepeatHits && repeatTimes.TryGetValue(key, out float next) && Time.time < next) continue;
            hitSet.Add(enemy); repeatTimes[key] = Time.time + track.repeatHitInterval;
            Vector3 impactPosition = GetSafeImpactPoint(overlapBuffer[i],
                hitboxCenter);
            resolver.Resolve(runtime, strike, enemy, attackReach, impactPosition,
                impactDirection, track, overlapBuffer[i]);
        }
    }

    private IPlayerAttackTarget FindUtilityTarget(Collider collider)
    {
        if (collider == null) return null;
        componentBuffer.Clear();
        collider.GetComponentsInParent(true, componentBuffer);
        foreach (MonoBehaviour behaviour in componentBuffer)
            if (behaviour is IPlayerAttackTarget target)
                return target;
        return null;
    }

    private int QueryCapsule(Vector3 a, Vector3 b, float radius)
    {
        int count;
        while (true)
        {
            count = Physics.OverlapCapsuleNonAlloc(a, b, radius,
                overlapBuffer, hitboxQueryMask, QueryTriggerInteraction.Collide);
            if (count < overlapBuffer.Length)
                return count;
            System.Array.Resize(ref overlapBuffer, overlapBuffer.Length * 2);
        }
    }

    private int QueryBox(Vector3 center, Vector3 halfExtents,
        Quaternion rotation)
    {
        int count;
        while (true)
        {
            count = Physics.OverlapBoxNonAlloc(center, halfExtents,
                overlapBuffer, rotation, hitboxQueryMask,
                QueryTriggerInteraction.Collide);
            if (count < overlapBuffer.Length)
                return count;
            System.Array.Resize(ref overlapBuffer, overlapBuffer.Length * 2);
        }
    }

    private int QuerySphere(Vector3 center, float radius)
    {
        int count;
        while (true)
        {
            count = Physics.OverlapSphereNonAlloc(center, radius,
                overlapBuffer, hitboxQueryMask, QueryTriggerInteraction.Collide);
            if (count < overlapBuffer.Length)
                return count;
            System.Array.Resize(ref overlapBuffer, overlapBuffer.Length * 2);
        }
    }

    /// <summary>
    /// MeshColliders nao convexos nao permitem Collider.ClosestPoint. Eles
    /// sao apropriados para geometria estatica de mundo, como uma alavanca
    /// modelada, portanto usa-se o ponto mais proximo dos bounds como uma
    /// aproximacao segura para efeitos de impacto.
    /// </summary>
    private static Vector3 GetSafeImpactPoint(Collider collider,
        Vector3 referencePoint)
    {
        if (collider == null)
            return referencePoint;

        if (collider is MeshCollider meshCollider && !meshCollider.convex)
            return meshCollider.bounds.ClosestPoint(referencePoint);

        return collider.ClosestPoint(referencePoint);
    }

    private Vector3 GetPlayerCenter() => transform.position +
        transform.rotation * (characterController != null
            ? characterController.center
            : Vector3.zero);

    private float CalculateHorizontalReach(PlayerHitboxTrackDefinition track,
        Vector3 center, Quaternion rotation, Vector3 capsuleA, Vector3 capsuleB,
        float radius)
    {
        float distanceFromPlayer = Vector2.Distance(ToHorizontal(GetPlayerCenter()),
            ToHorizontal(center));
        float horizontalExtent = track.shape switch
        {
            PlayerHitboxShape.Capsule => Mathf.Max(
                Vector2.Distance(ToHorizontal(center), ToHorizontal(capsuleA)),
                Vector2.Distance(ToHorizontal(center), ToHorizontal(capsuleB))) + radius,
            PlayerHitboxShape.Box => CalculateBoxHorizontalExtent(track.boxSize,
                rotation),
            _ => radius
        };
        return distanceFromPlayer + horizontalExtent;
    }

    private static float CalculateBoxHorizontalExtent(Vector3 size,
        Quaternion rotation)
    {
        Vector3 half = size * .5f;
        Vector3 x = rotation * Vector3.right * half.x;
        Vector3 y = rotation * Vector3.up * half.y;
        Vector3 z = rotation * Vector3.forward * half.z;
        return new Vector2(x.x, x.z).magnitude +
            new Vector2(y.x, y.z).magnitude +
            new Vector2(z.x, z.z).magnitude;
    }

    private static Vector2 ToHorizontal(Vector3 value) => new(value.x, value.z);

    private Vector3 GetInitialImpactDirectionReference(
        PlayerHitboxTrackDefinition track, float progress, Vector3 center,
        Quaternion rotation)
    {
        GetPose(track, Mathf.Max(0f, progress - .02f), out Vector3 previous,
            out _, out _, out _, out _);
        return (previous - center).sqrMagnitude > .0001f
            ? previous : center - GetFallbackImpactDirection(track, progress, center, rotation);
    }

    private Vector3 GetFallbackImpactDirection(PlayerHitboxTrackDefinition track,
        float progress, Vector3 center, Quaternion rotation)
    {
        GetPose(track, Mathf.Min(1f, progress + .02f), out Vector3 next,
            out _, out _, out _, out _);
        Vector3 direction = next - center;
        return direction.sqrMagnitude > .0001f
            ? direction.normalized : rotation * Vector3.forward;
    }

    private void GetPose(PlayerHitboxTrackDefinition track, float progress, out Vector3 center,
        out Quaternion rotation, out Vector3 a, out Vector3 b,
        out float effectiveRadius)
    {
        if (track.anchorMode == PlayerHitboxAnchorMode.FollowAnchors &&
            anchors.TryGetValue(track.primaryAnchorId ?? string.Empty, out PlayerHitboxAnchor primary))
        {
            center = primary.transform.position; rotation = primary.transform.rotation;
            effectiveRadius = track.radius;
            if (anchors.TryGetValue(track.secondaryAnchorId ?? string.Empty, out PlayerHitboxAnchor secondary))
            { a = primary.transform.position; b = secondary.transform.position; center = (a + b) * .5f; }
            else GetCapsuleEndpoints(track, center, rotation,
                effectiveRadius, out a, out b);
            return;
        }
        Vector3 origin = GetPlayerCenter();
        Vector3 local;
        if (track.trajectoryMode == PlayerHitboxTrajectoryMode.Orbit)
        {
            float angleProgress = Evaluate(track.orbitAngleCurve, progress);
            float distanceProgress = Evaluate(track.orbitDistanceCurve, progress);
            float radiusProgress = Evaluate(track.orbitRadiusCurve, progress);
            float angle = Mathf.LerpUnclamped(track.orbitStartAngleDegrees,
                track.orbitEndAngleDegrees, angleProgress) * Mathf.Deg2Rad;
            float distance = Mathf.LerpUnclamped(track.orbitStartDistance,
                track.orbitEndDistance, distanceProgress);
            local = new Vector3(Mathf.Sin(angle) * distance,
                track.orbitHeight, Mathf.Cos(angle) * distance);
            effectiveRadius = Mathf.Max(.01f, Mathf.LerpUnclamped(
                track.orbitStartRadius, track.orbitEndRadius, radiusProgress));
            center = origin + transform.rotation * local;
        }
        else if (track.trajectoryMode ==
            PlayerHitboxTrajectoryMode.TargetAssist &&
            runtime != null && runtime.HasAssistTarget &&
            runtime.IsGrappleAttack)
        {
            Vector3 start = origin +
                transform.rotation * track.startLocalPosition;
            Vector3 aimDirection = runtime.AssistTargetPosition - start;
            if (aimDirection.sqrMagnitude <= .0001f)
                aimDirection = transform.forward;

            // Mantem a inclinacao 3D do alvo, mas estende o trajeto ate o
            // alcance completo configurado no endpoint da track.
            float fullReach = Vector3.Distance(track.startLocalPosition,
                track.endLocalPosition);
            center = start + aimDirection.normalized * fullReach * progress;
            effectiveRadius = track.radius;
        }
        else if (track.trajectoryMode ==
            PlayerHitboxTrajectoryMode.TargetAssist &&
            runtime != null && runtime.HasAssistTarget)
        {
            Vector3 start = origin +
                transform.rotation * track.startLocalPosition;
            center = Vector3.Lerp(start, runtime.AssistTargetPosition,
                progress);
            effectiveRadius = track.radius;
        }
        else
        {
            // Trajetorias lineares e ataques sem alvo assistido usam os
            // endpoints locais configurados no asset.
            local = Vector3.Lerp(track.startLocalPosition,
                track.endLocalPosition, progress);
            effectiveRadius = track.radius;
            center = origin + transform.rotation * local;
        }

        rotation = transform.rotation * Quaternion.Euler(Vector3.Lerp(track.startLocalEuler, track.endLocalEuler, progress));
        GetCapsuleEndpoints(track, center, rotation, effectiveRadius,
            out a, out b);
    }

    private static void GetCapsuleEndpoints(PlayerHitboxTrackDefinition track, Vector3 center,
        Quaternion rotation, float effectiveRadius, out Vector3 a,
        out Vector3 b)
    {
        Vector3 axis = track.capsuleAxis == PlayerHitboxAxis.X ? Vector3.right :
            track.capsuleAxis == PlayerHitboxAxis.Y ? Vector3.up : Vector3.forward;
        float half = Mathf.Max(0f, track.capsuleHeight * .5f - effectiveRadius);
        Vector3 offset = rotation * axis * half; a = center - offset; b = center + offset;
    }

    private static float Evaluate(AnimationCurve curve, float progress) =>
        curve == null || curve.length == 0
            ? progress
            : curve.Evaluate(progress);

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !PlayerDebugToolkitHUD.IsVisible) return;
        Gizmos.color = Color.crimson;
        foreach (DebugVolume volume in debugVolumes)
        {
            if (volume.shape == PlayerHitboxShape.Sphere) Gizmos.DrawWireSphere(volume.center, volume.radius);
            else if (volume.shape == PlayerHitboxShape.Box)
            { Matrix4x4 old = Gizmos.matrix; Gizmos.matrix = Matrix4x4.TRS(volume.center, volume.rotation, Vector3.one); Gizmos.DrawWireCube(Vector3.zero, volume.size); Gizmos.matrix = old; }
            else DrawWireCapsule(volume.a, volume.b, volume.radius);
        }
    }

    private static void DrawWireCapsule(Vector3 a, Vector3 b, float radius)
    {
        Gizmos.DrawWireSphere(a, radius);
        Gizmos.DrawWireSphere(b, radius);
        Vector3 axis = (b - a).normalized;
        Vector3 side = Vector3.Cross(axis, Vector3.up);
        if (side.sqrMagnitude < .001f)
            side = Vector3.Cross(axis, Vector3.right);
        side.Normalize();
        Vector3 secondSide = Vector3.Cross(axis, side).normalized;
        Gizmos.DrawLine(a + side * radius, b + side * radius);
        Gizmos.DrawLine(a - side * radius, b - side * radius);
        Gizmos.DrawLine(a + secondSide * radius, b + secondSide * radius);
        Gizmos.DrawLine(a - secondSide * radius, b - secondSide * radius);
    }
}
