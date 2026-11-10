using UnityEngine;

/// <summary>Sons de interface: um cue por evento semantico de menu (Resources/Audio/InterfaceAudioSettings).</summary>
[CreateAssetMenu(fileName = "InterfaceAudioSettings", menuName = "Projeto PSX/Audio/Interface Audio Settings")]
public sealed class InterfaceAudioSettings : AudioModuleSettings
{
    public InterfaceAudioSettings()
    {
        volumeChannel = AudioVolumeChannel.Interface;
    }

    [Header("Sons de Interface")]
    [Tooltip("Som curto ao mudar o foco para uma opcao com mouse, teclado ou controle.")]
    public AudioCue focus = new();
    [Tooltip("Som ao confirmar uma opcao, abrir uma tela interna, trocar de aba ou alternar um botao.")]
    public AudioCue confirm = new();
    [Tooltip("Som ao voltar, cancelar ou fechar uma tela interna.")]
    public AudioCue back = new();
    [Tooltip("Som ao aumentar ou reduzir um valor de configuracao.")]
    public AudioCue adjust = new();
    [Tooltip("Som de opcao indisponivel, compra recusada ou requisito nao atendido.")]
    public AudioCue unavailable = new();
    [Tooltip("Som de compra, salvamento ou outra acao concluida com sucesso.")]
    public AudioCue success = new();
    [Tooltip("Som de aviso antes de uma acao importante ou de restaurar configuracoes.")]
    public AudioCue warning = new();
    [Tooltip("Intervalo minimo entre sons consecutivos de foco para evitar repeticao cansativa.")]
    [Min(0f)] public float focusMinimumInterval = .06f;

    public AudioCue GetCue(MenuAudioCue cue) => cue switch
    {
        MenuAudioCue.Focus => focus,
        MenuAudioCue.Confirm or MenuAudioCue.MenuOpen or MenuAudioCue.TabChange or
            MenuAudioCue.Toggle => confirm,
        MenuAudioCue.Back or MenuAudioCue.MenuClose => back,
        MenuAudioCue.Adjust => adjust,
        MenuAudioCue.Unavailable => unavailable,
        MenuAudioCue.Success => success,
        MenuAudioCue.Warning => warning,
        _ => null
    };
}
