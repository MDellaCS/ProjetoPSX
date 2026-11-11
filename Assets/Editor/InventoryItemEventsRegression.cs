using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>Regressao dos requisitos, consumo atomico e eventos ligados ao inventario.</summary>
public static class InventoryItemEventsRegression
{
    private const string TestScenePath = "Assets/Scenes/999. TestArena.unity";
    private const string InventorySaveKey = "ProjetoPSX.KeyItems.V1";

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    [MenuItem("Tools/Tests/Run Inventory Item Events Regression")]
    public static void Run()
    {
        Check(AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScenePath) != null,
            "A cena 999. TestArena nao foi encontrada.");

        bool hadStoredInventory = PlayerPrefs.HasKey(InventorySaveKey);
        string storedInventory = PlayerPrefs.GetString(InventorySaveKey, string.Empty);
        string[] previousItems = KeyItemInventory.ExportCollectedIds();
        string[] previousAcquired = KeyItemInventory.ExportAcquiredIds();
        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene testScene = SceneManager.GetSceneByPath(TestScenePath);
        bool openedTestScene = !testScene.IsValid() || !testScene.isLoaded;
        GameObject testRoot = null;

        try
        {
            if (openedTestScene)
                testScene = EditorSceneManager.OpenScene(TestScenePath,
                    OpenSceneMode.Additive);
            Check(SceneManager.SetActiveScene(testScene),
                "Nao foi possivel ativar a cena de teste.");

            KeyItemDefinition testKey = Resources.Load<KeyItemDefinition>("Itens/TesteChave");
            Check(testKey != null && !string.IsNullOrWhiteSpace(testKey.Id),
                "Falta Resources/Itens/TesteChave com ID valido.");

            KeyItemInventory.RestoreCollectedIds(Array.Empty<string>());
            testRoot = new GameObject("[Regression] Inventory Item Events");

            GameObject openMarker = CreateChild(testRoot, "Interaction success");
            openMarker.SetActive(false);
            GameObject lockedMarker = CreateChild(testRoot, "Interaction failure");
            lockedMarker.SetActive(false);
            GameObject automaticPassage = CreateChild(testRoot, "Automatic passage");

            InventoryEventInteraction interaction =
                testRoot.AddComponent<InventoryEventInteraction>();
            SetRequiredItems(interaction, testKey);
            SetBool(interaction, "consumeRequiredItems", true);
            AddBoolEvent(interaction, "onRequirementMet", openMarker, true);
            AddBoolEvent(interaction, "onRequirementFailed", lockedMarker, true);

            InventoryConditionListener listener =
                automaticPassage.AddComponent<InventoryConditionListener>();
            SetRequiredItems(listener, testKey);
            SetBool(listener, "triggerWhenEnabled", true);
            SetBool(listener, "triggerOnlyOnce", false);
            AddBoolEvent(listener, "onConditionMet", automaticPassage, false);
            AddBoolEvent(listener, "onConditionLost", automaticPassage, true);
            InvokeLifecycle(listener, "OnDisable");
            InvokeLifecycle(listener, "OnEnable");

            Check(!interaction.Interact(null), "A interacao bloqueada foi aceita.");
            Check(lockedMarker.activeSelf && !openMarker.activeSelf,
                "Sem a chave, executou o evento incorreto.");

            lockedMarker.SetActive(false);
            Check(KeyItemInventory.Grant(testKey, false), "Nao foi possivel conceder a chave de teste.");
            Check(!automaticPassage.activeSelf,
                "A condicao automatica nao reagiu ao evento do inventario.");

            Check(interaction.Interact(null), "Interacao com a chave nao foi consumida.");
            Check(openMarker.activeSelf && !lockedMarker.activeSelf,
                "Com a chave, o evento de sucesso nao foi executado.");
            Check(!KeyItemInventory.Contains(testKey),
                "O item configurado para consumo permaneceu no inventario.");
            Check(automaticPassage.activeSelf,
                "O evento de perda da condicao nao reagiu ao consumo do item.");

            GameObject collectibleObject = CreateChild(testRoot, "Collected world item");
            CollectableItem collectible = collectibleObject.AddComponent<CollectableItem>();
            InvokeLifecycle(collectible, "Awake");
            SetReference(collectible, "item", testKey);
            Check(KeyItemInventory.Grant(testKey, false),
                "Nao foi possivel preparar o coletavel para o teste de Novo Jogo.");
            collectibleObject.SetActive(false);
            KeyItemInventory.ResetForNewGame();
            CollectableItem.ResetAllForNewGame();
            Check(collectibleObject.activeSelf,
                "Novo Jogo nao reativou um coletavel previamente recolhido.");

            Debug.Log("PASS: inventory item events regression in 999. TestArena.");
        }
        catch (Exception exception)
        {
            Debug.LogError("FAIL: inventory item events regression: " + exception);
            throw;
        }
        finally
        {
            if (testRoot != null)
            {
                InventoryConditionListener listener =
                    testRoot.GetComponentInChildren<InventoryConditionListener>(true);
                if (listener != null)
                    InvokeLifecycle(listener, "OnDisable");
                UnityEngine.Object.DestroyImmediate(testRoot);
            }

            KeyItemInventory.RestoreCollectedIds(previousItems, previousAcquired);
            if (hadStoredInventory)
                PlayerPrefs.SetString(InventorySaveKey, storedInventory);
            else
                PlayerPrefs.DeleteKey(InventorySaveKey);
            PlayerPrefs.Save();

            if (openedTestScene && testScene.IsValid() && testScene.isLoaded)
                EditorSceneManager.CloseScene(testScene, true);
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);
        }
    }

    private static GameObject CreateChild(GameObject parent, string objectName)
    {
        GameObject child = new(objectName);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static void SetRequiredItems(Component target, KeyItemDefinition item)
    {
        FieldInfo requirementField = target.GetType().GetField("requirement",
            BindingFlags.Instance | BindingFlags.NonPublic);
        object requirement = requirementField.GetValue(target);
        FieldInfo itemsField = requirement.GetType().GetField("requiredItems",
            BindingFlags.Instance | BindingFlags.NonPublic);
        List<KeyItemDefinition> items =
            (List<KeyItemDefinition>)itemsField.GetValue(requirement);
        items.Add(item);
    }

    private static void SetBool(Component target, string fieldName, bool value)
    {
        FieldInfo field = target.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(field != null, "Campo de teste nao encontrado: " + fieldName);
        field.SetValue(target, value);
    }

    private static void SetReference(Component target, string fieldName, UnityEngine.Object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(field != null, "Campo de teste nao encontrado: " + fieldName);
        field.SetValue(target, value);
    }

    private static void AddBoolEvent(Component source, string fieldName,
        GameObject target, bool value)
    {
        FieldInfo field = source.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(field != null, "Evento de teste nao encontrado: " + fieldName);
        UnityEvent unityEvent = (UnityEvent)field.GetValue(source);
        unityEvent.AddListener(() => target.SetActive(value));
    }

    private static void InvokeLifecycle(Component component, string methodName)
    {
        MethodInfo method = component.GetType().GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(method != null, "Callback de teste nao encontrado: " + methodName);
        method.Invoke(component, null);
    }
}
