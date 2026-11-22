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
        public int version = 2;
        public int slotIndex = AutomaticSlotIndex;
        public string scenePath;
        public string spawnId;
        public string location;
        public string savedAtUtc;
        public double playedSeconds;
        public int currentHealth;
        public float currentHeat;
        public int currentVitalOil;
        public int currency;
        public string[] keyItemIds = Array.Empty<string>();
        public PlayerUpgradeSaveEntry[] upgrades = Array.Empty<PlayerUpgradeSaveEntry>();
        public string corpseRunJson;
        public string campaignId;
        public string[] acquiredItemIds;
        public GameWorldSaveState.Entry[] worldObjects;
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
    private static string activeCampaignId;

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
        activeCampaignId = null;
        SaveData activeSave = activeManualSlot >= 0 ? ReadManual(activeManualSlot) : null;
        savedPlayTimeBase = activeSave != null ? Math.Max(0d, activeSave.playedSeconds) : 0d;
        SaveData checkpoint = ReadCheckpoint();
        if (checkpoint != null && checkpoint.slotIndex == activeManualSlot)
            savedPlayTimeBase = Math.Max(savedPlayTimeBase, checkpoint.playedSeconds);
    }

    public static void Tick(float deltaTime)
    {
        if (deltaTime > 0f && !float.IsInfinity(deltaTime) && !GamePauseController.IsPaused)
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
        if (player == null || loader == null || !loader.isActiveAndEnabled || loader.IsLoading ||
            CutsceneController.IsPlaying || !loader.CanLoadSavedScene(scenePath))
        {
            error = "A cena inicial ou o jogador nao esta disponivel para iniciar a campanha.";
            return false;
        }

        ResetPlayerCampaign(player);
        GameWorldSaveState.ResetForNewGame();
        CollectableItem.ResetAllForNewGame();
        KeyItemInventory.NotifyRestored();
        PlayerPrefs.DeleteKey(CheckpointKey);
        PlayerPrefs.DeleteKey(CheckpointKey + ".backup");
        activeManualSlot = AutomaticSlotIndex;
        activeCampaignId = Guid.NewGuid().ToString("N");
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
        if (!CanSave(out error)) return false;
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
        SaveData checkpoint = ReadCheckpoint();
        if (checkpoint != null && checkpoint.campaignId == activeCampaignId)
        {
            checkpoint.slotIndex = slot;
            Write(CheckpointKey, checkpoint);
        }
        activeManualSlot = slot;
        PlayerPrefs.SetInt(ActiveSlotKey, slot);
        PlayerPrefs.Save();
        error = null;
        return true;
    }

    public static bool SaveCheckpoint(string location, string spawnId)
    {
        if (!CanSave(out _)) return false;
        GameObject player = FindPlayer();
        if (player == null) return false;
        SaveData data = Capture(player, activeManualSlot,
            SceneManager.GetActiveScene().path, spawnId, location);
        if (data == null) return false;
        data.slotIndex = activeManualSlot;
        Write(CheckpointKey, data);
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
        {
            PlayerPrefs.DeleteKey(ManualKey(i));
            PlayerPrefs.DeleteKey(ManualKey(i) + ".backup");
        }
        PlayerPrefs.DeleteKey(CheckpointKey);
        PlayerPrefs.DeleteKey(CheckpointKey + ".backup");
        PlayerPrefs.DeleteKey(ActiveSlotKey);
        activeManualSlot = AutomaticSlotIndex;
        sessionPlayedSeconds = 0d;
        savedPlayTimeBase = 0d;
        activeCampaignId = null;
        PlayerPrefs.Save();
    }

    public static string FormatPlayTime(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds)) seconds = 0d;
        TimeSpan time = TimeSpan.FromSeconds(Math.Clamp(seconds, 0d, 315537897599d));
        return time.TotalHours >= 1d
            ? $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{time.Minutes:00}:{time.Seconds:00}";
    }

    private static bool LoadData(SaveData data, out string error)
    {
        GameSceneLoader loader = UnityEngine.Object.FindAnyObjectByType<GameSceneLoader>();
        if (loader == null || !loader.isActiveAndEnabled || loader.IsLoading ||
            CutsceneController.IsPlaying || !loader.CanLoadSavedScene(data.scenePath))
        {
            error = loader != null && loader.IsLoading
                ? "Aguarde a transicao de cena terminar."
                : CutsceneController.IsPlaying ? "Aguarde a cutscene terminar."
                : "A cena deste save ou o carregador nao esta disponivel.";
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
        activeCampaignId = string.IsNullOrEmpty(data.campaignId)
            ? Guid.NewGuid().ToString("N") : data.campaignId;
        // Um checkpoint antigo nao pode sobrescrever uma campanha que ocupou
        // posteriormente o mesmo slot.
        if (activeManualSlot >= 0 && !string.IsNullOrEmpty(data.campaignId))
        {
            SaveData manual = ReadManual(activeManualSlot);
            if (manual != null && manual.campaignId != data.campaignId)
                activeManualSlot = AutomaticSlotIndex;
        }
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
        activeCampaignId ??= Guid.NewGuid().ToString("N");
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
            currentVitalOil = health != null ? health.CurrentVitalOil : 0,
            currency = wallet != null ? wallet.Current : 0,
            keyItemIds = KeyItemInventory.ExportCollectedIds(),
            upgrades = progression.ExportSaveData(),
            corpseRunJson = CorpseRun.ExportSaveData(),
            campaignId = activeCampaignId,
            acquiredItemIds = KeyItemInventory.ExportAcquiredIds(),
            worldObjects = GameWorldSaveState.Export()
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
        KeyItemInventory.RestoreCollectedIds(data.keyItemIds, data.acquiredItemIds, false);
        GameWorldSaveState.Restore(data.worldObjects);
        if (player.TryGetComponent(out PlayerHeat heat))
            heat.RestoreSaveState(data.currentHeat);
        if (player.TryGetComponent(out PlayerHealth health))
        {
            health.RestoreSaveHealth(data.currentHealth);
            health.RestoreSaveVitalOil(data.version < 2 ? data.currentHealth : data.currentVitalOil);
        }
        CorpseRun.RestoreSaveData(data.corpseRunJson);
    }

    private static void ResetPlayerCampaign(GameObject player)
    {
        if (player == null) return;
        PlayerProgression progression = PlayerProgression.GetOrAdd(player);
        progression.Initialize();
        progression.ResetProgress();
        PlayerCurrencyWallet wallet = player.GetComponent<PlayerCurrencyWallet>();
        if (wallet == null) wallet = player.AddComponent<PlayerCurrencyWallet>();
        wallet.ResetProgress();
        if (player.TryGetComponent(out PlayerHeat heat)) heat.ResetForNewGame();
        if (player.TryGetComponent(out PlayerHealth health)) health.ResetForNewGame();
        if (player.TryGetComponent(out PlayerMovement movement)) movement.ResetForNewGame();
        if (player.TryGetComponent(out PlayerCombatStatus combatStatus)) combatStatus.ResetForNewGame();
        if (player.TryGetComponent(out PlayerGameOver gameOver)) gameOver.ResetForNewGame();
        if (player.TryGetComponent(out PlayerState state)) state.ResetForNewGame();
        KeyItemInventory.ResetForNewGame(false);
        CorpseRun.ResetStoredProgress();
    }

    private static bool CanSave(out string error)
    {
        GameSceneLoader loader = UnityEngine.Object.FindAnyObjectByType<GameSceneLoader>();
        GameObject player = FindPlayer();
        bool dead = player != null && player.TryGetComponent(out PlayerState state) && state.IsDead();
        if (player == null || dead || loader == null || !loader.isActiveAndEnabled || loader.IsLoading ||
            PauseMenuHUD.IsMainMenuOpen ||
            CutsceneController.IsPlaying || !loader.CanLoadSavedScene(SceneManager.GetActiveScene().path))
        {
            error = "Nao e possivel salvar durante morte, cutscene ou transicao de cena.";
            return false;
        }
        error = null;
        return true;
    }

    private static SaveData ReadManual(int slot)
    {
        SaveData data = Read(ManualKey(slot));
        if (data != null) data.slotIndex = slot;
        return data;
    }
    private static SaveData ReadCheckpoint() => Read(CheckpointKey);

    private static SaveData Read(string key)
        => ReadValue(key) ?? ReadValue(key + ".backup");

    private static SaveData ReadValue(string key)
    {
        string json = PlayerPrefs.GetString(key, string.Empty);
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            return data != null && (data.version == 1 || data.version == 2) && !string.IsNullOrWhiteSpace(data.scenePath) &&
                !double.IsNaN(data.playedSeconds) && !double.IsInfinity(data.playedSeconds) &&
                data.playedSeconds >= 0d && data.playedSeconds <= 315537897599d &&
                !float.IsNaN(data.currentHeat) && !float.IsInfinity(data.currentHeat)
                ? data : null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Save] Dados corrompidos ignorados: " + exception.Message);
            return null;
        }
    }

    private static void WriteManual(int slot, SaveData data) =>
        Write(ManualKey(slot), data);

    private static void Write(string key, SaveData data)
    {
        string json = JsonUtility.ToJson(data);
        if (ReadValue(key) != null)
        {
            PlayerPrefs.SetString(key + ".backup", PlayerPrefs.GetString(key));
            PlayerPrefs.Save();
        }
        PlayerPrefs.SetString(key, json);
    }

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
