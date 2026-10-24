using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Acesso unico ao asset de acoes configurado em Project Settings > Input System.
/// Gameplay e UI nunca ficam ativos simultaneamente durante uma pausa.
/// </summary>
public static class GameInput
{
    private static InputActionMap gameplay;
    private static InputActionMap ui;
    private static InputActionAsset sourceAsset;
    private static bool initialized;
    public static InputActionAsset Asset
    {
        get
        {
            EnsureInitialized();
            return InputSystem.actions;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeSession()
    {
        GamePauseController.PauseChanged -= OnPauseChanged;
        gameplay = null;
        ui = null;
        sourceAsset = null;
        initialized = false;
        EnsureInitialized();
    }

    public static Vector2 Move => ReadVector2("Move");
    public static Vector2 Look => ReadVector2("Look");
    public static Vector2 CycleTarget => ReadVector2("CycleTarget");
    public static bool LightPressed => Pressed("LightAttack");
    public static bool HeavyPressed => Pressed("HeavyAttack");
    public static bool JumpPressed => Pressed("Jump");
    public static bool JumpHeld => Held("Jump");
    public static bool DashPressed => Pressed("Dash");
    public static bool CrouchHeld => Held("Crouch");
    public static bool LockOnPressed => Pressed("LockOn");
    public static bool UnlockCursorPressed => Pressed("UnlockCursor");
    public static bool RestartPressed => Pressed("Restart");
    public static bool DebugHeatDownHeld => Held("DebugHeatDown");
    public static bool DebugHeatUpHeld => Held("DebugHeatUp");
    public static bool DebugCombatPressed => Pressed("DebugCombat");
    public static bool DebugGodModePressed => Pressed("DebugGodMode");
    public static bool DebugMaxHeatPressed => Pressed("DebugMaxHeat");
    public static bool DebugSlowMotionPressed => Pressed("DebugSlowMotion");

    public static bool PausePressed
    {
        get
        {
            if (CutsceneController.IsPlaying)
                return false;
            EnsureInitialized();
            return CurrentMap()?.FindAction("Pause")?.WasPressedThisFrame() == true;
        }
    }

    public static bool DebugHUDPressed
    {
        get
        {
            EnsureInitialized();
            return CurrentMap()?.FindAction("DebugHUD")?.WasPressedThisFrame() == true;
        }
    }

    public static bool UIClickPressed
    {
        get
        {
            EnsureInitialized();
            return ui?.FindAction("Click")?.WasPressedThisFrame() == true;
        }
    }

    public static Vector2 UIPointer
    {
        get
        {
            EnsureInitialized();
            return ui?.FindAction("Point")?.ReadValue<Vector2>() ?? Vector2.zero;
        }
    }

    public static Vector2 UINavigate => ReadUIVector2("Navigate");
    public static bool UISubmitPressed => PressedUI("Submit");
    public static bool UICancelPressed => PressedUI("Cancel");

    private static bool PressedUI(string actionName)
    {
        EnsureInitialized();
        return ui?.FindAction(actionName)?.WasPressedThisFrame() == true;
    }

    private static Vector2 ReadUIVector2(string actionName)
    {
        EnsureInitialized();
        return ui?.FindAction(actionName)?.ReadValue<Vector2>() ?? Vector2.zero;
    }

    private static bool Pressed(string actionName)
    {
        if (CutsceneController.IsPlayerControlLocked)
            return false;
        EnsureInitialized();
        return gameplay?.FindAction(actionName)?.WasPressedThisFrame() == true;
    }

    private static bool Held(string actionName)
    {
        if (CutsceneController.IsPlayerControlLocked)
            return false;
        EnsureInitialized();
        return gameplay?.FindAction(actionName)?.IsPressed() == true;
    }

    private static Vector2 ReadVector2(string actionName)
    {
        if (CutsceneController.IsPlayerControlLocked)
            return Vector2.zero;
        EnsureInitialized();
        return gameplay?.FindAction(actionName)?.ReadValue<Vector2>() ?? Vector2.zero;
    }

    private static InputActionMap CurrentMap() =>
        GamePauseController.IsPaused ? ui : gameplay;

    private static void EnsureInitialized()
    {
        InputActionAsset asset = InputSystem.actions;
        if (asset == null)
            return;

        // Com Enter Play Mode sem Domain Reload, mapas de uma execucao
        // anterior podem sobreviver apontando para um asset antigo ou ficar
        // desabilitados apos uma pausa/interrupcao. Revalidamos a origem em
        // toda consulta e restauramos o mapa esperado para a sessao atual.
        if (initialized && sourceAsset == asset && gameplay != null && ui != null)
        {
            EnsureExpectedMapIsEnabled();
            return;
        }

        gameplay = asset.FindActionMap("Gameplay");
        ui = asset.FindActionMap("UI");
        if (gameplay == null || ui == null)
            return;

        initialized = true;
        sourceAsset = asset;
        GameInputBindings.InitializeSession(asset);
        GamePauseController.PauseChanged -= OnPauseChanged;
        GamePauseController.PauseChanged += OnPauseChanged;
        OnPauseChanged(GamePauseController.IsPaused);
    }

    private static void EnsureExpectedMapIsEnabled()
    {
        if (GamePauseController.IsPaused)
        {
            if (gameplay.enabled)
                gameplay.Disable();
            if (!ui.enabled)
                ui.Enable();
            return;
        }

        if (ui.enabled)
            ui.Disable();
        if (!gameplay.enabled)
            gameplay.Enable();
    }

    private static void OnPauseChanged(bool paused)
    {
        if (gameplay == null || ui == null)
            return;

        if (paused)
        {
            gameplay.Disable();
            ui.Enable();
        }
        else
        {
            ui.Disable();
            gameplay.Enable();
        }
    }
}
