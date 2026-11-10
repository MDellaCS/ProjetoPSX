using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Armazena cinco campanhas manuais e um checkpoint automatico isolado.</summary>
public static class GameSaveService
{
    public const int ManualSlotCount = 5;
    public const int AutomaticSlotIndex = -1;
    private const string ManualKeyPrefix = "ProjetoPSX.SaveGame.Manual.v1.";
    private const string CheckpointKey = "ProjetoPSX.SaveGame.Checkpoint.v1";
    private const string ActiveSlotKey = "ProjetoPSX.SaveGame.ActiveSlot.v1";

    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public int slotIndex = AutomaticSlotIndex;
        public string scenePath;
        public string spawnId;
        public string location;
        public string savedAtUtc;
        public double playedSeconds;
        public int currentHealth;
        public float currentHeat;
        public float currentSuperheat;
        public int currency;
        public string[] keyItemIds = Array.Empty<string>();
        public PlayerUpgradeSaveEntry[] upgrades = Array.Empty<PlayerUpgradeSaveEntry>();
        public string corpseRunJson;
    }

    public sealed class SlotInfo
    {
        public readonly int slotIndex;
        public readonly bool isAutomatic;
        public readonly SaveData data;
        public bool IsOccupied => data != null;
        public string DisplayName => isAutomatic ? "Checkpoint automatico" : $"Slot {slotIndex + 1}";

        internal SlotInfo(int slot, bool automatic, SaveData save)
        {
            slotIndex = slot;
            isAutomatic = automatic;
            data = save;
        }

        public string Summary
        {
            get
            {
                if (data == null) return "Vazio";
                string area = string.IsNullOrWhiteSpace(data.location)
                    ? SceneName(data.scenePath) : data.location;
                string date = DateTime.TryParse(data.savedAtUtc, null,
                    DateTimeStyles.RoundtripKind, out DateTime savedAt)
                    ? savedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "Data indisponivel";
                return $"{area}  |  {FormatPlayTime(data.playedSeconds)}  |  {date}";
            }
        }
    }

    private static int activeManualSlot = AutomaticSlotIndex;
    private static double sessionPlayedSeconds;
    private static double savedPlayTimeBase;

    public static int ActiveManualSlot => activeManualSlot;
    public static bool HasAnySave
    {
        get
        {
            if (ReadCheckpoint() != null) return true;
            for (int i = 0; i < ManualSlotCount; i++)
                if (ReadManual(i) != null) return true;
            return false;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        int storedSlot = PlayerPrefs.GetInt(ActiveSlotKey, AutomaticSlotIndex);
        activeManualSlot = IsManualSlotValid(storedSlot)
            ? storedSlot : AutomaticSlotIndex;
        sessionPlayedSeconds = 0d;
        SaveData activeSave = activeManualSlot >= 0 ? ReadManual(activeManualSlot) : null;
        savedPlayTimeBase = activeSave != null ? Math.Max(0d, activeSave.playedSeconds) : 0d;
        SaveData checkpoint = ReadCheckpoint();
        if (checkpoint != null && checkpoint.slotIndex == activeManualSlot)
            savedPlayTimeBase = Math.Max(savedPlayTimeBase, checkpoint.playedSeconds);
    }

    public static void Tick(float deltaTime)
    {
        if (deltaTime > 0f && !GamePauseController.IsPaused)
            sessionPlayedSeconds += deltaTime;
    }

    public static SlotInfo GetManualSlot(int index)
    {
        ValidateManualSlot(index);
        return new SlotInfo(index, false, ReadManual(index));
    }

    public static SlotInfo GetCheckpointSlot() =>
        new(AutomaticSlotIndex, true, ReadCheckpoint());

    /// <summary>Inicia uma campanha limpa sem escolher o slot manual antes do primeiro save.</summary>
    public static bool BeginNewCampaign(string scenePath, string spawnId,
        GameObject player, out string error)
    {
        GameSceneLoader loader = UnityEngine.Object.FindAnyObjectByType<GameSceneLoader>();
        if (player == null || loader == null || !loader.CanLoadSavedScene(scenePath))
        {
            error = "A cena inicial ou o jogador nao esta disponivel para iniciar a campanha.";
            return false;
        }

        ResetPlayerCampaign(player);
        CollectableItem.ResetAllForNewGame();
        PlayerPrefs.DeleteKey(CheckpointKey);
        activeManualSlot = AutomaticSlotIndex;
        PlayerPrefs.DeleteKey(ActiveSlotKey);
        sessionPlayedSeconds = 0d;
        savedPlayTimeBase = 0d;
        PlayerPrefs.Save();
        error = null;
        return true;
    }

    public static bool SaveManual(int slot, string location, string spawnId,
        out string error)
    {
        if (!IsManualSlotValid(slot) ||
            (activeManualSlot >= 0 && activeManualSlot != slot))
        {
            error = "Esta campanha ja esta vinculada a outro slot manual.";
            return false;
        }

        GameObject player = FindPlayer();
        SaveData data = Capture(player, slot, SceneManager.GetActiveScene().path,
            spawnId, location);
        if (data == null)
        {
            error = "Jogador indisponivel para salvar.";
            return false;
        }
        WriteManual(slot, data);
        activeManualSlot = slot;
        PlayerPrefs.SetInt(ActiveSlotKey, slot);
        PlayerPrefs.Save();
        error = null;
        return true;
    }

    public static bool SaveCheckpoint(string location, string spawnId)
    {
        GameObject player = FindPlayer();
        if (player == null) return false;
        SaveData data = Capture(player, activeManualSlot,
            SceneManager.GetActiveScene().path, spawnId, location);
        if (data == null) return false;
        data.slotIndex = activeManualSlot;
        PlayerPrefs.SetString(CheckpointKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        return true;
    }

    public static bool LoadManual(int slot, out SaveData data, out string error)
    {
        ValidateManualSlot(slot);
        data = ReadManual(slot);
        if (data == null)
        {
            error = "Este slot esta vazio.";
            return false;
        }
        return LoadData(data, out error);
    }

    public static bool LoadCheckpoint(out SaveData data, out string error)
    {
        data = ReadCheckpoint();
        if (data == null)
        {
            error = "Nao existe checkpoint automatico.";
            return false;
        }
        return LoadData(data, out error);
    }

    public static void DeleteAllSaves()
    {
        for (int i = 0; i < ManualSlotCount; i++)
            PlayerPrefs.DeleteKey(ManualKey(i));
        PlayerPrefs.DeleteKey(CheckpointKey);
        PlayerPrefs.DeleteKey(ActiveSlotKey);
        activeManualSlot = AutomaticSlotIndex;
        sessionPlayedSeconds = 0d;
        savedPlayTimeBase = 0d;
    }

    public static string FormatPlayTime(double seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Math.Max(0d, seconds));
        return time.TotalHours >= 1d
            ? $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{time.Minutes:00}:{time.Seconds:00}";
    }

    private static bool LoadData(SaveData data, out string error)
    {
        GameSceneLoader loader = UnityEngine.Object.FindAnyObjectByType<GameSceneLoader>();
        if (loader == null || loader.IsLoading || !loader.CanLoadSavedScene(data.scenePath))
        {
            error = loader != null && loader.IsLoading
                ? "Aguarde a transicao de cena terminar."
                : "A cena deste save nao esta disponivel na build atual.";
            return false;
        }
        GameObject player = FindPlayer();
        if (player == null)
        {
            error = "Jogador indisponivel para carregar.";
            return false;
        }

        Restore(player, data);
        activeManualSlot = IsManualSlotValid(data.slotIndex)
            ? data.slotIndex : AutomaticSlotIndex;
        if (activeManualSlot >= 0)
            PlayerPrefs.SetInt(ActiveSlotKey, activeManualSlot);
        else
            PlayerPrefs.DeleteKey(ActiveSlotKey);
        savedPlayTimeBase = Math.Max(0d, data.playedSeconds);
        sessionPlayedSeconds = 0d;
        PlayerPrefs.Save();
        loader.LoadSavedScene(data.scenePath, data.spawnId);
        error = null;
        return true;
    }

    private static SaveData Capture(GameObject player, int slot, string scenePath,
        string spawnId, string location)
    {
        if (player == null) return null;
        PlayerProgression progression = PlayerProgression.GetOrAdd(player);
        progression.Initialize();
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        PlayerHeat heat = player.GetComponent<PlayerHeat>();
        PlayerCurrencyWallet wallet = player.GetComponent<PlayerCurrencyWallet>();
        return new SaveData
        {
            slotIndex = slot,
            scenePath = scenePath,
            spawnId = spawnId ?? string.Empty,
            location = string.IsNullOrWhiteSpace(location) ? SceneName(scenePath) : location.Trim(),
            savedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            playedSeconds = savedPlayTimeBase + sessionPlayedSeconds,
            currentHealth = health != null ? health.CurrentHealth : 0,
            currentHeat = heat != null ? heat.CurrentHeat : 0f,
            currentSuperheat = heat != null ? heat.CurrentSuperheat : 0f,
            currency = wallet != null ? wallet.Current : 0,
            keyItemIds = KeyItemInventory.ExportCollectedIds(),
            upgrades = progression.ExportSaveData(),
            corpseRunJson = CorpseRun.ExportSaveData()
        };
    }

    private static void Restore(GameObject player, SaveData data)
    {
        ResetPlayerCampaign(player);
        PlayerProgression progression = PlayerProgression.GetOrAdd(player);
        progression.Initialize();
        progression.RestoreSaveData(data.upgrades);
        PlayerCurrencyWallet wallet = player.GetComponent<PlayerCurrencyWallet>();
        if (wallet == null) wallet = player.AddComponent<PlayerCurrencyWallet>();
        wallet.RestoreSaveValue(data.currency);
        KeyItemInventory.RestoreCollectedIds(data.keyItemIds);
        if (player.TryGetComponent(out PlayerHeat heat))
            heat.RestoreSaveState(data.currentHeat, data.currentSuperheat);
        if (player.TryGetComponent(out PlayerHealth health))
            health.RestoreSaveHealth(data.currentHealth);
        CorpseRun.RestoreSaveData(data.corpseRunJson);
    }

    private static void ResetPlayerCampaign(GameObject player)
    {
        if (player == null) return;
        PlayerProgression progression = PlayerProgression.GetOrAdd(player);
        progression.Initialize();
        progression.ResetProgress();
        if (player.TryGetComponent(out PlayerCurrencyWallet wallet)) wallet.ResetProgress();
        if (player.TryGetComponent(out PlayerHeat heat)) heat.ResetForNewGame();
        if (player.TryGetComponent(out PlayerHealth health)) health.ResetForNewGame();
        if (player.TryGetComponent(out PlayerMovement movement)) movement.ResetForNewGame();
        if (player.TryGetComponent(out PlayerCombatStatus combatStatus)) combatStatus.ResetForNewGame();
        KeyItemInventory.ResetForNewGame();
        CorpseRun.ResetStoredProgress();
    }

    private static SaveData ReadManual(int slot) => Read(ManualKey(slot));
    private static SaveData ReadCheckpoint() => Read(CheckpointKey);

    private static SaveData Read(string key)
    {
        string json = PlayerPrefs.GetString(key, string.Empty);
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            return data != null && data.version == 1 && !string.IsNullOrWhiteSpace(data.scenePath)
                ? data : null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Save] Dados corrompidos ignorados: " + exception.Message);
            return null;
        }
    }

    private static void WriteManual(int slot, SaveData data) =>
        PlayerPrefs.SetString(ManualKey(slot), JsonUtility.ToJson(data));

    private static string ManualKey(int slot) => ManualKeyPrefix + slot;

    private static void ValidateManualSlot(int slot)
    {
        if (!IsManualSlotValid(slot)) throw new ArgumentOutOfRangeException(nameof(slot));
    }

    private static bool IsManualSlotValid(int slot) => slot >= 0 && slot < ManualSlotCount;

    private static GameObject FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player : null;
    }

    private static string SceneName(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "Local desconhecido";
        string filename = System.IO.Path.GetFileNameWithoutExtension(path);
        int separator = filename.IndexOf('.');
        return separator >= 0 && separator + 1 < filename.Length
            ? filename.Substring(separator + 1).Trim() : filename;
    }
}

[Serializable]
public sealed class PlayerUpgradeSaveEntry
{
    public string id;
    public int level;
}
