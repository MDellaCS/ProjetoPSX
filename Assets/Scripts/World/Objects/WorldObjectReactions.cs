using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Dados do momento em que uma reacao de um objeto de mundo e executada.</summary>
public sealed class WorldObjectContext
{
    public WorldObject Owner { get; }
    /// <summary>Dano do golpe que disparou a reacao (0 quando nao veio de um golpe).</summary>
    public int Damage { get; }
    /// <summary>Verdadeiro quando a reacao veio de um golpe e as posicoes abaixo sao validas.</summary>
    public bool HasImpact { get; }
    public Vector3 ImpactPosition { get; }
    public Vector3 ImpactDirection { get; }

    public WorldObjectContext(WorldObject owner, int damage, bool hasImpact,
        Vector3 impactPosition, Vector3 impactDirection)
    {
        Owner = owner;
        Damage = damage;
        HasImpact = hasImpact;
        ImpactPosition = impactPosition;
        ImpactDirection = impactDirection;
    }

    public Vector3 GetEffectPosition(bool preferImpact) =>
        preferImpact && HasImpact ? ImpactPosition : Owner.CenterPosition;

    public Vector3 GetEffectDirection() =>
        ImpactDirection.sqrMagnitude > .0001f
            ? ImpactDirection.normalized : Vector3.up;
}

/// <summary>
/// Acao executada por um objeto de mundo ao entrar em um estado, receber um
/// golpe ou ser esgotado. Para criar uma nova, herde desta classe: ela aparece
/// sozinha no seletor de tipo do Inspector.
/// </summary>
[Serializable]
public abstract class WorldObjectReaction
{
    public abstract string DisplayName { get; }
    public abstract void Execute(WorldObjectContext context);
}

/// <summary>Emite particulas de combate (lascas, pecas, algodao...) a partir de um perfil.</summary>
[Serializable]
public sealed class EmitParticlesReaction : WorldObjectReaction
{
    [Tooltip("Perfil de particulas usado. Vazio usa o padrao do sistema (golpe ou morte, conforme a opcao abaixo).")]
    [SerializeField] private CombatParticleProfile profile;
    [Tooltip("Quando o perfil esta vazio, usa o padrao de morte (mais particulas e maiores) em vez do padrao de golpe.")]
    [SerializeField] private bool useDeathDefault;
    [Tooltip("Multiplica a quantidade de particulas calculada pelo perfil.")]
    [Min(0f)] [SerializeField] private float countMultiplier = 1f;
    [Tooltip("Dano usado para calcular quantidade e velocidade quando a reacao nao vem de um golpe (entrada de estado).")]
    [Min(0)] [SerializeField] private int fallbackDamage = 30;
    [Tooltip("Emite no ponto do impacto quando houver golpe; do contrario, no centro do objeto.")]
    [SerializeField] private bool atImpactPoint = true;

    public override string DisplayName => "Emitir particulas";

    public override void Execute(WorldObjectContext context)
    {
        int damage = context.Damage > 0 ? context.Damage : fallbackDamage;
        CombatParticleSystem.Instance.EmitWorldObjectParticles(profile,
            useDeathDefault, context.GetEffectPosition(atImpactPoint),
            context.GetEffectDirection(), damage, context.Owner.PrimaryRenderer,
            countMultiplier);
    }
}

/// <summary>Solta moedas (currency) como fragmentos que o jogador coleta.</summary>
[Serializable]
public sealed class DropCurrencyReaction : WorldObjectReaction
{
    [Tooltip("Valor minimo (X) e maximo (Y) de cada moeda solta.")]
    [SerializeField] private Vector2Int valuePerPiece = new(2, 5);
    [Tooltip("Quantidade de moedas soltas.")]
    [Min(1)] [SerializeField] private int pieceCount = 3;
    [Tooltip("Perfil visual das moedas. Vazio usa o padrao de morte do sistema.")]
    [SerializeField] private CombatParticleProfile profile;
    [Tooltip("Solta no ponto do impacto quando houver golpe; do contrario, no centro do objeto.")]
    [SerializeField] private bool atImpactPoint;

    public override string DisplayName => "Soltar dinheiro";

    public override void Execute(WorldObjectContext context)
    {
        int damage = Mathf.Max(1, context.Damage);
        CombatParticleSystem.Instance.EmitWorldObjectParticles(profile, true,
            context.GetEffectPosition(atImpactPoint), context.GetEffectDirection(),
            damage, context.Owner.PrimaryRenderer, 0f, valuePerPiece,
            Mathf.Max(1, pieceCount));
    }
}

/// <summary>Muda o objeto para outro estado (por nome).</summary>
[Serializable]
public sealed class ChangeStateReaction : WorldObjectReaction
{
    [Tooltip("Nome do estado de destino, exatamente como esta na lista de estados do objeto.")]
    [SerializeField] private string stateId;

    public override string DisplayName => "Mudar de estado";

    public override void Execute(WorldObjectContext context) =>
        context.Owner.SetState(stateId);
}

/// <summary>Troca este objeto por outro prefab na mesma posicao (ex.: caixa vira destrocos).</summary>
[Serializable]
public sealed class ReplaceWithPrefabReaction : WorldObjectReaction
{
    [Tooltip("Prefab que aparece no lugar deste objeto.")]
    [SerializeField] private GameObject replacement;
    [Tooltip("Destroi este objeto depois da troca.")]
    [SerializeField] private bool destroyOriginal = true;
    [Tooltip("Copia a escala deste objeto para o novo.")]
    [SerializeField] private bool inheritScale;

    public override string DisplayName => "Trocar por outro prefab";

    public override void Execute(WorldObjectContext context)
    {
        if (replacement == null)
            return;

        Transform source = context.Owner.transform;
        GameObject instance = UnityEngine.Object.Instantiate(replacement,
            source.position, source.rotation, source.parent);
        if (inheritScale)
            instance.transform.localScale = source.localScale;
        if (destroyOriginal)
            context.Owner.DestroyObject();
    }
}

/// <summary>Destroi o objeto (opcionalmente depois de um atraso).</summary>
[Serializable]
public sealed class DestroyObjectReaction : WorldObjectReaction
{
    [Tooltip("Atraso, em segundos, antes de destruir. Util para deixar uma animacao ou som terminar.")]
    [Min(0f)] [SerializeField] private float delay;

    public override string DisplayName => "Destruir objeto";

    public override void Execute(WorldObjectContext context) =>
        context.Owner.DestroyObject(delay);
}

/// <summary>Liga ou desliga outros objetos da cena (portas, luzes, visuais).</summary>
[Serializable]
public sealed class SetObjectsActiveReaction : WorldObjectReaction
{
    [Tooltip("Objetos que serao ativados.")]
    [SerializeField] private GameObject[] enable;
    [Tooltip("Objetos que serao desativados.")]
    [SerializeField] private GameObject[] disable;

    public override string DisplayName => "Ligar/desligar objetos";

    public override void Execute(WorldObjectContext context)
    {
        SetActive(enable, true);
        SetActive(disable, false);
    }

    private static void SetActive(GameObject[] targets, bool active)
    {
        if (targets == null)
            return;
        foreach (GameObject target in targets)
            if (target != null)
                target.SetActive(active);
    }
}

/// <summary>Toca um som no mundo.</summary>
[Serializable]
public sealed class PlaySoundReaction : WorldObjectReaction
{
    [Tooltip("Sons possiveis. Um deles e sorteado a cada execucao.")]
    [SerializeField] private AudioClip[] clips;
    [Tooltip("Volume do som.")]
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;

    public override string DisplayName => "Tocar som";

    public override void Execute(WorldObjectContext context)
    {
        if (clips == null || clips.Length == 0)
            return;

        AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
        if (clip != null)
            WorldAudio.PlayAt(clip, context.Owner.CenterPosition, volume);
    }
}

/// <summary>Dispara um UnityEvent (portas, animacoes, cutscenes, encontros...).</summary>
[Serializable]
public sealed class UnityEventReaction : WorldObjectReaction
{
    [Tooltip("Acoes executadas. Use para ligar o objeto a qualquer outro sistema sem codigo.")]
    [SerializeField] private UnityEvent onExecuted;

    public override string DisplayName => "Evento (UnityEvent)";

    public override void Execute(WorldObjectContext context) =>
        onExecuted?.Invoke();
}
