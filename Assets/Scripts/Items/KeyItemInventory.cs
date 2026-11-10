using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Inventario ilimitado de itens-chave, persistido entre cenas e sessoes.</summary>
public static class KeyItemInventory
{
    private const string SaveKey = "ProjetoPSX.KeyItems.V1";
    private static readonly HashSet<string> collectedIds = new(StringComparer.Ordinal);
    private static readonly List<KeyItemDefinition> definitions = new();
    private static bool initialized;

    public static event Action Changed;
    public static int Count { get { EnsureInitialized(); return collectedIds.Count; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        collectedIds.Clear();
        definitions.Clear();
        initialized = false;
        Changed = null;
    }

    public static IReadOnlyList<KeyItemDefinition> GetCollectedItems()
    {
        EnsureInitialized();
        List<KeyItemDefinition> result = new();
        for (int i = 0; i < definitions.Count; i++)
        {
            KeyItemDefinition definition = definitions[i];
            if (definition != null && collectedIds.Contains(definition.Id))
                result.Add(definition);
        }
        return result;
    }

    public static bool Contains(KeyItemDefinition item)
    {
        EnsureInitialized();
        return item != null && collectedIds.Contains(item.Id);
    }

    public static string[] ExportCollectedIds()
    {
        EnsureInitialized();
        string[] result = new string[collectedIds.Count];
        collectedIds.CopyTo(result);
        return result;
    }

    public static void RestoreCollectedIds(string[] ids)
    {
        EnsureInitialized();
        collectedIds.Clear();
        if (ids != null)
            foreach (string id in ids)
                if (!string.IsNullOrWhiteSpace(id)) collectedIds.Add(id);
        Persist();
        Changed?.Invoke();
    }

    public static bool Grant(KeyItemDefinition item, bool showModal = true)
    {
        EnsureInitialized();
        if (item == null || string.IsNullOrWhiteSpace(item.Id) ||
            !collectedIds.Add(item.Id))
            return false;

        if (!definitions.Contains(item))
            definitions.Add(item);
        Persist();
        Changed?.Invoke();
        if (showModal)
            ItemAcquisitionModalHUD.Show(item);
        return true;
    }

    /// <summary>Remove todos os itens informados, ou nenhum se algum estiver ausente.</summary>
    public static bool TryConsume(IReadOnlyList<KeyItemDefinition> items)
    {
        EnsureInitialized();
        if (items == null || items.Count == 0)
            return false;

        List<string> ids = new(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            KeyItemDefinition item = items[i];
            if (item == null || string.IsNullOrWhiteSpace(item.Id))
                return false;

            string id = item.Id.Trim();
            if (ids.Contains(id))
                continue;
            if (!collectedIds.Contains(id))
                return false;
            ids.Add(id);
        }

        for (int i = 0; i < ids.Count; i++)
            collectedIds.Remove(ids[i]);

        Persist();
        Changed?.Invoke();
        return true;
    }

    public static void ResetForNewGame()
    {
        EnsureInitialized();
        collectedIds.Clear();
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    internal static void DeleteStoredProgress() => PlayerPrefs.DeleteKey(SaveKey);

    private static void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;
        // Catalogo dedicado a itens-chave: Assets/Resources/Itens.
        KeyItemDefinition[] loaded = Resources.LoadAll<KeyItemDefinition>("Itens");
        Array.Sort(loaded, (a, b) => string.Compare(a != null ? a.ItemName : null,
            b != null ? b.ItemName : null, StringComparison.CurrentCulture));
        foreach (KeyItemDefinition definition in loaded)
        {
            if (definition != null && !string.IsNullOrWhiteSpace(definition.Id) &&
                !definitions.Exists(existing => existing != null && existing.Id == definition.Id))
                definitions.Add(definition);
        }

        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return;
        try
        {
            StoredItems stored = JsonUtility.FromJson<StoredItems>(json);
            if (stored == null || stored.ids == null)
                return;
            foreach (string id in stored.ids)
                if (!string.IsNullOrWhiteSpace(id))
                    collectedIds.Add(id);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Inventario] O save de itens-chave nao pode ser lido: " + exception.Message);
        }
    }

    private static void Persist()
    {
        StoredItems stored = new();
        stored.ids.AddRange(collectedIds);
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(stored));
        PlayerPrefs.Save();
    }

    [Serializable]
    private sealed class StoredItems { public List<string> ids = new(); }
}
