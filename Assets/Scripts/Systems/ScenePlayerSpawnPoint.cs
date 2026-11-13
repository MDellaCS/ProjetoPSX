using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Marca onde o Player persistente aparece ao carregar esta cena.
/// </summary>
[DisallowMultipleComponent]
public sealed class ScenePlayerSpawnPoint : MonoBehaviour
{
    [Tooltip("Identificador opcional deste ponto. O GameSceneLoader pode solicitar um spawn especifico por este ID.")]
    [SerializeField] private string spawnId;
    [Tooltip("Usado quando nenhum ID especifico foi solicitado e esta cena nao possui outro Spawn Point compativel.")]
    [SerializeField] private bool isDefault = true;
    [Tooltip("Trigger de camera ativado assim que o jogador aparece neste spawn. Deixe vazio para iniciar com a CameraFollow livre.")]
    [SerializeField] private FixedCameraZone initialCameraTrigger;

    public string SpawnId => spawnId;
    internal bool IsDefault => isDefault;
    internal FixedCameraZone InitialCameraTrigger => initialCameraTrigger;

    internal static ScenePlayerSpawnPoint FindForScene(Scene scene,
        string requestedSpawnId)
    {
        ScenePlayerSpawnPoint fallback = null;
        ScenePlayerSpawnPoint defaultPoint = null;
        // A ordem da hierarquia e estavel entre cargas, mesmo quando uma cena
        // possui por engano mais de um ponto marcado como padrao.
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (ScenePlayerSpawnPoint point in root.GetComponentsInChildren<ScenePlayerSpawnPoint>())
        {
            if (point == null || !point.isActiveAndEnabled)
                continue;

            if (!string.IsNullOrWhiteSpace(requestedSpawnId) &&
                point.SpawnId == requestedSpawnId)
            {
                return point;
            }

            if (fallback == null) fallback = point;
            if (defaultPoint == null && point.IsDefault) defaultPoint = point;
        }

        return defaultPoint != null ? defaultPoint : fallback;
    }

    private void OnDrawGizmos()
    {
        if (!DebugGizmoSettings.Enabled(DebugGizmoGroup.Spawns) && Application.isPlaying)
            return;

        Gizmos.color = new Color(.15f, .9f, 1f, .9f);
        Gizmos.DrawWireSphere(transform.position, .35f);
        Gizmos.DrawLine(transform.position,
            transform.position + transform.forward * .9f);
    }
}
