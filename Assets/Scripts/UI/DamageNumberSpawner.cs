using UnityEngine;

/// <summary>Configuracao temporaria dos numeros de dano de uma entidade.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Projeto PSX/Debug/Damage Number Spawner")]
public sealed class DamageNumberSpawner : MonoBehaviour
{
    [Header("Position")]
    [Tooltip("Deslocamento inicial do numero em relacao a esta entidade.")]
    [SerializeField] private Vector3 spawnOffset = new(0f, 2f, 0f);
    [Tooltip("Deslocamento horizontal aleatorio aplicado a cada numero.")]
    [Min(0f)] [SerializeField] private float horizontalScatter = .35f;
    [Tooltip("Deslocamento vertical aleatorio aplicado a cada numero.")]
    [Min(0f)] [SerializeField] private float verticalScatter = .15f;

    [Header("Text")]
    [Tooltip("Cor padrao dos numeros de dano desta entidade.")]
    [SerializeField] private Color damageColor = Color.white;
    [Tooltip("Tamanho da fonte desenhada na tela.")]
    [Min(1)] [SerializeField] private int fontSize = 32;
    [Tooltip("Tempo total, em segundos, em que o numero fica visivel.")]
    [Min(.01f)] [SerializeField] private float lifetime = 1.35f;
    [Tooltip("Velocidade vertical com que o numero sobe na tela.")]
    [Min(0f)] [SerializeField] private float riseSpeed = 1.5f;

    [Header("Shadow")]
    [Tooltip("Cor da sombra desenhada atras do numero.")]
    [SerializeField] private Color shadowColor = Color.black;
    [Tooltip("Deslocamento em pixels da sombra em relacao ao texto.")]
    [Min(0f)] [SerializeField] private float shadowOffsetPixels = 2f;

    public void ShowDamage(int damage) => ShowDamage(damage, damageColor);

    public void ShowDamage(int damage, Color colorOverride)
    {
        DamageNumber.Show(transform, damage, spawnOffset, colorOverride,
            horizontalScatter, verticalScatter, lifetime, riseSpeed, fontSize,
            shadowColor, shadowOffsetPixels);
    }
}
