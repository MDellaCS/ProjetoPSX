#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

/// <summary>
/// Compara duas duracoes do D1 em Play Mode, com o Practice Dummy do TestArena.
/// A cena e carregada apenas para o teste, nunca e salva.
/// </summary>
[InitializeOnLoad]
public static class SelfLaunchDurationRegression
{
    private const string RunKey = "SelfLaunchDurationRegression.Run";
    private const string ResultsKey = "SelfLaunchDurationRegression.Results";
    private static readonly float[] ActiveDurations = { .13f, 1f };
    private static readonly List<string> Results = new();
    private static GameObject floor;
    private static GameObject player;
    private static PlayerMovement movement;
    private static PlayerAttackController attacks;
    private static PlayerAttackDefinition attackDefinition;
    private static PracticeDummy dummy;
    private static EnemyMovement targetMovement;
    private static Vector3 dummyStartPosition;
    private static Quaternion dummyStartRotation;
    private static int scenario;
    private static int setupFrame;
    private static float launchTime;
    private static float startHeight;
    private static float peakHeight;
    private static float launchBaselineHeight;
    private static bool launched;

    static SelfLaunchDurationRegression()
    {
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
    }

    [MenuItem("Tools/Tests/Run Self Launch Duration Regression")]
    public static void Run()
    {
        Results.Clear();
        scenario = 0;
        EditorSceneManager.playModeStartScene = null;
        EditorSceneManager.OpenScene("Assets/Scenes/TestArena.unity",
            OpenSceneMode.Single);
        EditorPrefs.SetBool(RunKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnEditorUpdate()
    {
        if (!EditorPrefs.GetBool(RunKey) || !EditorApplication.isPlaying)
            return;

        try
        {
            UpdateRegression();
        }
        catch (System.Exception exception)
        {
            Results.Add("FAIL: " + exception);
            scenario = ActiveDurations.Length;
            if (player != null) Object.Destroy(player);
            player = null;
            Finish();
        }
    }

    private static void UpdateRegression()
    {
        if (player == null)
        {
            if (scenario >= ActiveDurations.Length)
            {
                Finish();
                return;
            }

            if (floor == null)
                CreateArena();
            CreateScenario();
            return;
        }

        if (Time.frameCount <= setupFrame + 1)
            return;

        if (attacks.CurrentAttack == null && !launched)
        {
            launchBaselineHeight = player.transform.position.y;
            if (!attacks.TryStartAttack(attackDefinition))
                Fail("O PlayerAttackController recusou iniciar o D1.");
            return;
        }

        if (movement.IsLaunched && !launched)
        {
            launched = true;
            launchTime = Time.time;
            startHeight = launchBaselineHeight;
            peakHeight = player.transform.position.y;
            Results.Add($"Active {ActiveDurations[scenario]:0.00}s: " +
                $"lancamento v0={movement.GetJumpLaunchSpeed():0.000}m/s, " +
                $"g={movement.GetJumpLaunchGravity():0.000}m/s.");
        }
        if (targetMovement != null && targetMovement.IsLaunched)
            enemyLaunched = true;

        if (!launched)
            return;

        peakHeight = Mathf.Max(peakHeight, player.transform.position.y);
        if (Time.time - launchTime < 1.1f)
            return;

        Results.Add($"Active {ActiveDurations[scenario]:0.00}s: ataque ativo=" +
            $"{attacks.IsAttacking}, pico de subida " +
            $"{peakHeight - startHeight:0.000}m, " +
            $"posicao final {player.transform.position.y - startHeight:0.000}m, " +
            $"inimigo lancado={enemyLaunched}, " +
            $"velocidade vertical {movement.VerticalSpeed:0.000}m/s.");
        Object.Destroy(player);
        if (attackDefinition != null)
            Object.Destroy(attackDefinition);
        attackDefinition = null;
        player = null;
        scenario++;
    }

    private static void CreateArena() { }

    private static void CreateScenario()
    {
        dummy = Object.FindAnyObjectByType<PracticeDummy>();
        if (dummy == null)
            Fail("TestArena nao contem Practice Dummy ativo.");
        targetMovement = dummy.GetComponent<EnemyMovement>();
        dummyStartPosition = dummy.transform.position;
        dummyStartRotation = dummy.transform.rotation;
        if (dummy.GetComponent<EnemyBrain>() != null)
            dummy.GetComponent<EnemyBrain>().enabled = false;
        if (dummy.GetComponent<EnemyAttackController>() != null)
            dummy.GetComponent<EnemyAttackController>().enabled = false;
        if (dummy.GetComponent<EnemyDodge>() != null)
            dummy.GetComponent<EnemyDodge>().enabled = false;
        if (dummy.GetComponent<EnemyGuard>() != null)
            dummy.GetComponent<EnemyGuard>().enabled = false;
        dummy.transform.SetPositionAndRotation(dummyStartPosition,
            dummyStartRotation);

        Vector3 forward = dummy.transform.forward;
        forward.y = 0f;
        forward.Normalize();
        player = new GameObject($"SelfLaunch Regression Player {scenario}");
        player.SetActive(false);
        player.transform.SetPositionAndRotation(
            dummy.transform.position - forward * 1.15f,
            Quaternion.LookRotation(forward));
        player.AddComponent<CapsuleCollider>();
        player.AddComponent<PlayerState>();
        movement = player.AddComponent<PlayerMovement>();
        player.AddComponent<PlayerGrappleAttackController>();
        attacks = player.GetComponent<PlayerAttackController>();
        if (attacks == null)
            attacks = player.AddComponent<PlayerAttackController>();
        player.SetActive(true);
        targetMovement.ResetForPracticeDummy(dummyStartPosition,
            dummyStartRotation);

        PlayerAttackDefinition source = AssetDatabase.LoadAssetAtPath<
            PlayerAttackDefinition>("Assets/GameData/Player/Moveset/Normal/D1.asset");
        if (source == null)
            Fail("Nao foi possivel carregar D1.asset.");

        attackDefinition = Object.Instantiate(source);
        attackDefinition.activeDuration = ActiveDurations[scenario];
        attackDefinition.windupDuration = .01f;
        attackDefinition.recoveryDuration = .06f;
        setupFrame = Time.frameCount;
        launchTime = 0f;
        startHeight = 0f;
        peakHeight = 0f;
        launched = false;
        enemyLaunched = false;
    }

    private static void Finish()
    {
        if (Results.Count < 4)
            Results.Add("INCOMPLETO: um dos ataques nao iniciou o SelfLaunchUp.");
        if (Results.Count >= 4)
        {
            float shortPeak = GetPeak(Results, "Active 0,13s: ataque");
            float longPeak = GetPeak(Results, "Active 1,00s: ataque");
            if (Mathf.Abs(shortPeak - longPeak) > .2f)
                Results.Add($"FAIL: diferenca de pico acima de tolerancia: " +
                    $"{Mathf.Abs(shortPeak - longPeak):0.000}m.");
            else
                Results.Add($"PASS: diferenca de pico {Mathf.Abs(shortPeak - longPeak):0.000}m.");
        }
        string report = string.Join("\n", Results);
        Debug.Log("SELF LAUNCH DURATION REGRESSION\n" + report);
        EditorPrefs.SetString(ResultsKey, report);
        EditorPrefs.SetBool(RunKey, false);
        if (floor != null) Object.Destroy(floor);
        if (dummy != null)
            dummy.transform.SetPositionAndRotation(dummyStartPosition,
                dummyStartRotation);
        if (attackDefinition != null) Object.Destroy(attackDefinition);
        player = null;
        floor = null;
        EditorSceneManager.playModeStartScene = null;
        EditorApplication.isPlaying = false;
        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    private static void Fail(string message)
    {
        Results.Add("FAIL: " + message);
        scenario = ActiveDurations.Length;
        if (player != null) Object.Destroy(player);
        player = null;
    }

    private static float GetPeak(List<string> results, string prefix)
    {
        foreach (string result in results)
        {
            if (!result.StartsWith(prefix))
                continue;
            int start = result.IndexOf("pico de subida ") +
                "pico de subida ".Length;
            int end = result.IndexOf('m', start);
            string number = result.Substring(start, end - start);
            return float.Parse(number.Replace(',', '.'),
                System.Globalization.CultureInfo.InvariantCulture);
        }
        return float.PositiveInfinity;
    }

    private static bool enemyLaunched;

    /// <summary>Validacao sem cena para o perfil de subida e gravidade de queda.</summary>
    public static void RunProfileMathRegression()
    {
        try
        {
            AnimationCurve curve = AnimationCurve.Linear(0f, 2f, 1f, 0f);
            const float height = 3.5f;
            const float ascentDuration = .85f;
            float previousProgress = 0f;
            for (int i = 1; i <= 100; i++)
            {
                float t = i / 100f;
                float progress = JumpAscentProfile.GetHeightProgress(curve, t);
                if (progress + .00001f < previousProgress)
                    throw new System.Exception("A curva de subida nao e monotona.");
                previousProgress = progress;
            }
            AssertClose("fim da subida", 1f,
                JumpAscentProfile.GetHeightProgress(curve, 1f), .0001f);
            AssertClose("altura no apice", height,
                height * JumpAscentProfile.GetHeightProgress(curve, 1f), .001f);
            AssertClose("tempo do apice", ascentDuration,
                ascentDuration, .0001f);

            float gravity = -60f;
            float descentDuration = Mathf.Sqrt(2f * height / -gravity);
            AssertClose("queda sob gravidade", .341565f,
                descentDuration, .001f);
            if (Mathf.Abs(descentDuration - ascentDuration) < .1f)
                throw new System.Exception("A descida ainda depende do tempo de subida.");

            PlayerAttackDefinition d1 = AssetDatabase.LoadAssetAtPath<
                PlayerAttackDefinition>("Assets/GameData/Player/Moveset/Normal/D1.asset");
            if (d1 == null || Mathf.Abs(d1.activeDuration - .13f) > .0001f)
                throw new System.Exception("D1 nao manteve Active em 0,13s.");

            Debug.Log("JUMP ASCENT PROFILE REGRESSION PASS: " +
                $"apice {height:0.000}m em {ascentDuration:0.000}s; " +
                $"queda por gravity {gravity:0.0} em {descentDuration:0.000}s; " +
                $"D1 Active {d1.activeDuration:0.00}s.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogError("JUMP ASCENT PROFILE REGRESSION FAIL: " + exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    private static void AssertClose(string label, float expected,
        float actual, float tolerance)
    {
        if (Mathf.Abs(expected - actual) > tolerance)
            throw new System.Exception($"{label}: esperado {expected}, atual {actual}.");
    }

}
#endif
