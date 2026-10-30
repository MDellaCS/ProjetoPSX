using UnityEngine;
using UnityEngine.Audio;

public enum GameplayAudioCueId
{
    PlayerHurt,
    PlayerFootstep,
    EnemyFootstep
}

public enum GameplayAudioSurfaceType
{
    Generic,
    Fabric,
    Metal,
    Glass,
    Stone
}

[System.Serializable]
public sealed class GameplayAudioCue
{
    [Tooltip("Variacoes aleatorias do mesmo efeito. Deixe vazio para desativar este som.")]
    public AudioClip[] clips = System.Array.Empty<AudioClip>();
    [Tooltip("Volume base deste som antes dos controles globais. Valores acima de 1 aumentam o ganho e podem saturar o audio.")]
    [Range(0f, 5f)] public float volume = 1f;
    [Tooltip("Variacao aleatoria somada ou subtraida do volume base a cada reproducao.")]
    [Range(0f, 1f)] public float volumeVariation;
    [Tooltip("Pitch base deste som.")]
    [Min(.01f)] public float pitch = 1f;
    [Tooltip("Variacao aleatoria somada ou subtraida do pitch base a cada reproducao.")]
    [Range(0f, 1f)] public float pitchVariation;
    [Tooltip("Quanto o som usa espacializacao 3D: zero e 2D, um e totalmente 3D.")]
    [Range(0f, 1f)] public float spatialBlend = 1f;
    [Tooltip("Distancia minima da fonte 3D antes de o volume comecar a diminuir.")]
    [Min(0f)] public float minDistance = 1f;
    [Tooltip("Distancia maxima de atenuacao da fonte 3D.")]
    [Min(.01f)] public float maxDistance = 24f;
    [Tooltip("Regra usada para diminuir o volume conforme a distancia da fonte.")]
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
    [Tooltip("Prioridade da voz no mixer Unity: zero e a prioridade mais alta, 255 a mais baixa.")]
    [Range(0, 255)] public int priority = 128;
    [Tooltip("Mixer Group opcional exclusivo deste som. Vazio usa o grupo global do sistema.")]
    public AudioMixerGroup outputMixerGroup;
}

[System.Serializable]
public sealed class GameplayAttackAudioSet
{
    [Tooltip("Som tocado imediatamente quando este tipo de ataque comeca. O clip e interrompido ao primeiro acerto.")]
    public GameplayAudioCue attackStart = new();
    [Tooltip("Som de conexao usado quando o alvo nao tem uma superficie especifica ou quando a superficie nao tem clips configurados.")]
    public GameplayAudioCue genericHit = new();
    [Tooltip("Som de conexao usado em alvos marcados como tecido.")]
    public GameplayAudioCue fabricHit = new();
    [Tooltip("Som de conexao usado em alvos marcados como metal.")]
    public GameplayAudioCue metalHit = new();
    [Tooltip("Som de conexao usado em alvos marcados como vidro.")]
    public GameplayAudioCue glassHit = new();
    [Tooltip("Som de conexao usado em alvos marcados como pedra.")]
    public GameplayAudioCue stoneHit = new();

    public GameplayAudioCue GetHitCue(GameplayAudioSurfaceType surface) =>
        surface switch
        {
            GameplayAudioSurfaceType.Fabric when HasClips(fabricHit) => fabricHit,
            GameplayAudioSurfaceType.Metal when HasClips(metalHit) => metalHit,
            GameplayAudioSurfaceType.Glass when HasClips(glassHit) => glassHit,
            GameplayAudioSurfaceType.Stone when HasClips(stoneHit) => stoneHit,
            _ => genericHit
        };

    private static bool HasClips(GameplayAudioCue cue)
    {
        if (cue?.clips == null)
            return false;

        foreach (AudioClip clip in cue.clips)
            if (clip != null)
                return true;
        return false;
    }
}

[CreateAssetMenu(fileName = "GameplayAudioSettings", menuName = "Projeto PSX/Audio/Gameplay Audio Settings")]
public sealed class GameplayAudioSettings : ScriptableObject
{
    [Header("Global")]
    [Tooltip("Liga ou desliga todos os efeitos de gameplay sem alterar os dialogos.")]
    public bool sfxEnabled = true;
    [Tooltip("Volume global adicional de todos os efeitos de gameplay.")]
    [Range(0f, 1f)] public float globalVolume = 1f;
    [Tooltip("Pitch global aplicado a todos os efeitos de gameplay.")]
    [Range(.1f, 3f)] public float globalPitch = 1f;
    [Tooltip("Numero de fontes de audio criadas ao iniciar o sistema.")]
    [Min(1)] public int initialVoiceCount = 16;
    [Tooltip("Limite de AudioSources simultaneas antes de reutilizar a voz mais antiga.")]
    [Min(1)] public int maximumVoiceCount = 48;
    [Tooltip("Mixer Group global usado quando um efeito nao define um grupo proprio.")]
    public AudioMixerGroup defaultOutputMixerGroup;

    [Header("Passos")]
    [Tooltip("Velocidade horizontal minima para emitir passos, em unidades do mundo por segundo.")]
    [Min(0f)] public float minimumFootstepSpeed = .6f;
    [Tooltip("Distancia percorrida pelo jogador entre passos consecutivos.")]
    [Min(.1f)] public float playerFootstepDistance = 1.35f;
    [Tooltip("Distancia percorrida pelo inimigo entre passos consecutivos.")]
    [Min(.1f)] public float enemyFootstepDistance = 1.5f;
    [Tooltip("Offset vertical aplicado a fonte de som dos passos em relacao a raiz da entidade.")]
    public float footstepHeightOffset = .08f;

    [Header("Player")]
    [Tooltip("Sons globais usados por todos os ataques leves do jogador, com variações de acerto por superfície.")]
    public GameplayAttackAudioSet playerLightAttack = new();
    [Tooltip("Sons globais usados por todos os ataques pesados do jogador, com variações de acerto por superfície.")]
    public GameplayAttackAudioSet playerHeavyAttack = new();
    [Tooltip("Som tocado quando o jogador recebe um golpe de inimigo.")]
    public GameplayAudioCue playerHurt = new();
    [Tooltip("Som de passos do jogador enquanto caminha no chao.")]
    public GameplayAudioCue playerFootstep = new();

    [Header("Inimigos")]
    [Tooltip("Som de passos dos inimigos enquanto caminham no chao.")]
    public GameplayAudioCue enemyFootstep = new();

    private void OnValidate()
    {
        playerLightAttack ??= new GameplayAttackAudioSet();
        playerHeavyAttack ??= new GameplayAttackAudioSet();
        initialVoiceCount = Mathf.Max(1, initialVoiceCount);
        maximumVoiceCount = Mathf.Max(initialVoiceCount, maximumVoiceCount);
        minimumFootstepSpeed = Mathf.Max(0f, minimumFootstepSpeed);
        playerFootstepDistance = Mathf.Max(.1f, playerFootstepDistance);
        enemyFootstepDistance = Mathf.Max(.1f, enemyFootstepDistance);
    }

    public GameplayAttackAudioSet GetPlayerAttackAudio(PlayerAttackKind kind) =>
        kind == PlayerAttackKind.Heavy ? playerHeavyAttack : playerLightAttack;

    public GameplayAudioCue GetCue(GameplayAudioCueId cueId) => cueId switch
    {
        GameplayAudioCueId.PlayerHurt => playerHurt,
        GameplayAudioCueId.PlayerFootstep => playerFootstep,
        GameplayAudioCueId.EnemyFootstep => enemyFootstep,
        _ => null
    };
}
