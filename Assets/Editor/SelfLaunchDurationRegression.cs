#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
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
    private static float enemyStartHeight;
    private static float enemyPeakHeight;
    private static bool launched;
    private static bool launchMetricsCaptured;
    private static int jumpTestStage;
    private static float jumpTestStartTime;
    private static float jumpTestStartHeight;
    private static float doubleJumpStartHeight;
    private static float expectedJumpSpeed;
    private static SceneAsset previousPlayModeStartScene;

    static SelfLaunchDurationRegression()
    {
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Tools/Tests/Run Self Launch Duration Regression")]
    public static void Run()
    {
        Results.Clear();
        scenario = 0;
        previousPlayModeStartScene = EditorSceneManager.playModeStartScene;
        EditorSceneManager.playModeStartScene = null;
        EditorSceneManager.OpenScene("Assets/Scenes/999. TestArena.unity",
            OpenSceneMode.Single);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
            "Assets/Scenes/999. TestArena.unity");
        EditorPrefs.SetBool(RunKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode ||
            !EditorPrefs.GetBool(RunKey))
            return;

        EditorPrefs.SetBool(RunKey, false);
        EditorSceneManager.playModeStartScene = previousPlayModeStartScene;
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
        {
            enemyLaunched = true;
            enemyPeakHeight = Mathf.Max(enemyPeakHeight,
                dummy.transform.position.y);
        }

        if (!launched)
            return;

        if (!launchMetricsCaptured)
        {
            peakHeight = Mathf.Max(peakHeight, player.transform.position.y);
            if (Time.time - launchTime < 1.1f)
                return;

            Results.Add($"Active {ActiveDurations[scenario]:0.00}s: ataque ativo=" +
            $"{attacks.IsAttacking}, pico de subida " +
            $"{peakHeight - startHeight:0.000}m, " +
            $"posicao final {player.transform.position.y - startHeight:0.000}m, " +
                $"pico do inimigo {enemyPeakHeight - enemyStartHeight:0.000}m, " +
            $"inimigo lancado={enemyLaunched}, " +
            $"velocidade vertical {movement.VerticalSpeed:0.000}m/s.");
            if (!enemyLaunched)
                Results.Add("FAIL: o inimigo nao recebeu Launch Up no TestArena.");
            else if (Mathf.Abs((peakHeight - startHeight) -
                (enemyPeakHeight - enemyStartHeight)) > .2f)
                Results.Add("FAIL: os picos do player e do inimigo divergiram mais de 0,2m.");
            else
                Results.Add("PASS: Launch Up do player e do inimigo atingiu alturas equivalentes no TestArena.");
            launchMetricsCaptured = true;
        }

        if (jumpTestStage < 4 && !RunDoubleJumpSceneCheck())
            return;

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
        {
            Fail("TestArena nao contem Practice Dummy ativo.");
            return;
        }
        targetMovement = dummy.GetComponent<EnemyMovement>();
        dummyStartPosition = dummy.transform.position;
        enemyStartHeight = dummyStartPosition.y;
        enemyPeakHeight = enemyStartHeight;
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
        launchMetricsCaptured = false;
        enemyLaunched = false;
        jumpTestStage = 0;
        jumpTestStartTime = 0f;
        jumpTestStartHeight = 0f;
        doubleJumpStartHeight = 0f;
        expectedJumpSpeed = 0f;
    }

    private static bool RunDoubleJumpSceneCheck()
    {
        PlayerInputBuffer buffer = player.GetComponent<PlayerInputBuffer>();
        MethodInfo queueJump = typeof(PlayerInputBuffer).GetMethod("Buffer",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo startJump = typeof(PlayerMovement).GetMethod("TryStartJump",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo jumpsUsed = typeof(PlayerMovement).GetField("jumpsUsed",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (buffer == null || queueJump == null || startJump == null ||
            jumpsUsed == null)
        {
            Fail("Nao foi possivel acessar a entrada de pulo para a verificacao em cena.");
            return false;
        }

        if (jumpTestStage == 0)
        {
            jumpTestStartHeight = player.transform.position.y;
            InvokeBufferJump(queueJump, buffer);
            InvokeWithDefaultParameters(startJump, movement);
            expectedJumpSpeed = movement.VerticalSpeed;
            if (expectedJumpSpeed <= 0f || (int)jumpsUsed.GetValue(movement) != 1)
            {
                Fail("O primeiro pulo em cena nao iniciou corretamente.");
                return false;
            }
            jumpTestStartTime = Time.time;
            jumpTestStage = 1;
            return false;
        }

        if (jumpTestStage == 1 && Time.time - jumpTestStartTime >= .12f)
        {
            if (player.transform.position.y <= jumpTestStartHeight + .1f)
            {
                Fail("O jogador nao ganhou altura no primeiro pulo em cena.");
                return false;
            }
            InvokeBufferJump(queueJump, buffer);
            InvokeWithDefaultParameters(startJump, movement);
            if ((int)jumpsUsed.GetValue(movement) != 2 ||
                Mathf.Abs(movement.VerticalSpeed - expectedJumpSpeed) > .05f)
            {
                Fail("O segundo pulo nao reiniciou com a mesma velocidade inicial.");
                return false;
            }
            doubleJumpStartHeight = player.transform.position.y;
            jumpTestStartTime = Time.time;
            jumpTestStage = 2;
            return false;
        }

        if (jumpTestStage == 2 && Time.time - jumpTestStartTime >= .12f)
        {
            if (player.transform.position.y <= doubleJumpStartHeight + .1f)
            {
                Fail("O pulo duplo nao deslocou o jogador para cima.");
                return false;
            }
            jumpTestStartTime = Time.time;
            jumpTestStage = 3;
            return false;
        }

        if (jumpTestStage == 3)
        {
            if (movement.IsGrounded && movement.VerticalSpeed <= 0f)
            {
                Results.Add("PASS: pulo duplo iniciou e ambos os impulsos moveram o jogador em TestArena.");
                jumpTestStage = 4;
                return true;
            }
            if (Time.time - jumpTestStartTime > 2f)
            {
                Fail("O jogador nao pousou apos o pulo duplo em TestArena.");
                return false;
            }
        }

        return jumpTestStage >= 4;
    }

    private static void InvokeBufferJump(MethodInfo bufferMethod,
        PlayerInputBuffer buffer)
    {
        ParameterInfo[] parameters = bufferMethod.GetParameters();
        object[] arguments = CreateDefaultArguments(parameters);
        arguments[0] = PlayerBufferedInput.Jump;
        bufferMethod.Invoke(buffer, arguments);
    }

    private static void InvokeWithDefaultParameters(MethodInfo method,
        object target) => method.Invoke(target,
            CreateDefaultArguments(method.GetParameters()));

    private static object[] CreateDefaultArguments(ParameterInfo[] parameters)
    {
        object[] arguments = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
            arguments[i] = parameters[i].HasDefaultValue
                ? parameters[i].DefaultValue
                : parameters[i].ParameterType.IsValueType
                    ? System.Activator.CreateInstance(parameters[i].ParameterType)
                    : null;
        return arguments;
    }

    private static void Finish()
    {
        if (Results.Count < 4)
            Results.Add("INCOMPLETO: um dos ataques nao iniciou o SelfLaunchUp.");
        if (Results.Count >= 4)
        {
            float shortPeak = GetPeak(Results, "Active 0");
            float longPeak = GetPeak(Results, "Active 1");
            if (float.IsInfinity(shortPeak) || float.IsInfinity(longPeak))
                Results.Add("FAIL: nao foi possivel comparar os dois picos de lancamento.");
            else if (Mathf.Abs(shortPeak - longPeak) > .2f)
                Results.Add($"FAIL: diferenca de pico acima de tolerancia: " +
                    $"{Mathf.Abs(shortPeak - longPeak):0.000}m.");
            else
                Results.Add($"PASS: diferenca de pico {Mathf.Abs(shortPeak - longPeak):0.000}m.");
            if (!enemyLaunched && !Results.Exists(result => result.StartsWith("FAIL:")))
                Results.Add("FAIL: o inimigo nao recebeu Launch Up no TestArena.");
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
        EditorSceneManager.playModeStartScene = previousPlayModeStartScene;
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
            if (float.TryParse(number.Replace(',', '.'),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out float value))
                return value;
            return float.PositiveInfinity;
        }
        return float.PositiveInfinity;
    }

    private static bool enemyLaunched;

    /// <summary>Validacao sem cena da balistica e do pulo variavel.</summary>
    public static void RunProfileMathRegression()
    {
        try
        {
            const float height = 3.5f;
            float gravity = -60f;
            const float holdDuration = .25f;
            const float heldGravityMultiplier = .35f;
            float launchSpeed = PlayerMovement.CalculateJumpInitialSpeed(
                height, gravity, holdDuration, heldGravityMultiplier);
            float fullHoldPeak = CalculatePeak(height, gravity,
                holdDuration, heldGravityMultiplier, launchSpeed, holdDuration);
            float shortTapPeak = CalculatePeak(height, gravity,
                holdDuration, heldGravityMultiplier, launchSpeed, 0f);
            float mediumHoldPeak = CalculatePeak(height, gravity,
                holdDuration, heldGravityMultiplier, launchSpeed, .1f);
            AssertClose("altura maxima com pulo segurado", height,
                fullHoldPeak, .002f);
            if (shortTapPeak >= mediumHoldPeak ||
                mediumHoldPeak >= fullHoldPeak || shortTapPeak <= 0f)
                throw new System.Exception("Segurar o pulo nao aumentou a altura de forma fisica.");

            float descentDuration = Mathf.Sqrt(2f * height / -gravity);
            AssertClose("queda sob gravidade", .341565f,
                descentDuration, .001f);

            PlayerAttackDefinition d1 = AssetDatabase.LoadAssetAtPath<
                PlayerAttackDefinition>("Assets/GameData/Player/Moveset/Normal/D1.asset");
            if (d1 == null || Mathf.Abs(d1.activeDuration - .13f) > .0001f)
                throw new System.Exception("D1 nao manteve Active em 0,13s.");

            PlayerMovesetDefinition normalMoveset = AssetDatabase.LoadAssetAtPath<
                PlayerMovesetDefinition>("Assets/GameData/Player/Moveset/Normal/NormalMoveset.asset");
            PlayerMovesetDefinition humanMoveset = AssetDatabase.LoadAssetAtPath<
                PlayerMovesetDefinition>("Assets/GameData/Player/Moveset/Human/HumanMoveset.asset");
            if (normalMoveset == null || humanMoveset == null ||
                Mathf.Abs(normalMoveset.jumpHeightMultiplier - 1f) > .0001f ||
                Mathf.Abs(normalMoveset.jumpDurationMultiplier - 1f) > .0001f ||
                Mathf.Abs(humanMoveset.jumpHeightMultiplier - 1f) > .0001f ||
                Mathf.Abs(humanMoveset.jumpDurationMultiplier - .85f) > .0001f)
                throw new System.Exception("Os modificadores dos movesets nao preservam os valores globais/anteriores.");

            Debug.Log("PHYSICAL VARIABLE JUMP REGRESSION PASS: " +
                $"altura segurando {fullHoldPeak:0.000}m; " +
                $"toque curto {shortTapPeak:0.000}m; " +
                $"hold parcial {mediumHoldPeak:0.000}m; " +
                $"queda por gravity {gravity:0.0} em {descentDuration:0.000}s; " +
                $"D1 Active {d1.activeDuration:0.00}s; " +
                $"multiplicadores Normal 1/1 e Human 1/0,85.");
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

    private static float CalculatePeak(float maximumHeight, float gravity,
        float holdDuration, float heldGravityMultiplier, float launchSpeed,
        float actualHoldTime)
    {
        float downGravity = -gravity;
        float heldGravity = downGravity * heldGravityMultiplier;
        float heldTime = Mathf.Min(actualHoldTime, holdDuration,
            launchSpeed / heldGravity);
        float position = launchSpeed * heldTime -
            .5f * heldGravity * heldTime * heldTime;
        float velocity = launchSpeed - heldGravity * heldTime;
        if (actualHoldTime >= launchSpeed / heldGravity)
            return position;
        return position + velocity * velocity / (2f * downGravity);
    }

}
#endif
