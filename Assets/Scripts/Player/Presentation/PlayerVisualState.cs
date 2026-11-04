/// <summary>
/// Intencoes visuais derivadas do gameplay. Isto nao e uma maquina de estados
/// de gameplay e nao deve ser usado para controlar o jogador.
/// </summary>
public enum PlayerVisualState
{
    Idle,
    Move,
    Crouch,
    Jump,
    Fall,
    Dash,
    Slide,
    Attack,
    Stunned,
    Dead,
    LadderClimb
}
