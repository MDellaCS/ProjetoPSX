using UnityEngine;

/// <summary>
/// Mostra o nome de uma area quando o jogador entra no volume deste objeto.
/// </summary>
[RequireComponent(typeof(Collider))]
public class AreaTitleTrigger : MonoBehaviour
{
    [Header("Texto da area")]
    [Tooltip("Nome principal exibido ao entrar na area.")]
    [SerializeField] private string title = "Nome da area";
    [Tooltip("Linha menor opcional exibida abaixo do nome principal.")]
    [TextArea(1, 2)] [SerializeField] private string subtitle;
    [Tooltip("Duracao total da exibicao. Valor negativo usa a duracao padrao do AreaTitleHUD.")]
    [SerializeField] private float displaySeconds = -1f;
    [Tooltip("Se ativado, permite exibir o titulo novamente depois que o jogador sair e entrar outra vez.")]
    [SerializeField] private bool showOnEveryEntry;

    private bool hasShown;
    private bool playerInside;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void Update()
    {
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        // Tambem consulta a sobreposicao para cobrir entradas que o Unity nao
        // reporta como OnTriggerEnter, como quando o player ja inicia dentro.
        bool overlappingPlayer = PlayerAreaOverlap.ContainsPlayer(
            GetComponent<Collider>(), playerMovement);
        if (overlappingPlayer && !playerInside)
            HandlePlayerEntered();
        else if (!overlappingPlayer && playerInside)
            playerInside = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        HandlePlayerEntered();
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
            playerInside = false;
    }

    private void HandlePlayerEntered()
    {
        if (playerInside)
            return;

        playerInside = true;
        if (!showOnEveryEntry && hasShown)
            return;

        AreaTitleHUD.ShowArea(title, subtitle, displaySeconds);
        hasShown = true;
    }

    private static bool IsPlayer(Collider other)
    {
        return other != null &&
            (other.CompareTag("Player") ||
             other.GetComponentInParent<PlayerMovement>() != null);
    }

    private void OnValidate()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && !DebugGizmoSettings.Enabled(DebugGizmoGroup.WorldAreaTitles))
            return;

        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
            return;

        Gizmos.color = new Color(.35f, .8f, 1f, .75f);
        Gizmos.matrix = transform.localToWorldMatrix;
        switch (triggerCollider)
        {
            case BoxCollider box:
                Gizmos.DrawWireCube(box.center, box.size);
                break;
            case SphereCollider sphere:
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                break;
            case CapsuleCollider capsule:
                Gizmos.DrawWireSphere(capsule.center, capsule.radius);
                break;
        }
        Gizmos.matrix = Matrix4x4.identity;
    }
}
