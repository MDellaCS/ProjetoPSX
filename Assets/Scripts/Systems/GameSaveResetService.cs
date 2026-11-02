using UnityEngine;

/// <summary>Apaga todos os dados persistentes do jogo e reinicia a fase atual.</summary>
public static class GameSaveResetService
{
    private static bool isResetting;

    public static void ResetAllSavesAndReload(GameObject player)
    {
        if (isResetting)
            return;

        isResetting = true;
        PlayerPrefs.DeleteAll();
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
        GameSceneLoader loader = Object.FindAnyObjectByType<GameSceneLoader>();
        if (loader != null)
            loader.ReloadCurrentScene();
        else
            LoadingScreenHUD.Instance?.LoadSceneWithoutSceneLoader(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);

        isResetting = false;
    }
}
