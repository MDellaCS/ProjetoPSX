using UnityEngine;

public enum SfxCueId
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

/// <summary>Sons de um notch de Heat: um ao ganhar (subindo) e outro ao perder (descendo).</summary>
[System.Serializable]
public sealed class HeatNotchSound
{
    [Tooltip("Som tocado ao ganhar este notch (Heat ou Cold subindo de nivel). Vazio = silencio.")]
    public AudioCue gain = CreateCue();
    [Tooltip("Som tocado ao perder este notch (Heat ou Cold descendo de nivel). Vazio = silencio.")]
    public AudioCue loss = CreateCue();

    public static AudioCue CreateCue() => new();
}

[System.Serializable]
public sealed class GameplayAttackAudioSet
{
    [Tooltip("Som tocado imediatamente quando este tipo de ataque comeca. O clip e interrompido ao primeiro acerto.")]
    public AudioCue attackStart = new();
    [Tooltip("Som de conexao usado quando o alvo nao tem uma superficie especifica ou quando a superficie nao tem clips configurados.")]
    public AudioCue genericHit = new();
    [Tooltip("Som de conexao usado em alvos marcados como tecido.")]
    public AudioCue fabricHit = new();
    [Tooltip("Som de conexao usado em alvos marcados como metal.")]
    public AudioCue metalHit = new();
    [Tooltip("Som de conexao usado em alvos marcados como vidro.")]
    public AudioCue glassHit = new();
    [Tooltip("Som de conexao usado em alvos marcados como pedra.")]
    public AudioCue stoneHit = new();

    public AudioCue GetHitCue(GameplayAudioSurfaceType surface) =>
        surface switch
        {
            GameplayAudioSurfaceType.Fabric when HasClips(fabricHit) => fabricHit,
            GameplayAudioSurfaceType.Metal when HasClips(metalHit) => metalHit,
            GameplayAudioSurfaceType.Glass when HasClips(glassHit) => glassHit,
            GameplayAudioSurfaceType.Stone when HasClips(stoneHit) => stoneHit,
            _ => genericHit
        };

    private static bool HasClips(AudioCue cue) => cue != null && cue.HasClip;
}

/// <summary>Configuracao do audio de SFX de gameplay (Resources/Audio/SfxAudioSettings).</summary>
[CreateAssetMenu(fileName = "SfxAudioSettings", menuName = "Projeto PSX/Audio/Sfx Audio Settings")]
public sealed class SfxAudioSettings : AudioModuleSettings
{
    public const int DefaultHeatNotchCount = 10;

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
    [Tooltip("Um som de ganho e um de perda para CADA notch de Heat/Cold, configurados a mao. Elemento 0 = notch 1 (10 de Heat), elemento 1 = notch 2, e assim por diante. Um notch maior que a lista usa o ultimo elemento. Ao perder um notch toca o som do nivel que acabou de ser deixado (descer de 2x para 1x toca a perda do notch 2).")]
    public HeatNotchSound[] heatNotches = CreateHeatNotches(DefaultHeatNotchCount);
    [Tooltip("Sons globais usados por todos os ataques leves do jogador, com variacoes de acerto por superficie.")]
    public GameplayAttackAudioSet playerLightAttack = new();
    [Tooltip("Sons globais usados por todos os ataques pesados do jogador, com variacoes de acerto por superficie.")]
    public GameplayAttackAudioSet playerHeavyAttack = new();
    [Tooltip("Som tocado quando o jogador recebe um golpe de inimigo.")]
    public AudioCue playerHurt = new();
    [Tooltip("Som de passos do jogador enquanto caminha no chao.")]
    public AudioCue playerFootstep = new();

    [Header("Inimigos")]
    [Tooltip("Som de passos dos inimigos enquanto caminham no chao.")]
    public AudioCue enemyFootstep = new();

    public static HeatNotchSound[] CreateHeatNotches(int count)
    {
        HeatNotchSound[] result = new HeatNotchSound[count];
        for (int i = 0; i < count; i++)
            result[i] = new HeatNotchSound();
        return result;
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        playerLightAttack ??= new GameplayAttackAudioSet();
        playerHeavyAttack ??= new GameplayAttackAudioSet();
        heatNotches ??= System.Array.Empty<HeatNotchSound>();
        minimumFootstepSpeed = Mathf.Max(0f, minimumFootstepSpeed);
        playerFootstepDistance = Mathf.Max(.1f, playerFootstepDistance);
        enemyFootstepDistance = Mathf.Max(.1f, enemyFootstepDistance);
    }

    public GameplayAttackAudioSet GetPlayerAttackAudio(PlayerAttackKind kind) =>
        kind == PlayerAttackKind.Heavy ? playerHeavyAttack : playerLightAttack;

    public AudioCue GetCue(SfxCueId cueId) => cueId switch
    {
        SfxCueId.PlayerHurt => playerHurt,
        SfxCueId.PlayerFootstep => playerFootstep,
        SfxCueId.EnemyFootstep => enemyFootstep,
        _ => null
    };

    /// <summary>Som configurado para o notch (1 = primeiro). Acima do ultimo configurado usa o ultimo.</summary>
    public AudioCue GetHeatNotchCue(bool gained, int notch)
    {
        if (heatNotches == null || heatNotches.Length == 0 || notch < 1)
            return null;

        HeatNotchSound sound = heatNotches[Mathf.Min(notch, heatNotches.Length) - 1];
        if (sound == null)
            return null;
        return gained ? sound.gain : sound.loss;
    }
}
