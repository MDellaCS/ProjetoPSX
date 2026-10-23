using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Impede que entidades terrestres ocupem o mesmo espaco horizontal.
/// A separacao e centralizada para continuar funcionando mesmo quando uma
/// entidade esta em stagger, hitstop ou sem IA ativa.
/// </summary>
[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
public sealed class GroundEntitySeparationSystem : MonoBehaviour
{
    [Header("Separacao de Entidades Terrestres")]
    [Tooltip("Espaco horizontal extra, em unidades do mundo, mantido entre as capsulas do player e dos inimigos terrestres.")]
    [Min(0f)] [SerializeField] private float horizontalClearance = .06f;
    [Tooltip("Velocidade maxima, por entidade, usada para desfazer uma sobreposicao. Mantem a correcao como um empurrao em vez de teletransportar os corpos.")]
    [Min(0f)] [SerializeField] private float separationSpeed = 6f;
    [Tooltip("Quantidade de passagens por frame. Mais passagens resolvem grupos comprimidos com maior firmeza, mas custam mais processamento.")]
    [Range(1, 8)] [SerializeField] private int resolutionIterations = 3;
    [Tooltip("Margem vertical usada para reconhecer uma entidade apoiada sobre outra. Ela remove o empilhamento sem empurrar entidades que apenas passam muito acima.")]
    [Min(0f)] [SerializeField] private float verticalContactTolerance = .3f;

    private readonly List<GroundEntity> entities = new();

    private struct GroundEntity
    {
        public CharacterController controller;
        public PlayerMovement player;
        public EnemyMovement enemy;

        public Transform Transform => player != null ? player.transform : enemy.transform;
        public float VerticalVelocity => player != null
            ? player.VerticalSpeed
            : enemy.VerticalVelocity;
    }

    private void LateUpdate()
    {
        if (GamePauseController.IsPaused)
            return;

        CollectEntities();
        for (int iteration = 0; iteration < resolutionIterations; iteration++)
        {
            for (int firstIndex = 0; firstIndex < entities.Count; firstIndex++)
            {
                for (int secondIndex = firstIndex + 1; secondIndex < entities.Count; secondIndex++)
                    ResolvePair(entities[firstIndex], entities[secondIndex]);
            }
        }
    }

    private void CollectEntities()
    {
        entities.Clear();

        foreach (PlayerMovement player in FindObjectsByType<PlayerMovement>(FindObjectsInactive.Exclude))
            TryAdd(player, null);

        foreach (EnemyMovement enemy in FindObjectsByType<EnemyMovement>(FindObjectsInactive.Exclude))
        {
            if (enemy.ParticipatesInGroundEntitySeparation)
                TryAdd(null, enemy);
        }
    }

    private void TryAdd(PlayerMovement player, EnemyMovement enemy)
    {
        CharacterController controller = player != null
            ? player.EntityCharacterController
            : enemy.EntityCharacterController;
        if (controller == null || !controller.enabled || !controller.gameObject.activeInHierarchy)
            return;

        entities.Add(new GroundEntity
        {
            controller = controller,
            player = player,
            enemy = enemy
        });
    }

    private void ResolvePair(GroundEntity first, GroundEntity second)
    {
        if (!HasRelevantVerticalContact(first, second))
            return;

        Vector3 firstCenter = first.controller.bounds.center;
        Vector3 secondCenter = second.controller.bounds.center;
        Vector3 offset = secondCenter - firstCenter;
        offset.y = 0f;

        float requiredDistance = first.controller.radius + second.controller.radius + horizontalClearance;
        float currentDistance = offset.magnitude;
        if (currentDistance >= requiredDistance)
            return;

        Vector3 direction;
        if (currentDistance > .0001f)
        {
            direction = offset / currentDistance;
        }
        else
        {
            // Duas capsulas exatamente no mesmo ponto nao possuem uma direcao
            // fisica para usar. A frente da primeira entidade e estavel entre
            // frames e evita que ambas continuem uma dentro da outra.
            direction = first.Transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude <= .0001f)
                direction = Vector3.right;
            direction.Normalize();
        }

        float penetration = requiredDistance - currentDistance;
        float maximumStep = separationSpeed * Time.deltaTime /
            Mathf.Max(1, resolutionIterations);
        float correctionPerEntity = Mathf.Min(
            penetration * .5f + .001f,
            maximumStep
        );
        Vector3 displacement = direction * correctionPerEntity;
        ApplyDisplacement(first, -displacement);
        ApplyDisplacement(second, displacement);

        MarkTopContact(first, second);
        MarkTopContact(second, first);
    }

    private bool HasRelevantVerticalContact(GroundEntity first, GroundEntity second)
    {
        Bounds firstBounds = first.controller.bounds;
        Bounds secondBounds = second.controller.bounds;
        bool verticalOverlap = firstBounds.min.y <= secondBounds.max.y + verticalContactTolerance &&
            secondBounds.min.y <= firstBounds.max.y + verticalContactTolerance;
        if (!verticalOverlap)
            return false;

        const float ascendingThreshold = .05f;
        float bottomDifference = firstBounds.min.y - secondBounds.min.y;

        // A entidade que esta visivelmente acima da outra so deve ser
        // afastada se esta caindo ou parada. Isso deixa o player pular perto,
        // por baixo ou atraves da lateral de um inimigo sem receber uma
        // correcao horizontal/vertical durante a subida.
        if (bottomDifference > verticalContactTolerance)
            return first.VerticalVelocity <= ascendingThreshold;
        if (bottomDifference < -verticalContactTolerance)
            return second.VerticalVelocity <= ascendingThreshold;

        // Quando ambas estao na mesma altura (corpos lado a lado), qualquer
        // subida indica um pulo em andamento e a separacao espera terminar.
        return first.VerticalVelocity <= ascendingThreshold &&
            second.VerticalVelocity <= ascendingThreshold;
    }

    private void ApplyDisplacement(GroundEntity entity, Vector3 displacement)
    {
        if (entity.player != null)
            entity.player.ApplyGroundEntitySeparation(displacement);
        else
            entity.enemy.ApplyGroundEntitySeparation(displacement);
    }

    private void MarkTopContact(GroundEntity top, GroundEntity bottom)
    {
        if (top.player == null)
            return;

        if (top.controller.bounds.min.y >= bottom.controller.bounds.max.y - verticalContactTolerance)
            top.player.MarkEntityAsInvalidGround();
    }
}
