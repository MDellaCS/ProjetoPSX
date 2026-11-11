using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>Preserva ativacoes de portas e objetos ligados aos eventos de itens, sem repetir seus efeitos.</summary>
public static class GameWorldSaveState
{
    [Serializable]
    public sealed class Entry
    {
        public string scenePath;
        public string objectPath;
        public bool active;
    }

    private sealed class Target
    {
        public GameObject gameObject;
        public string scenePath;
        public string objectPath;
        public bool initialActive;
    }

    private static readonly Dictionary<GameObject, Target> targets = new();
    private static readonly Dictionary<string, Entry> saved = new(StringComparer.Ordinal);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        targets.Clear();
        saved.Clear();
    }

    // Apenas SetActive possui estado que pode ser restaurado sem tocar novamente
    // dialogos, cutscenes, concessoes de itens ou outros UnityEvents arbitrarios.
    public static void Register(UnityEvent actions)
    {
        if (actions == null) return;
        for (int i = 0; i < actions.GetPersistentEventCount(); i++)
        {
            if (actions.GetPersistentMethodName(i) != nameof(GameObject.SetActive) ||
                actions.GetPersistentTarget(i) is not GameObject target ||
                string.IsNullOrEmpty(target.scene.path) || targets.ContainsKey(target))
                continue;
            targets.Add(target, new Target
            {
                gameObject = target,
                scenePath = target.scene.path,
                objectPath = ObjectPath(target.transform),
                initialActive = target.activeSelf
            });
        }
    }

    private static string ObjectPath(Transform transform)
    {
        // O ordinal diferencia nomes repetidos sem depender de objetos de runtime
        // adicionados em outros pontos da hierarquia.
        int ordinal = 0;
        if (transform.parent != null)
        {
            for (int i = 0; i < transform.GetSiblingIndex(); i++)
                if (transform.parent.GetChild(i).name == transform.name) ordinal++;
        }
        else
        {
            foreach (GameObject root in transform.gameObject.scene.GetRootGameObjects())
            {
                if (root == transform.gameObject) break;
                if (root.name == transform.name) ordinal++;
            }
        }
        string segment = Uri.EscapeDataString(transform.name) + "#" + ordinal;
        return transform.parent != null ? ObjectPath(transform.parent) + "/" + segment : segment;
    }

    private static string Key(string scene, string path) => scene + "|" + path;

    public static void CaptureCurrentObjects()
    {
        foreach (Target target in targets.Values)
        {
            if (target.gameObject == null) continue;
            saved[Key(target.scenePath, target.objectPath)] = new Entry
            {
                scenePath = target.scenePath,
                objectPath = target.objectPath,
                active = target.gameObject.activeSelf
            };
        }
    }

    public static Entry[] Export()
    {
        CaptureCurrentObjects();
        Entry[] result = new Entry[saved.Count];
        saved.Values.CopyTo(result, 0);
        return result;
    }

    public static void Restore(Entry[] entries)
    {
        saved.Clear();
        if (entries == null) return;
        foreach (Entry entry in entries)
            if (entry != null && !string.IsNullOrWhiteSpace(entry.scenePath) &&
                !string.IsNullOrWhiteSpace(entry.objectPath))
                saved[Key(entry.scenePath, entry.objectPath)] = entry;
    }

    public static void ApplyScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (InventoryEventInteraction interaction in root.GetComponentsInChildren<InventoryEventInteraction>(true))
                interaction.RegisterSaveTargets();
            foreach (InventoryConditionListener listener in root.GetComponentsInChildren<InventoryConditionListener>(true))
                listener.RegisterSaveTargets();
        }
        // SetActive pode registrar outros alvos por OnEnable.
        foreach (Target target in new List<Target>(targets.Values))
            if (target.gameObject != null && target.gameObject.scene == scene &&
                saved.TryGetValue(Key(target.scenePath, target.objectPath), out Entry entry))
                target.gameObject.SetActive(entry.active);
        foreach (GameObject target in new List<GameObject>(targets.Keys))
            if (target == null) targets.Remove(target);
    }

    public static void ResetForNewGame()
    {
        saved.Clear();
        foreach (InventoryConditionListener listener in UnityEngine.Object.FindObjectsByType<InventoryConditionListener>(FindObjectsInactive.Include))
            listener.ResetForNewGame();
        foreach (InteractableBase interaction in UnityEngine.Object.FindObjectsByType<InteractableBase>(FindObjectsInactive.Include))
            interaction.ResetInteractionState();
        foreach (Target target in new List<Target>(targets.Values))
            if (target.gameObject != null) target.gameObject.SetActive(target.initialActive);
    }
}
