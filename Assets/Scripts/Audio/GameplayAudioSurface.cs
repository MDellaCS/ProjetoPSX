using UnityEngine;

/// <summary>Marca uma entidade ou objeto com a superficie usada nos sons de impacto do jogador.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Projeto PSX/Audio/Gameplay Audio Surface")]
public sealed class GameplayAudioSurface : MonoBehaviour
{
    [Tooltip("Categoria de superficie usada para escolher o som quando um ataque do jogador conecta neste objeto.")]
    [SerializeField] private GameplayAudioSurfaceType surfaceType =
        GameplayAudioSurfaceType.Generic;

    public GameplayAudioSurfaceType SurfaceType => surfaceType;

    public static GameplayAudioSurfaceType GetSurfaceType(Component target)
    {
        if (target == null)
            return GameplayAudioSurfaceType.Generic;

        GameplayAudioSurface surface =
            target.GetComponentInParent<GameplayAudioSurface>();
        return surface != null
            ? surface.surfaceType
            : GameplayAudioSurfaceType.Generic;
    }
}
