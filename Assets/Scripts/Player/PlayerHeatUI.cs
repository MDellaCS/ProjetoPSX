using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHeatUI : MonoBehaviour
{
    [Header("References")]
    public PlayerHeat playerHeat;

    [Header("Heat")]
    public RectTransform coldFill;
    public RectTransform hotFill;
    public TMP_Text heatText;

    [Header("Superheat")]
    public Image superheatFill;
    public TMP_Text superheatText;

    void Update()
    {
        if (playerHeat == null)
            return;

        UpdateHeat();
        UpdateSuperheat();
    }

    void UpdateHeat()
    {
        int heat =
            playerHeat.CurrentHeat;

        float coldAmount = 0f;
        float hotAmount = 0f;

        if (heat < 0)
        {
            coldAmount =
                (float)heat /
                playerHeat.MinHeat;
        }
        else if (heat > 0)
        {
            hotAmount =
                (float)heat /
                playerHeat.MaxHeat;
        }

        coldAmount =
            Mathf.Clamp01(coldAmount);

        hotAmount =
            Mathf.Clamp01(hotAmount);

        coldFill.localScale =
            new Vector3(
                coldAmount,
                1f,
                1f
            );

        hotFill.localScale =
            new Vector3(
                hotAmount,
                1f,
                1f
            );

        string sign = "";

        if (heat > 0)
        {
            sign = "+";
        }

        heatText.text =
            "HEAT: " +
            sign +
            heat +
            " / " +
            playerHeat.MaxHeat;
    }

    void UpdateSuperheat()
    {
        int superheat =
            playerHeat.CurrentSuperheat;

        float amount =
            (float)superheat /
            playerHeat.MaxSuperheat;

        superheatFill.fillAmount =
            Mathf.Clamp01(amount);

        superheatText.text =
            "SUPERHEAT: " +
            superheat +
            " / " +
            playerHeat.MaxSuperheat;
    }
}