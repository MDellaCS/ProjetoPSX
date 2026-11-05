using System.Collections.Generic;
using UnityEngine;

/// <summary>Cuida dos sons de inicio e do primeiro acerto de cada hitbox do jogador.</summary>
[DisallowMultipleComponent]
public sealed class PlayerAttackAudioController : MonoBehaviour
{
    private PlayerAttackController attacks;
    private PlayerAttackRuntime currentRuntime;
    private readonly Dictionary<PlayerHitboxTrackDefinition,
        List<AudioPlaybackHandle>> trackStartPlaybacks = new();
    private readonly HashSet<PlayerHitboxTrackDefinition> startedTracks = new();
    private readonly HashSet<PlayerHitboxTrackDefinition> hitSoundPlayedTracks = new();
    private readonly List<PlayerHitboxTrackDefinition> tracksToStop = new();

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
        HashSet<PlayerHitboxTrackDefinition> activeTracks)
    {
        if (runtime == null || runtime != currentRuntime)
            return;

        tracksToStop.Clear();
        foreach (PlayerHitboxTrackDefinition track in startedTracks)
        {
            if (activeTracks.Contains(track))
                continue;

            tracksToStop.Add(track);
        }
        foreach (PlayerHitboxTrackDefinition track in tracksToStop)
            startedTracks.Remove(track);

        GameplayAttackAudioSet audioSet = SfxAudio.CurrentSettings
            .GetPlayerAttackAudio(runtime.Definition.kind);
        foreach (PlayerHitboxTrackDefinition track in activeTracks)
        {
            if (!startedTracks.Add(track))
                continue;

            AudioPlaybackHandle playback = SfxAudio.PlayTracked(
                audioSet != null ? audioSet.attackStart : null,
                transform.position);
            if (playback != null)
            {
                if (!trackStartPlaybacks.TryGetValue(track,
                    out List<AudioPlaybackHandle> playbacks))
                {
                    playbacks = new List<AudioPlaybackHandle>();
                    trackStartPlaybacks.Add(track, playbacks);
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

        StopTrackStartSound(track);
        if (!hitSoundPlayedTracks.Add(track))
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

    private void StopTrackStartSound(PlayerHitboxTrackDefinition track)
    {
        if (trackStartPlaybacks.TryGetValue(track,
            out List<AudioPlaybackHandle> playbacks))
        {
            foreach (AudioPlaybackHandle playback in playbacks)
                playback?.Stop();
            trackStartPlaybacks.Remove(track);
        }
    }

    private void StopAllTrackStartSounds()
    {
        foreach (List<AudioPlaybackHandle> playbacks in
            trackStartPlaybacks.Values)
        {
            foreach (AudioPlaybackHandle playback in playbacks)
                playback?.Stop();
        }
        ClearTrackStartSoundReferences();
    }

    private void ClearTrackStartSoundReferences()
    {
        trackStartPlaybacks.Clear();
        startedTracks.Clear();
        hitSoundPlayedTracks.Clear();
        tracksToStop.Clear();
    }

}
