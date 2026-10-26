using UnityEngine;

/// <summary>
/// Oculta somente a apresentacao visual do player quando a camera fica perto
/// demais. Collider, scripts, Animator e regras de gameplay continuam ativos.
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class CameraPlayerVisibility : MonoBehaviour
{
    [Header("Player Visual")]
    [Tooltip("Raiz que contem os renderers visuais do player. Se estiver vazia, procura todos os renderers dentro do Player.")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("Distancia entre a camera e o centro do player abaixo da qual o visual desaparece imediatamente.")]
    [Min(0f)] [SerializeField] private float hideDistance = .65f;
    [Tooltip("Distancia necessaria para o visual reaparecer. Deve ser maior que Hide Distance para evitar piscadas.")]
    [Min(0f)] [SerializeField] private float showDistance = .85f;

    private Transform player;
    private Camera controlledCamera;
    private PlayerCharacterController playerController;
    private Renderer[] playerRenderers = System.Array.Empty<Renderer>();
    private bool[] previousForceRenderingOff = System.Array.Empty<bool>();
    private bool isHidden;

    internal void Initialize(Transform playerTarget, Camera cameraComponent)
    {
        RestoreVisibility();
        player = playerTarget;
        controlledCamera = cameraComponent;
        playerController = player != null
            ? player.GetComponent<PlayerCharacterController>()
            : null;
        RefreshRenderers();
    }

    /// <summary>
    /// Rele os renderers depois de trocar ou adicionar o modelo visual.
    /// </summary>
    public void RefreshRenderers()
    {
        RestoreVisibility();
        Transform root = visualRoot != null ? visualRoot : player;
        playerRenderers = root != null
            ? root.GetComponentsInChildren<Renderer>(true)
            : System.Array.Empty<Renderer>();
        previousForceRenderingOff = new bool[playerRenderers.Length];
        isHidden = false;
    }

    private void OnValidate()
    {
        showDistance = Mathf.Max(hideDistance, showDistance);
    }

    private void LateUpdate()
    {
        if (player == null || controlledCamera == null)
            return;

        float distance = Vector3.Distance(
            controlledCamera.transform.position,
            GetPlayerCenter()
        );

        if (!isHidden && distance <= hideDistance)
            SetHidden(true);
        else if (isHidden && distance >= showDistance)
            SetHidden(false);
    }

    private Vector3 GetPlayerCenter()
    {
        if (playerController == null && player != null)
            playerController = player.GetComponent<PlayerCharacterController>();
        return playerController != null
            ? playerController.bounds.center
            : player.position;
    }

    private void SetHidden(bool hidden)
    {
        if (isHidden == hidden)
            return;

        for (int index = 0; index < playerRenderers.Length; index++)
        {
            Renderer playerRenderer = playerRenderers[index];
            if (playerRenderer == null)
                continue;

            if (hidden)
            {
                previousForceRenderingOff[index] =
                    playerRenderer.forceRenderingOff;
                playerRenderer.forceRenderingOff = true;
            }
            else
            {
                playerRenderer.forceRenderingOff =
                    previousForceRenderingOff[index];
            }
        }

        isHidden = hidden;
    }

    private void RestoreVisibility()
    {
        if (isHidden)
            SetHidden(false);
    }

    private void OnDisable() => RestoreVisibility();
    private void OnDestroy() => RestoreVisibility();
}
