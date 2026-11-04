using System;
using UnityEngine;

/// <summary>
/// Mede, a cada frame, como uma entidade (jogador ou inimigo) esta em relacao
/// a agua e expoe os multiplicadores e permissoes do perfil. Os sistemas de
/// movimento e combate so leem esta classe; nao sabem de volumes nem de colliders.
/// Sem nenhum WaterVolume na cena o custo e zero.
/// </summary>
[DefaultExecutionOrder(-1100)]
[DisallowMultipleComponent]
public sealed class WaterSensor : MonoBehaviour
{
    private PlayerCharacterController playerController;
    private CharacterController characterController;
    private Collider fallbackCollider;
    private bool resolved;
    private bool resolvedEnemy;
    private WaterVolume currentVolume;
    private EnemyBobbing bobbing;
    private EnemyBase enemyBase;
    private PlayerMovement playerMovement;
    private EnemyMovement enemyMovement;

    /// <summary>Chefes e outros: ignoram a agua por completo.</summary>
    public bool Immune { get; set; }
    public WaterZone Zone { get; private set; }
    public WaterProfile Profile { get; private set; }
    public float SurfaceY { get; private set; }
    public float FloorY { get; private set; }
    public float FeetDepth { get; private set; }

    public bool IsInWater => Zone != WaterZone.None;
    public bool IsSwimming => Zone == WaterZone.Deep;
    public float SpeedMultiplier => IsInWater ? Profile.SpeedMultiplier(Zone) : 1f;
    public float DashDistanceMultiplier => IsInWater ? Profile.DashDistanceMultiplier(Zone) : 1f;
    public float AttackMovementMultiplier => IsInWater ? Profile.AttackMovementMultiplier(Zone) : 1f;
    /// <summary>Limite de aceleracao na agua (m/s2); 0 = usar o valor normal do chao.</summary>
    public float AccelerationLimit => IsInWater ? Profile.Acceleration(Zone) : 0f;
    /// <summary>Limite de desaceleracao na agua (m/s2); 0 = usar o valor normal do chao.</summary>
    public float DecelerationLimit => IsInWater ? Profile.Deceleration(Zone) : 0f;
    /// <summary>Velocidade extra ao pular para fora da agua funda (m/s).</summary>
    public float JumpExitBoost => Zone == WaterZone.Deep ? Profile.deepJumpExitBoost : 0f;
    public bool AllowsDash => !IsInWater || Profile.AllowsDash(Zone);
    public bool AllowsAttacks => !IsInWater || Profile.AllowsAttacks(Zone);
    public bool CoolsHeat => IsInWater && Profile.CoolsHeat(Zone);
    /// <summary>Bobbing atual (subir e descer) de quem flutua, em metros.</summary>
    public float FloatBobOffset { get; private set; }
    /// <summary>Impulso vertical extra do bobbing ao retomar o movimento, em m/s.</summary>
    public float FloatBobSpeedBoost { get; private set; }
    /// <summary>Altura dos pes quando a entidade flutua na agua funda (com o bobbing).</summary>
    public float FloatTargetFeetY =>
        SurfaceY - (Profile != null ? Profile.floatFeetDepth : 0f) + FloatBobOffset;

    /// <summary>Lado de fora (efeitos, audio): zona anterior e nova.</summary>
    public static event Action<WaterSensor, WaterZone, WaterZone> ZoneChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ZoneChanged = null;

    public static WaterSensor GetOrAdd(GameObject target)
    {
        return target.TryGetComponent(out WaterSensor sensor)
            ? sensor : target.AddComponent<WaterSensor>();
    }

    private void OnDisable() => SetZone(WaterZone.None, null, 0f, 0f, 0f);

    private void Update()
    {
        if (WaterVolume.Active.Count == 0 || Immune)
        {
            if (Zone != WaterZone.None)
                SetZone(WaterZone.None, null, 0f, 0f, 0f);
            UpdateBobbing();
            return;
        }

        // Cadaveres nao participam da agua (e nao gastam raycast).
        if (enemyBase == null && !resolvedEnemy)
        {
            resolvedEnemy = true;
            TryGetComponent(out enemyBase);
        }
        if (enemyBase != null && enemyBase.IsDead)
        {
            SetZone(WaterZone.None, null, 0f, 0f, 0f);
            UpdateBobbing();
            return;
        }

        // Sem collider valido (morte, spawn, teleporte): nao fica com a zona antiga.
        if (!TryGetBounds(out Bounds bounds))
        {
            SetZone(WaterZone.None, null, 0f, 0f, 0f);
            UpdateBobbing();
            return;
        }
        UpdateBobbing();

        Vector3 center = bounds.center;
        float feetY = bounds.min.y;
        // Dentro de uma agua o minimo de contato vem do perfil do volume; o
        // primeiro teste usa o menor valor possivel e o volume refina.
        if (!WaterVolume.TryGetSample(center.x, center.z, feetY, transform,
                currentVolume, out WaterSample sample))
        {
            SetZone(WaterZone.None, null, 0f, 0f, 0f);
            return;
        }

        WaterProfile profile = sample.Profile;
        bool wasDeepInSameVolume = Zone == WaterZone.Deep &&
            currentVolume == sample.volume;
        WaterZone zone = profile.ClassifyDepth(sample.ColumnDepth,
            wasDeepInSameVolume);
        SetZone(zone, sample.volume, sample.surfaceY, sample.floorY,
            sample.surfaceY - feetY);
    }

    private void SetZone(WaterZone zone, WaterVolume volume, float surfaceY,
        float floorY, float feetDepth)
    {
        WaterZone previous = Zone;
        WaterVolume previousVolume = currentVolume;
        Zone = zone;
        currentVolume = volume;
        Profile = volume != null ? volume.Profile : null;
        SurfaceY = surfaceY;
        FloorY = floorY;
        FeetDepth = feetDepth;

        if (previous == zone && previousVolume == volume)
            return;

        if (previousVolume != volume)
        {
            if (previousVolume != null)
                previousVolume.NotifyExited(gameObject);
            if (volume != null)
                volume.NotifyEntered(gameObject);
        }

        ZoneChanged?.Invoke(this, previous, zone);
    }

    // Bobbing de quem flutua: mesmo componente do inimigo voador (EnemyBobbing),
    // em um objeto filho proprio para nao competir com o bobbing de voo.
    private void UpdateBobbing()
    {
        if (Zone != WaterZone.Deep || Profile == null || !Profile.floatBobbingEnabled)
        {
            FloatBobOffset = 0f;
            FloatBobSpeedBoost = 0f;
            return;
        }

        if (bobbing == null)
        {
            GameObject child = new("Water Bobbing (Runtime)")
            {
                hideFlags = HideFlags.HideInHierarchy
            };
            child.transform.SetParent(transform, false);
            bobbing = child.AddComponent<EnemyBobbing>();
            TryGetComponent(out playerMovement);
            TryGetComponent(out enemyMovement);
        }

        float horizontalSpeed = playerMovement != null
            ? playerMovement.HorizontalSpeed
            : enemyMovement != null ? enemyMovement.LastHorizontalSpeed : 0f;
        // O bobbing nao avanca durante hitstop (evita salto ao retomar).
        if (CombatImpactSystem.IsGameplayFrozen(this))
            return;
        bobbing.Tick(horizontalSpeed, Time.deltaTime);
        float scale = Mathf.Max(0f, Profile.floatBobbingScale);
        FloatBobOffset = bobbing.CurrentHeightOffset * scale;
        FloatBobSpeedBoost = bobbing.CurrentVerticalSpeedBoost * scale;
    }

    private bool TryGetBounds(out Bounds bounds)
    {
        if (!resolved)
        {
            resolved = true;
            TryGetComponent(out playerController);
            TryGetComponent(out characterController);
            TryGetComponent(out fallbackCollider);
        }

        if (playerController != null && playerController.IsColliderEnabled)
        {
            bounds = playerController.bounds;
            return true;
        }

        if (characterController != null && characterController.enabled)
        {
            bounds = characterController.bounds;
            return true;
        }

        if (fallbackCollider != null && fallbackCollider.enabled)
        {
            bounds = fallbackCollider.bounds;
            return true;
        }

        bounds = default;
        return false;
    }
}
