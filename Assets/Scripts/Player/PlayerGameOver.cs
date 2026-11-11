using UnityEngine;

public class PlayerGameOver : MonoBehaviour
{
    [Header("Interface")]
    [Tooltip("Opcional. Se vazio, encontra o GameOverHUD global da cena.")]
    [SerializeField] private GameOverHUD gameOverHUD;

    [Header("Timing")]
    [Tooltip("Tempo em segundos reais entre a morte e a exibicao da tela de Game Over.")]
    [SerializeField] private float showDelay = 1.5f;

    private PlayerState playerState;
    private PlayerHeat playerHeat;

    private bool deathDetected;
    private bool gameOverVisible;
    private bool restartRequested;
    private float deathTimer;
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
            deathTimer = 0f;
            if (corpseRun == null) corpseRun = CorpseRun.EnsureOn(gameObject);
            if (corpseRun != null) corpseRun.HandlePlayerDeath();
        }

        if (!gameOverVisible)
        {
            deathTimer += Time.unscaledDeltaTime;

            if (deathTimer >= showDelay)
                ShowGameOver();
        }

        if (!restartRequested && gameOverVisible && GameInput.RestartPressed)
        {
            RestartGame();
        }
    }

    void ShowGameOver()
    {
        gameOverVisible = true;
        ResolveGameOverHUD();

        if (gameOverHUD != null)
            gameOverHUD.Show(GetDeathMessage());
    }

    private string GetDeathMessage()
    {
        if (playerHeat == null || !playerHeat.IsHeatModeActive)
            return "VOCE MORREU";

        if (playerHeat.CurrentHeat <= playerHeat.MinHeat)
            return "VOCE SUCUMBIU AO FRIO EXTREMO";

        return "VOCE MORREU";
    }

    private void ResolveGameOverHUD()
    {
        if (gameOverHUD == null)
            gameOverHUD = FindAnyObjectByType<GameOverHUD>();
    }

    private void RestartGame()
    {
        restartRequested = true;

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
        if (gameOverHUD != null)
            gameOverHUD.Hide();
        GameTimeScaleEffects.ResetForGameRestart();

        deathDetected = false;
        gameOverVisible = false;
        deathTimer = 0f;
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
        deathDetected = false;
        gameOverVisible = false;
        restartRequested = false;
        deathTimer = 0f;
        ResolveGameOverHUD();
        if (gameOverHUD != null) gameOverHUD.Hide();
    }
}
