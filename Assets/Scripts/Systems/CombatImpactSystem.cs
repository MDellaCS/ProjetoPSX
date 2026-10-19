using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Camada central de apresentacao para impactos de combate.
/// Ela nao altera Time.timeScale: congela localmente a simulacao e os
/// Animators dos envolvidos, sem parar a camera, HUD ou outros combatentes.
/// </summary>
[DisallowMultipleComponent]
public class CombatImpactSystem : MonoBehaviour
{
    [Serializable]
    public struct ImpactProfile
    {
        [Tooltip("Tempo em segundos reais que o gameplay e os visuais dos envolvidos ficam congelados.")]
        [Min(0f)] public float hitstopDuration;
        [Tooltip("Deslocamento maximo da camera durante o impulso.")]
        [Min(0f)] public float cameraShakeDistance;
        [Min(0f)] public float cameraShakeDuration;
    }

    private sealed class FrozenAnimator
    {
        public Animator animator;
        public float restoreAt;
        public float originalSpeed;
    }

    private sealed class FrozenGameplay
    {
        public Transform combatantRoot;
        public float restoreAt;
    }

    private static CombatImpactSystem instance;
    private static readonly List<FrozenAnimator> frozenAnimators = new();
    private static readonly List<FrozenGameplay> frozenGameplay = new();

    [Header("Player Attack Impact")]
    [SerializeField] private ImpactProfile lightAttackImpact = new ImpactProfile
    {
        hitstopDuration = .005f,
        cameraShakeDistance = .01f,
        cameraShakeDuration = .015f
    };
    [SerializeField] private ImpactProfile heavyAttackImpact = new ImpactProfile
    {
        hitstopDuration = .2f,
        cameraShakeDistance = .16f,
        cameraShakeDuration = .1f
    };

    [Header("Impact Sounds")]
    [Tooltip("Som reproduzido quando um ataque leve do player conecta.")]
    [SerializeField] private AudioCue lightAttackHitSound;
    [Tooltip("Som reproduzido quando um ataque pesado do player conecta.")]
    [SerializeField] private AudioCue heavyAttackHitSound;
    [Tooltip("Som reproduzido quando um ataque inimigo conecta no player.")]
    [SerializeField] private AudioCue enemyAttackHitSound;

    [Header("Global Camera Shake")]
    [Tooltip("Curva global do impacto. X=0 e o inicio do shake; X=1 e o fim.")]
    [SerializeField] private AnimationCurve impactShakeIntensity =
        AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Header("Heavy Heat Scaling")]
    [Tooltip("Hitstop adicional de um pesado ao consumir 100% do Heat maximo.")]
    [Min(0f)] [SerializeField] private float maxHeatHitstopBonus = .045f;
    [Tooltip("Tremor adicional de um pesado ao consumir 100% do Heat maximo.")]
    [Min(0f)] [SerializeField] private float maxHeatShakeDistanceBonus = .16f;
    [Tooltip("Duracao adicional do tremor de um pesado ao consumir 100% do Heat maximo.")]
    [Min(0f)] [SerializeField] private float maxHeatShakeDurationBonus = .07f;

    [Header("Enemy Attack Impact")]
    [SerializeField] private ImpactProfile enemyAttackImpact = new ImpactProfile
    {
        hitstopDuration = .035f,
        cameraShakeDistance = .10f,
        cameraShakeDuration = .08f
    };
    [Tooltip("Dano usado como referencia para um inimigo atingir a intensidade maxima adicional.")]
    [Min(1f)] [SerializeField] private float enemyDamageReference = 30f;
    [Min(0f)] [SerializeField] private float maxEnemyDamageHitstopBonus = .025f;
    [Min(0f)] [SerializeField] private float maxEnemyDamageShakeDistanceBonus = .10f;

    public static CombatImpactSystem Instance
    {
        get
        {
            if (instance != null)
                return instance;

            instance = FindAnyObjectByType<CombatImpactSystem>();
            if (instance != null)
                return instance;

            GameObject systemObject = new GameObject("CombatImpactSystem");
            instance = systemObject.AddComponent<CombatImpactSystem>();
            DontDestroyOnLoad(systemObject);
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void LateUpdate()
    {
        float now = Time.unscaledTime;
        for (int index = frozenAnimators.Count - 1; index >= 0; index--)
        {
            FrozenAnimator frozen = frozenAnimators[index];
            if (frozen.animator == null)
            {
                frozenAnimators.RemoveAt(index);
                continue;
            }

            if (now >= frozen.restoreAt)
            {
                frozen.animator.speed = frozen.originalSpeed;
                frozenAnimators.RemoveAt(index);
                continue;
            }

            // LateUpdate vence qualquer presenter que ajuste Animator.speed
            // durante Update enquanto o hitstop estiver ativo.
            frozen.animator.speed = 0f;
        }

        for (int index = frozenGameplay.Count - 1; index >= 0; index--)
        {
            FrozenGameplay frozen = frozenGameplay[index];
            if (frozen.combatantRoot == null || now >= frozen.restoreAt)
                frozenGameplay.RemoveAt(index);
        }
    }

    private void OnDisable()
    {
        foreach (FrozenAnimator frozen in frozenAnimators)
        {
            if (frozen.animator != null)
                frozen.animator.speed = frozen.originalSpeed;
        }
        frozenAnimators.Clear();
        frozenGameplay.Clear();
    }

    public static void ReportPlayerAttackImpact(
        Transform attacker,
        Transform target,
        bool isHeavy,
        float normalizedHeatConsumed,
        bool requestCameraShake = true)
    {
        CombatImpactSystem system = Instance;
        ImpactProfile profile = isHeavy
            ? system.GetHeavyImpact(normalizedHeatConsumed)
            : system.lightAttackImpact;
        system.PlayImpact(
            attacker,
            target,
            profile,
            null,
            isHeavy ? system.heavyAttackHitSound : system.lightAttackHitSound,
            requestCameraShake
        );
    }

    /// <summary>
    /// Permite que apresentadores que controlam Animator.Play manualmente
    /// parem de sobrescrever o Animator.speed durante um hitstop.
    /// </summary>
    public static bool IsPresentationFrozen(Animator animator)
    {
        if (animator == null)
            return false;

        float now = Time.unscaledTime;
        return frozenAnimators.Exists(
            frozen => frozen.animator == animator && now < frozen.restoreAt
        );
    }

    /// <summary>
    /// Retorna se este participante esta em hitstop real. Sistemas de
    /// movimento e combate devem interromper sua simulacao enquanto for true.
    /// </summary>
    public static bool IsGameplayFrozen(Component participant)
    {
        if (participant == null)
            return false;

        Transform combatantRoot = GetCombatantRoot(participant.transform);
        FrozenGameplay frozen = frozenGameplay.Find(
            entry => entry.combatantRoot == combatantRoot
        );
        return frozen != null && Time.unscaledTime < frozen.restoreAt;
    }

    public static void ReportEnemyAttackImpact(
        Transform attacker,
        Transform target,
        Vector3 attackerPosition,
        int damage)
    {
        CombatImpactSystem system = Instance;
        float normalizedDamage = Mathf.Clamp01(
            Mathf.Max(0, damage) / system.enemyDamageReference
        );
        ImpactProfile profile = system.enemyAttackImpact;
        profile.hitstopDuration +=
            system.maxEnemyDamageHitstopBonus * normalizedDamage;
        profile.cameraShakeDistance +=
            system.maxEnemyDamageShakeDistanceBonus * normalizedDamage;
        system.PlayImpact(
            attacker,
            target,
            profile,
            attackerPosition,
            system.enemyAttackHitSound,
            true
        );
    }

    private ImpactProfile GetHeavyImpact(float normalizedHeatConsumed)
    {
        float heat = Mathf.Clamp01(normalizedHeatConsumed);
        ImpactProfile profile = heavyAttackImpact;
        profile.hitstopDuration += maxHeatHitstopBonus * heat;
        profile.cameraShakeDistance += maxHeatShakeDistanceBonus * heat;
        profile.cameraShakeDuration += maxHeatShakeDurationBonus * heat;
        return profile;
    }

    private void PlayImpact(
        Transform attacker,
        Transform target,
        ImpactProfile profile,
        Vector3? explicitAttackerPosition,
        AudioCue sound,
        bool requestCameraShake)
    {
        FreezePresentation(attacker, profile.hitstopDuration);
        FreezePresentation(target, profile.hitstopDuration);
        FreezeGameplay(attacker, profile.hitstopDuration);
        FreezeGameplay(target, profile.hitstopDuration);

        PlayImpactSound(attacker, target, sound);

        if (target == null || !requestCameraShake)
            return;

        Vector3 origin = explicitAttackerPosition ??
            (attacker != null ? attacker.position : target.position);
        Vector3 attackDirection = target.position - origin;
        attackDirection.y = 0f;

        if (CameraEffectStack.Instance != null)
        {
            CameraEffectStack.Instance.AddDirectionalImpulse(
                attackDirection,
                profile.cameraShakeDistance,
                profile.cameraShakeDuration,
                impactShakeIntensity
            );
        }
    }

    private void PlayImpactSound(
        Transform attacker,
        Transform target,
        AudioCue cue)
    {
        if (cue == null || !cue.HasClip)
            return;

        Vector3 soundPosition = target != null
            ? target.position
            : attacker != null ? attacker.position : Vector3.zero;
        GameAudioSystem.PlayAt(cue, soundPosition);
    }

    private static void FreezePresentation(Transform root, float duration)
    {
        if (root == null || duration <= 0f)
            return;

        Animator[] animators = root.GetComponentsInChildren<Animator>(true);
        float restoreAt = Time.unscaledTime + duration;
        foreach (Animator animator in animators)
        {
            if (animator == null)
                continue;

            FrozenAnimator existing = frozenAnimators.Find(
                frozen => frozen.animator == animator
            );
            if (existing != null)
            {
                existing.restoreAt = Mathf.Max(existing.restoreAt, restoreAt);
                continue;
            }

            frozenAnimators.Add(new FrozenAnimator
            {
                animator = animator,
                originalSpeed = animator.speed,
                restoreAt = restoreAt
            });
        }
    }

    private static void FreezeGameplay(Transform participant, float duration)
    {
        if (participant == null || duration <= 0f)
            return;

        Transform combatantRoot = GetCombatantRoot(participant);
        float freezeUntil = Time.unscaledTime + duration;
        FrozenGameplay existing = frozenGameplay.Find(
            entry => entry.combatantRoot == combatantRoot
        );
        if (existing != null)
            existing.restoreAt = Mathf.Max(existing.restoreAt, freezeUntil);
        else
        {
            frozenGameplay.Add(new FrozenGameplay
            {
                combatantRoot = combatantRoot,
                restoreAt = freezeUntil
            });
        }
    }

    private static Transform GetCombatantRoot(Transform transform)
    {
        EnemyBase enemy = transform.GetComponentInParent<EnemyBase>();
        if (enemy != null)
            return enemy.transform;

        PlayerHealth player = transform.GetComponentInParent<PlayerHealth>();
        if (player != null)
            return player.transform;

        return transform.root;
    }
}
