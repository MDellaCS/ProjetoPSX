using UnityEngine;

/// <summary>
/// Raiz unica dos objetos que devem sobreviver a mudancas de cena.
/// Coloque Player, camera, HUDs e sistemas globais como filhos deste objeto.
/// </summary>
[DisallowMultipleComponent]
public sealed class PersistentGameRoot : MonoBehaviour
{
    public static PersistentGameRoot Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Este sistema e deliberadamente global: o Player pode trocar de
        // cena, entrar em stagger ou perder a IA do inimigo sem voltar a
        // permitir que duas entidades terrestres se empilhem.
        if (GetComponent<GroundEntitySeparationSystem>() == null)
            gameObject.AddComponent<GroundEntitySeparationSystem>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
