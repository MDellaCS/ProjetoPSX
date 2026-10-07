using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLightCombo : MonoBehaviour
{
    [Header("Light Combo 1")]
    public PlayerAttackData lightAttack1;
    public PlayerAttackData lightAttack2;
    public PlayerAttackData lightAttack3;

    [Header("Light Combo 2 - Pause after L1")]
    public PlayerAttackData lightPauseAttack2;
    public PlayerAttackData lightPauseAttack3;

    [Header("Light Combo 3 - Pause after L2")]
    public PlayerAttackData lightPauseAttack3Alt;

    [Header("Combo Timing")]
    public float pauseComboDelay = 0.25f;
    public float comboResetTime = 0.55f;

    private enum LightComboRoute
    {
        None,
        Normal,
        PauseAfterL1,
        PauseAfterL2
    }

    private PlayerCombat playerCombat;
    private PlayerState playerState;
    private PlayerHeat playerHeat;
    private PlayerAttackData currentLightAttack;

    private LightComboRoute comboRoute = LightComboRoute.None;

    private int comboStep = 0;

    private bool waitingForCombo = false;

    private bool lightAttackBuffered = false;

    private float comboWaitTimer = 0f;

    void Start()
    {
        playerCombat = GetComponent<PlayerCombat>();
        playerState = GetComponent<PlayerState>();
        playerHeat = GetComponent<PlayerHeat>();
    }

    void Update()
    {
        UpdateComboWait();

        ReadInput();
    }

    void ReadInput()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        // Estamos esperando a continuação
        // de um combo.
        if (waitingForCombo)
        {
            ContinueComboFromWait();
            return;
        }

        // Estamos no meio de um golpe.
        if (playerCombat.IsAttacking)
        {
            lightAttackBuffered = true;
            return;
        }

        // Nenhum combo acontecendo.
        if (playerState.IsNormal())
        {
            StartCombo();
        }
    }

    void StartCombo()
    {
        comboStep = 1;

        comboRoute =
            LightComboRoute.None;

        waitingForCombo = false;

        lightAttackBuffered = false;

        comboWaitTimer = 0f;

        ExecuteAttack(lightAttack1);
    }

    void ExecuteAttack(PlayerAttackData attack)
    {
        lightAttackBuffered = false;
        waitingForCombo = false;

        currentLightAttack = attack;

        playerCombat.StartAttack(
            attack,
            OnAttackFinished,
            OnLightAttackHit,
            attack.damage,
            attack.attackRadius
        );
    }

    void OnLightAttackHit()
    {
        if (currentLightAttack == null)
            return;

        playerHeat.CoolFromLight(
            currentLightAttack.heatRemovedOnHit,
            currentLightAttack.superheatRemovedOnHit
        );
    }

    void OnAttackFinished()
    {
        // O terceiro golpe sempre encerra.
        if (comboStep >= 3)
        {
            EndCombo();
            return;
        }

        // O jogador já clicou enquanto
        // o golpe estava acontecendo.
        if (lightAttackBuffered)
        {
            ContinueBufferedCombo();
            return;
        }

        // O golpe acabou, mas mantemos
        // uma janela para continuar.
        waitingForCombo = true;

        comboWaitTimer = 0f;
    }

    void UpdateComboWait()
    {
        if (!waitingForCombo)
            return;

        comboWaitTimer += Time.deltaTime;

        if (comboWaitTimer >= comboResetTime)
        {
            EndCombo();
        }
    }

    void ContinueComboFromWait()
    {
        bool playerPaused =
            comboWaitTimer >= pauseComboDelay;

        // Estamos depois de L1.
        if (comboStep == 1)
        {
            if (playerPaused)
            {
                comboRoute =
                    LightComboRoute.PauseAfterL1;

                Debug.Log(
                    "Rota: Pause After L1"
                );
            }
            else
            {
                comboRoute =
                    LightComboRoute.Normal;

                Debug.Log(
                    "Rota: Normal Combo"
                );
            }

            ContinueCurrentRoute();
            return;
        }

        // Estamos depois de L2 normal.
        if (comboStep == 2 &&
            comboRoute == LightComboRoute.Normal)
        {
            if (playerPaused)
            {
                comboRoute =
                    LightComboRoute.PauseAfterL2;

                Debug.Log(
                    "Rota: Pause After L2"
                );
            }

            ContinueCurrentRoute();
            return;
        }

        // Já estamos na rota PauseAfterL1.
        ContinueCurrentRoute();
    }

    void ContinueBufferedCombo()
    {
        // Buffer significa que não houve
        // uma pausa deliberada.
        if (comboStep == 1)
        {
            comboRoute =
                LightComboRoute.Normal;

            Debug.Log(
                "Rota: Normal Combo"
            );
        }

        ContinueCurrentRoute();
    }

    void ContinueCurrentRoute()
    {
        waitingForCombo = false;

        comboWaitTimer = 0f;

        // SEGUNDO GOLPE
        if (comboStep == 1)
        {
            comboStep = 2;

            if (comboRoute ==
                LightComboRoute.PauseAfterL1)
            {
                ExecuteAttack(
                    lightPauseAttack2
                );
            }
            else
            {
                ExecuteAttack(
                    lightAttack2
                );
            }

            return;
        }

        // TERCEIRO GOLPE
        if (comboStep == 2)
        {
            comboStep = 3;

            if (comboRoute ==
                LightComboRoute.PauseAfterL1)
            {
                ExecuteAttack(
                    lightPauseAttack3
                );
            }
            else if (comboRoute ==
                     LightComboRoute.PauseAfterL2)
            {
                ExecuteAttack(
                    lightPauseAttack3Alt
                );
            }
            else
            {
                ExecuteAttack(
                    lightAttack3
                );
            }

            return;
        }

        EndCombo();
    }

    void EndCombo()
    {
        comboStep = 0;

        comboRoute =
            LightComboRoute.None;

        waitingForCombo = false;

        lightAttackBuffered = false;

        comboWaitTimer = 0f;
    }

    public void CancelCombo()
    {
        EndCombo();
    }
}