using UnityEditor;
using UnityEngine;

public static class EnemyCombatSetupEditor
{
    [MenuItem("Tools/Projeto PSX/Enemy Combat/Configure Selected Enemy")]
    private static void ConfigureSelectedEnemy()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null || selected.GetComponentInParent<EnemyBase>() == null)
            return;
        GameObject root = selected.GetComponentInParent<EnemyBase>().gameObject;
        if (root.GetComponent<EnemyAttackMovement>() == null)
            Undo.AddComponent<EnemyAttackMovement>(root);
        if (root.GetComponent<EnemyAttackHitboxController>() == null)
            Undo.AddComponent<EnemyAttackHitboxController>(root);
        if (root.GetComponent<EnemyAttackPreview>() == null)
            Undo.AddComponent<EnemyAttackPreview>(root);
        EditorUtility.SetDirty(root);
    }

    [MenuItem("Tools/Projeto PSX/Enemy Combat/Configure Selected Enemy", true)]
    private static bool ValidateConfigure() => Selection.activeGameObject != null &&
        Selection.activeGameObject.GetComponentInParent<EnemyBase>() != null;
}
