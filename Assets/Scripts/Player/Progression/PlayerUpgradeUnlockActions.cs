using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Conecta especiais da progressao a objetos, ataques e habilidades da cena/prefab.</summary>
[DisallowMultipleComponent]
public sealed class PlayerUpgradeUnlockActions : MonoBehaviour
{
    [Serializable]
    public sealed class Binding
    {
        [Tooltip("ID do especial configurado em um nivel do atributo, por exemplo rage.")]
        public string specialId;
        [Tooltip("Acoes executadas ao desbloquear ou restaurar um especial salvo. Devem poder ser reaplicadas, como ativar uma habilidade.")]
        public UnityEvent onUnlocked = new();
        [Tooltip("Acoes executadas se o especial deixar de estar disponivel, como desativar uma habilidade.")]
        public UnityEvent onLocked = new();
        [NonSerialized] public bool initialized;
        [NonSerialized] public bool unlocked;
    }
    [Tooltip("Ligacoes entre os especiais globais e comportamentos do Player. Cada ligacao reage somente quando o estado muda.")]
    [SerializeField] private List<Binding> bindings = new();
    private PlayerProgression progression;
    private void OnEnable()
    {
        progression = PlayerProgression.GetOrAdd(gameObject);
        progression.Initialize();
        progression.Changed += Refresh;
        foreach (Binding binding in bindings)
            if (binding != null) binding.initialized = false;
        Refresh();
    }
    private void OnDisable()
    {
        if (progression != null) progression.Changed -= Refresh;
    }
    private void Refresh()
    {
        foreach (Binding binding in bindings)
        {
            if (binding == null) continue;
            bool unlocked = progression.HasSpecial(binding.specialId);
            if (binding.initialized && binding.unlocked == unlocked) continue;
            binding.initialized = true;
            binding.unlocked = unlocked;
            if (unlocked) binding.onUnlocked?.Invoke();
            else binding.onLocked?.Invoke();
        }
    }
}
