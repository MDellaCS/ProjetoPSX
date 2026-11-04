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
    public static bool LookIsMouseDelta
    {
        get
        {
            EnsureInitialized();
            return gameplay?.FindAction("Look")?.activeControl?.device is Mouse;
        }
    }
    public static Vector2 CycleTarget => ReadVector2("CycleTarget");
    public static bool LightPressed => !MouseClickWillLockCursor() &&
        Pressed("LightAttack");
    public static bool HeavyPressed => Pressed("HeavyAttack");
    public static bool LightHeld => Held("LightAttack");
    public static bool HeavyHeld => Held("HeavyAttack");
    public static bool JumpPressed => Pressed("Jump");
    public static bool JumpHeld => Held("Jump");
    public static bool DashPressed => Pressed("Dash");
    public static bool CrouchHeld => Held("Crouch");
    public static bool LockOnPressed => Pressed("LockOn");
    public static bool InteractPressed => Pressed("Interact");
    public static bool SwingPressed => Pressed("Swing");
    public static bool CutsceneSkipPressed
    {
        get
        {
            if (LoadingScreenHUD.BlocksGameplayInput)
                return false;
            EnsureInitialized();
            return gameplay?.FindAction("CutsceneSkip")?.WasPressedThisFrame() == true;
        }
    }
    public static bool UnlockCursorPressed => Pressed("UnlockCursor");
    public static bool RestartPressed => Pressed("Restart");
    public static bool DebugHeatDownHeld => Debug.isDebugBuild && Held("DebugHeatDown");
    public static bool DebugHeatUpHeld => Debug.isDebugBuild && Held("DebugHeatUp");
    public static bool DebugCombatPressed => Debug.isDebugBuild && Pressed("DebugCombat");
    public static bool DebugGodModePressed => Debug.isDebugBuild && Pressed("DebugGodMode");
    public static bool DebugMaxHeatPressed => Debug.isDebugBuild && Pressed("DebugMaxHeat");
    public static bool DebugSlowMotionPressed => Debug.isDebugBuild && Pressed("DebugSlowMotion");

    public static bool PausePressed
    {
        get
        {
            if (CutsceneController.IsPlaying ||
                LoadingScreenHUD.BlocksGameplayInput)
                return false;
            EnsureInitialized();
            return CurrentMap()?.FindAction("Pause")?.WasPressedThisFrame() == true;
        }
    }

    public static bool DebugHUDPressed
    {
        get
        {
            if (!Debug.isDebugBuild || LoadingScreenHUD.BlocksGameplayInput)
                return false;
            EnsureInitialized();
            return CurrentMap()?.FindAction("DebugHUD")?.WasPressedThisFrame() == true;
        }
    }

    public static bool UIClickPressed
    {
        get
        {
            if (LoadingScreenHUD.BlocksGameplayInput)
                return false;
            EnsureInitialized();
            return ui?.FindAction("Click")?.WasPressedThisFrame() == true;
        }
    }

    public static Vector2 UIPointer
    {
        get
        {
            if (LoadingScreenHUD.BlocksGameplayInput)
                return Vector2.zero;
            EnsureInitialized();
            return ui?.FindAction("Point")?.ReadValue<Vector2>() ?? Vector2.zero;
        }
    }

    public static Vector2 UINavigate => ReadUIVector2("Navigate");
    public static bool UISubmitPressed => PressedUI("Submit");
    public static bool UICancelPressed => PressedUI("Cancel");

    private static bool PressedUI(string actionName)
    {
        if (LoadingScreenHUD.BlocksGameplayInput)
            return false;
        EnsureInitialized();
        return ui?.FindAction(actionName)?.WasPressedThisFrame() == true;
    }

    private static Vector2 ReadUIVector2(string actionName)
    {
        if (LoadingScreenHUD.BlocksGameplayInput)
            return Vector2.zero;
        EnsureInitialized();
        return ui?.FindAction(actionName)?.ReadValue<Vector2>() ?? Vector2.zero;
    }

    private static bool Pressed(string actionName)
    {
        if (CutsceneController.IsPlayerControlLocked ||
            LoadingScreenHUD.BlocksGameplayInput)
            return false;
        EnsureInitialized();
        return gameplay?.FindAction(actionName)?.WasPressedThisFrame() == true;
    }

    private static bool MouseClickWillLockCursor() =>
        CameraFollow.Instance != null &&
        CameraFollow.Instance.LocksCursorOnStart &&
        Cursor.lockState != CursorLockMode.Locked && Mouse.current != null &&
        Mouse.current.leftButton.wasPressedThisFrame;

    private static bool Held(string actionName)
    {
        if (CutsceneController.IsPlayerControlLocked ||
            LoadingScreenHUD.BlocksGameplayInput)
            return false;
        EnsureInitialized();
        return gameplay?.FindAction(actionName)?.IsPressed() == true;
    }

    private static Vector2 ReadVector2(string actionName)
    {
        if (CutsceneController.IsPlayerControlLocked ||
            LoadingScreenHUD.BlocksGameplayInput)
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
