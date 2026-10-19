using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerHitboxController : MonoBehaviour
{
    private sealed class TrackState { public Vector3 position; public Quaternion rotation; public float radius; public bool valid; }
    private readonly Dictionary<string, PlayerHitboxAnchor> anchors = new();
    private readonly Dictionary<PlayerHitboxTrackDefinition, TrackState> trackStates = new();
    private readonly Dictionary<(PlayerAttackStrikeDefinition, int), HashSet<EnemyBase>> groupHits = new();
    private readonly HashSet<PlayerAttackStrikeDefinition> startedStrikes = new();
    private readonly Dictionary<(PlayerHitboxTrackDefinition, EnemyBase), float> repeatTimes = new();
    private readonly Collider[] overlapBuffer = new Collider[64];
    private readonly List<DebugVolume> debugVolumes = new();
    private PlayerAttackRuntime runtime;
    private PlayerAttackResolver resolver;
    private CharacterController characterController;

    private struct DebugVolume { public PlayerHitboxShape shape; public Vector3 center, a, b, size; public Quaternion rotation; public float radius; }

    private void Awake()
    {
        resolver = GetComponent<PlayerAttackResolver>();
        characterController = GetComponent<CharacterController>();
        foreach (PlayerHitboxAnchor anchor in GetComponentsInChildren<PlayerHitboxAnchor>(true))
            if (anchor != null && !string.IsNullOrWhiteSpace(anchor.AnchorId)) anchors[anchor.AnchorId] = anchor;
    }

    public void Begin(PlayerAttackRuntime attack)
    {
        runtime = attack; trackStates.Clear(); groupHits.Clear(); repeatTimes.Clear();
        startedStrikes.Clear(); debugVolumes.Clear();
    }
    public void End() { runtime = null; debugVolumes.Clear(); trackStates.Clear(); }

    public void Tick(float activeProgress)
    {
        if (runtime == null) return;
        debugVolumes.Clear();
        foreach (PlayerAttackStrikeDefinition strike in runtime.Definition.strikes)
        {
            if (strike == null || activeProgress < strike.activeStartNormalized || activeProgress > strike.activeEndNormalized) continue;
            if (startedStrikes.Add(strike) && strike.effects.selfLaunchUp)
                resolver.ApplySelfLaunchUp();
            float strikeProgress = Mathf.InverseLerp(strike.activeStartNormalized, strike.activeEndNormalized, activeProgress);
            foreach (PlayerHitboxTrackDefinition track in strike.hitboxes)
                EvaluateTrack(strike, track, strikeProgress);
        }
    }

    private void EvaluateTrack(PlayerAttackStrikeDefinition strike, PlayerHitboxTrackDefinition track, float strikeProgress)
    {
        if (track == null || strikeProgress < track.activeStartNormalized || strikeProgress > track.activeEndNormalized) return;
        float progress = Mathf.InverseLerp(track.activeStartNormalized, track.activeEndNormalized, strikeProgress);
        GetPose(track, progress, out Vector3 center, out Quaternion rotation,
            out Vector3 a, out Vector3 b, out float effectiveRadius);
        if (!trackStates.TryGetValue(track, out TrackState state)) { state = new TrackState(); trackStates.Add(track, state); }
        int steps = 1;
        if (track.sweep && state.valid)
            steps = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(state.position, center) / track.sweepStepDistance), 1, 24);
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 sampleCenter = state.valid ? Vector3.Lerp(state.position, center, t) : center;
            Quaternion sampleRotation = state.valid ? Quaternion.Slerp(state.rotation, rotation, t) : rotation;
            float sampleRadius = state.valid
                ? Mathf.Lerp(state.radius, effectiveRadius, t)
                : effectiveRadius;
            Query(strike, track, sampleCenter, sampleRotation,
                a + (sampleCenter - center), b + (sampleCenter - center),
                sampleRadius);
            if (runtime.ExtendedArmHitboxRadius > 0f)
                QueryExtendedArm(strike, track, GetPlayerCenter(),
                    sampleCenter, runtime.ExtendedArmHitboxRadius);
        }
        state.position = center; state.rotation = rotation;
        state.radius = effectiveRadius; state.valid = true;
        debugVolumes.Add(new DebugVolume { shape = track.shape, center = center, rotation = rotation, a = a, b = b, radius = effectiveRadius, size = track.boxSize });
        if (runtime.ExtendedArmHitboxRadius > 0f)
        {
            debugVolumes.Add(new DebugVolume
            {
                shape = PlayerHitboxShape.Capsule,
                a = GetPlayerCenter(),
                b = center,
                center = (GetPlayerCenter() + center) * .5f,
                radius = runtime.ExtendedArmHitboxRadius
            });
        }
    }

    private void Query(PlayerAttackStrikeDefinition strike, PlayerHitboxTrackDefinition track,
        Vector3 center, Quaternion rotation, Vector3 a, Vector3 b,
        float effectiveRadius)
    {
        int count = track.shape switch
        {
            PlayerHitboxShape.Capsule => Physics.OverlapCapsuleNonAlloc(a, b, effectiveRadius, overlapBuffer),
            PlayerHitboxShape.Box => Physics.OverlapBoxNonAlloc(center, track.boxSize * .5f, overlapBuffer, rotation),
            _ => Physics.OverlapSphereNonAlloc(center, effectiveRadius, overlapBuffer)
        };
        float radius = track.shape == PlayerHitboxShape.Box
            ? Mathf.Max(track.boxSize.x, track.boxSize.z) * .5f : effectiveRadius;
        ResolveHits(strike, track, count, radius);
    }

    private void QueryExtendedArm(PlayerAttackStrikeDefinition strike,
        PlayerHitboxTrackDefinition track, Vector3 playerCenter,
        Vector3 hitboxCenter, float radius)
    {
        int count = Physics.OverlapCapsuleNonAlloc(playerCenter,
            hitboxCenter, radius, overlapBuffer);
        ResolveHits(strike, track, count, radius);
    }

    private void ResolveHits(PlayerAttackStrikeDefinition strike,
        PlayerHitboxTrackDefinition track, int count, float attackRadius)
    {
        var groupKey = (strike, track.hitGroup);
        if (!groupHits.TryGetValue(groupKey, out HashSet<EnemyBase> hitSet))
        { hitSet = new HashSet<EnemyBase>(); groupHits.Add(groupKey, hitSet); }
        for (int i = 0; i < count; i++)
        {
            EnemyBase enemy = overlapBuffer[i] != null ? overlapBuffer[i].GetComponentInParent<EnemyBase>() : null;
            if (enemy == null) continue;
            var key = (track, enemy);
            if (!track.allowRepeatHits && hitSet.Contains(enemy)) continue;
            if (track.allowRepeatHits && repeatTimes.TryGetValue(key, out float next) && Time.time < next) continue;
            hitSet.Add(enemy); repeatTimes[key] = Time.time + track.repeatHitInterval;
            resolver.Resolve(runtime, strike, enemy, attackRadius);
        }
    }

    private Vector3 GetPlayerCenter() => transform.position +
        transform.rotation * (characterController != null
            ? characterController.center
            : Vector3.zero);

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
        }
        else
        {
            local = Vector3.Lerp(track.startLocalPosition,
                track.endLocalPosition, progress);
            effectiveRadius = track.radius;
        }
        Vector3 origin = GetPlayerCenter();
        center = origin + transform.rotation * local;
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
