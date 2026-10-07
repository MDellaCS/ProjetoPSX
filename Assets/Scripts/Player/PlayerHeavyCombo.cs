using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHeavyCombo : MonoBehaviour
{
    [Header("Heavy Combo 1")]
    public PlayerAttackData heavyAttack1;
    public PlayerAttackData heavyAttack2;
    public PlayerAttackData heavyAttack3;
    public PlayerAttackData heavyAttack4;

    [Header("Heavy Combo 2 - Pause after H1")]
    public PlayerAttackData heavyPauseAttack2;
    public PlayerAttackData heavyPauseAttack3;

    [Header("Heavy Combo 3 - Multiple Pauses")]
    public PlayerAttackData heavyPauseAttack3Alt;
    public PlayerAttackData heavyPauseAttack4;

    [Header("Combo Timing")]
    public float pauseComboDelay = 0.30f;
    public float comboResetTime = 0.70f;

    private enum HeavyComboRoute
    {
        None,
        Normal,
        PauseAfterH1,
        MultiplePauses
    }

    private PlayerCombat playerCombat;
    private PlayerState playerState;
    private PlayerHeat playerHeat;

    private HeavyComboRoute comboRoute =
        HeavyComboRoute.None;

    private int comboStep = 0;

    private bool waitingForCombo = false;
    private bool heavyAttackBuffered = false;

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

        // Botão direito.
        if (!Mouse.current.rightButton.wasPressedThisFrame)
            return;

        // Existe um Heavy Combo esperando continuação.
        if (waitingForCombo)
        {
            ContinueComboFromWait();
            return;
        }

        // Estamos executando algum ataque.
        if (playerCombat.IsAttacking)
        {
            heavyAttackBuffered = true;
            return;
        }

        // Podemos começar um novo Heavy Combo.
        if (playerState.IsNormal())
        {
            StartCombo();
        }
    }

    void StartCombo()
    {
        comboStep = 1;

        comboRoute =
            HeavyComboRoute.None;

        waitingForCombo = false;

        heavyAttackBuffered = false;

        comboWaitTimer = 0f;

        ExecuteAttack(heavyAttack1);
    }

    void ExecuteAttack(
        PlayerAttackData attack
    )
    {
        heavyAttackBuffered = false;

        waitingForCombo = false;

        // Primeiro calculamos o poder do golpe usando o Heat atual
        int finalDamage = playerHeat.CalculateHeavyDamage(attack.damage);
        float finalRadius = playerHeat.CalculateHeavyArea(attack.attackRadius);

        // Depois calculamos quanto Heat este Heavy gera
        int generatedHeat = playerHeat.CalculateHeavyHeatGeneration(attack.heatGenerated);

        // Agora o Heavy gera seu Heat
        playerHeat.AddHeat(generatedHeat);

        // Finalmente executamos o golpe com os valores calculados
        playerCombat.StartAttack(
            attack,
            OnAttackFinished,
            null,
            finalDamage,
            finalRadius
        );
    }

    void OnAttackFinished()
    {
        // H4 sempre encerra o combo.
        if (comboStep >= 4)
        {
            EndCombo();
            return;
        }

        // O jogador já clicou durante o ataque.
        if (heavyAttackBuffered)
        {
            ContinueBufferedCombo();
            return;
        }

        // Entramos na janela de continuação.
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

        // ==================================
        // DEPOIS DE H1
        // ==================================

        if (comboStep == 1)
        {
            if (playerPaused)
            {
                comboRoute =
                    HeavyComboRoute.PauseAfterH1;

                Debug.Log(
                    "Heavy: Pause After H1"
                );
            }
            else
            {
                comboRoute =
                    HeavyComboRoute.Normal;

                Debug.Log(
                    "Heavy: Normal Combo"
                );
            }

            ContinueCurrentRoute();
            return;
        }

        // ==================================
        // DEPOIS DE H2'
        // ==================================

        if (comboStep == 2 &&
            comboRoute ==
            HeavyComboRoute.PauseAfterH1)
        {
            if (playerPaused)
            {
                comboRoute =
                    HeavyComboRoute.MultiplePauses;

                Debug.Log(
                    "Heavy: Multiple Pauses"
                );
            }

            ContinueCurrentRoute();
            return;
        }

        // ==================================
        // DEPOIS DE H3''
        // ==================================

        if (comboStep == 3 &&
            comboRoute ==
            HeavyComboRoute.MultiplePauses)
        {
            // H4' exige a terceira pausa.
            if (playerPaused)
            {
                ExecuteNextAttack();
            }
            else
            {
                // Sem a terceira pausa,
                // terminamos com H4 normal.
                comboRoute =
                    HeavyComboRoute.PauseAfterH1;

                ExecuteNextAttack();
            }

            return;
        }

        ContinueCurrentRoute();
    }

    void ContinueBufferedCombo()
    {
        // Um clique colocado no buffer significa
        // que não houve uma pausa deliberada.

        if (comboStep == 1)
        {
            comboRoute =
                HeavyComboRoute.Normal;
        }

        // Se estávamos na rota H1 → pausa → H2'
        // e atacamos rapidamente depois de H2',
        // seguimos para H3'.
        if (comboStep == 2 &&
            comboRoute ==
            HeavyComboRoute.PauseAfterH1)
        {
            // Mantemos PauseAfterH1.
        }

        // Se H3'' recebeu um clique no buffer,
        // não houve a última pausa.
        if (comboStep == 3 &&
            comboRoute ==
            HeavyComboRoute.MultiplePauses)
        {
            comboRoute =
                HeavyComboRoute.PauseAfterH1;
        }

        ContinueCurrentRoute();
    }

    void ContinueCurrentRoute()
    {
        ExecuteNextAttack();
    }

    void ExecuteNextAttack()
    {
        // ==================================
        // SEGUNDO GOLPE
        // ==================================

        if (comboStep == 1)
        {
            comboStep = 2;

            if (comboRoute ==
                HeavyComboRoute.PauseAfterH1)
            {
                ExecuteAttack(
                    heavyPauseAttack2
                );
            }
            else
            {
                ExecuteAttack(
                    heavyAttack2
                );
            }

            return;
        }

        // ==================================
        // TERCEIRO GOLPE
        // ==================================

        if (comboStep == 2)
        {
            comboStep = 3;

            if (comboRoute ==
                HeavyComboRoute.MultiplePauses)
            {
                ExecuteAttack(
                    heavyPauseAttack3Alt
                );
            }
            else if (comboRoute ==
                     HeavyComboRoute.PauseAfterH1)
            {
                ExecuteAttack(
                    heavyPauseAttack3
                );
            }
            else
            {
                ExecuteAttack(
                    heavyAttack3
                );
            }

            return;
        }

        // ==================================
        // QUARTO GOLPE
        // ==================================

        if (comboStep == 3)
        {
            comboStep = 4;

            if (comboRoute ==
                HeavyComboRoute.MultiplePauses)
            {
                ExecuteAttack(
                    heavyPauseAttack4
                );
            }
            else
            {
                ExecuteAttack(
                    heavyAttack4
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
            HeavyComboRoute.None;

        waitingForCombo = false;

        heavyAttackBuffered = false;

        comboWaitTimer = 0f;
    }

    public void CancelCombo()
    {
        EndCombo();
    }
}