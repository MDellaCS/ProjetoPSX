using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Projeto PSX/Progression/Catalog")]
public sealed class PlayerUpgradeCatalog : ScriptableObject
{
    public const string ResourcePath = "PlayerUpgradeCatalog";
    [Tooltip("Titulo da unica loja compartilhada por todos os terminais.")]
    public string shopTitle = "APRIMORAMENTOS";
    [Tooltip("Atributos disponiveis, na ordem da tela. Adicione ou remova assets para alterar a loja global.")]
    public List<PlayerUpgradeDefinition> attributes = new();

    public static PlayerUpgradeCatalog Load() => Resources.Load<PlayerUpgradeCatalog>(ResourcePath);
}
