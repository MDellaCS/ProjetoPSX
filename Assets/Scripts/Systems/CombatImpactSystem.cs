using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Camada central de apresentacao para impactos de combate.
/// Ela nao altera Time.timeScale: congela apenas os Animators envolvidos e
/// solicita um impulso visual para a camera.
/// </summary>
[DisallowMultipleComponent]
public class CombatImpactSystem : MonoBehaviour
{
    [Serializable]
    public struct ImpactProfile
    {
        [Tooltip("Tempo em segundos reais que os visuais dos envolvidos congelam.")]
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

    private static CombatImpactSystem instance;
    private static readonly List<FrozenAnimator> frozenAnimators = new();

    [Header("Player Attack Impact")]
    [SerializeField] private ImpactProfile lightAttackImpact = new ImpactProfile
    {
        hitstopDuration = .018f,
        cameraShakeDistance = .045f,
        cameraShakeDuration = .055f
    };
    [SerializeField] private ImpactProfile heavyAttackImpact = new ImpactProfile
    {
        hitstopDuration = .055f,
        cameraShakeDistance = .16f,
        cameraShakeDuration = .10f
    };

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
            // durante Update, sem pausar fisica, input ou IA.
            frozen.animator.speed = 0f;
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
        system.PlayImpact(attacker, target, profile, null, requestCameraShake);
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
        system.PlayImpact(attacker, target, profile, attackerPosition, true);
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
        bool requestCameraShake)
    {
        FreezePresentation(attacker, profile.hitstopDuration);
        FreezePresentation(target, profile.hitstopDuration);

        if (target == null || !requestCameraShake)
            return;

        Vector3 origin = explicitAttackerPosition ??
            (attacker != null ? attacker.position : target.position);
        Vector3 attackDirection = target.position - origin;
        attackDirection.y = 0f;

        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.RequestImpactShake(
                attackDirection,
                profile.cameraShakeDistance,
                profile.cameraShakeDuration,
                impactShakeIntensity
            );
        }
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
}
