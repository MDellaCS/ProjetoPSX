using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registro central das interacoes. Cada <see cref="InteractableBase"/> se
/// registra em OnEnable; o driver escolhe um unico candidato por frame, mostra
/// o prompt e entrega o input Interagir a ele. Sem objetos interativos na cena
/// nao existe driver nem custo.
/// </summary>
public static class InteractionSystem
{
    private static readonly List<InteractableBase> items = new();
    private static InteractionDriver driver;

    /// <summary>Interacao atualmente oferecida ao jogador (ou nulo).</summary>
    public static InteractableBase Current => driver != null ? driver.Current : null;

    internal static IReadOnlyList<InteractableBase> Items => items;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        items.Clear();
        driver = null;
    }

    internal static void Register(InteractableBase item)
    {
        if (item == null || item is HandLadder ladder && !ladder.IsClimbable)
            return;
        if (!items.Contains(item))
            items.Add(item);
        if (driver == null)
            driver = InteractionDriver.Create();
    }

    internal static void Unregister(InteractableBase item)
    {
        items.Remove(item);
        if (driver != null)
            driver.Forget(item);
    }
}
