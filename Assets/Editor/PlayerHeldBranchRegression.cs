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

/// <summary>Verificacoes sem cena para as regras puras do frame do movimento.</summary>
public static class PlayerMovementFrameRegression
{
    [MenuItem("Tools/Tests/Run Player Movement Frame Regression")]
    public static void RunMovementFrameChecks()
    {
        try
        {
            Vector3 locomotion = new(1f, 2f, 3f);
            Vector3 external = new(-.25f, 0f, 4f);
            Assert(PlayerMovementFrameMath.CombineDisplacements(locomotion,
                external) == new Vector3(.75f, 2f, 7f),
                "A soma deve preservar os deslocamentos de locomocao e externos.");
            Assert(!PlayerMovementFrameMath.ShouldAllowGroundSnap(false, -.1f),
                "Uma entidade no ar nao pode receber ground snap.");
            Assert(!PlayerMovementFrameMath.ShouldAllowGroundSnap(true, .001f),
                "Um deslocamento ascendente nao pode receber ground snap.");
            Assert(PlayerMovementFrameMath.ShouldAllowGroundSnap(true, 0f),
                "Uma entidade apoiada sem subida pode receber ground snap.");

            Vector3 walkableSlope = Quaternion.AngleAxis(30f,
                Vector3.forward) * Vector3.up;
            Vector3 steepSlope = Quaternion.AngleAxis(60f,
                Vector3.forward) * Vector3.up;
            Assert(PlayerMovementFrameMath.IsGroundSupport(Vector3.up),
                "O plano horizontal deve contar como suporte.");
            Assert(!PlayerMovementFrameMath.IsGroundSupport(Vector3.right),
                "Uma parede vertical nao deve contar como suporte.");
            Assert(PlayerMovementFrameMath.IsGroundSupport(steepSlope) &&
                !PlayerMovementFrameMath.IsWalkableGround(steepSlope, 45f),
                "Uma rampa ingreme e contato, mas nao chao caminhavel.");
            Assert(PlayerMovementFrameMath.IsWalkableGround(walkableSlope,
                45f), "Uma rampa abaixo do limite deve ser caminhavel.");

            Debug.Log("PASS: PlayerMovement frame math regression.");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogError("FAIL: PlayerMovement frame math regression: " +
                exception);
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new System.InvalidOperationException(message);
    }
}

/// <summary>Verifica a classificacao dinamica de profundidade sem abrir cena.</summary>
public static class WaterDepthRegression
{
    [MenuItem("Tools/Tests/Run Water Depth Regression")]
    public static void Run()
    {
        WaterProfile profile = ScriptableObject.CreateInstance<WaterProfile>();
        try
        {
            profile.deepWaterMinDepth = 1.4f;
            profile.deepExitHysteresis = .15f;

            Assert(profile.ClassifyDepth(1.39f, false) == WaterZone.Shallow,
                "Coluna menor que o limite deve ser rasa.");
            Assert(profile.ClassifyDepth(1.4f, false) == WaterZone.Deep,
                "Coluna igual ao limite deve ser funda.");
            Assert(profile.ClassifyDepth(1.3f, true) == WaterZone.Deep,
                "Agua funda deve manter histerese ao ficar um pouco mais rasa.");
            Assert(profile.ClassifyDepth(1.24f, true) == WaterZone.Shallow,
                "Abaixo do limite com histerese a coluna deve voltar a rasa.");

            // Simula o mesmo volume enquanto um leito se eleva e depois desce.
            Assert(profile.ClassifyDepth(2f, false) == WaterZone.Deep &&
                profile.ClassifyDepth(.8f, false) == WaterZone.Shallow &&
                profile.ClassifyDepth(2f, false) == WaterZone.Deep,
                "Alterar a profundidade amostrada deve alterar a zona no mesmo volume.");

            Debug.Log("PASS: Water depth classification regression.");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogError("FAIL: Water depth classification regression: " +
                exception);
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }
        finally
        {
            Object.DestroyImmediate(profile);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new System.InvalidOperationException(message);
    }
}

#endif
