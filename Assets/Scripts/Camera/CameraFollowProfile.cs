using UnityEngine;

/// <summary>Enquadramento base da camera de acompanhamento.</summary>
[CreateAssetMenu(menuName = "Projeto PSX/Camera/Follow Profile", fileName = "CameraFollowProfile")]
public sealed class CameraFollowProfile : ScriptableObject
{
    [Tooltip("Distancia base entre a camera e o alvo, em unidades da Unity.")]
    [Min(.1f)] public float distance = 5.2f;
    [Tooltip("Angulo horizontal da camera em graus no mundo. Zonas de camera podem substituir este valor.")]
    public float yaw;
    [Tooltip("Angulo vertical da camera em graus; valores positivos olham de cima. Zonas de camera podem substituir este valor.")]
    public float pitch = 22.1f;
    [Tooltip("Campo de visao vertical da camera em graus. Efeitos e zonas podem modificar este valor.")]
    [Range(1f, 179f)] public float fieldOfView = 60f;
}
