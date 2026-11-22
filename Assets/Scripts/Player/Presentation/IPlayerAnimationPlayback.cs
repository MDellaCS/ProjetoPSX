/// <summary>
/// Ponto de extensao de apresentacao. Implementacoes futuras podem encaminhar
/// o estado para Animator, sprites ou outro visual sem o presenter conhecer
/// clips, hashes ou Animator Controllers.
/// </summary>
public interface IPlayerAnimationPlayback
{
    void Present(PlayerVisualState visualState);
}

/// <summary>
/// Playback opcional de ataques. O presenter envia somente a identificacao e
/// o progresso que ja pertencem ao PlayerAttackController; este contrato nao controla
/// nem altera a simulacao.
/// </summary>
public interface IPlayerAttackAnimationPlayback
{
    void PresentAttack(
        PlayerAttackRuntime attack,
        PlayerAttackPhase phase,
        float phaseProgress
    );
}

/// <summary>
/// Atualizacao opcional de locomocao visual. A velocidade recebida e medida
/// pelo gameplay e nunca deve ser usada para mover o player.
/// </summary>
public interface IPlayerMovementAnimationPlayback
{
    void PresentMovement(float horizontalSpeed, bool isMoving);
}
