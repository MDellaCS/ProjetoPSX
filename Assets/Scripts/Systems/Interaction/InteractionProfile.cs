using UnityEngine;
using System.Collections.Generic;

public enum InteractionAreaShape
{
    [InspectorName("Caixa")]
    Box,
    [InspectorName("Esfera")]
    Sphere,
    [InspectorName("Capsula")]
    Capsule,
    [InspectorName("Entrada de escada")]
    LadderEntry
}

/// <summary>
/// Configuracao reutilizavel de apresentacao e acionamento de uma interacao.
/// O comportamento continua no InteractableBase que consome o perfil.
/// </summary>
[CreateAssetMenu(fileName = "InteractionProfile", menuName = "Projeto PSX/Interaction/Profile")]
public sealed class InteractionProfile : ScriptableObject
{
    private static readonly Dictionary<int, InteractionProfile> runtimeDefaults = new();

    [Header("Prompt")]
    [Tooltip("Texto exibido no prompt. Vazio preserva o texto definido pelo componente de interacao.")]
    [SerializeField] private string promptMessage;
    [Tooltip("Icone opcional exibido no prompt. Sem icone, usa o definido pelo componente de interacao.")]
    [SerializeField] private Sprite promptIcon;

    [Header("Acionamento")]
    [Tooltip("Prioridade quando varias interacoes estao disponiveis; valores maiores vencem antes da distancia.")]
    [SerializeField] private int priority;
    [Tooltip("Define se a interacao acontece ao pressionar ou apos segurar o botao Interagir.")]
    [SerializeField] private InteractionActivationMode activationMode;
    [Tooltip("Tempo que o jogador precisa segurar Interagir para concluir a acao quando o modo Segurar esta ativo.")]
    [Min(.1f)] [SerializeField] private float holdDuration = .65f;

    [Header("Visibilidade")]
    [Tooltip("Exige caminho desobstruido entre o jogador e o alvo para mostrar e executar a interacao.")]
    [SerializeField] private bool blockByGeometry;
    [Tooltip("Layers que bloqueiam a interacao quando Bloquear por Geometria esta ativo.")]
    [SerializeField] private LayerMask blockingLayers = Physics.DefaultRaycastLayers;

    [Header("Area de interacao")]
    [Tooltip("Forma do volume que habilita a interacao. O volume e sempre definido neste perfil, sem depender de colliders do objeto.")]
    [SerializeField] private InteractionAreaShape areaShape = InteractionAreaShape.Sphere;
    [Tooltip("Centro local do volume e ponto de visibilidade quando Bloquear por Geometria estiver ativo.")]
    [SerializeField] private Vector3 areaCenter;
    [Tooltip("Dimensoes locais do volume quando a forma Caixa estiver selecionada.")]
    [Min(.01f)] [SerializeField] private Vector3 boxSize = new(2f, 2f, 2f);
    [Tooltip("Raio local do volume quando a forma Esfera ou Capsula estiver selecionada.")]
    [Min(.01f)] [SerializeField] private float areaRadius = 2f;
    [Tooltip("Altura local do volume quando a forma Capsula estiver selecionada.")]
    [Min(.02f)] [SerializeField] private float capsuleHeight = 2f;
    [Tooltip("Eixo local da capsula: X, Y ou Z.")]
    [SerializeField] private int capsuleDirection = 1;
    [Tooltip("Profundidade da zona de entrada a frente da escada quando a forma Entrada de escada estiver selecionada.")]
    [Min(.1f)] [SerializeField] private float ladderEntryFrontDepth = 1.1f;
    [Tooltip("Profundidade da zona de entrada atras do plano da escada quando a forma Entrada de escada estiver selecionada.")]
    [Min(0f)] [SerializeField] private float ladderEntryBackDepth = .8f;
    [Tooltip("Folga lateral, inferior e superior da zona de entrada da escada.")]
    [Min(0f)] [SerializeField] private float ladderEntryMargin = .35f;

    [Header("Regras")]
    [Tooltip("Depois de uma interacao bem-sucedida, esta area nao pode ser usada novamente.")]
    [SerializeField] private bool interactOnce;
    [Tooltip("Tempo real de espera apos uma interacao bem-sucedida antes de liberar a area novamente.")]
    [Min(0f)] [SerializeField] private float cooldown;
    [Tooltip("So permite a interacao enquanto o jogador estiver no chao.")]
    [SerializeField] private bool requireGrounded;
    [Tooltip("Quando ativo, o dialogo termina ou usa as falas de saida quando o jogador deixa o volume desta InteractionArea.")]
    [SerializeField] private bool dialogueLeaveArea;
    [Tooltip("Multiplicador do volume de saida do dialogo em relacao ao volume principal. 1 usa o mesmo volume.")]
    [Min(1f)] [SerializeField] private float dialogueLeaveAreaScale = 1.5f;

    public string PromptMessage => promptMessage;
    public Sprite PromptIcon => promptIcon;
    public int Priority => priority;
    public InteractionActivationMode ActivationMode => activationMode;
    public float HoldDuration => Mathf.Max(.1f, holdDuration);
    public bool BlockByGeometry => blockByGeometry;
    public LayerMask BlockingLayers => blockingLayers;
    public InteractionAreaShape AreaShape => areaShape;
    public Vector3 AreaCenter => areaCenter;
    public Vector3 BoxSize => Vector3.Max(boxSize, Vector3.one * .01f);
    public float AreaRadius => Mathf.Max(.01f, areaRadius);
    public float CapsuleHeight => Mathf.Max(AreaRadius * 2f, capsuleHeight);
    public int CapsuleDirection => Mathf.Clamp(capsuleDirection, 0, 2);
    public float LadderEntryFrontDepth => Mathf.Max(.1f, ladderEntryFrontDepth);
    public float LadderEntryBackDepth => Mathf.Max(0f, ladderEntryBackDepth);
    public float LadderEntryMargin => Mathf.Max(0f, ladderEntryMargin);
    public bool InteractOnce => interactOnce;
    public float Cooldown => Mathf.Max(0f, cooldown);
    public bool RequireGrounded => requireGrounded;
    public bool DialogueLeaveArea => dialogueLeaveArea;
    public float DialogueLeaveAreaScale => Mathf.Max(1f, dialogueLeaveAreaScale);

    public static InteractionProfile DefaultFor(Component owner)
    {
        int category = owner switch
        {
            DialogueInteraction => 1,
            CutsceneInteraction => 2,
            SavePoint => 3,
            CollectableItem => 4,
            CurrencyShopInteractionArea => 5,
            HandLadder => 6,
            _ => 0
        };
        if (!runtimeDefaults.TryGetValue(category, out InteractionProfile profile) ||
            profile == null)
        {
            profile = CreateInstance<InteractionProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;
            profile.areaRadius = 2f;
            switch (category)
            {
                case 1:
                    profile.name = "Default Dialogue Interaction Profile (Runtime)";
                    profile.promptMessage = "Falar";
                    profile.interactOnce = true;
                    break;
                case 2:
                    profile.name = "Default Cutscene Interaction Profile (Runtime)";
                    profile.promptMessage = "Iniciar cutscene";
                    profile.interactOnce = true;
                    break;
                case 3:
                    profile.name = "Default Sanctuary Interaction Profile (Runtime)";
                    profile.promptMessage = "Salvar jogo";
                    break;
                case 4:
                    profile.name = "Default Key Item Interaction Profile (Runtime)";
                    profile.promptMessage = "Pegar";
                    profile.areaShape = InteractionAreaShape.Box;
                    break;
                case 5:
                    profile.name = "Default Shop Interaction Profile (Runtime)";
                    profile.promptMessage = "Abrir loja";
                    break;
                case 6:
                    profile.name = "Default Ladder Interaction Profile (Runtime)";
                    profile.promptMessage = "Escalar";
                    profile.areaShape = InteractionAreaShape.LadderEntry;
                    break;
                default:
                    profile.name = "Default Interaction Profile (Runtime)";
                    profile.promptMessage = "Interagir";
                    break;
            }
            runtimeDefaults[category] = profile;
        }
        return profile;
    }

    private void OnValidate()
    {
        boxSize = Vector3.Max(boxSize, Vector3.one * .01f);
        areaRadius = Mathf.Max(.01f, areaRadius);
        capsuleHeight = Mathf.Max(areaRadius * 2f, capsuleHeight);
        capsuleDirection = Mathf.Clamp(capsuleDirection, 0, 2);
        ladderEntryFrontDepth = Mathf.Max(.1f, ladderEntryFrontDepth);
        ladderEntryBackDepth = Mathf.Max(0f, ladderEntryBackDepth);
        ladderEntryMargin = Mathf.Max(0f, ladderEntryMargin);
        dialogueLeaveAreaScale = Mathf.Max(1f, dialogueLeaveAreaScale);
    }
}
