using UnityEngine;

/// <summary>Apaga os dados persistentes do jogo sem afetar as preferencias da Unity.</summary>
public static class GameSaveResetService
{
    private static bool isResetting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => isResetting = false;

    public static void ResetAllSavesAndReload(GameObject player)
    {
        if (isResetting)
            return;

        GameSceneLoader loader = Object.FindAnyObjectByType<GameSceneLoader>();
        LoadingScreenHUD loadingScreen = LoadingScreenHUD.Instance;
        if ((loader != null && loader.IsLoading) ||
            (loader == null && (loadingScreen == null || loadingScreen.IsLoading)))
            return;

        isResetting = true;
        try
        {
            DeleteProjectSaveKeys(player);
            GameInputBindings.ResetToDefaults();
            GameSettingsService.ResetToDefaults();

            if (player != null)
            {
                if (player.TryGetComponent(out PlayerProgression progression))
                    progression.ResetProgress();
                if (player.TryGetComponent(out PlayerCurrencyWallet wallet))
                    wallet.ResetProgress();
                if (player.TryGetComponent(out PlayerHeat heat))
                    heat.ResetForNewGame();
                if (player.TryGetComponent(out PlayerHealth health))
                    health.ResetForNewGame();
            }

            PlayerPrefs.Save();
            if (loader != null)
                loader.ReloadCurrentScene();
            else
                loadingScreen.LoadSceneWithoutSceneLoader(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        }
        finally
        {
            isResetting = false;
        }
    }

    private static void DeleteProjectSaveKeys(GameObject player)
    {
        GameSaveService.DeleteAllSaves();
        string[] keys =
        {
            "ProjetoPSX.InputBindings.V1", "ProjetoPSX.Settings.V1",
            "ProjetoPSX.PlayerCurrency.v1", "ProjetoPSX.KeyItems.V1",
            CorpseRun.SaveKey
        };
        foreach (string key in keys)
            PlayerPrefs.DeleteKey(key);
        CorpseRun.ResetStoredProgress();
        if (player != null)
        {
            if (player.TryGetComponent(out PlayerProgression progression))
                progression.DeleteStoredProgress();
        }
    }
}
