using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Reserva ataques exclusivos por tipo e os inicia apos alguns frames.</summary>
public static class EnemySpecialAttackQueue
{
    private sealed class Reservation
    {
        public EnemyBase owner;
        public int startFrame;
        public Func<bool> startAttack;
        public bool isStarting;
    }

    private static readonly Dictionary<string, Reservation> reservations = new();
    private static int lastTickFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        reservations.Clear();
        lastTickFrame = -1;
    }

    public static bool IsAvailableFor(string attackType, EnemyBase requester)
    {
        if (string.IsNullOrWhiteSpace(attackType))
            return true;
        Tick();
        return !reservations.TryGetValue(attackType, out Reservation reservation) ||
            reservation.owner == requester;
    }

    public static bool TryQueue(string attackType, EnemyBase requester,
        int delayFrames, Func<bool> startAttack)
    {
        if (string.IsNullOrWhiteSpace(attackType) || requester == null ||
            startAttack == null)
            return false;
        Tick();
        if (reservations.ContainsKey(attackType))
            return false;

        var reservation = new Reservation
        {
            owner = requester,
            startFrame = Time.frameCount + Mathf.Max(0, delayFrames),
            startAttack = startAttack
        };
        reservations.Add(attackType, reservation);
        if (delayFrames <= 0)
        {
            StartReservedAttack(attackType, reservation);
            return reservations.TryGetValue(attackType, out Reservation current) &&
                current == reservation;
        }

        requester.SetWaitingForSpecialAttackQueue(true);
        return true;
    }

    public static void Tick()
    {
        if (lastTickFrame == Time.frameCount)
            return;
        lastTickFrame = Time.frameCount;

        List<string> ready = null;
        foreach (KeyValuePair<string, Reservation> pair in reservations)
        {
            Reservation reservation = pair.Value;
            if (reservation == null || reservation.owner == null ||
                reservation.owner.IsDead || !reservation.owner.isActiveAndEnabled ||
                (!reservation.isStarting &&
                    (reservation.owner.IsLaunched ||
                     reservation.owner.IsLaunchedForwards ||
                     reservation.owner.CurrentState !=
                        EnemyBase.State.SpecialAttackQueue)))
            {
                (ready ??= new List<string>()).Add(pair.Key);
                continue;
            }
            if (!reservation.isStarting && Time.frameCount >= reservation.startFrame)
                (ready ??= new List<string>()).Add(pair.Key);
        }

        if (ready == null)
            return;
        foreach (string attackType in ready)
        {
            if (!reservations.TryGetValue(attackType, out Reservation reservation))
                continue;
            if (reservation.owner == null || reservation.owner.IsDead ||
                !reservation.owner.isActiveAndEnabled ||
                (!reservation.isStarting &&
                    (reservation.owner.IsLaunched ||
                     reservation.owner.IsLaunchedForwards ||
                     reservation.owner.CurrentState !=
                        EnemyBase.State.SpecialAttackQueue)))
            {
                reservations.Remove(attackType);
                if (reservation.owner != null && !reservation.owner.IsDead &&
                    reservation.owner.isActiveAndEnabled)
                    reservation.owner.NotifySpecialAttackQueueFailed(attackType);
                continue;
            }
            StartReservedAttack(attackType, reservation);
        }
    }

    public static void Release(EnemyBase owner, string attackType = null)
    {
        if (owner == null)
            return;
        if (!string.IsNullOrWhiteSpace(attackType))
        {
            ReleaseReservation(attackType, owner);
            return;
        }

        List<string> owned = null;
        foreach (KeyValuePair<string, Reservation> pair in reservations)
            if (pair.Value != null && pair.Value.owner == owner)
                (owned ??= new List<string>()).Add(pair.Key);
        if (owned == null)
            return;
        foreach (string key in owned)
            ReleaseReservation(key, owner);
    }

    private static void StartReservedAttack(string attackType,
        Reservation reservation)
    {
        if (reservation.isStarting)
            return;
        reservation.isStarting = true;
        bool started = reservation.startAttack != null &&
            reservation.startAttack();
        if (started)
            return;
        reservations.Remove(attackType);
        reservation.owner?.NotifySpecialAttackQueueFailed(attackType);
    }

    private static void ReleaseReservation(string attackType, EnemyBase owner)
    {
        if (reservations.TryGetValue(attackType, out Reservation reservation) &&
            reservation != null && reservation.owner == owner)
            reservations.Remove(attackType);
    }
}
