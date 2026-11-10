using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Persiste e reconstrui o unico inimigo Corpse Run do jogo.</summary>
[DisallowMultipleComponent]
public sealed class CorpseRun : MonoBehaviour
{
    internal const string SaveKey = "ProjetoPSX.CorpseRun.v1";

    [Serializable]
    private sealed class SavedCorpse
    {
        public string scenePath;
        public Vector3 position;
        public Quaternion rotation;
        public int currency;
        public string runId;
    }

    private static CorpseRun instance;

    [SerializeField] private CorpseRunEnemy currentCorpse;
    [SerializeField] private bool deathHandled;
    private Coroutine sceneLoadSpawnRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    public static CorpseRun EnsureOn(GameObject player)
    {
        if (player == null) return null;
        if (instance != null && instance.gameObject == player)
            return instance;
        if (!player.TryGetComponent(out CorpseRun system))
            system = player.AddComponent<CorpseRun>();
        instance = system;
        return system;
    }

    private void OnEnable()
    {
        instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (sceneLoadSpawnRoutine != null)
        {
            StopCoroutine(sceneLoadSpawnRoutine);
            sceneLoadSpawnRoutine = null;
        }
        if (instance == this) instance = null;
    }

    public void HandlePlayerDeath()
    {
        if (deathHandled) return;
        deathHandled = true;

        if (currentCorpse != null)
            Destroy(currentCorpse.gameObject);
        currentCorpse = null;

        PlayerCurrencyWallet wallet = GetComponent<PlayerCurrencyWallet>();
        int carried = wallet != null ? wallet.Current : 0;
        if (carried <= 0)
        {
            ClearSavedCorpse();
            if (wallet != null) wallet.TakeAll();
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        SavedCorpse saved = new()
        {
            scenePath = scene.path,
            position = transform.position,
            rotation = transform.rotation,
            currency = carried,
            runId = Guid.NewGuid().ToString("N")
        };
        Save(saved);
        if (wallet != null) wallet.TakeAll();
        // O inimigo so e criado por OnSceneLoaded, nunca pela morte.
    }

    public void ResetAfterRestart() => deathHandled = false;

    /// <summary>Apaga o registro global ao iniciar um jogo novo ou resetar o save.</summary>
    public static void ResetStoredProgress()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        if (instance == null) return;
        instance.deathHandled = false;
        if (instance.sceneLoadSpawnRoutine != null)
        {
            instance.StopCoroutine(instance.sceneLoadSpawnRoutine);
            instance.sceneLoadSpawnRoutine = null;
        }
        if (instance.currentCorpse != null)
            Destroy(instance.currentCorpse.gameObject);
        instance.currentCorpse = null;
    }

    /// <summary>Exporta o registro do Corpse Run para incluirlo no slot da campanha.</summary>
    public static string ExportSaveData() => PlayerPrefs.GetString(SaveKey, string.Empty);

    /// <summary>Troca o registro global pelo snapshot pertencente a campanha carregada.</summary>
    public static void RestoreSaveData(string json)
    {
        ResetStoredProgress();
        if (!string.IsNullOrWhiteSpace(json))
            PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    internal void NotifyCorpseDefeated(CorpseRunEnemy corpse)
    {
        if (corpse == null) return;
        SavedCorpse saved = Load();
        if (saved == null || saved.currency <= 0 ||
            saved.runId != corpse.RunId)
            return;

        PlayerCurrencyWallet wallet = GetComponent<PlayerCurrencyWallet>();
        if (wallet == null)
            wallet = PlayerCurrencyWallet.FindOrCreate();
        if (wallet == null) return;

        wallet.Add(saved.currency);
        ClearSavedCorpse();
        if (currentCorpse == corpse) currentCorpse = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode _)
    {
        SavedCorpse saved = Load();
        if (saved == null || string.IsNullOrEmpty(saved.scenePath) ||
            !string.Equals(saved.scenePath, scene.path,
                StringComparison.OrdinalIgnoreCase))
            return;

        if (sceneLoadSpawnRoutine != null)
            StopCoroutine(sceneLoadSpawnRoutine);
        sceneLoadSpawnRoutine = StartCoroutine(SpawnAfterSceneLoad(scene.path));
    }

    private IEnumerator SpawnAfterSceneLoad(string loadedScenePath)
    {
        // GameSceneLoader coloca o Player no ponto da fase no frame seguinte.
        yield return null;
        sceneLoadSpawnRoutine = null;

        if (SceneManager.GetActiveScene().path != loadedScenePath)
            yield break;

        SavedCorpse saved = Load();
        if (saved == null || saved.scenePath != loadedScenePath ||
            saved.currency <= 0)
            yield break;
        if (currentCorpse != null)
        {
            if (currentCorpse.RunId == saved.runId) yield break;
            Destroy(currentCorpse.gameObject);
            currentCorpse = null;
        }

        CorpseRunSettings settings = CorpseRunSettings.Load();
        if (settings == null || settings.corpseEnemyPrefab == null)
        {
            Debug.LogError("CorpseRun: configure Corpse Enemy Prefab em Resources/CorpseRun/CorpseRunSettings.");
            yield break;
        }

        GameObject spawned = Instantiate(settings.corpseEnemyPrefab,
            saved.position, saved.rotation);
        currentCorpse = spawned.GetComponent<CorpseRunEnemy>();
        if (currentCorpse == null)
            currentCorpse = spawned.AddComponent<CorpseRunEnemy>();
        currentCorpse.Initialize(saved.currency, saved.runId, this);
    }

    private static SavedCorpse Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonUtility.FromJson<SavedCorpse>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("CorpseRun: registro salvo invalido; removendo. " + exception.Message);
            ClearSavedCorpse();
            return null;
        }
    }

    private static void Save(SavedCorpse saved)
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saved));
        PlayerPrefs.Save();
    }

    private static void ClearSavedCorpse()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}
