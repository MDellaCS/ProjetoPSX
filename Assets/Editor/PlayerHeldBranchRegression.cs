#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Testes sem cena para as regras de ramificacoes por clique segurado.</summary>
public static class PlayerHeldBranchRegression
{
    [MenuItem("Tools/Tests/Run Player Held Branch Regression")]
    public static void Run()
    {
        try
        {
            var normal = new PlayerMoveTransition
            {
                input = PlayerAttackInput.Light,
                targetNodeId = "L2"
            };
            var held = new PlayerMoveTransition
            {
                input = PlayerAttackInput.Light,
                targetNodeId = "L1h",
                requiresHeldInput = true
            };
            var transitions = new List<PlayerMoveTransition>
            {
                normal,
                held
            };

            AssertSame("clique simples", normal,
                PlayerMovesetDefinition.FindTransition(transitions,
                    PlayerAttackInput.Light, false, false));
            AssertSame("branch segurada", held,
                PlayerMovesetDefinition.FindTransition(transitions,
                    PlayerAttackInput.Light, true, false));

            AssertEligible("mesmo clique segurado", true,
                PlayerHeldBranchRules.IsEligible(7, 7, true, false, .2f, .18f));
            AssertEligible("hold acumulado durante o ataque", true,
                PlayerHeldBranchRules.IsEligible(7, 7, true, false, .6f, .18f));
            AssertEligible("clique solto", false,
                PlayerHeldBranchRules.IsEligible(7, 7, false, false, .2f, .18f));
            AssertEligible("clique diferente", false,
                PlayerHeldBranchRules.IsEligible(7, 8, true, false, .2f, .18f));
            AssertEligible("limite de tempo ainda nao atingido", false,
                PlayerHeldBranchRules.IsEligible(7, 7, true, false, .17f, .18f));
            AssertEligible("branch ja consumida", false,
                PlayerHeldBranchRules.IsEligible(7, 7, true, true, .2f, .18f));

            Debug.Log("PLAYER HELD BRANCH REGRESSION PASS: clique simples, " +
                "hold bufferizado durante o ataque, release, nova press, " +
                "threshold e consumo unico.");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogError("PLAYER HELD BRANCH REGRESSION FAIL: " + exception);
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }

    private static void AssertSame(string label, object expected,
        object actual)
    {
        if (!ReferenceEquals(expected, actual))
            throw new System.Exception($"Transicao incorreta para {label}.");
    }

    private static void AssertEligible(string label, bool expected,
        bool actual)
    {
        if (actual != expected)
            throw new System.Exception($"Elegibilidade incorreta: {label}.");
    }
}
#endif
