using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Desenha campos [SerializeReference, ReferenceTypePicker] com um botao que
/// lista todas as classes concretas derivadas do tipo do campo. Criar uma nova
/// classe derivada (novo modelo de dano, nova reacao) a faz aparecer aqui.
/// </summary>
[CustomPropertyDrawer(typeof(ReferenceTypePickerAttribute))]
public sealed class ReferenceTypePickerDrawer : PropertyDrawer
{
    private const float ButtonWidth = 150f;

    public override float GetPropertyHeight(SerializedProperty property,
        GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
            return EditorGUI.GetPropertyHeight(property, label, true);
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property,
        GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        object value = property.managedReferenceValue;
        string typeLabel = value != null
            ? GetTypeDisplayName(value) : "(escolher tipo)";
        GUIContent content = new(label.text, label.tooltip);

        // O botao precisa ser criado ANTES do PropertyField: o foldout do
        // campo ocupa a linha inteira e consumiria o clique se viesse primeiro.
        Rect button = new(position.xMax - ButtonWidth, position.y,
            ButtonWidth, EditorGUIUtility.singleLineHeight);
        bool clicked = GUI.Button(button, typeLabel, EditorStyles.popup);

        EditorGUI.PropertyField(position, property, content, true);

        if (clicked)
            ShowMenu(property, button);
    }

    private void ShowMenu(SerializedProperty property, Rect buttonRect)
    {
        Type baseType = GetBaseType();
        if (baseType == null)
            return;

        SerializedObject serializedObject = property.serializedObject;
        string path = property.propertyPath;
        GenericMenu menu = new();
        menu.AddItem(new GUIContent("(Nenhum)"), property.managedReferenceValue == null,
            () => Assign(serializedObject, path, null));

        foreach (Type type in TypeCache.GetTypesDerivedFrom(baseType)
                     .Where(t => !t.IsAbstract && !t.IsGenericType &&
                         t.GetConstructor(Type.EmptyTypes) != null &&
                         !typeof(UnityEngine.Object).IsAssignableFrom(t))
                     .OrderBy(t => GetTypeDisplayName(t)))
        {
            Type captured = type;
            bool selected = property.managedReferenceValue != null &&
                property.managedReferenceValue.GetType() == type;
            menu.AddItem(new GUIContent(GetTypeDisplayName(type)), selected,
                () => Assign(serializedObject, path,
                    Activator.CreateInstance(captured)));
        }

        menu.DropDown(buttonRect);
    }

    private static void Assign(SerializedObject serializedObject, string path,
        object instance)
    {
        serializedObject.Update();
        SerializedProperty property = serializedObject.FindProperty(path);
        if (property == null)
            return;
        property.managedReferenceValue = instance;
        serializedObject.ApplyModifiedProperties();
    }

    private Type GetBaseType()
    {
        Type fieldType = fieldInfo != null ? fieldInfo.FieldType : null;
        if (fieldType == null)
            return null;
        if (fieldType.IsArray)
            return fieldType.GetElementType();
        if (fieldType.IsGenericType &&
            fieldType.GetGenericTypeDefinition() == typeof(List<>))
            return fieldType.GetGenericArguments()[0];
        return fieldType;
    }

    private static string GetTypeDisplayName(object instance)
    {
        // Os modelos e reacoes expoem um DisplayName amigavel.
        if (instance is WorldDamageModel model)
            return model.DisplayName;
        if (instance is WorldObjectReaction reaction)
            return reaction.DisplayName;
        return ObjectNames.NicifyVariableName(instance.GetType().Name);
    }

    private static string GetTypeDisplayName(Type type)
    {
        try
        {
            object instance = Activator.CreateInstance(type);
            return GetTypeDisplayName(instance);
        }
        catch (Exception)
        {
            return ObjectNames.NicifyVariableName(type.Name);
        }
    }
}
