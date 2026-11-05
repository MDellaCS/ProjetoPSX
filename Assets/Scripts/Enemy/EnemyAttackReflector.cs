using UnityEngine;

/// <summary>Configura quais ataques do jogador o inimigo reflete e as reacoes causadas.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Projeto PSX/Enemy/Attack Reflector")]
public sealed class EnemyAttackReflector : MonoBehaviour
{
    [Header("Ataques refletidos")]
    [Tooltip("Quando ativo, ataques leves do jogador nao causam dano e aplicam LightStagger ao atacante.")]
    [SerializeField] private bool reflectLightAttacks = true;
    [Tooltip("Quando ativo, ataques pesados do jogador tambem sao refletidos e nao causam dano.")]
    [SerializeField] private bool reflectHeavyAttacks;

    [Header("Stagger do inimigo")]
    [Tooltip("Quando ativo, o proprio inimigo entra em stagger ao refletir um ataque leve, sem depender de poise.")]
    [SerializeField] private bool staggerEnemyWhenReflectingLightAttacks;
    [Tooltip("Quando ativo, o proprio inimigo entra em stagger ao refletir um ataque pesado, sem depender de poise.")]
    [SerializeField] private bool staggerEnemyWhenReflectingHeavyAttacks;

    [Header("Audio")]
    [Tooltip("AudioSource opcional usado para tocar o som da reflexao. Se vazio, o som toca na posicao do inimigo.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Som tocado quando um ataque e refletido. Vazio desativa o feedback sonoro.")]
    [SerializeField] private AudioClip reflectSound;
    [Tooltip("Volume do som de reflexao, de zero a um.")]
    [Range(0f, 1f)] [SerializeField] private float reflectSoundVolume = 1f;

    internal bool TryReflect(PlayerAttackType attackType, Transform attacker,
        Vector3 attackerPosition)
    {
        bool isLight = IsLightAttack(attackType);
        bool isHeavy = IsHeavyAttack(attackType);
        if ((!isLight || !reflectLightAttacks) &&
            (!isHeavy || !reflectHeavyAttacks))
            return false;

        if (isLight && attacker != null)
        {
            PlayerHealth playerHealth = attacker.GetComponent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = attacker.GetComponentInParent<PlayerHealth>();
            playerHealth?.ApplyReflectedLightAttackStagger(transform.position);
        }

        bool staggerReflector = isLight
            ? staggerEnemyWhenReflectingLightAttacks
            : staggerEnemyWhenReflectingHeavyAttacks;
        if (staggerReflector)
            GetComponent<EnemyHealth>()?.ApplyPoiseStagger(attackerPosition,
                false);

        PlayReflectSound();
        return true;
    }

    private static bool IsLightAttack(PlayerAttackType attackType) =>
        attackType == PlayerAttackType.Light ||
        attackType == PlayerAttackType.AerialLight ||
        attackType == PlayerAttackType.DashLight;

    private static bool IsHeavyAttack(PlayerAttackType attackType) =>
        attackType == PlayerAttackType.Heavy ||
        attackType == PlayerAttackType.AerialHeavy ||
        attackType == PlayerAttackType.DashHeavy;

    private void PlayReflectSound()
    {
        if (reflectSound == null || reflectSoundVolume <= 0f)
            return;

        if (audioSource != null)
            audioSource.PlayOneShot(reflectSound, reflectSoundVolume);
        else
            WorldAudio.PlayAt(reflectSound, transform.position, reflectSoundVolume);
    }
}
