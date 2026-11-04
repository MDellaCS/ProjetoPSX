using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

internal static class GrappleTargetHUDRegression
{
    private sealed class Fake : IGrappleTargetProvider
    {
        public int GrappleTargetPriority { get; set; }
        public bool Active { get; set; } = true;
        public GrappleTargetKind Kind { get; set; }

        public bool TryGetGrappleTarget(out GrappleTarget target)
        {
            target = new GrappleTarget { kind = Kind, point = Vector3.one };
            return Active;
        }
    }

    [MenuItem("Tools/Tests/Run Grapple Target HUD Regression")]
    public static void Run()
    {
        try
        {
            VisualTreeAsset layout = Resources.Load<VisualTreeAsset>("GrappleTargetHUD/GrappleTargetHUD");
            PanelSettings settings = Resources.Load<PanelSettings>("GrappleTargetHUD/PanelSettings");
            if (layout == null || settings == null)
                throw new System.InvalidOperationException("Assets do HUD do gancho nao encontrados em Resources.");
            VisualElement tree = layout.CloneTree();
            if (tree.Q<VisualElement>("grapple-marker") == null ||
                tree.Q<VisualElement>("grapple-glow") == null ||
                tree.Q<VisualElement>("grapple-disc") == null)
                throw new System.InvalidOperationException("UXML sem os elementos do marcador.");

            Fake low = new() { GrappleTargetPriority = 100, Kind = GrappleTargetKind.Ladder };
            Fake high = new() { GrappleTargetPriority = 300, Kind = GrappleTargetKind.SwingAnchor };
            GrappleTargetSystem.Register(low);
            GrappleTargetSystem.Register(high);
            bool pickedHigh = GrappleTargetSystem.Current.kind == GrappleTargetKind.SwingAnchor;
            high.Active = false;
            GrappleTargetSystem.Unregister(high);
            bool fellBack = GrappleTargetSystem.Current.kind == GrappleTargetKind.Ladder;
            GrappleTargetSystem.Unregister(low);
            bool empty = !GrappleTargetSystem.Current.IsValid;
            if (!pickedHigh || !fellBack || !empty)
                throw new System.InvalidOperationException(
                    $"Prioridade dos provedores: alto={pickedHigh}, queda={fellBack}, vazio={empty}");

            Debug.Log("PASS: Grapple target HUD regression.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogError("FAIL: Grapple target HUD regression: " + exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
