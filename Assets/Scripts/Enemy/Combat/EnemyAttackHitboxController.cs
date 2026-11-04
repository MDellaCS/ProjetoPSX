using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyAttackHitboxController : MonoBehaviour
{
    private sealed class TrackState
    {
        public Vector3 position;
        public Quaternion rotation;
        public float radius;
        public bool valid;
    }

    private struct DebugVolume
    {
        public EnemyHitboxShape shape;
        public Vector3 center, a, b, size;
        public Quaternion rotation;
        public float radius;
    }

    private readonly Dictionary<string, EnemyHitboxAnchor> anchors = new();
    private readonly Dictionary<EnemyHitboxTrackDefinition, TrackState> tracks = new();
    private readonly HashSet<(EnemyAttackStrikeDefinition, int)> hitGroups = new();
    private readonly Dictionary<EnemyHitboxTrackDefinition, float> repeatTimes = new();
    private Collider[] overlapBuffer = new Collider[64];
    [Tooltip("Camadas consultadas pelos volumes de ataque dos inimigos. Inclua a camada do jogador e remova camadas que nunca podem receber dano.")]
    [SerializeField] private LayerMask hitboxQueryMask = Physics.DefaultRaycastLayers;
    private readonly List<DebugVolume> debugVolumes = new();
    private EnemyBase owner;
    private EnemyAttackRuntime runtime;

    public void Initialize(EnemyBase enemy)
    {
        owner = enemy;
        anchors.Clear();
        foreach (EnemyHitboxAnchor anchor in
            GetComponentsInChildren<EnemyHitboxAnchor>(true))
            if (anchor != null && !string.IsNullOrWhiteSpace(anchor.AnchorId))
                anchors[anchor.AnchorId] = anchor;
    }

    public void Begin(EnemyAttackRuntime attack)
    {
        runtime = attack;
        tracks.Clear();
        hitGroups.Clear();
        repeatTimes.Clear();
        debugVolumes.Clear();
    }

    public void End()
    {
        runtime = null;
        tracks.Clear();
        debugVolumes.Clear();
    }

    public void Tick(float activeProgress)
    {
        if (runtime == null || runtime.Definition.strikes == null)
            return;
        debugVolumes.Clear();
        foreach (EnemyAttackStrikeDefinition strike in runtime.Definition.strikes)
        {
            if (strike == null || activeProgress < strike.activeStartNormalized ||
                activeProgress > strike.activeEndNormalized)
                continue;
            float strikeProgress = Mathf.InverseLerp(strike.activeStartNormalized,
                strike.activeEndNormalized, activeProgress);
            if (strike.hitboxes == null)
                continue;
            foreach (EnemyHitboxTrackDefinition track in strike.hitboxes)
                EvaluateTrack(strike, track, strikeProgress);
        }
    }

    private void EvaluateTrack(EnemyAttackStrikeDefinition strike,
        EnemyHitboxTrackDefinition track, float strikeProgress)
    {
        if (track == null || strikeProgress < track.activeStartNormalized ||
            strikeProgress > track.activeEndNormalized)
            return;
        float progress = Mathf.InverseLerp(track.activeStartNormalized,
            track.activeEndNormalized, strikeProgress);
        GetPose(track, progress, out Vector3 center, out Quaternion rotation,
            out Vector3 a, out Vector3 b, out float radius);
        if (!tracks.TryGetValue(track, out TrackState state))
        {
            state = new TrackState();
            tracks.Add(track, state);
        }
        int steps = track.sweep && state.valid
            ? Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(state.position, center) /
                Mathf.Max(.01f, track.sweepStepDistance)), 1, 24) : 1;
        for (int index = 1; index <= steps; index++)
        {
            float t = index / (float)steps;
            Vector3 sample = state.valid
                ? Vector3.Lerp(state.position, center, t) : center;
            Quaternion sampleRotation = state.valid
                ? Quaternion.Slerp(state.rotation, rotation, t) : rotation;
            float sampleRadius = state.valid
                ? Mathf.Lerp(state.radius, radius, t) : radius;
            Vector3 offset = sample - center;
            Query(strike, track, sample, sampleRotation, a + offset,
                b + offset, sampleRadius);
            if (runtime.ExtendedArmHitboxRadius > 0f)
                QueryExtended(strike, track, owner.AttackOrigin, sample,
                    runtime.ExtendedArmHitboxRadius);
        }
        state.position = center;
        state.rotation = rotation;
        state.radius = radius;
        state.valid = true;
        debugVolumes.Add(new DebugVolume { shape = track.shape, center = center,
            rotation = rotation, a = a, b = b, radius = radius,
            size = track.boxSize });
    }

    private void Query(EnemyAttackStrikeDefinition strike,
        EnemyHitboxTrackDefinition track, Vector3 center, Quaternion rotation,
        Vector3 a, Vector3 b, float radius)
    {
        int count;
        while (true)
        {
            count = track.shape switch
            {
                EnemyHitboxShape.Capsule => Physics.OverlapCapsuleNonAlloc(a,
                    b, radius, overlapBuffer, hitboxQueryMask,
                    QueryTriggerInteraction.Collide),
                EnemyHitboxShape.Box => Physics.OverlapBoxNonAlloc(center,
                    track.boxSize * .5f, overlapBuffer, rotation,
                    hitboxQueryMask, QueryTriggerInteraction.Collide),
                _ => Physics.OverlapSphereNonAlloc(center, radius,
                    overlapBuffer, hitboxQueryMask,
                    QueryTriggerInteraction.Collide)
            };
            if (count < overlapBuffer.Length)
                break;
            System.Array.Resize(ref overlapBuffer, overlapBuffer.Length * 2);
        }
        ResolveHits(strike, track, count);
    }

    private void QueryExtended(EnemyAttackStrikeDefinition strike,
        EnemyHitboxTrackDefinition track, Vector3 origin, Vector3 center,
        float radius)
    {
        int count;
        while (true)
        {
            count = Physics.OverlapCapsuleNonAlloc(origin, center, radius,
                overlapBuffer, hitboxQueryMask, QueryTriggerInteraction.Collide);
            if (count < overlapBuffer.Length)
                break;
            System.Array.Resize(ref overlapBuffer, overlapBuffer.Length * 2);
        }
        ResolveHits(strike, track, count);
    }

    private void ResolveHits(EnemyAttackStrikeDefinition strike,
        EnemyHitboxTrackDefinition track, int count)
    {
        var group = (strike, track.hitGroup);
        if (!track.allowRepeatHits && hitGroups.Contains(group))
            return;
        if (track.allowRepeatHits && repeatTimes.TryGetValue(track,
            out float nextTime) && Time.time < nextTime)
            return;

        for (int index = 0; index < count; index++)
        {
            PlayerHealth player = overlapBuffer[index] != null
                ? overlapBuffer[index].GetComponentInParent<PlayerHealth>() : null;
            if (player == null)
                continue;
            int damage = Mathf.RoundToInt(runtime.Damage *
                strike.damageMultiplier);
            int poise = Mathf.RoundToInt(runtime.PoiseDamage *
                strike.poiseMultiplier);
            runtime.HitTargetPosition = player.transform.position;
            // Um golpe posterior ignorado (i-frames, bloqueio) nao deve
            // desfazer um acerto ja confirmado neste ataque.
            if (player.TakeDamage(damage,
                owner.AttackOrigin, poise,
                strike.reaction, true, transform,
                runtime.Definition.causesHeavyStagger))
                runtime.HitConfirmed = true;
            hitGroups.Add(group);
            repeatTimes[track] = Time.time + track.repeatHitInterval;
            return;
        }
    }

    private void GetPose(EnemyHitboxTrackDefinition track, float progress,
        out Vector3 center, out Quaternion rotation, out Vector3 a,
        out Vector3 b, out float radius)
    {
        if (track.anchorMode == EnemyHitboxAnchorMode.FollowAnchors &&
            anchors.TryGetValue(track.primaryAnchorId ?? string.Empty,
                out EnemyHitboxAnchor primary))
        {
            center = primary.transform.position;
            rotation = primary.transform.rotation;
            radius = track.radius;
            if (anchors.TryGetValue(track.secondaryAnchorId ?? string.Empty,
                out EnemyHitboxAnchor secondary))
            {
                a = primary.transform.position;
                b = secondary.transform.position;
                center = (a + b) * .5f;
            }
            else GetCapsuleEndpoints(track, center, rotation, radius,
                out a, out b);
            return;
        }

        Vector3 local;
        if (track.trajectoryMode == EnemyHitboxTrajectoryMode.Orbit)
        {
            float angle = Mathf.LerpUnclamped(track.orbitStartAngleDegrees,
                track.orbitEndAngleDegrees,
                Evaluate(track.orbitAngleCurve, progress)) * Mathf.Deg2Rad;
            float distance = Mathf.LerpUnclamped(track.orbitStartDistance,
                track.orbitEndDistance,
                Evaluate(track.orbitDistanceCurve, progress));
            local = new Vector3(Mathf.Sin(angle) * distance, track.orbitHeight,
                Mathf.Cos(angle) * distance);
            radius = Mathf.LerpUnclamped(track.orbitStartRadius,
                track.orbitEndRadius, Evaluate(track.orbitRadiusCurve, progress));
        }
        else
        {
            local = Vector3.Lerp(track.startLocalPosition,
                track.endLocalPosition, progress);
            radius = track.radius;
        }
        center = owner.AttackOrigin + transform.rotation * local;
        rotation = transform.rotation * Quaternion.Euler(Vector3.Lerp(
            track.startLocalEuler, track.endLocalEuler, progress));
        GetCapsuleEndpoints(track, center, rotation, radius, out a, out b);
    }

    private static void GetCapsuleEndpoints(EnemyHitboxTrackDefinition track,
        Vector3 center, Quaternion rotation, float radius, out Vector3 a,
        out Vector3 b)
    {
        Vector3 axis = track.capsuleAxis == EnemyHitboxAxis.X ? Vector3.right :
            track.capsuleAxis == EnemyHitboxAxis.Y ? Vector3.up : Vector3.forward;
        Vector3 offset = rotation * axis * Mathf.Max(0f,
            track.capsuleHeight * .5f - radius);
        a = center - offset;
        b = center + offset;
    }

    private static float Evaluate(AnimationCurve curve, float progress) =>
        curve == null || curve.length == 0 ? progress : curve.Evaluate(progress);

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !PlayerDebugToolkitHUD.IsVisible)
            return;
        Gizmos.color = Color.red;
        foreach (DebugVolume volume in debugVolumes)
        {
            if (volume.shape == EnemyHitboxShape.Sphere)
                Gizmos.DrawWireSphere(volume.center, volume.radius);
            else if (volume.shape == EnemyHitboxShape.Box)
            {
                Matrix4x4 old = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(volume.center, volume.rotation,
                    Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, volume.size);
                Gizmos.matrix = old;
            }
            else
            {
                Gizmos.DrawWireSphere(volume.a, volume.radius);
                Gizmos.DrawWireSphere(volume.b, volume.radius);
                Gizmos.DrawLine(volume.a, volume.b);
            }
        }
    }
}
