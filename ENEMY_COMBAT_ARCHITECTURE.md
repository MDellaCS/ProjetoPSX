# Arquitetura de combate dos inimigos

## Fonte de verdade

- `EnemyAttackDefinition`: um ataque isolado. Ordem de autoria equivalente a do Player: Identity, Timing, Power, Movement, Requirements, Strikes e Extended Arm Hitbox.
- `EnemyMovesetDefinition`: grafo formado por Openings, Nodes e Transitions.
- `EnemyAttackController`: executa a sequencia e as fases; nao armazena ataques inline.
- `EnemyAttackSelector`: valida distancia, angulo, solo/ar, agressividade, cooldown, linha de visao e pesos.
- `EnemyAttackMovement`: aplica movimento e rotacao autorados, sem depender de root motion.
- `EnemyAttackHitboxController`: executa Strikes, Tracks, Sweep e dano/Poise.
- `EnemyAttackRuntime`: snapshot da execucao atual.
- `EnemyAttackPreview`: visualizacao de autoria na Scene View.

Animacao e apenas apresentacao. `animationStateId` e os eventos de `EnemyAttackController` permitem conectar um presenter sem dar autoridade de gameplay ao Animator.

## Criar um ataque

1. Use `Create > Projeto PSX > Enemy > Attack Definition`.
2. Configure Timing, Power e Requirements.
3. Adicione ao menos um Strike e uma Hitbox Track.
4. Selecione o asset. O Inspector mostra `Preview Enemy`, `Animate Preview`, velocidade e progresso do Active.
5. Adicione o ataque a um Node no moveset e ligue o Node a uma Opening ou Transition.

## Hitboxes

- `Sphere`: impacto radial.
- `Capsule`: golpes longos, membros e armas.
- `Box`: areas retangulares.
- `Linear`: interpola posicao e rotacao entre inicio e fim.
- `Orbit`: circula ao redor do centro real do CharacterController.
- `Follow Anchors`: acompanha `EnemyHitboxAnchor` no modelo.
- `Sweep`: amostra o caminho entre frames para impedir que golpes rapidos atravessem o jogador.

Os gizmos de runtime aparecem somente com F3. O preview de edicao permanece disponivel fora do Play Mode.

## Combos e mixups

Uma Opening escolhe o primeiro Node por peso. Cada Node pode apontar para varias Transitions; chance e peso sao independentes. `maximumAttacksPerSequence` impede ciclos infinitos. Requirements e cooldown sao reavaliados antes de cada continuacao.

O Inspector de `EnemyMovesetDefinition` valida IDs duplicados e referencias inexistentes.
