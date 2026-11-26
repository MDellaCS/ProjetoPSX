using System.Collections.Generic;
using UnityEngine;

/// <summary>Cuida dos sons de inicio e do primeiro acerto de cada grupo e ciclo.</summary>
[DisallowMultipleComponent]
public sealed class PlayerAttackAudioController : MonoBehaviour
{
    private PlayerAttackController attacks;
    private PlayerAttackRuntime currentRuntime;
    private readonly Dictionary<(int group, int cycle),
        List<AudioPlaybackHandle>> groupStartPlaybacks = new();
    private readonly Dictionary<PlayerHitboxTrackDefinition, int> activeTrackCycles = new();
    private readonly HashSet<(int group, int cycle)> startedGroups = new();
    private readonly HashSet<(int group, int cycle)> hitSoundPlayedGroups = new();

    internal void Initialize(PlayerAttackController attackController)
    {
        if (attacks == attackController)
            return;

        if (attacks != null)
        {
            attacks.AttackStarted -= OnAttackStarted;
            attacks.AttackFinished -= OnAttackFinished;
        }

        attacks = attackController;
        if (attacks == null)
            return;

        attacks.AttackStarted += OnAttackStarted;
        attacks.AttackFinished += OnAttackFinished;
    }

    private void OnDestroy()
    {
        if (attacks != null)
        {
            attacks.AttackStarted -= OnAttackStarted;
            attacks.AttackFinished -= OnAttackFinished;
        }
        StopAllTrackStartSounds();
    }

    private void OnAttackStarted(PlayerAttackRuntime runtime)
    {
        ClearTrackStartSoundReferences();
        currentRuntime = runtime;
    }

    internal void SyncActiveHitboxTracks(PlayerAttackRuntime runtime,
        Dictionary<PlayerHitboxTrackDefinition, int> activeTracks)
    {
        if (runtime == null || runtime != currentRuntime)
            return;

        activeTrackCycles.Clear();
        GameplayAttackAudioSet audioSet = SfxAudio.CurrentSettings
            .GetPlayerAttackAudio(runtime.Definition.kind);
        foreach (var entry in activeTracks)
        {
            PlayerHitboxTrackDefinition track = entry.Key;
            int cycle = entry.Value;
            activeTrackCycles[track] = cycle;
            var group = (track.hitGroup, cycle);
            if (!startedGroups.Add(group))
                continue;

            AudioPlaybackHandle playback = SfxAudio.PlayTracked(
                audioSet != null ? audioSet.attackStart : null,
                transform.position);
            if (playback != null)
            {
                if (!groupStartPlaybacks.TryGetValue(group,
                    out List<AudioPlaybackHandle> playbacks))
                {
                    playbacks = new List<AudioPlaybackHandle>();
                    groupStartPlaybacks.Add(group, playbacks);
                }
                playbacks.Add(playback);
            }
        }
    }

    internal void CancelAttackSounds(PlayerAttackRuntime runtime)
    {
        if (runtime == null || runtime != currentRuntime)
            return;

        StopAllTrackStartSounds();
        currentRuntime = null;
    }

    internal void NotifyHit(PlayerAttackRuntime runtime,
        PlayerHitboxTrackDefinition track, Component target, Vector3 position)
    {
        if (runtime == null || runtime != currentRuntime)
            return;

        if (track == null || !activeTrackCycles.TryGetValue(track, out int cycle))
            return;
        var group = (track.hitGroup, cycle);
        StopGroupStartSound(group);
        if (!hitSoundPlayedGroups.Add(group))
            return;

        GameplayAttackAudioSet audioSet = SfxAudio.CurrentSettings
            .GetPlayerAttackAudio(runtime.Definition.kind);
        GameplayAudioSurfaceType surface =
            GameplayAudioSurface.GetSurfaceType(target);
        SfxAudio.Play(audioSet != null
            ? audioSet.GetHitCue(surface) : null, position);
    }

    private void OnAttackFinished(PlayerAttackRuntime runtime)
    {
        if (runtime != currentRuntime)
            return;

        // Ao fim normal do ataque, deixa os clipes terminarem naturalmente.
        ClearTrackStartSoundReferences();
        currentRuntime = null;
    }

    private void StopGroupStartSound((int group, int cycle) group)
    {
        if (groupStartPlaybacks.TryGetValue(group,
            out List<AudioPlaybackHandle> playbacks))
        {
            foreach (AudioPlaybackHandle playback in playbacks)
                playback?.Stop();
            groupStartPlaybacks.Remove(group);
        }
    }

    private void StopAllTrackStartSounds()
    {
        foreach (List<AudioPlaybackHandle> playbacks in
            groupStartPlaybacks.Values)
        {
            foreach (AudioPlaybackHandle playback in playbacks)
                playback?.Stop();
        }
        ClearTrackStartSoundReferences();
    }

    private void ClearTrackStartSoundReferences()
    {
        groupStartPlaybacks.Clear();
        activeTrackCycles.Clear();
        startedGroups.Clear();
        hitSoundPlayedGroups.Clear();
    }

}
