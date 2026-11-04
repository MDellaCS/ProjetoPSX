using UnityEngine;

/// <summary>Escada de mão escalável, com entrada, trilhos e saídas configuráveis.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Projeto PSX/Level Design/Hand Ladder")]
public sealed class HandLadder : MonoBehaviour
{
    [Header("Geração visual")]
    [Tooltip("Perfil do degrau modular. Trocar o perfil atualiza a escada sem alterar o asset.")]
    [SerializeField] private HandLadderProfile profile;
    [Tooltip("Altura entre a base e o topo da escada, em metros.")]
    [Min(.3f)] [SerializeField] private float height = 3f;
    [Tooltip("Largura dos degraus. Zero usa a largura padrão do perfil.")]
    [Min(0f)] [SerializeField] private float width;
    [Tooltip("Espaço adicional entre degraus, somado ao espaçamento do perfil.")]
    [Min(0f)] [SerializeField] private float extraGap;
    [Tooltip("Gera os dois trilhos laterais quando o perfil tem um prefab de rail atribuido.")]
    [SerializeField] private bool generateSideRails = true;
    [Tooltip("Direcao local, no objeto da escada, que aponta da base para o topo. O vetor pode ser rotacionado para escadas inclinadas.")]
    [SerializeField] private Vector3 climbAxis = Vector3.up;
    [Tooltip("Direcao local que aponta para fora da face de aproximação. O jogador entra por este lado; a largura e calculada perpendicularmente aos dois eixos.")]
    [SerializeField] private Vector3 approachAxis = Vector3.forward;

    [Header("Interação")]
    [Tooltip("Distância máxima, em metros, entre o jogador e o trilho para aceitar o início da escalada.")]
    [Min(.1f)] [SerializeField] private float interactionRange = 1.25f;
    [Tooltip("Velocidade ao longo do eixo de subida configurado, em metros por segundo.")]
    [Min(.1f)] [SerializeField] private float climbSpeed = 2.2f;
    [Tooltip("Recuo do centro do jogador em relação ao plano da escada.")]
    [SerializeField] private float playerForwardOffset = .38f;
    [Tooltip("Ponto de saída no topo. É criado automaticamente no extremo da escada; mova este Transform para ajustar manualmente a saída.")]
    [SerializeField] private Transform topExit;
    [Tooltip("Ponto de saída na base. É criado automaticamente no extremo da escada; mova este Transform para ajustar manualmente a saída.")]
    [SerializeField] private Transform bottomExit;
    [SerializeField, HideInInspector] private bool topExitGenerated;
    [SerializeField, HideInInspector] private bool bottomExitGenerated;
    [SerializeField, HideInInspector] private Vector3 lastGeneratedTopExitPosition;
    [SerializeField, HideInInspector] private Vector3 lastGeneratedBottomExitPosition;

    [Header("Prévia gerada (somente leitura)")]
    [SerializeField] private Transform generatedRoot;
    [SerializeField] private int generatedRungCount;

    public float Height => height;
    public float ClimbSpeed => climbSpeed;
    public float PlayerForwardOffset => playerForwardOffset;
    public Transform TopExit => topExit;
    public Transform BottomExit => bottomExit;
    public int GeneratedRungCount => generatedRungCount;
    public Vector3 LocalClimbAxis => climbAxis.sqrMagnitude > .0001f
        ? climbAxis.normalized : Vector3.up;
    public Vector3 LocalApproachAxis
    {
        get
        {
            Vector3 climb = LocalClimbAxis;
            Vector3 approach = Vector3.ProjectOnPlane(approachAxis, climb);
            if (approach.sqrMagnitude < .0001f)
                approach = Vector3.ProjectOnPlane(Vector3.forward, climb);
            if (approach.sqrMagnitude < .0001f)
                approach = Vector3.ProjectOnPlane(Vector3.right, climb);
            return approach.normalized;
        }
    }
    public Vector3 LocalWidthAxis => Vector3.Cross(LocalClimbAxis,
        LocalApproachAxis).normalized;
    public Vector3 ClimbDirection => transform.TransformDirection(LocalClimbAxis).normalized;
    public Vector3 ApproachDirection => transform.TransformDirection(LocalApproachAxis).normalized;
    public Vector3 WidthDirection => transform.TransformDirection(LocalWidthAxis).normalized;

    private void OnValidate()
    {
        height = Mathf.Max(.3f, height);
        width = Mathf.Max(0f, width);
        extraGap = Mathf.Max(0f, extraGap);
        interactionRange = Mathf.Max(.1f, interactionRange);
        climbSpeed = Mathf.Max(.1f, climbSpeed);
        if (climbAxis.sqrMagnitude < .0001f) climbAxis = Vector3.up;
        climbAxis.Normalize();
        approachAxis = Vector3.ProjectOnPlane(approachAxis, climbAxis);
        if (approachAxis.sqrMagnitude < .0001f)
            approachAxis = Vector3.ProjectOnPlane(Vector3.forward, climbAxis);
        if (approachAxis.sqrMagnitude < .0001f)
            approachAxis = Vector3.ProjectOnPlane(Vector3.right, climbAxis);
        approachAxis.Normalize();
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && !PlayerDebugToolkitHUD.IsVisible)
            return;
        Vector3 climb = ClimbDirection;
        Vector3 widthDirection = WidthDirection;
        Vector3 bottom = transform.position;
        Vector3 top = bottom + climb * height;
        float halfWidth = ResolvedWidth() * .5f;
        Gizmos.color = new Color(.1f, .65f, .95f, .45f);
        Gizmos.DrawLine(bottom - widthDirection * halfWidth, top - widthDirection * halfWidth);
        Gizmos.DrawLine(bottom + widthDirection * halfWidth, top + widthDirection * halfWidth);
        Gizmos.DrawLine(bottom, top);
        Gizmos.DrawWireSphere(top, .09f);
        if (topExit != null)
        {
            Gizmos.color = new Color(.2f, 1f, .4f, .9f);
            Gizmos.DrawWireSphere(topExit.position, .12f);
            Vector3 exitForward = topExitGenerated ? ApproachDirection : topExit.forward;
            Gizmos.DrawLine(topExit.position, topExit.position + exitForward * .35f);
        }
        if (bottomExit != null)
        {
            Gizmos.color = new Color(1f, .7f, .15f, .9f);
            Gizmos.DrawWireSphere(bottomExit.position, .12f);
            Vector3 exitForward = bottomExitGenerated ? ApproachDirection : bottomExit.forward;
            Gizmos.DrawLine(bottomExit.position, bottomExit.position + exitForward * .35f);
        }
    }

    private float ResolvedWidth() => width > .01f ? width : profile != null ? profile.defaultWidth : 1f;

    public bool TryGetEntry(Vector3 playerPosition, out Vector3 rootPosition, out Quaternion facing)
    {
        Vector3 climb = ClimbDirection;
        Vector3 approach = ApproachDirection;
        facing = Quaternion.LookRotation(-approach, climb);
        Vector3 ladderPlane = transform.position + approach * playerForwardOffset;
        Vector3 closest = ladderPlane + climb * Vector3.Dot(playerPosition - ladderPlane, climb);
        rootPosition = closest;
        Vector3 delta = playerPosition - closest;
        delta = Vector3.ProjectOnPlane(delta, climb);
        return delta.sqrMagnitude <= interactionRange * interactionRange &&
            Vector3.Dot(delta, approach) > -.25f;
    }

    public Vector3 GetExitPosition(bool top)
    {
        Transform marker = top ? topExit : bottomExit;
        if (marker != null) return marker.position;
        Vector3 end = transform.position + ClimbDirection * (top ? height : 0f);
        return end + ApproachDirection * (playerForwardOffset + .55f);
    }

    public Vector3 GetDefaultRailPosition(Vector3 playerPosition)
    {
        Vector3 climb = ClimbDirection;
        float level = Mathf.Clamp(Vector3.Dot(playerPosition - transform.position, climb), 0f, height);
        return transform.position + climb * level + ApproachDirection * playerForwardOffset;
    }

    public void SetGenerated(Transform root, int rungCount)
    {
        generatedRoot = root;
        generatedRungCount = rungCount;
    }

    public bool TopExitGenerated
    {
        get => topExitGenerated;
        set => topExitGenerated = value;
    }
    public bool BottomExitGenerated
    {
        get => bottomExitGenerated;
        set => bottomExitGenerated = value;
    }
    public Vector3 LastGeneratedTopExitPosition
    {
        get => lastGeneratedTopExitPosition;
        set => lastGeneratedTopExitPosition = value;
    }
    public Vector3 LastGeneratedBottomExitPosition
    {
        get => lastGeneratedBottomExitPosition;
        set => lastGeneratedBottomExitPosition = value;
    }
    public void SetExitMarkers(Transform top, Transform bottom)
    {
        topExit = top;
        bottomExit = bottom;
    }

    public HandLadderProfile Profile => profile;
    public float Width => ResolvedWidth();
    public float ExtraGap => extraGap;
    public bool GenerateSideRails => generateSideRails;
    public Transform GeneratedRoot => generatedRoot;
}
