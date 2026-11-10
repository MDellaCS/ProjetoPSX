using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Validacoes sem cenas para os slots manuais e o checkpoint automatico.</summary>
public static class GameSaveRegression
{
    private const string ManualPrefix = "ProjetoPSX.SaveGame.Manual.v1.";
    private const string CheckpointKey = "ProjetoPSX.SaveGame.Checkpoint.v1";
    private const string ActiveSlotKey = "ProjetoPSX.SaveGame.ActiveSlot.v1";

    private readonly struct StringBackup
    {
        public readonly string key;
        public readonly bool existed;
        public readonly string value;

        public StringBackup(string key)
        {
            this.key = key;
            existed = PlayerPrefs.HasKey(key);
            value = existed ? PlayerPrefs.GetString(key) : string.Empty;
        }

        public void Restore()
        {
            if (existed) PlayerPrefs.SetString(key, value);
            else PlayerPrefs.DeleteKey(key);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    [MenuItem("Tools/Tests/Run Game Save Regression")]
    public static void Run()
    {
        List<StringBackup> backups = new();
        for (int i = 0; i < GameSaveService.ManualSlotCount; i++)
            backups.Add(new StringBackup(ManualPrefix + i));
        backups.Add(new StringBackup(CheckpointKey));
        bool activeSlotExisted = PlayerPrefs.HasKey(ActiveSlotKey);
        int previousActiveSlot = PlayerPrefs.GetInt(ActiveSlotKey, -1);
        try
        {
            for (int i = 0; i < GameSaveService.ManualSlotCount; i++)
                PlayerPrefs.DeleteKey(ManualPrefix + i);
            PlayerPrefs.DeleteKey(CheckpointKey);
            PlayerPrefs.DeleteKey(ActiveSlotKey);

            GameSaveService.SaveData manual = new()
            {
                slotIndex = 0,
                scenePath = "Assets/Scenes/4. Test.unity",
                location = "Cripta",
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                playedSeconds = 3723d
            };
            GameSaveService.SaveData automatic = new()
            {
                slotIndex = 0,
                scenePath = "Assets/Scenes/5. Test.unity",
                location = "Ponte",
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                playedSeconds = 90d
            };
            PlayerPrefs.SetString(ManualPrefix + 0, JsonUtility.ToJson(manual));
            PlayerPrefs.SetString(CheckpointKey, JsonUtility.ToJson(automatic));

            Check(GameSaveService.GetManualSlot(0).IsOccupied, "Slot manual salvo nao foi listado.");
            Check(GameSaveService.GetManualSlot(0).Summary.Contains("Cripta"), "Resumo manual nao mostra a area.");
            Check(!GameSaveService.GetManualSlot(1).IsOccupied, "Slots manuais vazios devem continuar vazios.");
            Check(GameSaveService.GetCheckpointSlot().IsOccupied, "Checkpoint automatico nao foi listado.");
            Check(GameSaveService.GetCheckpointSlot().data.scenePath == automatic.scenePath,
                "Checkpoint deve permanecer separado dos slots manuais.");
            Check(GameSaveService.FormatPlayTime(3723d) == "01:02:03", "Formatacao de tempo incorreta.");
            Check(GameSaveService.HasAnySave, "A lista de saves ocupados nao foi detectada.");
            Debug.Log("PASS: game save regression.");
        }
        catch (Exception exception)
        {
            Debug.LogError("FAIL: game save regression: " + exception.Message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
        finally
        {
            foreach (StringBackup backup in backups) backup.Restore();
            if (activeSlotExisted) PlayerPrefs.SetInt(ActiveSlotKey, previousActiveSlot);
            else PlayerPrefs.DeleteKey(ActiveSlotKey);
            PlayerPrefs.Save();
        }
    }

    public static void RunBatch()
    {
        Run();
        EditorApplication.Exit(0);
    }
}
