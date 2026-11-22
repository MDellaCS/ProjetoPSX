using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Faz o Inspector mostrar um seletor de tipo para campos [SerializeReference].</summary>
public sealed class ReferenceTypePickerAttribute : PropertyAttribute { }

/// <summary>O que acontece quando o modelo de dano do estado e esgotado.</summary>
public enum WorldObjectDepletedAction
{
    /// <summary>Nada alem das reacoes "Ao ser esgotado".</summary>
    None,
    /// <summary>Muda para o estado indicado em "Proximo estado".</summary>
    GoToState,
    /// <summary>Destroi o objeto.</summary>
    DestroyObject,
    /// <summary>Desativa o objeto (pode ser reativado por outro sistema).</summary>
    DisableObject
}

/// <summary>
/// Um estado de gameplay de um objeto de mundo: ele define se o objeto pode ser
/// atingido, como o dano e medido e o que
/// acontece ao entrar, ao ser atingido e ao ser esgotado.
/// </summary>
[Serializable]
public sealed class WorldObjectState
{
    [Header("Identidade")]
    [Tooltip("Nome unico do estado dentro deste objeto. Usado por 'Mudar de estado' e pelo estado inicial.")]
    public string id = "Intacto";

    [Header("Comportamento")]
    [Tooltip("Se desmarcado, golpes atravessam o objeto: ele nao recebe dano, nao gera hitstop nem reacoes.")]
    public bool hittable = true;

    [Header("Visual")]
    [Tooltip("Objetos filhos (ou da cena) ativados ao entrar neste estado. Ex.: a malha 'rachada'.")]
    public GameObject[] activateOnEnter;
    [Tooltip("Objetos desativados ao entrar neste estado. Ex.: a malha 'intacta'.")]
    public GameObject[] deactivateOnEnter;

    [Header("Dano")]
    [Tooltip("Como o dano e medido neste estado: vida, dano acumulado em X tempo ou dano em um unico golpe. Vazio = o objeto recebe golpes mas nunca e esgotado.")]
    [SerializeReference, ReferenceTypePicker]
    public WorldDamageModel damageModel;

    [Header("Reacoes")]
    [Tooltip("Executadas ao entrar neste estado.")]
    [SerializeReference, ReferenceTypePicker]
    public List<WorldObjectReaction> onEnter = new();
    [Tooltip("Executadas a cada golpe aceito (particulas, dinheiro, som...).")]
    [SerializeReference, ReferenceTypePicker]
    public List<WorldObjectReaction> onHit = new();
    [Tooltip("Executadas quando o modelo de dano e esgotado, antes da acao abaixo.")]
    [SerializeReference, ReferenceTypePicker]
    public List<WorldObjectReaction> onDepleted = new();

    [Header("Ao ser esgotado")]
    [Tooltip("O que fazer depois das reacoes 'Ao ser esgotado'.")]
    public WorldObjectDepletedAction depletedAction = WorldObjectDepletedAction.DestroyObject;
    [Tooltip("Estado de destino quando a acao for 'Go To State'.")]
    public string nextStateId;
}
