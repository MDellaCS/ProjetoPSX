using UnityEngine;

/// <summary>
/// Amostra somente a malha renderizada. O Animator e os ossos continuam
/// atualizando normalmente para os sistemas que dependem deles.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SkinnedMeshRenderer))]
public sealed class SteppedSkinnedMesh : MonoBehaviour
{
    private SkinnedMeshRenderer source;
    private MeshRenderer display;
    private Mesh mesh;
    private GameObject displayObject;
    private bool previousForceRenderingOff;
    private long lastSampleFrame;

    public bool IsDisplay(Renderer renderer) => renderer != null && renderer == display;

    private void OnEnable()
    {
        source = GetComponent<SkinnedMeshRenderer>();
        if (source.sharedMesh == null)
        {
            enabled = false;
            return;
        }

        mesh = new Mesh { name = source.sharedMesh.name + " (poses visuais)" };
        mesh.MarkDynamic();
        displayObject = new GameObject("Stepped Mesh Display");
        displayObject.transform.SetParent(transform, false);
        displayObject.layer = gameObject.layer;
        displayObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        display = displayObject.AddComponent<MeshRenderer>();
        display.sharedMaterials = source.sharedMaterials;
        display.shadowCastingMode = source.shadowCastingMode;
        display.receiveShadows = source.receiveShadows;
        display.lightProbeUsage = source.lightProbeUsage;
        display.reflectionProbeUsage = source.reflectionProbeUsage;
        display.probeAnchor = source.probeAnchor;
        display.renderingLayerMask = source.renderingLayerMask;
        display.sortingLayerID = source.sortingLayerID;
        display.sortingOrder = source.sortingOrder;

        previousForceRenderingOff = source.forceRenderingOff;
        source.BakeMesh(mesh, true);
        source.forceRenderingOff = true;
        lastSampleFrame = AnimationFrameRateSettings.CurrentFrame;
    }

    private void LateUpdate()
    {
        if (source == null || display == null)
            return;

        display.enabled = source.enabled && !previousForceRenderingOff;
        long frame = AnimationFrameRateSettings.CurrentFrame;
        if (frame == lastSampleFrame)
            return;

        source.BakeMesh(mesh, true);
        lastSampleFrame = frame;
    }

    private void OnDisable()
    {
        if (source != null && displayObject != null)
            source.forceRenderingOff = previousForceRenderingOff;
        if (displayObject != null)
            Destroy(displayObject);
        if (mesh != null)
            Destroy(mesh);
        displayObject = null;
        display = null;
        mesh = null;
    }
}
