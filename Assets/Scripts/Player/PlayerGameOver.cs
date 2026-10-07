using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;

public class PlayerGameOver : MonoBehaviour
{
    [Header("Interface")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text deathMessageText;

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

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
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

        if (gameOverVisible && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartGame();
        }
    }

    void ShowGameOver()
    {
        gameOverVisible = true;

        if (deathMessageText != null && playerHeat != null)
        {
            if (playerHeat.CurrentHeat <= playerHeat.MinHeat)
            {
                deathMessageText.text =
                    "VOCE SUCUMBIU AO FRIO EXTREMO";
            }
            else if (
                playerHeat.CurrentSuperheat >=
                playerHeat.MaxSuperheat
            )
            {
                deathMessageText.text =
                    "VOCE FOI CONSUMIDO PELO CALOR";
            }
            else
            {
                deathMessageText.text = "VOCE MORREU";
            }
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    void RestartGame()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}