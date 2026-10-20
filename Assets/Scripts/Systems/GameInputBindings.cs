using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Overrides de input, conflito entre ações e rebinding interativo.</summary>
public static class GameInputBindings
{
    private const string SaveKey = "ProjetoPSX.InputBindings.V1";
    private static InputActionAsset asset;
    private static InputActionRebindingExtensions.RebindingOperation operation;
    public static event Action BindingsChanged;
    public static bool IsRebinding => operation != null;
    public static int LastRebindFrame { get; private set; } = -1;

    internal static void InitializeSession(InputActionAsset inputAsset)
    {
        CancelRebind();
        asset = inputAsset;
        BindingsChanged = null;
        LastRebindFrame = -1;
        asset.RemoveAllBindingOverrides();
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return;
        try
        {
            asset.LoadBindingOverridesFromJson(json);
            if (HasAnyConflict(asset.FindActionMap("Gameplay")) ||
                HasAnyConflict(asset.FindActionMap("UI")))
                asset.RemoveAllBindingOverrides();
        }
        catch (Exception)
        {
            asset.RemoveAllBindingOverrides();
        }
    }

    public static string Snapshot() =>
        GameInput.Asset?.SaveBindingOverridesAsJson() ?? string.Empty;

    public static void Restore(string json)
    {
        CancelRebind();
        InputActionAsset actions = GameInput.Asset;
        if (actions == null)
            return;
        actions.RemoveAllBindingOverrides();
        if (!string.IsNullOrEmpty(json))
        {
            try { actions.LoadBindingOverridesFromJson(json); }
            catch (Exception) { actions.RemoveAllBindingOverrides(); }
        }
        BindingsChanged?.Invoke();
    }

    public static void Save()
    {
        PlayerPrefs.SetString(SaveKey, Snapshot());
        PlayerPrefs.Save();
        BindingsChanged?.Invoke();
    }

    public static void ResetToDefaults()
    {
        Restore(string.Empty);
    }

    public static string GetDisplayString(string actionName)
    {
        InputAction action = GameInput.Asset?.FindAction("Gameplay/" + actionName);
        if (action == null)
            return "?";
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (!binding.isComposite && IsKeyboardOrMouse(binding))
                return action.GetBindingDisplayString(i);
        }
        return "?";
    }

    public static bool IsKeyboardOrMouse(InputBinding binding)
    {
        string path = binding.effectivePath;
        return !string.IsNullOrEmpty(path) &&
            (path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("<Mouse>/", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("<Pointer>/", StringComparison.OrdinalIgnoreCase));
    }

    public static void BeginRebind(InputAction action, int bindingIndex,
        Action<bool, string> finished)
    {
        if (action == null || bindingIndex < 0 ||
            bindingIndex >= action.bindings.Count || IsRebinding)
            return;

        string before = Snapshot();
        operation = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(op =>
            {
                string path = action.bindings[bindingIndex].effectivePath;
                bool conflict = HasConflict(action, bindingIndex, path);
                if (!conflict && (action.name == "Pause" || action.name == "DebugHUD"))
                {
                    InputAction twin = asset.FindAction("UI/" + action.name);
                    if (twin != null && twin.bindings.Count > 0)
                    {
                        conflict = HasConflict(twin, 0, path);
                        if (!conflict)
                            twin.ApplyBindingOverride(0, path);
                    }
                }
                DisposeOperation();
                LastRebindFrame = Time.frameCount;
                if (conflict)
                    Restore(before);
                else
                    BindingsChanged?.Invoke();
                finished?.Invoke(!conflict, conflict
                    ? "Tecla já usada por outra ação." : "Atalho alterado. Clique Aplicar para salvar.");
            })
            .OnCancel(_ =>
            {
                DisposeOperation();
                LastRebindFrame = Time.frameCount;
                finished?.Invoke(false, "Alteração cancelada.");
            });
        operation.Start();
    }

    public static void CancelRebind() => operation?.Cancel();

    private static void DisposeOperation()
    {
        InputActionRebindingExtensions.RebindingOperation completed = operation;
        operation = null;
        completed?.Dispose();
    }

    private static bool HasConflict(InputAction action, int bindingIndex, string path)
    {
        if (string.IsNullOrEmpty(path))
            return true;
        foreach (InputAction other in action.actionMap.actions)
        {
            for (int i = 0; i < other.bindings.Count; i++)
            {
                if (other == action && i == bindingIndex)
                    continue;
                InputBinding binding = other.bindings[i];
                if (!binding.isComposite && string.Equals(binding.effectivePath,
                    path, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    private static bool HasAnyConflict(InputActionMap map)
    {
        if (map == null)
            return false;
        foreach (InputAction action in map.actions)
            for (int i = 0; i < action.bindings.Count; i++)
                if (action.bindings[i].hasOverrides &&
                    HasConflict(action, i, action.bindings[i].effectivePath))
                    return true;
        return false;
    }
}
