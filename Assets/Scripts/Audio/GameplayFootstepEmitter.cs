using UnityEngine;

/// <summary>Emite passos por distancia percorrida enquanto a entidade anda apoiada no chao.</summary>
[DisallowMultipleComponent]
public sealed class GameplayFootstepEmitter : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private PlayerState playerState;
    private EnemyMovement enemyMovement;
    private EnemyBase enemyBase;
    private Vector3 previousPosition;
    private float distanceSinceLastStep;

    private bool IsPlayer => playerMovement != null;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        enemyMovement = GetComponent<EnemyMovement>();
        playerState = GetComponent<PlayerState>();
        enemyBase = GetComponent<EnemyBase>();
        previousPosition = transform.position;
    }

    private void LateUpdate()
    {
        Vector3 currentPosition = transform.position;
        Vector3 frameMovement = currentPosition - previousPosition;
        previousPosition = currentPosition;

        if (GamePauseController.IsPaused || Time.deltaTime <= 0f ||
            !CanEmitSteps())
        {
            distanceSinceLastStep = 0f;
            return;
        }

        frameMovement.y = 0f;
        float distance = frameMovement.magnitude;
        if (distance / Time.deltaTime <
            GameplayAudioSystem.Instance.Settings.minimumFootstepSpeed)
        {
            distanceSinceLastStep = 0f;
            return;
        }

        distanceSinceLastStep += distance;
        float stepDistance = IsPlayer
            ? GameplayAudioSystem.Instance.Settings.playerFootstepDistance
            : GameplayAudioSystem.Instance.Settings.enemyFootstepDistance;
        // Teletransportes de spawn/reset nao devem soar como uma longa corrida.
        if (distance >= stepDistance * 3f)
        {
            distanceSinceLastStep = 0f;
            return;
        }

        if (distanceSinceLastStep < stepDistance)
            return;

        distanceSinceLastStep %= stepDistance;
        GameplayAudioSettings settings = GameplayAudioSystem.Instance.Settings;
        GameplayAudioCueId cue = IsPlayer
            ? GameplayAudioCueId.PlayerFootstep
            : GameplayAudioCueId.EnemyFootstep;
        GameplayAudioSystem.Play(cue, transform.position +
            Vector3.up * settings.footstepHeightOffset);
    }

    private bool CanEmitSteps()
    {
        if (IsPlayer)
        {
            if (!playerMovement.IsGrounded || playerMovement.IsCrouching ||
                playerState == null)
                return false;

            return playerState.IsNormal() && !playerState.IsAttacking() &&
                !playerState.IsDashing() && !playerState.IsSliding();
        }

        if (enemyMovement == null || !enemyMovement.IsGrounded)
            return false;

        return enemyBase == null ||
            enemyBase.CurrentState == EnemyBase.State.Idle ||
            enemyBase.CurrentState == EnemyBase.State.Combat ||
            enemyBase.CurrentState == EnemyBase.State.Guarding;
    }
}
