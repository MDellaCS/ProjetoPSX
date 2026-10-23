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

    internal string SpawnId => spawnId;
    internal bool IsDefault => isDefault;

    internal static ScenePlayerSpawnPoint FindForScene(Scene scene,
        string requestedSpawnId)
    {
        ScenePlayerSpawnPoint fallback = null;
        foreach (ScenePlayerSpawnPoint point in
                 FindObjectsByType<ScenePlayerSpawnPoint>(
                     FindObjectsInactive.Exclude))
        {
            if (point == null || point.gameObject.scene != scene)
                continue;

            if (!string.IsNullOrWhiteSpace(requestedSpawnId) &&
                point.SpawnId == requestedSpawnId)
            {
                return point;
            }

            if (fallback == null || point.IsDefault)
                fallback = point;
        }

        return fallback;
    }

    private void OnDrawGizmos()
    {
        if (!PlayerDebugToolkitHUD.IsVisible && Application.isPlaying)
            return;

        Gizmos.color = new Color(.15f, .9f, 1f, .9f);
        Gizmos.DrawWireSphere(transform.position, .35f);
        Gizmos.DrawLine(transform.position,
            transform.position + transform.forward * .9f);
    }
}
