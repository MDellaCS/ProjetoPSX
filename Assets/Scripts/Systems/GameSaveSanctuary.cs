using UnityEngine;

/// <summary>Ponto de descanso que abre a confirmacao de save do slot manual ativo.</summary>
[AddComponentMenu("Projeto PSX/Save/Save Point")]
[RequireComponent(typeof(InteractionArea))]
public sealed class SavePoint : InteractableBase
{
    [Header("Save Point")]
    [Tooltip("Nome da area mostrado no resumo do slot salvo.")]
    [SerializeField] private string locationName = "Save Point";
    [Tooltip("Ponto de surgimento usado ao carregar este save. Vazio usa o ponto padrao da cena.")]
    [SerializeField] private ScenePlayerSpawnPoint spawnPoint;

    private void OnValidate()
    {
        if (spawnPoint != null && spawnPoint.gameObject.scene != gameObject.scene)
            spawnPoint = null;
    }

    protected override void OnEnable()
    {
        InteractionArea area = GetComponent<InteractionArea>();
        if (area == null)
            return;
        InteractionSystem.Register(this);
    }

    protected override void OnDisable()
    {
        InteractionSystem.Unregister(this);
    }

    public override string InteractionMessage => "Salvar jogo";

    public override bool CanInteract(PlayerMovement player) => base.CanInteract(player);

    public override bool Interact(PlayerMovement player) =>
        CanInteract(player) && PauseMenuHUD.OpenSanctuarySave(locationName,
            spawnPoint != null ? spawnPoint.SpawnId : string.Empty);

}
