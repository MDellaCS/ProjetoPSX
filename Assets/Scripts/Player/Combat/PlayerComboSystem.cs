using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>Conta golpes confirmados, termina combos e concede recompensas por breakpoint.</summary>
[DisallowMultipleComponent]
public sealed class PlayerComboSystem : MonoBehaviour
{
    private const string SettingsResourcePath = "Combat/PlayerComboSettings";
    private readonly HashSet<int> awardedBreakpoints = new();
    private PlayerComboSettings settings;
    private PlayerHeat heat;
    private PlayerCurrencyWallet wallet;
    private int currentHits;
    private float secondsRemaining;

    public event Action<int, float, bool> ComboChanged;
    public event Action<string> RewardGiven;

    public int CurrentHits => currentHits;
    public float SecondsRemaining => secondsRemaining;
    public float TimeoutSeconds => settings != null ? settings.IdleTimeout : 4f;

    public static PlayerComboSystem EnsureOn(GameObject player)
    {
        if (player == null)
            return null;
        PlayerComboSystem system = player.GetComponent<PlayerComboSystem>();
        return system != null ? system : player.AddComponent<PlayerComboSystem>();
    }

    public static void RegisterConfirmedHit(Transform player)
    {
        if (player == null)
            return;
        PlayerComboSystem system = EnsureOn(player.gameObject);
        if (system != null)
            system.RegisterConfirmedHit();
    }

    public static void RegisterPlayerDamaged(Transform player)
    {
        if (player == null || !player.TryGetComponent(out PlayerComboSystem system))
            return;
        system.EndCombo(true);
    }

    private void Awake()
    {
        settings = Resources.Load<PlayerComboSettings>(SettingsResourcePath);
        if (settings == null)
            Debug.LogWarning("[Combo] Crie PlayerComboSettings em Assets/Resources/Combat/PlayerComboSettings.asset para configurar tempo e breakpoints.", this);
        heat = GetComponent<PlayerHeat>();
        wallet = GetComponent<PlayerCurrencyWallet>();
    }

    private void Update()
    {
        if (currentHits <= 0 || GamePauseController.IsPaused ||
            CutsceneController.IsPlayerControlLocked)
            return;

        secondsRemaining = Mathf.Max(0f, secondsRemaining - Time.deltaTime);
        if (secondsRemaining <= 0f)
        {
            EndCombo(false);
            return;
        }
        ComboChanged?.Invoke(currentHits, secondsRemaining, false);
    }

    public void RegisterConfirmedHit()
    {
        if (currentHits < int.MaxValue)
            currentHits++;
        secondsRemaining = TimeoutSeconds;
        ComboChanged?.Invoke(currentHits, secondsRemaining, false);
        AwardReachedBreakpoints();
    }

    public void ResetForNewGame() => EndCombo(false);

    private void EndCombo(bool brokenByDamage)
    {
        if (currentHits <= 0)
            return;
        currentHits = 0;
        secondsRemaining = 0f;
        awardedBreakpoints.Clear();
        ComboChanged?.Invoke(0, 0f, brokenByDamage);
    }

    private void AwardReachedBreakpoints()
    {
        if (settings == null || settings.Breakpoints == null)
            return;

        IReadOnlyList<PlayerComboBreakpoint> breakpoints = settings.Breakpoints;
        StringBuilder message = null;
        for (int i = 0; i < breakpoints.Count; i++)
        {
            PlayerComboBreakpoint breakpoint = breakpoints[i];
            if (breakpoint == null || currentHits < breakpoint.requiredHits ||
                !awardedBreakpoints.Add(i))
                continue;

            bool heatGranted = breakpoint.heatReward > 0f &&
                CanGrantHeat();
            if (heatGranted)
                heat.AddHeat(breakpoint.heatReward);

            bool currencyGranted = breakpoint.currencyReward > 0;
            if (currencyGranted)
            {
                if (wallet == null)
                    wallet = GetComponent<PlayerCurrencyWallet>();
                if (wallet == null)
                    wallet = gameObject.AddComponent<PlayerCurrencyWallet>();
                wallet.Add(breakpoint.currencyReward);
            }

            if (!heatGranted && !currencyGranted)
                continue;
            if (message == null)
                message = new StringBuilder(48);
            else
                message.Append("   ");
            if (heatGranted)
                message.Append("+ ").Append(breakpoint.heatReward.ToString("0.#"))
                    .Append(" HEAT");
            if (currencyGranted)
            {
                if (heatGranted)
                    message.Append("   ");
                message.Append("+ ").Append(breakpoint.currencyReward)
                    .Append(" CURRENCY");
            }
        }

        if (message != null)
            RewardGiven?.Invoke(message.ToString());
    }

    private bool CanGrantHeat()
    {
        if (heat == null)
            heat = GetComponent<PlayerHeat>();
        return heat != null && heat.IsHeatModeActive && !heat.IsBurning;
    }
}
