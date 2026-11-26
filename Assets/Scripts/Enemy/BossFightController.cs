using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BossHealthHUD))]
public sealed class BossFightController : MonoBehaviour
{
    [Tooltip("Segundos que a barra zerada permanece após a morte de cada chefe.")]
    [Min(0f)] [SerializeField] private float removalDelay = 1.5f;

    private sealed class Entry
    {
        public EnemyBase boss;
        public bool defeated;
        public float remaining;
        public int deathFrame;
        public PlayerState player;
    }

    private readonly Dictionary<EnemyBase, Entry> entries = new();
    private readonly List<EnemyBase> expired = new();
    private readonly List<EnemyEncounter> pendingEncounters = new();
    private BossHealthHUD hud;

    private void OnEnable()
    {
        hud = GetComponent<BossHealthHUD>();
        EnemyBase.EnemyDied += OnDeath;
        SceneManager.activeSceneChanged += SceneChanged;
    }

    private void OnDisable()
    {
        EnemyBase.EnemyDied -= OnDeath;
        SceneManager.activeSceneChanged -= SceneChanged;
        Clear();
    }

    private void SceneChanged(Scene before, Scene after) => Clear();

    private void Clear()
    {
        foreach (Entry entry in entries.Values) Remove(entry);
        entries.Clear();
        pendingEncounters.Clear();
    }

    private void Remove(Entry entry)
    {
        if (entry.boss != null)
        {
            entry.boss.HealthChanged -= HealthChanged;
            entry.boss.HoldBossTarget(false);
        }
        hud.Hide(entry.boss);
    }

    private static bool InCombat(EnemyBase boss) =>
        boss.isActiveAndEnabled && boss.IsInBossCombat &&
        (boss.BossEncounter == null || (boss.BossEncounter.isActiveAndEnabled &&
            boss.BossEncounter.CurrentState == EnemyEncounter.EncounterState.Active));

    private Entry Register(EnemyBase boss)
    {
        if (entries.TryGetValue(boss, out Entry entry)) return entry;
        entry = new Entry { boss = boss };
        entries.Add(boss, entry);
        boss.HoldBossTarget(true);
        boss.HealthChanged += HealthChanged;
        hud.Show(boss, boss.BossName);
        return entry;
    }

    private void HealthChanged(EnemyBase boss, int current, int maximum) =>
        hud.SetHealth(boss, current, maximum);

    private void LateUpdate()
    {
        if (GamePauseController.IsPaused) return;
        foreach (EnemyBase boss in EnemyBase.ActiveEnemies)
            if (boss.IsBoss && !boss.IsDead && InCombat(boss))
                Register(boss);

        foreach (Entry entry in entries.Values)
        {
            if (entry.defeated)
            {
                if (Time.frameCount != entry.deathFrame)
                    entry.remaining -= Time.unscaledDeltaTime;
                if (entry.remaining > 0f && entry.player != null &&
                    entry.player.gameObject.activeInHierarchy && !entry.player.IsDead()) continue;
            }
            else if (entry.boss != null && InCombat(entry.boss)) continue;
            expired.Add(entry.boss);
            Remove(entry);
        }
        foreach (EnemyBase boss in expired) entries.Remove(boss);
        expired.Clear();

        for (int i = pendingEncounters.Count - 1; i >= 0; i--)
        {
            EnemyEncounter encounter = pendingEncounters[i];
            if (encounter != null && encounter.isActiveAndEnabled && encounter.HasLivingPlayer)
            {
                if (!HasLivingBoss(encounter))
                {
                    if (encounter.HasUpcomingBoss) continue;
                    SplashTextMessages.Request(SplashTextMessage.BossDefeated);
                }
                // Um novo chefe vivo passa a ser o responsavel pela vitoria.
            }
            pendingEncounters.RemoveAt(i);
        }
    }

    private static bool HasLivingBoss(EnemyEncounter encounter)
    {
        foreach (EnemyBase other in EnemyBase.ActiveEnemies)
            if (other.IsBoss && !other.IsDead && other.BossEncounter == encounter &&
                (encounter != null || InCombat(other))) return true;
        return false;
    }

    private void OnDeath(EnemyBase boss)
    {
        if (!boss.IsBoss || !boss.HasLivingPlayerTarget || boss.WaitingForEncounter) return;
        EnemyEncounter encounter = boss.BossEncounter;
        if (encounter != null && (!encounter.isActiveAndEnabled ||
            encounter.CurrentState == EnemyEncounter.EncounterState.Inactive)) return;
        Entry entry = Register(boss);
        if (entry.defeated) return;
        entry.defeated = true;
        entry.remaining = removalDelay;
        entry.deathFrame = Time.frameCount;
        entry.player = boss.Target.GetComponent<PlayerState>();
        hud.SetHealth(boss, 0, boss.MaxHealth);
        if (HasLivingBoss(encounter)) return;

        // Uma onda futura pode sortear um chefe. Reavaliar após o último sorteio.
        if (encounter != null && encounter.HasUpcomingBoss)
        {
            if (!pendingEncounters.Contains(encounter)) pendingEncounters.Add(encounter);
        }
        else
        {
            pendingEncounters.Remove(encounter);
            SplashTextMessages.Request(SplashTextMessage.BossDefeated);
        }
    }
}
