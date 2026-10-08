using UnityEngine;

// Adaptador para referencias existentes em PlayerHealth e PlayerGuard.
public class PlayerHeavyCombo : MonoBehaviour
{
    public void CancelCombo()
    {
        GetComponent<PlayerComboController>()?.CancelCombo();
    }
}
