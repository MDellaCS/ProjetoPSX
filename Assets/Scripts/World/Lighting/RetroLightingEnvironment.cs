using UnityEngine;

/// <summary>
/// Define o ambiente (luz ambiente) da cena. O ultimo ambiente ativado
/// vence; sem nenhum, vale o Default Environment do RetroLightingSettings.
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
[AddComponentMenu("Projeto PSX/Lighting/Retro Lighting Environment")]
public sealed class RetroLightingEnvironment : MonoBehaviour
{
    [Tooltip("Perfil de luz ambiente aplicado enquanto este componente estiver ativo.")]
    [SerializeField] private RetroEnvironmentProfile profile;

    public RetroEnvironmentProfile Profile => profile;

    private void OnEnable() => RetroLightSystem.PushEnvironment(this);
    private void OnDisable() => RetroLightSystem.RemoveEnvironment(this);
    private void OnValidate() => RetroLightSystem.MarkEnvironmentDirty();
}
