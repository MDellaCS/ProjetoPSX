using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerGameOver : MonoBehaviour
{
    [Header("Interface")]
    [Tooltip("Opcional. Se vazio, encontra o GameOverHUD global da cena.")]
    [SerializeField] private GameOverHUD gameOverHUD;

    [Header("Timing")]
    [SerializeField] private float showDelay = 1.5f;

    private PlayerState playerState;
    private PlayerHeat playerHeat;

    private bool deathDetected;
    private bool gameOverVisible;
    private bool restartRequested;
    private float deathTimer;

    void Awake()
    {
        playerState = GetComponent<PlayerState>();
        playerHeat = GetComponent<PlayerHeat>();
    }

    void Update()
    {
        if (playerState == null || !playerState.IsDead())
            return;

        if (!deathDetected)
        {
            deathDetected = true;
            deathTimer = 0f;
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

        gameOverHUD?.Show(GetDeathMessage());
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
        GetComponent<PlayerHealth>()?.ResetForNewGame();
        playerHeat?.ResetForNewGame();
        GetComponent<PlayerMovement>()?.ResetForNewGame();
        gameOverHUD?.Hide();
        GameTimeScaleEffects.ResetForGameRestart();

        deathDetected = false;
        gameOverVisible = false;
        deathTimer = 0f;

        GameSceneLoader loader = FindAnyObjectByType<GameSceneLoader>();
        if (loader != null)
        {
            loader.ReloadCurrentScene();
            restartRequested = false;
            return;
        }

        // O nome continua funcionando para cenas cujo buildIndex seja -1.
        restartRequested = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
