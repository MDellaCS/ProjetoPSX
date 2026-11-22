using UnityEngine;

/// <summary>Ativa a apresentacao em poses discretas nas malhas deste modelo.</summary>
[DefaultExecutionOrder(-500)]
[DisallowMultipleComponent]
public sealed class SteppedMeshGroup : MonoBehaviour
{
    private void Awake()
    {
        foreach (SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.GetComponent<SteppedSkinnedMesh>() == null)
                renderer.gameObject.AddComponent<SteppedSkinnedMesh>();
        }
    }
}
