/// <summary>
/// Ponto de extensao de apresentacao. Implementacoes futuras podem encaminhar
/// o estado para Animator, sprites ou outro visual sem o presenter conhecer
/// clips, hashes ou Animator Controllers.
/// </summary>
public interface IPlayerAnimationPlayback
{
    void Present(PlayerVisualState visualState);
}
