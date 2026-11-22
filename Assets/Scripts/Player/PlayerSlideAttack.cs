using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Faz a ponte entre o movimento de slide e o ataque configurado no moveset.
/// </summary>
[DisallowMultipleComponent]
public class PlayerSlideAttack : MonoBehaviour
{
    [Header("Slide Launch Attack")]
    [Tooltip("Velocidade horizontal minima para a hitbox do Slide ficar ativa.")]
    [Min(0f)] public float minimumSlideSpeed = 12f;
    [Tooltip("Tempo em que o player atravessa apenas o inimigo que o Slide acabou de lancar.")]
    [Min(0f)] public float launchedEnemyCollisionIgnoreDuration = .15f;
    [Tooltip("Converte a velocidade atual do Slide em impulso horizontal para o inimigo lancado.")]
    [Min(0f)] public float launchForwardConversion = 0f;
    private readonly List<IgnoredEnemyCollision> ignoredEnemyCollisions = new();

    private PlayerMovement playerMovement;
    private PlayerState playerState;
    private PlayerAttackController attacks;
    private PlayerMovesetController moveset;
    private PlayerCharacterController controller;

    private sealed class IgnoredEnemyCollision
    {
        public EnemyBase enemy;
        public float restoreTime;
    }

    internal bool IsIgnoringMovementCollision(EnemyBase enemy)
    {
        for (int index = 0; index < ignoredEnemyCollisions.Count; index++)
        {
            IgnoredEnemyCollision ignored = ignoredEnemyCollisions[index];
            if (ignored.enemy == enemy && Time.time < ignored.restoreTime)
                return true;
        }
        return false;
    }

    public bool IsActive => playerMovement != null && playerState != null &&
        playerState.IsSliding() &&
        playerMovement.HorizontalSpeed >= minimumSlideSpeed;

    public static PlayerSlideAttack GetOrAdd(GameObject target)
    {
        PlayerSlideAttack slideAttack = target.GetComponent<PlayerSlideAttack>();
        if (slideAttack == null)
            slideAttack = target.AddComponent<PlayerSlideAttack>();

        slideAttack.enabled = true;
        return slideAttack;
    }

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerState = GetComponent<PlayerState>();
        attacks = GetComponent<PlayerAttackController>();
        moveset = GetComponent<PlayerMovesetController>();
        controller = GetComponent<PlayerCharacterController>();
    }

    private void Update()
    {
        if (GamePauseController.IsPaused || CutsceneController.IsPlayerControlLocked ||
            CombatImpactSystem.IsGameplayFrozen(this))
            return;

        RestoreExpiredEnemyCollisions();

        if (!IsActive || attacks == null || attacks.IsAttacking || moveset == null)
            return;
        PlayerMovesetDefinition definition = moveset.Moveset;
        if (definition != null && definition.slideAttack != null)
            attacks.TryStartSlideAttack(definition.slideAttack);
    }

    public void HandleSlideLaunch(EnemyBase enemy)
    {
        if (attacks == null || !attacks.IsSlideAttack || enemy == null || !enemy.IsLaunched)
            return;
        enemy.ApplyLaunchUpHorizontalImpulse(transform.forward,
            playerMovement.HorizontalSpeed * launchForwardConversion);
        IgnoreEnemyCollisionTemporarily(enemy);
    }

    private void IgnoreEnemyCollisionTemporarily(EnemyBase enemy)
    {
        if (enemy == null || controller == null ||
            launchedEnemyCollisionIgnoreDuration <= 0f)
        {
            return;
        }

        for (int index = 0; index < ignoredEnemyCollisions.Count; index++)
        {
            IgnoredEnemyCollision existing = ignoredEnemyCollisions[index];
            if (existing.enemy != enemy)
                continue;
            existing.restoreTime = Time.time +
                launchedEnemyCollisionIgnoreDuration;
            return;
        }

        ignoredEnemyCollisions.Add(new IgnoredEnemyCollision
        {
            enemy = enemy,
            restoreTime = Time.time + launchedEnemyCollisionIgnoreDuration
        });
    }

    private void RestoreExpiredEnemyCollisions()
    {
        for (int index = ignoredEnemyCollisions.Count - 1; index >= 0; index--)
        {
            IgnoredEnemyCollision ignored = ignoredEnemyCollisions[index];
            if (Time.time < ignored.restoreTime)
                continue;

            ignoredEnemyCollisions.RemoveAt(index);
        }
    }

    private void OnDisable() => ignoredEnemyCollisions.Clear();

}
