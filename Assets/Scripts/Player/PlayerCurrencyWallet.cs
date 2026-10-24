using System;
using UnityEngine;

/// <summary>Carteira persistente do jogador para recursos coletados no mundo.</summary>
[DisallowMultipleComponent]
public sealed class PlayerCurrencyWallet : MonoBehaviour
{
    private const string SaveKey = "ProjetoPSX.PlayerCurrency.v1";

    [Header("Coleta de Gore")]
    [Tooltip("Distancia em que um fragmento de gore com valor e coletado pelo jogador.")]
    [Min(.01f)] [SerializeField] private float collectRadius = .65f;
    [Tooltip("Alcance maximo em que fragmentos de gore com currency comecam a ser sugados pelo jogador.")]
    [Min(.01f)] [SerializeField] private float attractionRadius = 8f;
    [Tooltip("Offset vertical do ponto central do Player usado para atrair e coletar currency.")]
    [SerializeField] private float playerCurrencyCenterHeightOffset = .8f;
    [Tooltip("Tempo minimo apos nascer antes de um fragmento de gore poder ser atraido pelo jogador.")]
    [Min(0f)] [SerializeField] private float attractionDelay = .3f;
    [Tooltip("Aceleracao usada para puxar fragmentos de gore coletaveis ao jogador.")]
    [Min(0f)] [SerializeField] private float attractionAcceleration = 32f;
    [Tooltip("Velocidade maxima dos fragmentos de gore enquanto sao atraidos ao jogador.")]
    [Min(0f)] [SerializeField] private float maximumAttractionSpeed = 14f;

    /// <summary>Disparado quando a quantidade de currency muda.</summary>
    public event Action<int> Changed;

    public int Current { get; private set; }
    public float CollectRadius => collectRadius;
    public float AttractionRadius => attractionRadius;
    public float PlayerCurrencyCenterHeightOffset => playerCurrencyCenterHeightOffset;
    public float AttractionDelay => attractionDelay;
    public float AttractionAcceleration => attractionAcceleration;
    public float MaximumAttractionSpeed => maximumAttractionSpeed;

    private void Awake()
    {
        Current = Mathf.Max(0, PlayerPrefs.GetInt(SaveKey, 0));
    }

    /// <summary>Encontra ou instala a carteira no Player atual.</summary>
    public static PlayerCurrencyWallet FindOrCreate()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return null;

        PlayerCurrencyWallet wallet = player.GetComponent<PlayerCurrencyWallet>();
        return wallet != null ? wallet : player.AddComponent<PlayerCurrencyWallet>();
    }

    /// <summary>Adiciona currency coletada e a persiste entre cenas e sessoes.</summary>
    public void Add(int amount)
    {
        if (amount <= 0)
            return;

        Current = Mathf.Max(0, Current + amount);
        PlayerPrefs.SetInt(SaveKey, Current);
        PlayerPrefs.Save();
        Changed?.Invoke(Current);
    }

    /// <summary>Tenta gastar currency. Retorna falso se o saldo nao for suficiente.</summary>
    public bool TrySpend(int amount)
    {
        if (amount < 0 || amount > Current)
            return false;

        Current -= amount;
        PlayerPrefs.SetInt(SaveKey, Current);
        PlayerPrefs.Save();
        Changed?.Invoke(Current);
        return true;
    }
}
