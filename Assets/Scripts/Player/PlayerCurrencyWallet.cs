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
    [Tooltip("Tempo dentro do alcance de atracao para a aceleracao percorrer toda a curva configurada abaixo.")]
    [Min(.01f)] [SerializeField] private float attractionAccelerationRampDuration = 1.2f;
    [Tooltip("Multiplicador da aceleracao conforme o tempo que a currency esta sendo atraida. Aumente a curva para os fragmentos ganharem velocidade progressivamente.")]
    [SerializeField] private AnimationCurve attractionAccelerationCurve =
        new AnimationCurve(new Keyframe(0f, .25f), new Keyframe(1f, 1f));
    [Tooltip("Velocidade maxima dos fragmentos de gore enquanto sao atraidos ao jogador.")]
    [Min(0f)] [SerializeField] private float maximumAttractionSpeed = 14f;

    /// <summary>Disparado quando a quantidade de currency muda.</summary>
    public event Action<int> Changed;

    public int Current { get; private set; }
    private bool hasUnsavedChanges;
    private float nextAutomaticSaveAt;
    public float CollectRadius => collectRadius;
    public float AttractionRadius => attractionRadius;
    public float PlayerCurrencyCenterHeightOffset => playerCurrencyCenterHeightOffset;
    public float AttractionDelay => attractionDelay;
    public float AttractionAcceleration => attractionAcceleration;
    public float MaximumAttractionSpeed => maximumAttractionSpeed;

    public float EvaluateAttractionAcceleration(float elapsedAttractionTime)
    {
        float normalizedTime = Mathf.Clamp01(elapsedAttractionTime /
            Mathf.Max(.01f, attractionAccelerationRampDuration));
        float curveMultiplier = attractionAccelerationCurve == null ||
            attractionAccelerationCurve.length == 0
            ? normalizedTime
            : Mathf.Max(0f, attractionAccelerationCurve.Evaluate(normalizedTime));
        return attractionAcceleration * curveMultiplier;
    }

    private void Awake()
    {
        Current = Mathf.Max(0, PlayerPrefs.GetInt(SaveKey, 0));
    }

    private void Update()
    {
        if (!hasUnsavedChanges || Time.unscaledTime < nextAutomaticSaveAt)
            return;
        SavePendingCurrency();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SavePendingCurrency();
    }

    private void OnApplicationQuit() => SavePendingCurrency();

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
        MarkCurrencyDirty();
        Changed?.Invoke(Current);
    }

    /// <summary>Tenta gastar currency. Retorna falso se o saldo nao for suficiente.</summary>
    public bool TrySpend(int amount)
    {
        if (amount < 0 || amount > Current)
            return false;

        Current -= amount;
        PlayerPrefs.SetInt(SaveKey, Current);
        hasUnsavedChanges = false;
        PlayerPrefs.Save();
        Changed?.Invoke(Current);
        return true;
    }

    /// <summary>Limpa o saldo em memoria apos o servico apagar os dados persistentes.</summary>
    public void ResetProgress()
    {
        Current = 0;
        PlayerPrefs.DeleteKey(SaveKey);
        hasUnsavedChanges = false;
        Changed?.Invoke(Current);
    }

    private void MarkCurrencyDirty()
    {
        hasUnsavedChanges = true;
        if (nextAutomaticSaveAt <= Time.unscaledTime)
            nextAutomaticSaveAt = Time.unscaledTime + 1f;
    }

    private void SavePendingCurrency()
    {
        if (!hasUnsavedChanges)
            return;
        PlayerPrefs.Save();
        hasUnsavedChanges = false;
    }
}
