using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Marca um ponto fixo do mapa onde o jogador pode iniciar um balanco.</summary>
[DisallowMultipleComponent]
public sealed class PlayerSwingAnchor : MonoBehaviour
{
    private static readonly HashSet<PlayerSwingAnchor> activeAnchors = new();
    private static PlayerMovement cachedPlayerMovement;
    private static PlayerCharacterController cachedPlayerController;

    [Tooltip("Permite que o jogador se prenda a este ponto ao apertar a tecla de balanco.")]
    [SerializeField] private bool canAttach = true;
    [Tooltip("Transform exato onde a corda invisivel fica presa. Se vazio, usa este objeto.")]
    [SerializeField] private Transform anchorPoint;
    [Tooltip("Transform cuja direcao azul (Z) define o eixo horizontal do balanco. Se vazio, usa este objeto.")]
    [SerializeField] private Transform swingPlaneReference;
    [Tooltip("Distancia maxima do jogador ao ponto em que ainda e permitido iniciar o Swing nesta ancora.")]
    [Min(.1f)] [SerializeField] private float interactionRange = 12f;
    [Tooltip("Comprimento fixo da corda invisivel desta ancora durante todo o Swing.")]
    [Min(.1f)] [SerializeField] private float ropeLength = 7f;
    [Tooltip("Raio visual permanente do ponto de ancoragem na Scene View.")]
    [Min(.01f)] [SerializeField] private float debugGizmoRadius = .18f;

    public static HashSet<PlayerSwingAnchor> ActiveAnchors => activeAnchors;
    public bool CanAttach => canAttach && isActiveAndEnabled;
    public Vector3 Position => (anchorPoint != null ? anchorPoint : transform).position;
    public float InteractionRange => Mathf.Max(.1f, interactionRange);
    public float RopeLength => Mathf.Max(.1f, ropeLength);

    public Vector3 SwingForward
    {
        get
        {
            Transform reference = swingPlaneReference != null
                ? swingPlaneReference : transform;
            Vector3 forward = Vector3.ProjectOnPlane(reference.forward,
                Vector3.up);
            return forward.sqrMagnitude > .0001f
                ? forward.normalized
                : Vector3.forward;
        }
    }

    private void OnEnable() => activeAnchors.Add(this);
    private void OnDisable() => activeAnchors.Remove(this);

    private void OnValidate()
    {
        interactionRange = Mathf.Max(.1f, interactionRange);
        ropeLength = Mathf.Max(.1f, ropeLength);
        debugGizmoRadius = Mathf.Max(.01f, debugGizmoRadius);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()
    {
        activeAnchors.Clear();
        cachedPlayerMovement = null;
        cachedPlayerController = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RebuildRegistry()
    {
        foreach (PlayerSwingAnchor anchor in Object.FindObjectsByType<
            PlayerSwingAnchor>(FindObjectsInactive.Exclude))
            activeAnchors.Add(anchor);
    }

    private void OnDrawGizmos()
    {
        Vector3 position = (anchorPoint != null ? anchorPoint : transform).position;
        Color anchorColor = !canAttach
            ? Color.gray
            : IsPlayerWithinInteractionRange(position)
                ? new Color(.1f, 1f, .15f, 1f)
                : new Color(.15f, 1f, .9f, 1f);
        float visibleRadius = GetVisibleGizmoRadius(position);

        Gizmos.color = new Color(anchorColor.r, anchorColor.g,
            anchorColor.b, .78f);
        Gizmos.DrawSphere(position, visibleRadius);
        Gizmos.color = anchorColor;
        Gizmos.DrawWireSphere(position, visibleRadius);
        Gizmos.DrawLine(position, position + SwingForward *
            Mathf.Max(.8f, visibleRadius * 2.5f));
    }

    private bool IsPlayerWithinInteractionRange(Vector3 anchorPosition)
    {
        if (cachedPlayerMovement == null)
        {
            cachedPlayerMovement = FindAnyObjectByType<PlayerMovement>();
        }

        if (cachedPlayerMovement == null ||
            !cachedPlayerMovement.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (cachedPlayerController == null)
        {
            cachedPlayerController = cachedPlayerMovement
                .GetComponent<PlayerCharacterController>();
        }

        Vector3 centerOffset = cachedPlayerController != null
            ? cachedPlayerController.center
            : Vector3.zero;
        Vector3 playerCenter = cachedPlayerMovement.transform.position +
            cachedPlayerMovement.transform.rotation * centerOffset;
        return Vector3.Distance(playerCenter, anchorPosition) <=
            InteractionRange;
    }

    private float GetVisibleGizmoRadius(Vector3 position)
    {
        float radius = debugGizmoRadius;
#if UNITY_EDITOR
        SceneView sceneView = SceneView.currentDrawingSceneView;
        if (sceneView != null)
        {
            // Mantem o marcador legivel em qualquer zoom da Scene View.
            radius = Mathf.Max(radius,
                HandleUtility.GetHandleSize(position) * .14f);
            return radius;
        }
#endif

        // Em Game View, Gizmos.DrawSphere usa tamanho do mundo. Compensa a
        // distancia da camera para manter o marcador visivel durante o jogo.
        Camera viewCamera = Camera.main;
        if (viewCamera == null || viewCamera.pixelHeight <= 0)
            return radius;

        const float screenRadius = 16f;
        float cameraDistance = Vector3.Distance(
            viewCamera.transform.position, position);
        float worldRadius;
        if (viewCamera.orthographic)
        {
            worldRadius = 2f * viewCamera.orthographicSize *
                screenRadius / viewCamera.pixelHeight;
        }
        else
        {
            worldRadius = 2f * cameraDistance *
                Mathf.Tan(viewCamera.fieldOfView * .5f * Mathf.Deg2Rad) *
                screenRadius / viewCamera.pixelHeight;
        }

        radius = Mathf.Max(radius, worldRadius);
        return radius;
    }
}
