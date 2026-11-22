using System.Collections;
using UnityEngine;

public class PlayerGameOver : MonoBehaviour
{
    private PlayerState playerState;
    private PlayerHeat playerHeat;

    private bool deathDetected;
    private bool gameOverVisible;
    private bool restartRequested;
    private CorpseRun corpseRun;

    void Awake()
    {
        playerState = GetComponent<PlayerState>();
        TryGetComponent(out playerHeat);
        corpseRun = CorpseRun.EnsureOn(gameObject);
    }

    void Update()
    {
        if (playerState == null || !playerState.IsDead())
            return;

        if (!deathDetected)
        {
            deathDetected = true;
            SplashTextMessages.Request(SplashTextMessage.PlayerDied);
            if (corpseRun == null) corpseRun = CorpseRun.EnsureOn(gameObject);
            if (corpseRun != null) corpseRun.HandlePlayerDeath();
        }

        if (GamePauseController.IsPaused) return;

        gameOverVisible = SplashTextMessages.DeathPresented;

        if (!restartRequested && SplashTextMessages.DeathCompleted)
        {
            restartRequested = true;
            StartCoroutine(RestartAfterLoading());
        }
    }

    private IEnumerator RestartAfterLoading()
    {
        LoadingScreenHUD loadingScreen = LoadingScreenHUD.Instance;
        if (loadingScreen != null)
        {
            loadingScreen.Show();
            yield return loadingScreen.WaitForFadeInComplete();
        }

        RestartGame();
        if (loadingScreen != null && FindAnyObjectByType<GameSceneLoader>() == null &&
            !loadingScreen.IsLoading)
            loadingScreen.HideAfterSceneReady();
    }

    private void RestartGame()
    {
        restartRequested = true;
        SplashTextMessages.Reset();

        // O Player vive no GameRoot persistente e nao e recriado ao recarregar
        // uma fase. Limpa primeiro os estados da campanha anterior.
        if (TryGetComponent(out PlayerHealth health))
            health.ResetForNewGame();
        if (playerHeat != null)
            playerHeat.ResetForNewGame();
        if (TryGetComponent(out PlayerProgression progression))
            progression.ReapplyActiveEffects();
        if (TryGetComponent(out PlayerMovement movement))
            movement.ResetForNewGame();
        GameTimeScaleEffects.ResetForGameRestart();

        deathDetected = false;
        gameOverVisible = false;
        if (corpseRun != null) corpseRun.ResetAfterRestart();

        GameSceneLoader loader = FindAnyObjectByType<GameSceneLoader>();
        if (loader != null)
        {
            loader.ReloadCurrentScene();
            restartRequested = false;
            return;
        }

        restartRequested = false;
        LoadingScreenHUD loadingScreen = LoadingScreenHUD.Instance;
        if (loadingScreen != null)
            loadingScreen.LoadSceneWithoutSceneLoader(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
    }

    public void ResetForNewGame()
    {
        SplashTextMessages.Reset();
        deathDetected = false;
        gameOverVisible = false;
        restartRequested = false;
    }
}
