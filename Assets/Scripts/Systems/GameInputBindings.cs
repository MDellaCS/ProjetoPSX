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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Com Domain Reload desligado, a operacao de rebind e os assinantes do
        // evento sobreviveriam entre execucoes do Play Mode.
        if (operation != null)
            operation.Dispose();
        operation = null;
        asset = null;
        BindingsChanged = null;
        LastRebindFrame = -1;
    }

    internal static void InitializeSession(InputActionAsset inputAsset)
    {
        CancelRebind();
        asset = inputAsset;
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

    public static string Snapshot()
    {
        InputActionAsset actions = GameInput.Asset;
        return actions != null
            ? actions.SaveBindingOverridesAsJson() : string.Empty;
    }

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
        InputActionAsset inputAsset = GameInput.Asset;
        InputAction action = inputAsset != null
            ? inputAsset.FindAction("Gameplay/" + actionName) : null;
        if (action == null)
            return "?";

        // Mostra o binding do dispositivo usado mais recentemente.
        bool preferGamepad = IsGamepadMoreRecent();
        string fallback = null;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (binding.isComposite || binding.isPartOfComposite)
                continue;
            bool keyboard = IsKeyboardOrMouse(binding);
            string display = action.GetBindingDisplayString(i);
            if (keyboard != preferGamepad)
                return display;
            if (fallback == null)
                fallback = display;
        }
        return fallback ?? "?";
    }

    private static bool IsGamepadMoreRecent()
    {
        Gamepad pad = Gamepad.current;
        if (pad == null)
            return false;
        double keyboardTime = Keyboard.current != null
            ? Keyboard.current.lastUpdateTime : 0d;
        double mouseTime = Mouse.current != null
            ? Mouse.current.lastUpdateTime : 0d;
        return pad.lastUpdateTime > Math.Max(keyboardTime, mouseTime);
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
                    int twinBindingIndex = FindMatchingBindingIndex(twin, path);
                    if (twinBindingIndex >= 0)
                    {
                        conflict = HasConflict(twin, twinBindingIndex, path);
                        if (!conflict)
                            twin.ApplyBindingOverride(twinBindingIndex, path);
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

    private static int FindMatchingBindingIndex(InputAction action, string path)
    {
        if (action == null || string.IsNullOrEmpty(path))
            return -1;
        int separator = path.IndexOf('/');
        string device = separator >= 0 ? path.Substring(0, separator) : path;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (binding.isComposite || binding.isPartOfComposite)
                continue;
            string candidate = binding.effectivePath;
            int candidateSeparator = candidate.IndexOf('/');
            string candidateDevice = candidateSeparator >= 0
                ? candidate.Substring(0, candidateSeparator) : candidate;
            if (string.Equals(device, candidateDevice,
                StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
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
