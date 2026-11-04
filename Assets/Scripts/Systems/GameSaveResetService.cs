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
                player.GetComponent<PlayerProgression>()?.ResetProgress();
                player.GetComponent<PlayerCurrencyWallet>()?.ResetProgress();
                player.GetComponent<PlayerHeat>()?.ResetForNewGame();
                player.GetComponent<PlayerHealth>()?.ResetForNewGame();
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
        string[] keys =
        {
            "ProjetoPSX.InputBindings.V1", "ProjetoPSX.Settings.V1",
            "ProjetoPSX.PlayerCurrency.v1"
        };
        foreach (string key in keys)
            PlayerPrefs.DeleteKey(key);
        if (player != null)
        {
            PlayerProgression progression = player.GetComponent<PlayerProgression>();
            progression?.DeleteStoredProgress();
        }
    }
}
