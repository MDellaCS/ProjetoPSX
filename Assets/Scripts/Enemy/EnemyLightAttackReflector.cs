using UnityEngine;

/// <summary>Opcionalmente reflete ataques leves e aplica LightStagger ao jogador.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Projeto PSX/Enemy/Light Attack Reflector")]
public sealed class EnemyLightAttackReflector : MonoBehaviour
{
    [Tooltip("Quando ativo, ataques leves nao causam dano ao inimigo e aplicam LightStagger ao jogador atacante.")]
    [SerializeField] private bool lightAttackReflective = true;
    [Tooltip("AudioSource opcional usado para tocar o som da reflexao. Se vazio, o som toca na posicao do inimigo.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Som tocado quando um ataque leve e refletido. Vazio desativa o feedback sonoro.")]
    [SerializeField] private AudioClip reflectSound;
    [Tooltip("Volume do som de reflexao, de zero a um.")]
    [Range(0f, 1f)] [SerializeField] private float reflectSoundVolume = 1f;

    public bool LightAttackReflective => lightAttackReflective;

    internal bool TryReflect(PlayerAttackType attackType, Transform attacker)
    {
        if (!lightAttackReflective || !IsLightAttack(attackType))
            return false;

        if (attacker != null)
        {
            PlayerHealth playerHealth = attacker.GetComponent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = attacker.GetComponentInParent<PlayerHealth>();
            playerHealth?.ApplyReflectedLightAttackStagger(transform.position);
        }

        PlayReflectSound();
        return true;
    }

    private static bool IsLightAttack(PlayerAttackType attackType) =>
        attackType == PlayerAttackType.Light ||
        attackType == PlayerAttackType.AerialLight ||
        attackType == PlayerAttackType.DashLight;

    private void PlayReflectSound()
    {
        if (reflectSound == null || reflectSoundVolume <= 0f)
            return;

        if (audioSource != null)
            audioSource.PlayOneShot(reflectSound, reflectSoundVolume);
        else
            AudioSource.PlayClipAtPoint(reflectSound, transform.position,
                reflectSoundVolume);
    }
}
