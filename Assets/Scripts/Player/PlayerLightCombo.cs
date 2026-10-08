using UnityEngine;

// Mantido para cenas existentes. O moveset e controlado por PlayerComboController.
public class PlayerLightCombo : MonoBehaviour
{
    private void Awake()
    {
        if (GetComponent<PlayerComboController>() == null)
            gameObject.AddComponent<PlayerComboController>();
    }

    public void CancelCombo()
    {
        GetComponent<PlayerComboController>()?.CancelCombo();
    }
}
