using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>Regressao opt-in com Bootstrap real, preservando preferencias e cenas do usuario.</summary>
[InitializeOnLoad]
public static class GameSavePlayModeRegression
{
    private const string Pending = "GameSavePlayModeRegression.Pending";
    private const string BackupPath = "Library/GameSavePlayModeRegression.backup.json";
    private const string ReportPath = "Library/GameSavePlayModeRegression.result.txt";
    private const string Manual = "ProjetoPSX.SaveGame.Manual.v1.";
    private const string Checkpoint = "ProjetoPSX.SaveGame.Checkpoint.v1";
    private static IEnumerator routine;
    private static double deadline;
    private static readonly object pauseOwner = new();

    [Serializable] private sealed class Pref
    {
        public string key;
        public bool exists;
        public bool integer;
        public string text;
        public int number;
    }
    [Serializable] private sealed class Backup { public List<Pref> values = new(); }

    static GameSavePlayModeRegression()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    [MenuItem("Tools/Tests/Run Game Save Play Mode Regression")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(Pending, false))
            throw new InvalidOperationException("Execute a regressao fora do Play Mode.");
        Backup backup = new();
        for (int i = 0; i < GameSaveService.ManualSlotCount; i++)
        {
            Remember(backup, Manual + i);
            Remember(backup, Manual + i + ".backup");
        }
        Remember(backup, Checkpoint);
        Remember(backup, Checkpoint + ".backup");
        Remember(backup, "ProjetoPSX.SaveGame.ActiveSlot.v1", true);
        Remember(backup, "ProjetoPSX.PlayerCurrency.v1", true);
        Remember(backup, "ProjetoPSX.KeyItems.V1");
        Remember(backup, "ProjetoPSX.CorpseRun.v1");
        foreach (PlayerUpgradeDefinition attribute in PlayerUpgradeCatalog.Load().attributes)
            if (attribute != null) Remember(backup, "ProjetoPSX.Progression.v2." + attribute.id, true);
        File.WriteAllText(BackupPath, JsonUtility.ToJson(backup));
        File.WriteAllText(ReportPath, "RUNNING\n");
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    private static void Remember(Backup backup, string key, bool integer = false) => backup.values.Add(new Pref
    {
        key = key, integer = integer, exists = PlayerPrefs.HasKey(key),
        text = integer ? null : PlayerPrefs.GetString(key, string.Empty),
        number = integer ? PlayerPrefs.GetInt(key, 0) : 0
    });

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Application.runInBackground = true;
            deadline = EditorApplication.timeSinceStartup + 240d;
            routine = Exercise();
            EditorApplication.update += Step;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Step;
            Backup backup = JsonUtility.FromJson<Backup>(File.ReadAllText(BackupPath));
            foreach (Pref pref in backup.values)
            {
                if (!pref.exists) PlayerPrefs.DeleteKey(pref.key);
                else if (pref.integer) PlayerPrefs.SetInt(pref.key, pref.number);
                else PlayerPrefs.SetString(pref.key, pref.text);
            }
            PlayerPrefs.Save();
            SessionState.SetBool(Pending, false);
            File.AppendAllText(ReportPath, "Preferencias originais restauradas.\n");
        }
    }

    private static void Step()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
                throw new TimeoutException("Timeout esperando a Unity concluir o fluxo.");
            if (routine != null && routine.MoveNext()) return;
            Finish(null);
        }
        catch (Exception exception) { Finish(exception); }
    }

    private static void Finish(Exception failure)
    {
        EditorApplication.update -= Step;
        (routine as IDisposable)?.Dispose();
        routine = null;
        string message = failure == null ? "PASS: save/load Play Mode regression." : "FAIL: " + failure;
        File.AppendAllText(ReportPath, message + "\n");
        if (failure == null) Debug.Log(message); else Debug.LogError(message);
        GamePauseController.ReleasePause(pauseOwner);
        EditorApplication.ExitPlaymode();
    }

    private static bool Ready(GameSceneLoader loader) => loader != null && !loader.IsLoading &&
        !LoadingScreenHUD.BlocksGameplayInput;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        File.AppendAllText(ReportPath, "OK: " + message + "\n");
    }

    private static object Field(object obj, string name) => obj.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);

    private static void Call(object obj, string method) => obj.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, null);

    private static void Press(PauseMenuHUD menu, string name)
    {
        VisualElement root = (VisualElement)Field(menu, "root");
        Button button = root.Q<Button>(name);
        Dictionary<Button, Action> commands = (Dictionary<Button, Action>)Field(menu, "commands");
        Check(button != null && button.enabledInHierarchy && commands.ContainsKey(button), "Botao disponivel: " + name);
        commands[button]();
    }

    private static IEnumerator Exercise()
    {
        GameSceneLoader loader;
        do { yield return null; loader = UnityEngine.Object.FindAnyObjectByType<GameSceneLoader>(); }
        while (!Ready(loader) || !PauseMenuHUD.IsOpen);
        GamePauseController.RequestPause(pauseOwner);
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerProgression progression = PlayerProgression.GetOrAdd(player);
        PlayerHeat heat = player.GetComponent<PlayerHeat>();
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        PlayerCurrencyWallet wallet = PlayerCurrencyWallet.FindOrCreate();
        GameSaveService.DeleteAllSaves();
        wallet.RestoreSaveValue(12345);
        Call(loader, "BeginNewGame");
        Check(CutsceneController.IsPlaying, "Novo Jogo inicia a introducao real.");
        Check(wallet.Current == 0 && KeyItemInventory.Count == 0, "Novo Jogo limpa moeda e inventario.");
        Check(progression.ExportSaveData().All(entry => entry.level == 1), "Novo Jogo limpa todos os atributos.");
        CutsceneController.Active.SkipCutscene();
        while (!Ready(loader)) yield return null;
        string scenePath = SceneManager.GetActiveScene().path;
        Check(!PauseMenuHUD.IsOpen, "Introducao devolve o controle ao gameplay.");

        wallet.RestoreSaveValue(1000000);
        foreach (PlayerUpgradeDefinition attribute in progression.Catalog.attributes)
        {
            if (attribute == null || !attribute.purchasable) continue;
            while (progression.Level(attribute) < attribute.MaximumLevel)
                Check(progression.TryPurchase(attribute, out _), "Compra de atributo: " + attribute.id);
        }
        bool hadRage = heat.HasRageUnlocked;
        float maxHeat = heat.MaxHeat;
        heat.RestoreSaveState(maxHeat - 1f);
        health.RestoreSaveHealth(47);
        wallet.RestoreSaveValue(321);

        InventoryEventInteraction door = UnityEngine.Object.FindObjectsByType<InventoryEventInteraction>(FindObjectsInactive.Include)
            .FirstOrDefault(value => (bool)Field(value, "consumeRequiredItems"));
        Check(door != null, "Cena atual possui porta real que consome chave.");
        InventoryItemRequirement requirement = (InventoryItemRequirement)Field(door, "requirement");
        List<KeyItemDefinition> keys = (List<KeyItemDefinition>)Field(requirement, "requiredItems");
        foreach (KeyItemDefinition key in keys) KeyItemInventory.Grant(key, false);
        string doorName = door.name;
        Check(door.Interact(null) && !door.gameObject.activeSelf, "Consumir chave abre a porta real.");
        Check(keys.All(key => !KeyItemInventory.Contains(key) && KeyItemInventory.WasAcquired(key)),
            "Chave consumida continua no historico de coleta.");

        Check(GameSaveService.SaveCheckpoint("Regressao", ""), "Checkpoint salva antes do primeiro slot manual.");
        Check(PauseMenuHUD.OpenSanctuarySave("Regressao", "2"), "Save point abre interface de slots.");
        PauseMenuHUD menu = UnityEngine.Object.FindAnyObjectByType<PauseMenuHUD>();
        Press(menu, "save-slot-0");
        Check(GameSaveService.GetManualSlot(0).IsOccupied, "Primeiro save pela interface funciona.");
        wallet.RestoreSaveValue(322);
        Press(menu, "sanctuary-save-button");
        Press(menu, "save-slot-confirm-no");
        Check(GameSaveService.GetManualSlot(0).data.currency == 321, "Cancelar confirmacao preserva save.");
        Press(menu, "sanctuary-save-button");
        Press(menu, "save-slot-confirm-yes");
        Check(GameSaveService.GetManualSlot(0).data.currency == 322, "Confirmar atualiza save.");
        wallet.RestoreSaveValue(321);
        Press(menu, "sanctuary-save-button");
        Press(menu, "save-slot-confirm-yes");
        menu.Resume();
        Check(GameSaveService.GetCheckpointSlot().data.slotIndex == 0, "Primeiro save vincula checkpoint a campanha.");
        Check(!GameSaveService.SaveManual(1, "Regressao", "", out _), "Campanha vinculada nao grava em outro slot.");
        GameSaveService.SaveData snapshot = GameSaveService.GetManualSlot(0).data;
        string originalCheckpoint = PlayerPrefs.GetString(Checkpoint);
        wallet.RestoreSaveValue(999);
        progression.ResetProgress();
        heat.RestoreSaveState(0f);
        health.RestoreSaveHealth(5);
        Check(GameSaveService.LoadManual(0, out _, out _), "Carregar save manual inicia transicao.");
        Check(!GameSaveService.SaveCheckpoint("Invalido", ""), "Checkpoint bloqueado durante carregamento.");
        Check(!GameSaveService.BeginNewCampaign(scenePath, "", player, out _), "Novo Jogo bloqueado durante carregamento.");
        while (!Ready(loader)) yield return null;
        Check(wallet.Current == 321 && health.CurrentHealth == 47, "Load restaura moeda e vida exatas.");
        Check(Mathf.Approximately(heat.MaxHeat, maxHeat) && Mathf.Approximately(heat.CurrentHeat, maxHeat - 1f), "Load restaura Carga e Heat.");
        Check(heat.HasRageUnlocked == hadRage, "Load preserva desbloqueio de Rage.");
        Check(JsonUtility.ToJson(new Levels { entries = progression.ExportSaveData() }) ==
            JsonUtility.ToJson(new Levels { entries = snapshot.upgrades }), "Load restaura todos os niveis.");
        door = UnityEngine.Object.FindObjectsByType<InventoryEventInteraction>(FindObjectsInactive.Include)
            .First(value => value.name == doorName);
        Check(!door.gameObject.activeSelf, "Porta permanece aberta depois do Load.");
        Check(keys.All(key => !KeyItemInventory.Contains(key) && KeyItemInventory.WasAcquired(key)),
            "Load preserva consumo e historico da chave.");
        ScenePlayerSpawnPoint spawn = UnityEngine.Object.FindObjectsByType<ScenePlayerSpawnPoint>(FindObjectsInactive.Exclude)
            .FirstOrDefault(point => point.gameObject.scene == SceneManager.GetActiveScene() && point.SpawnId == "2");
        Check(spawn != null && Vector3.Distance(player.transform.position, spawn.transform.position) < 1f,
            "Load posiciona o jogador no spawn da cena.");

        PlayerGameOver gameOver = player.GetComponent<PlayerGameOver>();
        player.GetComponent<PlayerState>().ChangeState(PlayerState.State.Dead);
        Call(gameOver, "Update");
        Check(!GameSaveService.SaveCheckpoint("Morto", ""), "Salvar jogador morto e bloqueado.");
        Check(GameSaveService.LoadManual(0, out _, out _), "Load apos morte funciona.");
        while (!Ready(loader)) yield return null;
        Check(!(bool)Field(gameOver, "deathDetected") && !player.GetComponent<PlayerState>().IsDead(),
            "Load limpa Game Over e morte anterior.");

        wallet.RestoreSaveValue(85);
        player.GetComponent<PlayerState>().ChangeState(PlayerState.State.Dead);
        Call(gameOver, "Update");
        Call(gameOver, "RestartGame");
        while (!Ready(loader)) yield return null;
        Check(wallet.Current == 0 && UnityEngine.Object.FindAnyObjectByType<CorpseRunEnemy>() != null,
            "Morte e reinicio geram Corpse Run sem duplicar moeda.");
        Check(GameSaveService.SaveManual(0, "Corpse", "2", out _), "Save inclui Corpse Run.");
        string corpseSnapshot = GameSaveService.GetManualSlot(0).data.corpseRunJson;
        CorpseRun.ResetStoredProgress();
        Check(GameSaveService.LoadManual(0, out _, out _), "Load de campanha com Corpse Run.");
        while (!Ready(loader)) yield return null;
        CorpseRunEnemy corpse = UnityEngine.Object.FindAnyObjectByType<CorpseRunEnemy>();
        Check(corpse != null && corpse.CarriedCurrency == 85 && CorpseRun.ExportSaveData() == corpseSnapshot,
            "Load reconstroi Corpse Run e seu saldo exato.");

        loader.LoadScene("Assets/Scenes/4. Fase1.unity");
        while (!Ready(loader)) yield return null;
        loader.LoadScene(scenePath, "2");
        while (!Ready(loader)) yield return null;
        door = UnityEngine.Object.FindObjectsByType<InventoryEventInteraction>(FindObjectsInactive.Include)
            .First(value => value.name == doorName);
        Check(!door.gameObject.activeSelf, "Troca normal de cenas preserva a porta aberta.");
        UnityEngine.Object.FindAnyObjectByType<PauseMenuHUD>().Resume();

        Check(GameSaveService.BeginNewCampaign(scenePath, "", player, out _), "Segunda campanha inicia limpa.");
        Check(door.gameObject.activeSelf, "Novo Jogo restaura porta aberta pela campanha anterior.");
        Check(!heat.HasRageUnlocked && heat.MaxHeat == heat.InitialMaxHeat, "Novo Jogo remove bonus e Rage anteriores.");
        Check(progression.Catalog.attributes.All(attribute => attribute == null ||
            !PlayerPrefs.HasKey("ProjetoPSX.Progression.v2." + attribute.id)), "Novo Jogo limpa cache de atributos.");
        Check(GameSaveService.GetManualSlot(0).IsOccupied, "Novo Jogo preserva saves manuais.");
        Check(!GameSaveService.GetCheckpointSlot().IsOccupied, "Novo Jogo limpa checkpoint anterior e backup.");
        Check(GameSaveService.SaveManual(0, "Nova campanha", "", out _), "Nova campanha pode substituir slot escolhido.");
        PlayerPrefs.SetString(Checkpoint, originalCheckpoint);
        Check(GameSaveService.LoadCheckpoint(out _, out _), "Checkpoint anterior ainda pode ser carregado.");
        while (!Ready(loader)) yield return null;
        Check(GameSaveService.ActiveManualSlot == -1, "Checkpoint antigo nao se vincula a slot de outra campanha.");
        Check(wallet.Current == 321 && heat.HasRageUnlocked == hadRage, "Checkpoint restaura recursos e upgrades.");

        for (int i = 1; i < GameSaveService.ManualSlotCount; i++)
        {
            Check(GameSaveService.BeginNewCampaign(scenePath, "", player, out _), "Inicia campanha para slot " + i);
            wallet.RestoreSaveValue(i * 100);
            Check(GameSaveService.SaveManual(i, "Slot " + i, "", out _), "Grava slot " + i);
        }
        for (int i = 1; i < GameSaveService.ManualSlotCount; i++)
        {
            Check(GameSaveService.LoadManual(i, out _, out _), "Carrega slot " + i);
            while (!Ready(loader)) yield return null;
            Check(wallet.Current == i * 100 && GameSaveService.ActiveManualSlot == i, "Isolamento do slot " + i);
        }
        wallet.RestoreSaveValue(777);
        Check(GameSaveService.SaveManual(4, "Backup", "", out _), "Atualiza slot para produzir backup.");
        PlayerPrefs.SetString(Manual + 4, "{ quebrado");
        Check(GameSaveService.LoadManual(4, out _, out _), "Recupera save corrompido pelo backup.");
        while (!Ready(loader)) yield return null;
        Check(wallet.Current == 400, "Backup restaura snapshot anterior completo.");
        loader.LoadMainMenu();
        while (!Ready(loader)) yield return null;
        Check(PauseMenuHUD.IsOpen, "Retornar ao menu funciona depois de carregar campanhas.");
        Check(!GameSaveService.SaveCheckpoint("Menu", ""), "Menu inicial nao sobrescreve checkpoint.");
    }

    [Serializable] private sealed class Levels { public PlayerUpgradeSaveEntry[] entries; }
}
