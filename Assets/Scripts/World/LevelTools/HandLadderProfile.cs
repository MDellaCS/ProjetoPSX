using UnityEngine;

public enum HandLadderPrefabAxis
{
    X,
    Y,
    Z
}

/// <summary>Visual e dimensoes de cada modulo repetido pelo gerador de escada.</summary>
[CreateAssetMenu(menuName = "Projeto PSX/Level Design/Hand Ladder Profile", fileName = "HandLadderProfile")]
public sealed class HandLadderProfile : ScriptableObject
{
    [Tooltip("Prefab visual repetido em cada degrau. Pode incluir colisor visual; o gerador desativa colliders filhos para a escada nao criar bloqueios redundantes.")]
    public GameObject rungPrefab;
    [Tooltip("Eixo local do prefab que atravessa a largura do degrau. O gerador alinha este eixo com a largura da escada e ajusta o tamanho pela malha real.")]
    public HandLadderPrefabAxis rungWidthAxis = HandLadderPrefabAxis.X;
    [Tooltip("Giro, em graus, em torno do eixo de largura do degrau. Use para orientar a face visual sem mudar a direcao da escada.")]
    public float rungRollDegrees;
    [Tooltip("Prefab opcional para os dois trilhos laterais. O eixo configurado abaixo e alinhado com a subida; deixe vazio para gerar somente os degraus.")]
    public GameObject railPrefab;
    [Tooltip("Eixo local do prefab que percorre o comprimento dos trilhos. Este eixo e alinhado com a direcao de subida.")]
    public HandLadderPrefabAxis railLengthAxis = HandLadderPrefabAxis.Y;
    [Tooltip("Eixo local usado para ajustar a espessura dos trilhos.")]
    public HandLadderPrefabAxis railThicknessAxis = HandLadderPrefabAxis.X;
    [Tooltip("Giro, em graus, em torno do comprimento dos trilhos.")]
    public float railRollDegrees;
    [Tooltip("Distancia entre o centro de dois degraus consecutivos, em metros.")]
    [Min(.05f)] public float rungSpacing = .32f;
    [Tooltip("Largura inicial da escada em metros. Cada instância pode sobrescrever este valor.")]
    [Min(.1f)] public float defaultWidth = 1.2f;
    [Tooltip("Espessura visual sugerida para trilhos e gizmos, em metros.")]
    [Min(.01f)] public float railThickness = .08f;
}
