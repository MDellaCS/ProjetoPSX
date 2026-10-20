using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

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
    private float deathTimer;

    void Awake()
    {
        playerState = GetComponent<PlayerState>();
        playerHeat = GetComponent<PlayerHeat>();

    }

    void Update()
    {
        if (!playerState.IsDead())
            return;

        if (!deathDetected)
        {
            deathDetected = true;
            deathTimer = 0f;
        }

        if (!gameOverVisible)
        {
            deathTimer += Time.deltaTime;

            if (deathTimer >= showDelay)
                ShowGameOver();
        }

        if (!GamePauseController.IsPaused && gameOverVisible &&
            GameInput.RestartPressed)
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
        if (playerHeat == null)
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

    void RestartGame()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}
