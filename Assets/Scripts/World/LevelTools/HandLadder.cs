using System.Collections.Generic;
using UnityEngine;

public enum HandLadderAttachment
{
    /// <summary>Escada solta no espaco (pode ser escalada pelos dois lados).</summary>
    Free,
    /// <summary>Escada encostada em uma parede/objeto; o topo sai para tras, sobre a borda.</summary>
    Surface
}

/// <summary>
/// Perfil da superficie ao longo da escada, em espaco local: tres faixas
/// (esquerda, centro, direita) e a normal central em varias alturas. Permite a
/// escada acompanhar objetos curvos (rochas, colunas).
/// </summary>
[System.Serializable]
public sealed class HandLadderSpine
{
    public float[] progress;
    public Vector3[] left;
    public Vector3[] center;
    public Vector3[] right;
    public Vector3[] normal;
    public float sampledWidth;

    public int Count => progress != null ? progress.Length : 0;

    public bool ApproximatelyEquals(HandLadderSpine other, float tolerance = .004f)
    {
        if (other == null || other.Count != Count)
            return false;
        for (int i = 0; i < Count; i++)
            if (Mathf.Abs(progress[i] - other.progress[i]) > tolerance ||
                (left[i] - other.left[i]).sqrMagnitude > tolerance * tolerance ||
                (center[i] - other.center[i]).sqrMagnitude > tolerance * tolerance ||
                (right[i] - other.right[i]).sqrMagnitude > tolerance * tolerance)
                return false;
        return true;
    }

    // Remove amostras apenas quando as tres faixas e a normal continuam
    // suficientemente proximas da interpolacao entre as extremidades.
    public HandLadderSpine Simplified(Matrix4x4 localToWorld)
    {
        if (Count <= 2) return this;
        bool[] keep = new bool[Count];
        keep[0] = keep[Count - 1] = true;
        var ranges = new Stack<(int first, int last)>();
        ranges.Push((0, Count - 1));
        while (ranges.Count > 0)
        {
            var (first, last) = ranges.Pop();
            float span = progress[last] - progress[first];
            float worst = 1f;
            int split = -1;
            for (int i = first + 1; i < last; i++)
            {
                float t = span > .000001f ? (progress[i] - progress[first]) / span : 0f;
                float error = Mathf.Max(Mathf.Max(
                    localToWorld.MultiplyVector(left[i] - Vector3.Lerp(left[first], left[last], t)).magnitude,
                    localToWorld.MultiplyVector(center[i] - Vector3.Lerp(center[first], center[last], t)).magnitude),
                    localToWorld.MultiplyVector(right[i] - Vector3.Lerp(right[first], right[last], t)).magnitude) / .01f;
                float normalError = Vector3.Angle(normal[i], Vector3.Slerp(normal[first], normal[last], t)) / 2f;
                error = Mathf.Max(error, normalError);
                if (error > worst) { worst = error; split = i; }
            }
            if (split < 0) continue;
            keep[split] = true;
            ranges.Push((first, split));
            ranges.Push((split, last));
        }
        int count = 0;
        for (int i = 0; i < keep.Length; i++) if (keep[i]) count++;
        HandLadderSpine result = new() { sampledWidth = sampledWidth, progress = new float[count],
            left = new Vector3[count], center = new Vector3[count], right = new Vector3[count], normal = new Vector3[count] };
        for (int i = 0, j = 0; i < keep.Length; i++)
        {
            if (!keep[i]) continue;
            result.progress[j] = progress[i]; result.left[j] = left[i]; result.center[j] = center[i];
            result.right[j] = right[i]; result.normal[j] = normal[i]; j++;
        }
        return result;
    }
}

public enum HandLadderExitSide
{
    /// <summary>Escada solta sai pela frente; escada em superficie sai por tras no topo.</summary>
    Auto,
    Front,
    Back
}

/// <summary>Posicao do jogador na escada: lado, altura e deslocamento lateral.</summary>
public struct HandLadderGrip
{
    public int side;
    public float progress;
    public float lateral;
}

/// <summary>
/// Escada de mao. Basta adicionar o componente, ajustar Height (e Width) e
/// posicionar: nao precisa de colliders nem de objetos filhos. A zona de
/// entrada, os pontos de saida e o salto/queda sao calculados a partir da
/// propria escada e desenhados como gizmos. A escada pode ser Free (solta) ou
/// Surface (encaixada em uma parede), e o visual pode ser gerado pelo perfil ou
/// ser sua propria malha (deixe o perfil vazio).
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Projeto PSX/Level Design/Hand Ladder")]
public sealed class HandLadder : InteractableBase
{
    private const int GroundHitBufferSize = 8;
    private static readonly RaycastHit[] groundHits = new RaycastHit[GroundHitBufferSize];

    [Header("Forma")]
    [Tooltip("Altura entre a base e o topo da escada, em metros.")]
    [Min(.3f)] [SerializeField] private float height = 3f;
    [SerializeField, HideInInspector] private float width;
    [Tooltip("Direcao local, no objeto da escada, que aponta da base para o topo. Gire o vetor para escadas inclinadas.")]
    [SerializeField] private Vector3 climbAxis = Vector3.up;
    [Tooltip("Direcao local que aponta para fora da escada, para onde o jogador sobe. Numa escada em parede e a normal da parede.")]
    [SerializeField] private Vector3 approachAxis = Vector3.forward;

    [Header("Funcao")]
    [Tooltip("Escalavel: o jogador pode agarrar (Interagir ou gancho). Desmarque para uma escada apenas cosmetica: sem prompt, zona de entrada nem alvo do gancho.")]
    [SerializeField] private bool isClimbable = true;
    [Tooltip("Colisao: degraus e trilhos gerados bloqueiam o jogador e os inimigos. A colisao e ignorada enquanto o jogador escala (e por um instante depois de soltar). Ao mudar, o visual e regerado.")]
    [SerializeField] private bool hasCollision;
    [Tooltip("Inverte qual extremidade e o inicio da escalada sem mover a geometria. Tambem inverte a direcao de subida e troca as saidas manuais de topo/base.")]
    [SerializeField] private bool invertStartAndEnd;

    [Header("Escada de teto (monkey bars)")]
    [Tooltip("Escada horizontal no teto: o jogador fica pendurado por baixo e anda ao longo dela. O eixo de subida vira a direcao das barras (projetada no plano horizontal; se vertical, usa a frente do objeto) e a normal e sempre para baixo. Agachar solta, Pular salta para a frente e as pontas soltam o jogador.")]
    [SerializeField] private bool overhead;
    [Tooltip("Escada de teto: distancia, em metros, das barras aos pes do jogador pendurado.")]
    [Min(.5f)] [SerializeField] private float hangDepth = 1.75f;

    [Header("Gancho")]
    [Tooltip("Permite atracar nesta escada com o gancho de movimento (tecla de grapple/swing). Alcance, mira e velocidade sao globais: GrappleSettings.Movement.")]
    [SerializeField] private bool allowGrapple = true;

    [Header("Fixacao")]
    [Tooltip("Free: escada solta. Surface: encostada numa parede ou objeto (use o botao Encaixar na superficie do Inspector); o topo sai para tras, sobre a borda.")]
    [SerializeField] private HandLadderAttachment attachment = HandLadderAttachment.Free;
    [Tooltip("Permite subir tambem pelo lado oposto (escada solta, como uma escada de pintor).")]
    [SerializeField] private bool climbableFromBothSides;
    [Tooltip("Camadas que contam como superficie ao encaixar a escada. Evite camadas de personagens.")]
    [SerializeField] private LayerMask surfaceLayers = ~0;
    [SerializeField, HideInInspector] private float surfaceSearchDistance = 2f;
    [SerializeField, HideInInspector] private float surfaceStandoff = .1f;
    [Tooltip("No modo Surface, a escada acompanha a curvatura do objeto (rocha, coluna): trilhos e degraus seguem a superficie e o jogador escala pela curva. Em paredes retas nada muda.")]
    [SerializeField] private bool conformToSurface = true;
    [SerializeField, HideInInspector] private float curveSampleSpacing = .3f;
    [Tooltip("No modo Surface, encaixa a escada na superficie sozinha: ao mudar/mover no Editor e ao iniciar o jogo. Desmarque para posicionar a mao.")]
    [SerializeField] private bool autoSnapToSurface = true;

    [Header("Visual gerado (opcional)")]
    [Tooltip("Perfil da escada: visual (prefabs), espacamento dos degraus e todo o comportamento (prompt, velocidades, encaixe, salto, saidas). Vazio = comportamento padrao, sem visual gerado e subida continua (use sua propria malha). Crie um perfil sem prefabs para ter degraus na sua malha.")]
    [SerializeField] private HandLadderProfile profile;
    [SerializeField, HideInInspector] private float extraGap;
    [SerializeField, HideInInspector] private bool generateSideRails = true;

    [Header("Area efetiva")]
    [Tooltip("Trecho da escada que terá degraus, trilhos e área de escalada. Os valores vão de 0 (base) a 1 (topo).")]
    [SerializeField, HideInInspector] private Vector2 effectiveArea = new(0f, 1f);

    [Header("Saidas")]
    [Tooltip("Lado por onde o topo sai. Auto: escada solta sai pela frente; escada em superficie sai por tras (sobre a borda).")]
    [SerializeField] private HandLadderExitSide topExitSide = HandLadderExitSide.Auto;
    [Tooltip("Ao sair por tras no topo, ate onde procurar o piso (a partir de Exit Distance), em metros. Aumente se houver um vao entre a escada e a caixa.")]
    [Min(0f)] [SerializeField] private float ledgeSearchDepth = 1f;
    [Tooltip("Camadas usadas para encontrar o chao nas saidas. Gatilhos e o proprio jogador sao ignorados.")]
    [SerializeField] private LayerMask exitGroundLayers = ~0;
    [Tooltip("Ponto de saida do topo (opcional). Vazio = calculado. Se definido, substitui o calculo.")]
    [SerializeField] private Transform topExit;
    [Tooltip("Ponto de saida da base (opcional). Vazio = calculado. Se definido, substitui o calculo.")]
    [SerializeField] private Transform bottomExit;

    [SerializeField, HideInInspector] private HandLadderSpine spine;
    [SerializeField, HideInInspector] private Transform generatedRoot;
    [SerializeField, HideInInspector] private int generatedRungCount;

    private static readonly List<HandLadder> activeLadders = new();

    /// <summary>Escadas ativas (alvos possiveis do gancho).</summary>
    public static IReadOnlyList<HandLadder> ActiveLadders => activeLadders;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => activeLadders.Clear();

    private readonly List<Collider> solidColliders = new();
    private Coroutine restoreCollision;
    private const float CollisionRestoreDelay = .35f;

    /// <summary>
    /// Liga/desliga a colisao da propria escada (degraus e trilhos gerados e
    /// colisores nao-gatilho dos filhos). Chamado pelo jogador: a colisao e
    /// ignorada enquanto ele escala e volta pouco depois de soltar.
    /// </summary>
    public void SetPlayerClimbing(bool climbing)
    {
        if (restoreCollision != null)
        {
            StopCoroutine(restoreCollision);
            restoreCollision = null;
        }

        if (climbing)
            SetSolidCollidersEnabled(false);
        else if (isActiveAndEnabled)
            restoreCollision = StartCoroutine(RestoreCollisionLater());
        else
            SetSolidCollidersEnabled(true);
    }

    private System.Collections.IEnumerator RestoreCollisionLater()
    {
        yield return new WaitForSeconds(CollisionRestoreDelay);
        restoreCollision = null;
        SetSolidCollidersEnabled(true);
    }

    private void SetSolidCollidersEnabled(bool value)
    {
        solidColliders.Clear();
        GetComponentsInChildren(true, solidColliders);
        for (int i = 0; i < solidColliders.Count; i++)
        {
            Collider collider = solidColliders[i];
            if (collider != null && !collider.isTrigger)
                collider.enabled = value;
        }

        solidColliders.Clear();
    }

    private void Awake()
    {
        // Escada em superficie: a posicao salva pode ter ficado sem encaixe.
        if (attachment == HandLadderAttachment.Surface && autoSnapToSurface)
            TrySnapToSurface();
    }

    protected override void OnEnable()
    {
        if (!IsClimbable)
            return;
        base.OnEnable();
        if (!activeLadders.Contains(this))
            activeLadders.Add(this);
    }

    protected override void OnDisable()
    {
        restoreCollision = null;
        base.OnDisable();
        activeLadders.Remove(this);
    }

    public float Height => height;
    public bool IsClimbable => isClimbable && ClimbLength > .0001f;
    public float ClimbLength => height * (EffectiveArea.y - EffectiveArea.x);
    public bool HasCollision => hasCollision;
    public bool InvertStartAndEnd => invertStartAndEnd;
    // Comportamento vem do perfil (ou do padrao, sem perfil).
    private HandLadderProfile Feel => profile != null ? profile : HandLadderProfile.Default;
    public float ClimbSpeed => Feel.climbSpeed;
    public float LateralClimbSpeed => Feel.lateralClimbSpeed;
    public float PlayerForwardOffset => Feel.playerForwardOffset;
    public float MountSpeed => Feel.mountSpeed;
    public float ExitDistance => Feel.exitDistance;
    public float TopExitRise => Feel.topExitRise;
    public bool IsOverhead => overhead;
    public bool AllowGrapple => IsClimbable && allowGrapple;
    /// <summary>Distancia do jogador ao plano da escada (ou das barras aos pes, na escada de teto).</summary>
    public float GripOffset => overhead ? hangDepth : PlayerForwardOffset;
    public float MountMinDuration => Feel.mountMinDuration;
    public float ExitDuration => Feel.exitDuration;
    /// <summary>Altura maxima, a partir da base, em que o jogador fica agarrado.</summary>
    public float MaxProgress => overhead ? ClimbLength : Mathf.Max(0f, ClimbLength - Feel.topGripClearance);
    public float JumpOffSpeed => Feel.jumpOffSpeed;
    public float JumpOffHeightMultiplier => Feel.jumpOffHeightMultiplier;
    public float DropAwaySpeed => Feel.dropAwaySpeed;
    public HandLadderAttachment Attachment => attachment;
    public bool ClimbableFromBothSides => climbableFromBothSides;
    public LayerMask SurfaceLayers => surfaceLayers;
    public float SurfaceSearchDistance => Mathf.Max(.1f, profile != null ? profile.surfaceSearchDistance : surfaceSearchDistance);
    public float SurfaceStandoff => Mathf.Max(0f, profile != null ? profile.surfaceStandoff : surfaceStandoff);
    public bool AutoSnapToSurface => autoSnapToSurface;

    /// <summary>Distancia do plano da escada ate a superficie: folga + meia espessura da peca mais grossa.</summary>
    public float SurfaceOffset
    {
        get
        {
            float thickness = 0f;
            if (profile != null)
            {
                if (profile.rungPrefab != null)
                    thickness = profile.rungThickness;
                if (profile.generateSideRails && profile.railPrefab != null)
                    thickness = Mathf.Max(thickness, profile.railThickness);
            }

            return SurfaceStandoff + thickness * .5f;
        }
    }
    public float StepPhase => Feel.stepPhase;
    public Transform TopExit => topExit;
    public Transform BottomExit => bottomExit;
    public HandLadderProfile Profile => profile;
    public float ExtraGap => profile != null ? Mathf.Max(0f, profile.extraGap) : Mathf.Max(0f, extraGap);
    public float CurveSampleSpacing => Mathf.Max(.1f, profile != null ? profile.sampleSpacing : curveSampleSpacing);
    public bool GenerateSideRails => profile != null ? profile.generateSideRails : generateSideRails;
    public Transform GeneratedRoot => generatedRoot;
    public int GeneratedRungCount => generatedRungCount;
    public Vector2 EffectiveArea => new(
        Mathf.Clamp01(Mathf.Min(effectiveArea.x, effectiveArea.y)),
        Mathf.Clamp01(Mathf.Max(effectiveArea.x, effectiveArea.y)));
    public float Width => profile != null ? Mathf.Max(.1f, profile.width) : width > .01f ? width : 1f;

    /// <summary>Distancia entre degraus (perfil + Extra Gap, ou o valor manual). 0 = sem degraus (subida continua).</summary>
    public float RungSpacing => profile != null
        ? Mathf.Max(.05f, profile.rungSpacing + ExtraGap)
        : 0f;

    public int RungCount => RungSpacing > 0f
        ? Mathf.Max(1, Mathf.FloorToInt(height / RungSpacing + .0001f) + 1) : 0;

    /// <summary>Posicoes em que o jogador pode parar: uma por vao entre degraus.</summary>
    public int SlotCount => RungSpacing > 0f
        ? Mathf.Max(1, Mathf.FloorToInt(MaxProgress / RungSpacing - StepPhase + .0001f) + 1) : 0;

    /// <summary>Altura (a partir da base) da posicao de parada <paramref name="slot"/>.</summary>
    public float SlotHeight(int slot) => RungSpacing > 0f
        ? Mathf.Min(MaxProgress, (Mathf.Clamp(slot, 0, SlotCount - 1) + StepPhase) * RungSpacing)
        : Mathf.Clamp(slot, 0, 0);

    /// <summary>Posicao de parada mais proxima de uma altura.</summary>
    public int NearestSlot(float progress)
    {
        if (RungSpacing <= 0f)
            return 0;
        int best = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < SlotCount; i++)
        {
            float distance = Mathf.Abs(SlotHeight(i) - progress);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }

    // ------------------------------------------------------- superficie curva

    /// <summary>Verdadeiro se a escada em superficie acompanha uma curva (espinha gravada).</summary>
    public bool IsCurved => attachment == HandLadderAttachment.Surface && !overhead &&
        spine != null && spine.Count >= 2;
    public bool HasSpine => spine != null && spine.Count >= 2;
    public HandLadderSpine Spine => spine;

    public void SetSpine(HandLadderSpine value) => spine = value;

    // Indice i e fracao t entre as amostras i e i+1 para uma altura.
    private void Locate(float progress, out int index, out float t)
    {
        int last = spine.Count - 1;
        index = 0;
        t = 0f;
        if (progress <= spine.progress[0])
            return;
        if (progress >= spine.progress[last])
        {
            index = last - 1;
            t = 1f;
            return;
        }

        int low = 0;
        int high = last;
        while (high - low > 1)
        {
            int middle = (low + high) >> 1;
            if (spine.progress[middle] <= progress)
                low = middle;
            else
                high = middle;
        }
        index = low;
        float span = spine.progress[index + 1] - spine.progress[index];
        t = span > .0001f ? (progress - spine.progress[index]) / span : 0f;
    }

    /// <summary>Ponto local da faixa (-1 esquerda, 0 centro, 1 direita) em uma altura. Reta sem espinha.</summary>
    public Vector3 LocalLanePoint(int lane, float progress)
    {
        if (!IsCurved)
            return LocalClimbAxis * progress + LocalWidthAxis * (Width * .5f * lane);
        Locate(progress, out int i, out float t);
        Vector3 center = Vector3.Lerp(spine.center[i], spine.center[i + 1], t);
        if (lane == 0) return center;
        Vector3[] points = lane < 0 ? spine.left : spine.right;
        Vector3 point = Vector3.Lerp(points[i], points[i + 1], t);
        // Espinhas antigas nao guardavam a largura usada na amostragem.
        float sampledWidth = spine.sampledWidth > .01f ? spine.sampledWidth :
            Vector3.Distance(spine.left[0], spine.right[0]);
        return sampledWidth > .01f ? center + (point - center) * (Width / sampledWidth) : point;
    }

    /// <summary>Ponto do plano da escada (onde ficam os trilhos) em uma altura e deslocamento lateral.</summary>
    public Vector3 PlanePointAt(float progress, float lateral = 0f)
    {
        if (!IsCurved)
            return StartPosition + ClimbDirection * progress + WidthDirection * lateral;

        float geometryProgress = ToGeometryProgress(progress);

        Vector3 center = transform.TransformPoint(LocalLanePoint(0, geometryProgress));
        if (Mathf.Abs(lateral) < .0001f)
            return center;
        float k = Mathf.Clamp01(Mathf.Abs(lateral) / Mathf.Max(.01f, Width * .5f));
        Vector3 lane = transform.TransformPoint(LocalLanePoint(lateral < 0f ? -1 : 1,
            geometryProgress));
        return Vector3.Lerp(center, lane, k);
    }

    /// <summary>Normal da superficie (para onde o jogador sobe) em uma altura, do lado do jogador.</summary>
    public Vector3 GetNormal(float progress, int side = 1)
    {
        progress = ToGeometryProgress(progress);
        Vector3 normal = ApproachDirection;
        if (IsCurved)
        {
            Locate(progress, out int i, out float t);
            normal = transform.TransformDirection(
                Vector3.Slerp(spine.normal[i], spine.normal[i + 1], t)).normalized;
        }

        return overhead || side >= 0 ? normal : -normal;
    }

    /// <summary>Direcao de subida em uma altura (segue a curva).</summary>
    public Vector3 GetTangent(float progress)
    {
        if (!IsCurved)
            return ClimbDirection;
        float a = Mathf.Max(0f, progress - .1f);
        float b = Mathf.Min(ClimbLength, progress + .1f);
        Vector3 tangent = PlanePointAt(b) - PlanePointAt(a);
        return tangent.sqrMagnitude > 1e-8f ? tangent.normalized : ClimbDirection;
    }

    // Altura, lado e distancia a frente de um ponto, no referencial da escada.
    private void ToFrame(Vector3 point, out float along, out float across, out float forward)
    {
        if (!IsCurved)
        {
            Vector3 relative = point - StartPosition;
            along = Vector3.Dot(relative, ClimbDirection);
            across = Vector3.Dot(relative, WidthDirection);
            forward = Vector3.Dot(relative, ApproachDirection);
            return;
        }

        float bestProgress = 0f;
        float bestDistance = float.MaxValue;
        for (int i = -1; i <= spine.Count; i++)
        {
            float geometry = i < 0 ? height * EffectiveArea.x : i == spine.Count
                ? height * EffectiveArea.y : Mathf.Clamp(spine.progress[i], height * EffectiveArea.x, height * EffectiveArea.y);
            float distance = (transform.TransformPoint(LocalLanePoint(0, geometry)) - point).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestProgress = geometry;
            }
        }

        float progress = ToClimbProgress(bestProgress);
        Vector3 center = transform.TransformPoint(LocalLanePoint(0, bestProgress));
        Vector3 offset = point - center;
        along = progress + Vector3.Dot(offset, GetTangent(progress));
        across = Vector3.Dot(offset, WidthDirection);
        forward = Vector3.Dot(offset, GetNormal(progress));
    }

    /// <summary>
    /// Mede a superficie atras da escada em tres faixas e devolve a espinha, ou nulo
    /// se a superficie for reta (nada a acompanhar) ou nao houver superficie.
    /// </summary>
    public HandLadderSpine BuildSurfaceSpine()
    {
        if (!conformToSurface || attachment != HandLadderAttachment.Surface || overhead)
            return null;

        bool autoSampling = profile == null || profile.autoSampleSpacing;
        float spacing = autoSampling ? Mathf.Min(CurveSampleSpacing, .25f) : CurveSampleSpacing;
        int count = Mathf.Clamp(Mathf.CeilToInt(height / spacing) + 1, 2, 1024);
        HandLadderSpine result = new()
        {
            sampledWidth = Width,
            progress = new float[count],
            left = new Vector3[count],
            center = new Vector3[count],
            right = new Vector3[count],
            normal = new Vector3[count]
        };
        Vector3 origin = transform.position;
        Vector3 climb = GeometryClimbDirection;
        Vector3 approach = ApproachDirection;
        Vector3 across = WidthDirection;
        float half = Width * .5f;
        float frontProbe = Mathf.Min(1f, SurfaceSearchDistance * .5f);
        float offset = SurfaceOffset;
        float maxDeviation = 0f;
        float maxTilt = 0f;
        bool anyHit = false;
        // Cada faixa parte de uma amostra "semente" (a mais proxima do meio onde o
        // raio reto acha a superficie) e segue para os dois lados, amostra a
        // amostra: o raio parte da superficie anterior, ao longo da normal
        // anterior. Assim a escada continua na superficie mesmo quando ela se
        // afasta muito do plano reto (cano, curva fechada, tubo inclinado), nas
        // duas pontas. Um raio reto fixo perderia a superficie e a ponta ficaria solta.
        float step = height / (count - 1);
        Vector3[][] surface = { new Vector3[count], new Vector3[count], new Vector3[count] };
        Vector3[][] normals = { new Vector3[count], new Vector3[count], new Vector3[count] };
        int middle = count / 2;
        for (int i = 0; i < count; i++)
            result.progress[i] = step * i;

        // Primeiro percorre apenas o centro. Seguir cada lateral como uma
        // trajetoria independente faz as faixas abrirem em curvas e aumenta
        // artificialmente a largura da escada.
        int centerIndex = 1;
        int seed = -1;
        RaycastHit seedHit = default;
        for (int k = 0; k < count && seed < 0; k++)
        {
            // 0, +1, -1, +2, -2...: do meio para as pontas.
            int candidate = middle + ((k & 1) == 1 ? (k + 1) / 2 : -k / 2);
            if (candidate < 0 || candidate >= count)
                continue;
            Vector3 plane = origin + climb * result.progress[candidate];
            if (HandLadderSurfaceProbe.CastInitial(this, plane, out seedHit))
                seed = candidate;
        }

        for (int i = 0; i < count; i++)
        {
            surface[centerIndex][i] = origin + climb * result.progress[i];
            normals[centerIndex][i] = approach;
        }

        if (seed >= 0)
        {
            anyHit = true;
            surface[centerIndex][seed] = seedHit.point;
            normals[centerIndex][seed] = seedHit.normal;
            for (int direction = -1; direction <= 1; direction += 2)
            {
                Vector3 lastPoint = seedHit.point;
                Vector3 lastNormal = seedHit.normal;
                for (int i = seed + direction; i >= 0 && i < count; i += direction)
                {
                    Vector3 tangent = Vector3.ProjectOnPlane(climb * direction, lastNormal);
                    tangent = tangent.sqrMagnitude > 1e-4f ? tangent.normalized : climb * direction;
                    Vector3 expected = lastPoint + tangent * step;
                    float reach = frontProbe + Mathf.Max(SurfaceSearchDistance, step * 2f);
                    if (HandLadderSurfaceProbe.Cast(this, expected + lastNormal * frontProbe,
                            -lastNormal, reach, out RaycastHit hit) &&
                        Vector3.Dot(hit.normal, lastNormal) > .2f)
                    {
                        lastPoint = hit.point;
                        lastNormal = hit.normal;
                    }
                    else
                    {
                        // Sem superficie a frente: segue em linha pela tangente anterior.
                        lastPoint = expected;
                    }

                    surface[centerIndex][i] = lastPoint;
                    normals[centerIndex][i] = lastNormal;
                }
            }
        }

        // As laterais mantem a largura definida em relacao ao centro. A
        // sondagem so encosta cada ponto na parede; se nao achar superficie,
        // usa o offset fixo e continua acompanhando a normal central.
        float reachSide = frontProbe + Mathf.Max(SurfaceSearchDistance, half * 2f);
        for (int i = 0; i < count; i++)
        {
            int before = Mathf.Max(0, i - 1);
            int after = Mathf.Min(count - 1, i + 1);
            Vector3 tangent = surface[centerIndex][after] - surface[centerIndex][before];
            if (tangent.sqrMagnitude < 1e-6f)
                tangent = climb;
            tangent.Normalize();

            Vector3 normal = normals[centerIndex][i].sqrMagnitude > .0001f
                ? normals[centerIndex][i].normalized : approach;
            Vector3 localAcross = Vector3.Cross(tangent, normal);
            localAcross = Vector3.ProjectOnPlane(localAcross, normal);
            if (localAcross.sqrMagnitude < 1e-6f)
                localAcross = Vector3.ProjectOnPlane(across, normal);
            if (localAcross.sqrMagnitude < 1e-6f)
                localAcross = across;
            localAcross.Normalize();
            if (Vector3.Dot(localAcross, across) < 0f)
                localAcross = -localAcross;

            for (int lane = -1; lane <= 1; lane += 2)
            {
                int laneIndex = lane + 1;
                Vector3 expected = surface[centerIndex][i] + localAcross * (half * lane);
                if (anyHit && HandLadderSurfaceProbe.Cast(this,
                        expected + normal * frontProbe, -normal, reachSide,
                        out RaycastHit sideHit) &&
                    Vector3.Dot(sideHit.normal, normal) > .2f)
                {
                    surface[laneIndex][i] = sideHit.point;
                    normals[laneIndex][i] = sideHit.normal;
                }
                else
                {
                    surface[laneIndex][i] = expected;
                    normals[laneIndex][i] = normal;
                }
            }
        }

        for (int i = 0; i < count; i++)
            for (int lane = -1; lane <= 1; lane++)
            {
                int index = lane + 1;
                Vector3 plane = origin + climb * result.progress[i] + across * (half * lane);
                Vector3 normal = normals[index][i];
                Vector3 point = surface[index][i] + normal * offset;
                if (!anyHit)
                    point = plane;
                maxDeviation = Mathf.Max(maxDeviation, (point - plane).magnitude);
                Vector3 local = transform.InverseTransformPoint(point);
                if (lane < 0) result.left[i] = local;
                else if (lane > 0) result.right[i] = local;
                else
                {
                    result.center[i] = local;
                    result.normal[i] = transform.InverseTransformDirection(normal).normalized;
                    maxTilt = Mathf.Max(maxTilt, Vector3.Angle(normal, approach));
                }
            }

        if (!anyHit || (maxDeviation < .004f && maxTilt < .4f))
            return null;

        // Suaviza ruido de malhas irregulares, mantendo as pontas.
        for (int pass = 0; pass < 2; pass++)
            for (int i = 1; i < count - 1; i++)
            {
                result.left[i] = result.left[i - 1] * .25f + result.left[i] * .5f + result.left[i + 1] * .25f;
                result.center[i] = result.center[i - 1] * .25f + result.center[i] * .5f + result.center[i + 1] * .25f;
                result.right[i] = result.right[i - 1] * .25f + result.right[i] * .5f + result.right[i + 1] * .25f;
                result.normal[i] = (result.normal[i - 1] * .25f + result.normal[i] * .5f +
                    result.normal[i + 1] * .25f).normalized;
            }

        return autoSampling ? result.Simplified(transform.localToWorldMatrix) : result;
    }

    public override string InteractionMessage => "Escalar";

    // Escada de teto com os eixos ainda no padrao vertical: barras para a frente
    // e normal para baixo (o resto segue a rotacao do objeto).
    private bool OverheadNeedsDefaultAxes =>
        overhead && Mathf.Abs(climbAxis.normalized.y) > .9f;

    public Vector3 LocalClimbAxis
    {
        get
        {
            if (OverheadNeedsDefaultAxes)
                return Vector3.forward;
            return climbAxis.sqrMagnitude > .0001f ? climbAxis.normalized : Vector3.up;
        }
    }

    public Vector3 LocalApproachAxis
    {
        get
        {
            if (OverheadNeedsDefaultAxes)
                return Vector3.down;
            Vector3 climb = LocalClimbAxis;
            Vector3 approach = Vector3.ProjectOnPlane(approachAxis, climb);
            if (approach.sqrMagnitude < .0001f)
                approach = Vector3.ProjectOnPlane(Vector3.forward, climb);
            if (approach.sqrMagnitude < .0001f)
                approach = Vector3.ProjectOnPlane(Vector3.right, climb);
            return approach.normalized;
        }
    }

    public Vector3 LocalWidthAxis => Vector3.Cross(LocalClimbAxis, LocalApproachAxis).normalized;
    private Vector3 GeometryClimbDirection => transform.TransformDirection(LocalClimbAxis).normalized;
    public Vector3 ClimbDirection => GeometryClimbDirection * (invertStartAndEnd ? -1f : 1f);
    private float GeometryStart => height * (invertStartAndEnd ? EffectiveArea.y : EffectiveArea.x);
    private Vector3 StartPosition => transform.position + GeometryClimbDirection * GeometryStart;
    internal Vector3 ClimbStartPosition => transform.position +
        (invertStartAndEnd ? GeometryClimbDirection * height : Vector3.zero);
    public Vector3 ApproachDirection => transform.TransformDirection(LocalApproachAxis).normalized;
    public Vector3 WidthDirection => transform.TransformDirection(LocalWidthAxis).normalized;

    private float ToGeometryProgress(float progress) =>
        GeometryStart + (invertStartAndEnd ? -progress : progress);

    private float ToClimbProgress(float progress) =>
        (progress - GeometryStart) * (invertStartAndEnd ? -1f : 1f);

    /// <summary>Verdadeiro se o topo sai por tras do plano da escada.</summary>
    public bool TopExitsBack => topExitSide == HandLadderExitSide.Back ||
        (topExitSide == HandLadderExitSide.Auto && attachment == HandLadderAttachment.Surface);

    private bool BothSides => climbableFromBothSides && !overhead;
    private float EntryFront => overhead
        ? InteractionSettings.LadderEntryFrontDepth + hangDepth
        : InteractionSettings.LadderEntryFrontDepth;
    // Zona atras do plano: escada em superficie (para descer pela borda do topo)
    // ou escada de dois lados. Escada solta de um lado so tem uma folga minima.
    private float EntryBack => BothSides ? InteractionSettings.LadderEntryFrontDepth
        : attachment == HandLadderAttachment.Surface && !overhead
            ? InteractionSettings.LadderEntryBackDepth : .1f;

    public override bool ContainsPlayer(PlayerMovement player) =>
        IsClimbable && TryGetEntry(player, out _);

    public override float DistanceSqrTo(Vector3 position) => GetEntryDistanceSqr(position);

    private void OnValidate()
    {
        height = Mathf.Max(.3f, height);
        width = Mathf.Max(0f, width);
        if (climbAxis.sqrMagnitude < .0001f)
            climbAxis = Vector3.up;
        climbAxis.Normalize();
        // Escada de teto com eixos padrao: o fallback e so em tempo de execucao,
        // para desmarcar Overhead devolver os eixos originais.
        if (!OverheadNeedsDefaultAxes)
            approachAxis = LocalApproachAxis;
    }

    // ---------------------------------------------------------------- zona

    public override bool CanInteract(PlayerMovement player) =>
        IsClimbable && player != null && player.CanBeginLadderClimb &&
        base.CanInteract(player);

    public override bool Interact(PlayerMovement player) =>
        TryGetEntry(player, out HandLadderGrip grip) &&
        player.TryBeginLadderClimb(this, grip);

    /// <summary>
    /// Zona de entrada para o jogador: vale o centro da capsula ou os pes (para
    /// descer pelo topo de uma borda). A altura em que agarra vem dos pes.
    /// </summary>
    public bool TryGetEntry(PlayerMovement player, out HandLadderGrip grip)
    {
        grip = default;
        if (player == null)
            return false;

        Vector3 feet = player.transform.position;
        if (!TryGetEntryAt(PlayerCenter(player), out grip) &&
            !TryGetEntryAt(feet, out grip))
            return false;

        // Escada de teto: altura do centro e lado = direcao em que o jogador olha.
        Vector3 reference = overhead ? PlayerCenter(player) : feet;
        ToFrame(reference, out float referenceAlong, out _, out _);
        grip.progress = Mathf.Clamp(referenceAlong, 0f, MaxProgress);
        if (overhead)
            grip.side = Vector3.Dot(player.transform.forward, ClimbDirection) >= 0f ? 1 : -1;
        return true;
    }

    public float GetEntryDistanceSqr(Vector3 position)
    {
        if (IsCurved)
        {
            float nearest = float.MaxValue;
            for (int i = -1; i <= spine.Count; i++)
            {
                float geometry = i < 0 ? height * EffectiveArea.x : i == spine.Count
                    ? height * EffectiveArea.y : Mathf.Clamp(spine.progress[i], height * EffectiveArea.x, height * EffectiveArea.y);
                nearest = Mathf.Min(nearest,
                    (transform.TransformPoint(LocalLanePoint(0, geometry)) - position).sqrMagnitude);
            }
            return nearest;
        }

        Vector3 origin = StartPosition;
        Vector3 climb = ClimbDirection;
        float along = Mathf.Clamp(Vector3.Dot(position - origin, climb), 0f, ClimbLength);
        return (origin + climb * along - position).sqrMagnitude;
    }

    /// <summary>
    /// Posicao <paramref name="point"/> dentro de alguma zona de entrada? Devolve o
    /// lado, a altura e o deslocamento lateral em que o jogador agarraria.
    /// </summary>
    public bool TryGetEntryAt(Vector3 point, out HandLadderGrip grip)
    {
        grip = default;
        if (!IsClimbable)
            return false;
        ToFrame(point, out float along, out float across, out float forward);
        float half = Width * .5f;

        float margin = InteractionSettings.LadderEntryMargin;
        float lower = (invertStartAndEnd ? EffectiveArea.y < 1f : EffectiveArea.x > 0f) ? 0f : -margin;
        float upper = ClimbLength + ((invertStartAndEnd ? EffectiveArea.x > 0f : EffectiveArea.y < 1f) ? 0f : margin);
        bool inside = Mathf.Abs(across) <= half + margin &&
            along >= lower && along <= upper &&
            forward <= EntryFront && forward >= -EntryBack;
        if (!inside)
            return false;

        float limit = Mathf.Max(0f, half - .2f);
        grip = new HandLadderGrip
        {
            side = BothSides && forward < 0f ? -1 : 1,
            progress = Mathf.Clamp(along, 0f, ClimbLength),
            lateral = LateralClimbSpeed > 0f ? Mathf.Clamp(across, -limit, limit) : 0f
        };
        return true;
    }

    // ------------------------------------------------------------ geometria

    public Vector3 GetApproachDirection(int side) =>
        overhead ? ApproachDirection : ApproachDirection * (side < 0 ? -1f : 1f);

    public Vector3 GetGripPosition(HandLadderGrip grip) =>
        PlanePointAt(grip.progress, grip.lateral) +
        GetNormal(grip.progress, grip.side) * GripOffset;

    public Quaternion GetGripFacing(int side, float progress = 0f) => overhead
        ? Quaternion.LookRotation(ClimbDirection * (side < 0 ? -1f : 1f), Vector3.up)
        : Quaternion.LookRotation(-GetNormal(progress, side), GetTangent(progress));

    /// <summary>Largura utilizavel para o deslocamento lateral (de -limite a +limite).</summary>
    public float LateralLimit => Mathf.Max(0f, Width * .5f - .2f);

    /// <summary>Direcao horizontal do salto: a normal da escada, achatada no plano do chao.</summary>
    public Vector3 GetJumpDirection(int side, float progress = 0f)
    {
        Vector3 direction = GetNormal(progress, side);
        Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
        return (flat.sqrMagnitude > .0001f ? flat : direction).normalized;
    }

    /// <summary>Posicao de saida (pes do jogador) no topo ou na base.</summary>
    public Vector3 GetExitPosition(HandLadderGrip grip, bool top) =>
        ResolveExit(grip, top, out _);

    /// <summary>Direcao em que o jogador fica virado depois de sair.</summary>
    public Quaternion GetExitFacing(HandLadderGrip grip, bool top)
    {
        ResolveExit(grip, top, out bool back);
        Vector3 approach = GetNormal(top ? ClimbLength : 0f, grip.side);
        Vector3 forward = Vector3.ProjectOnPlane(back ? -approach : approach, Vector3.up);
        return forward.sqrMagnitude > .0001f
            ? Quaternion.LookRotation(forward.normalized) : Quaternion.identity;
    }

    // Saida do topo: por tras (sobre a borda/caixa) quando ha piso na altura do
    // topo; senao pela frente. Em Auto, escada em superficie prefere por tras e
    // escada solta so sai por tras se encontrar piso ali.
    private Vector3 ResolveExit(HandLadderGrip grip, bool top, out bool back)
    {
        back = false;
        Transform marker = top
            ? (invertStartAndEnd ? bottomExit : topExit)
            : (invertStartAndEnd ? topExit : bottomExit);
        Vector3 approach = GetNormal(top ? ClimbLength : 0f, grip.side);
        bool cutEnd = top ? (invertStartAndEnd ? EffectiveArea.x > 0f : EffectiveArea.y < 1f)
            : (invertStartAndEnd ? EffectiveArea.y < 1f : EffectiveArea.x > 0f);
        if (marker != null && !cutEnd)
        {
            back = top && TopExitsBack;
            return marker.position;
        }

        Vector3 end = PlanePointAt(top ? ClimbLength : 0f, grip.lateral);
        if (top && topExitSide != HandLadderExitSide.Front)
        {
            if (TryFindTopLedge(end, -approach, out Vector3 ledge))
            {
                back = true;
                return ledge;
            }

            if (TopExitsBack)
            {
                back = true;
                return SnapToGround(end - approach * ExitDistance +
                    Vector3.up * TopExitRise);
            }
        }

        Vector3 position = end + approach * (PlayerForwardOffset + ExitDistance);
        if (top)
            position += Vector3.up * TopExitRise;
        return SnapToGround(position);
    }

    // Procura piso andavel atras do topo, a uma altura proxima ao topo da escada.
    private bool TryFindTopLedge(Vector3 end, Vector3 backward, out Vector3 point)
    {
        point = default;
        backward = Vector3.ProjectOnPlane(backward, Vector3.up);
        if (backward.sqrMagnitude < .0001f)
            return false;
        backward.Normalize();
        float topY = end.y;
        for (float distance = ExitDistance; distance <= ExitDistance + ledgeSearchDepth + .001f;
             distance += .15f)
        {
            Vector3 probe = end + backward * distance;
            Vector3 origin = new(probe.x, topY + 1.2f, probe.z);
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits, 2.4f,
                exitGroundLayers, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = groundHits[i];
                Collider collider = hit.collider;
                if (collider == null || hit.distance >= nearest || hit.normal.y < .5f ||
                    hit.point.y < topY - .6f || collider.transform.IsChildOf(transform) ||
                    collider.GetComponentInParent<PlayerMovement>() != null)
                    continue;
                nearest = hit.distance;
                point = hit.point + Vector3.up * .02f;
            }

            if (nearest < float.MaxValue)
                return true;
        }

        return false;
    }

    private Vector3 SnapToGround(Vector3 position)
    {
        Vector3 origin = position + Vector3.up;
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits, 2f,
            exitGroundLayers, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        float groundY = position.y;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = groundHits[i];
            Collider collider = hit.collider;
            if (collider == null || hit.distance >= best ||
                collider.transform.IsChildOf(transform) ||
                collider.GetComponentInParent<PlayerMovement>() != null)
                continue;
            best = hit.distance;
            groundY = hit.point.y + .02f;
            found = true;
        }

        if (found)
            position.y = groundY;
        return position;
    }

    // -------------------------------------------------------------- gancho

    /// <summary>
    /// Alvo do gancho nesta escada: o degrau mais proximo da mira (raio da camera).
    /// Devolve onde o jogador ficara (<paramref name="grip"/>), o ponto do degrau
    /// para o indicador e o angulo entre a mira e o degrau.
    /// </summary>
    public bool TryGetGrappleTarget(Vector3 rayOrigin, Vector3 rayDirection,
        Vector3 playerCenter, out HandLadderGrip grip, out Vector3 point, out float angle)
    {
        grip = default;
        point = default;
        angle = 180f;
        if (!AllowGrapple || rayDirection.sqrMagnitude < .0001f)
            return false;
        rayDirection.Normalize();

        Vector3 origin = StartPosition;
        Vector3 climb = ClimbDirection;
        Vector3 approach = ApproachDirection;
        // Ponto da escada mais proximo do raio da mira.
        float along;
        if (IsCurved)
        {
            // Escada curva: amostra mais proxima do raio da mira.
            along = 0f;
            float bestMiss = float.MaxValue;
            for (int i = -1; i <= spine.Count; i++)
            {
                float geometry = i < 0 ? height * EffectiveArea.x : i == spine.Count
                    ? height * EffectiveArea.y : Mathf.Clamp(spine.progress[i], height * EffectiveArea.x, height * EffectiveArea.y);
                Vector3 sample = transform.TransformPoint(LocalLanePoint(0, geometry)) - rayOrigin;
                float ahead = Mathf.Max(0f, Vector3.Dot(sample, rayDirection));
                float miss = (sample - rayDirection * ahead).sqrMagnitude;
                if (miss < bestMiss)
                {
                    bestMiss = miss;
                    along = ToClimbProgress(geometry);
                }
            }
        }
        else
        {
            Vector3 w0 = rayOrigin - origin;
            float b = Vector3.Dot(rayDirection, climb);
            float d = Vector3.Dot(rayDirection, w0);
            float e = Vector3.Dot(climb, w0);
            float denom = 1f - b * b;
            float t = denom > .00001f ? Mathf.Max(0f, (b * e - d) / denom) : 0f;
            along = Mathf.Clamp(e + t * b, 0f, ClimbLength);
        }
        float spacing = RungSpacing;
        float rung = spacing > 0f
            ? Mathf.Clamp(ToClimbProgress(Mathf.Round(ToGeometryProgress(along) / spacing) * spacing), 0f, ClimbLength) : along;

        int side = 1;
        float front = Vector3.Dot(playerCenter - PlanePointAt(rung), GetNormal(rung));
        if (overhead)
        {
            if (front < 0f)
                return false;
            side = Vector3.Dot(Vector3.ProjectOnPlane(rayDirection, Vector3.up), climb) >= 0f ? 1 : -1;
        }
        else if (front < -.2f)
        {
            if (!BothSides)
                return false;
            side = -1;
        }

        point = PlanePointAt(rung) + GetNormal(rung, side) * .05f;
        MovementGrappleSettings settings = GrappleSettings.Movement;
        if (Vector3.Distance(playerCenter, point) > settings.range)
            return false;
        angle = Vector3.Angle(rayDirection, point - rayOrigin);
        if (angle > settings.ladderAimAngle)
            return false;

        float progress = spacing > 0f ? SlotHeight(NearestSlot(rung)) : Mathf.Min(rung, MaxProgress);
        grip = new HandLadderGrip { side = side, progress = progress, lateral = 0f };
        return true;
    }

    // ----------------------------------------------------------- gerador

    public void SetGenerated(Transform root, int rungCount)
    {
        generatedRoot = root;
        generatedRungCount = rungCount;
    }

    public void SetAttachment(HandLadderAttachment value) => attachment = value;

    /// <summary>
    /// Calcula a pose que encosta a escada na superficie logo atras dela: gira em
    /// torno do eixo de subida para a normal apontar para fora e deixa a folga.
    /// </summary>
    public bool TryComputeSurfaceSnap(out Vector3 position, out Quaternion rotation)
    {
        position = transform.position;
        rotation = transform.rotation;
        if (!HandLadderSurfaceProbe.TryFind(this, out RaycastHit hit))
            return false;

        Vector3 climb = GeometryClimbDirection;
        Vector3 normal = Vector3.ProjectOnPlane(hit.normal, climb);
        if (normal.sqrMagnitude < .0001f)
            return false;
        normal.Normalize();

        float angle = Vector3.SignedAngle(ApproachDirection, normal, climb);
        rotation = Quaternion.AngleAxis(angle, climb) * transform.rotation;
        float distance = Vector3.Dot(transform.position - hit.point, normal);
        position = transform.position + normal * (SurfaceOffset - distance);
        return true;
    }

    /// <summary>Encaixa a escada na superficie atras dela. Falso se nao houver superficie ao alcance.</summary>
    public bool TrySnapToSurface()
    {
        if (!TryComputeSurfaceSnap(out Vector3 position, out Quaternion rotation))
            return false;
        transform.SetPositionAndRotation(position, rotation);
        attachment = HandLadderAttachment.Surface;
        spine = BuildSurfaceSpine();
        return true;
    }

    // ------------------------------------------------------------- gizmos

    protected override void OnDrawGizmos()
    {
        if (!IsClimbable || !InteractionGizmos.Visible)
            return;

        base.OnDrawGizmos();

        Vector3 origin = StartPosition;
        Vector3 climb = ClimbDirection;
        Vector3 approach = ApproachDirection;
        Vector3 across = WidthDirection;
        float half = Width * .5f;
        Vector3 top = origin + climb * ClimbLength;

        // Quadro reto da escada (trilhos e eixo). Em escada curva ele ficaria solto
        // no ar; ali valem os trilhos que acompanham a superficie, desenhados abaixo.
        if (!IsCurved)
        {
            Gizmos.color = new Color(.1f, .65f, .95f, .8f);
            Gizmos.DrawLine(origin - across * half, top - across * half);
            Gizmos.DrawLine(origin + across * half, top + across * half);
            Gizmos.DrawLine(origin - across * half, origin + across * half);
            Gizmos.DrawLine(top - across * half, top + across * half);
            Gizmos.color = new Color(.1f, .65f, .95f, .35f);
            Gizmos.DrawLine(origin, top);
        }

        // Normal: para onde o jogador salta.
        Gizmos.color = new Color(1f, .35f, .85f, .95f);
        float midProgress = ClimbLength * .5f;
        Vector3 mid = PlanePointAt(midProgress);
        DrawArrow(mid, GetNormal(midProgress) * .9f);
        if (BothSides)
            DrawArrow(mid, -GetNormal(midProgress) * .9f);

        // Trilho do jogador (posicao ao escalar), seguindo a curva.
        Gizmos.color = new Color(1f, .9f, .2f, .6f);
        const int railSamples = 12;
        Vector3 previousRail = PlanePointAt(0f) + GetNormal(0f) * GripOffset;
        for (int i = 1; i <= railSamples; i++)
        {
            float progress = MaxProgress * i / railSamples;
            Vector3 railPoint = PlanePointAt(progress) + GetNormal(progress) * GripOffset;
            Gizmos.DrawLine(previousRail, railPoint);
            previousRail = railPoint;
        }

        // Posicoes de parada entre os degraus.
        for (int i = 0; i < SlotCount; i++)
        {
            float progress = SlotHeight(i);
            Gizmos.DrawWireCube(PlanePointAt(progress) + GetNormal(progress) * GripOffset,
                new Vector3(.12f, .04f, .12f));
        }

        if (IsCurved)
        {
            // Trilhos acompanhando a superficie.
            Gizmos.color = new Color(.1f, .65f, .95f, .9f);
            for (int i = 0; i < spine.Count - 1; i++)
            {
                float a = Mathf.Max(height * EffectiveArea.x, spine.progress[i]);
                float b = Mathf.Min(height * EffectiveArea.y, spine.progress[i + 1]);
                if (b <= a) continue;
                Gizmos.DrawLine(transform.TransformPoint(LocalLanePoint(-1, a)),
                    transform.TransformPoint(LocalLanePoint(-1, b)));
                Gizmos.DrawLine(transform.TransformPoint(LocalLanePoint(1, a)),
                    transform.TransformPoint(LocalLanePoint(1, b)));
            }
        }

        if (!overhead)
        {
            DrawExitGizmo(true, new Color(.2f, 1f, .4f, .95f));
            DrawExitGizmo(false, new Color(1f, .7f, .15f, .95f));
        }
    }

    public void DrawEntryAreaGizmo()
    {
        if (!IsClimbable)
            return;
        Matrix4x4 previous = Gizmos.matrix;
        float margin = InteractionSettings.LadderEntryMargin;
        float depth = EntryFront + EntryBack;
        float zoneCenter = (EntryFront - EntryBack) * .5f;
        if (IsCurved)
        {
            for (int i = 0; i < spine.Count - 1; i++)
            {
                float geometryA = Mathf.Max(height * EffectiveArea.x, spine.progress[i]);
                float geometryB = Mathf.Min(height * EffectiveArea.y, spine.progress[i + 1]);
                if (geometryB <= geometryA) continue;
                float a = ToClimbProgress(geometryA);
                float b = ToClimbProgress(geometryB);
                float middle = (a + b) * .5f;
                Vector3 from = PlanePointAt(a);
                Vector3 to = PlanePointAt(b);
                Gizmos.matrix = Matrix4x4.TRS((from + to) * .5f,
                    Quaternion.LookRotation(GetNormal(middle), GetTangent(middle)), Vector3.one);
                InteractionGizmos.DrawBox(new Vector3(0f, 0f, zoneCenter),
                    new Vector3(Width + margin * 2f, (to - from).magnitude + .02f, depth),
                    InteractionGizmos.ZoneFill, InteractionGizmos.ZoneLine);
            }
        }
        else
        {
            Vector3 origin = StartPosition;
            Gizmos.matrix = Matrix4x4.TRS(origin,
                Quaternion.LookRotation(ApproachDirection, ClimbDirection), Vector3.one);
            Vector3 size = new(Width + margin * 2f,
                ClimbLength, depth);
            InteractionGizmos.DrawBox(new Vector3(0f, ClimbLength * .5f, zoneCenter), size,
                InteractionGizmos.ZoneFill, InteractionGizmos.ZoneLine);
        }
        Gizmos.matrix = previous;
    }

    private void OnDrawGizmosSelected()
    {
        if (!IsClimbable || !InteractionGizmos.Visible || Application.isPlaying)
            return;

        // Previa da superficie: mostra onde o botao Encaixar vai bater.
        Vector3 start = StartPosition + ClimbDirection * (height * .5f);
        Vector3 direction = -ApproachDirection;
        bool hit = HandLadderSurfaceProbe.TryFind(this, out RaycastHit surface);
        Gizmos.color = hit ? new Color(.3f, 1f, .5f) : new Color(1f, .3f, .3f);
        Vector3 end = hit ? surface.point : start + direction * SurfaceSearchDistance;
        Gizmos.DrawLine(start, end);
        if (hit)
            Gizmos.DrawWireSphere(surface.point, .08f);
    }

    private void DrawExitGizmo(bool top, Color color)
    {
        HandLadderGrip grip = new() { side = 1, progress = top ? ClimbLength : 0f };
        Vector3 exit = GetExitPosition(grip, top);
        Vector3 from = PlanePointAt(grip.progress) +
            GetNormal(grip.progress, grip.side) * PlayerForwardOffset;
        Gizmos.color = color;
        Gizmos.DrawLine(from, exit);
        Gizmos.DrawWireSphere(exit, .12f);
        Vector3 forward = GetExitFacing(grip, top) * Vector3.forward;
        Gizmos.DrawLine(exit, exit + forward * .35f);
    }

    private static void DrawArrow(Vector3 from, Vector3 vector)
    {
        Vector3 to = from + vector;
        Gizmos.DrawLine(from, to);
        Vector3 side = Vector3.Cross(vector, Vector3.up);
        if (side.sqrMagnitude < .0001f)
            side = Vector3.Cross(vector, Vector3.right);
        side = side.normalized * (vector.magnitude * .15f);
        Vector3 back = -vector * .2f;
        Gizmos.DrawLine(to, to + back + side);
        Gizmos.DrawLine(to, to + back - side);
    }
}

/// <summary>Procura a superficie atras da escada (usado pelo gizmo e pelo botao Encaixar).</summary>
public static class HandLadderSurfaceProbe
{
    private static readonly RaycastHit[] hits = new RaycastHit[16];
    private static readonly float[] samples = { .5f, .8f, .2f };

    /// <summary>Raio contra a superficie (ignora a escada, o jogador e gatilhos).</summary>
    public static bool Cast(HandLadder ladder, Vector3 start, Vector3 direction,
        float distance, out RaycastHit best)
        => Cast(ladder, start, direction, distance, out best, Vector3.zero);

    /// <summary>Como acima; com <paramref name="facing"/> diferente de zero, ignora superficies cuja normal nao aponta para esse lado (faces de tras).</summary>
    public static bool Cast(HandLadder ladder, Vector3 start, Vector3 direction,
        float distance, out RaycastHit best, Vector3 facing)
    {
        best = default;
        int count = Physics.RaycastNonAlloc(start, direction, hits, distance,
            ladder.SurfaceLayers, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.distance >= nearest ||
                hit.collider.transform.IsChildOf(ladder.transform) ||
                hit.collider.GetComponentInParent<PlayerMovement>() != null ||
                (facing != Vector3.zero && Vector3.Dot(hit.normal, facing) <= .05f))
                continue;
            nearest = hit.distance;
            best = hit;
        }

        return nearest < float.MaxValue;
    }

    /// <summary>
    /// Primeira amostra de uma faixa. Escada de um lado so gruda no lado nao
    /// escalavel (atras): so aceita superficie cuja normal aponta para a frente
    /// da escada. Escada de face dupla aceita a superficie de qualquer lado.
    /// </summary>
    public static bool CastInitial(HandLadder ladder, Vector3 plane, out RaycastHit best)
    {
        Vector3 approach = ladder.ApproachDirection;
        float search = ladder.SurfaceSearchDistance;
        if (!ladder.ClimbableFromBothSides || ladder.IsOverhead)
        {
            float lead = Mathf.Min(.15f, search * .5f);
            return Cast(ladder, plane + approach * lead, -approach, lead + search,
                out best, approach);
        }

        float probe = Mathf.Min(.15f, search * .5f);
        bool back = Cast(ladder, plane + approach * probe, -approach, probe + search,
            out RaycastHit backHit, approach);
        bool front = Cast(ladder, plane - approach * probe, approach, probe + search,
            out RaycastHit frontHit, -approach);
        best = back ? backHit : frontHit;
        if (back && front && frontHit.distance < backHit.distance)
            best = frontHit;
        return back || front;
    }

    public static bool TryFind(HandLadder ladder, out RaycastHit best)
    {
        best = default;
        if (ladder == null)
            return false;

        Vector3 climb = ladder.ClimbDirection;
        // Tenta o meio, depois perto do topo e da base, para nao falhar em vaos.
        for (int s = 0; s < samples.Length; s++)
        {
            Vector3 start = ladder.ClimbStartPosition + climb * (ladder.Height * samples[s]);
            if (CastInitial(ladder, start, out best))
                return true;
        }

        return false;
    }
}
