# Arquitetura do combate do player

O gameplay dos ataques nao depende do `Animator`. Animacoes leem o estado do ataque, mas nao criam dano, movimento ou janelas de acerto.

## Responsabilidades

1. `PlayerAttackDefinition`: asset de um ataque; contem somente aquilo que diferencia o golpe: timing, poder, movimento, Strikes e Hitbox Tracks.
2. `PlayerMovesetDefinition`: grafo de entradas, Nodes e transicoes, alem das regras globais de cancelamento, velocidades de rotacao e tempos de transicao separados entre leves e pesados. O moveset inicial possui somente `L1` e `H1`, sem combo.
3. `PlayerMovesetController`: consome o input global e escolhe um Node do grafo.
4. `PlayerAttackController`: autoridade da timeline `Windup -> Active -> Recovery`; cria um snapshot imutavel do poder do golpe.
5. `PlayerAttackMovement`: aplica avanco e giro durante Windup + Active sem depender da animacao. A direcao trava ao entrar em Recovery.
6. `PlayerHitboxController`: avalia Sphere, Capsule e Box, incluindo Sweep, Anchors, grupos e repeticao de acertos.
7. `PlayerAttackResolver`: converte uma deteccao em dano, Poise, Heat, Impact, Launch Up, Push e Juggle.
8. `PlayerCombatStatus`: controla exclusivamente dentro/fora de combate e o override de F6.
9. `PlayerAttackPreview`: permite inspecionar volumes no Scene View sem entrar em Play Mode.
10. `PlayerAnimationPresenter`: continua somente como apresentacao; recebe ID, fase e progresso do ataque.

## Hitboxes complexas

Um ataque contem varios `Strikes`; cada Strike pode conter varias `Hitbox Tracks`. Uma Track procedural interpola posicao e rotacao localmente ao centro real do `CharacterController`. Uma Track `Follow Anchors` acompanha `PlayerHitboxAnchor` colocados, por exemplo, na base e ponta de uma arma.

Tracks procedurais possuem dois modos de trajetoria. `Linear` interpola entre Start e End. `Orbit` calcula um arco ao redor do centro real do player, com angulo, distancia e raio inicial/final controlados por curvas independentes. A orbita pertence ao gameplay e nao depende de o Animator realmente girar o modelo.

Tracks do mesmo Strike e com o mesmo `Hit Group` causam apenas um acerto por inimigo. Strikes diferentes podem atingir o mesmo inimigo novamente, permitindo um clique produzir varios golpes distintos. `Sweep` cria amostras entre frames para uma arma rapida nao atravessar o alvo sem detectar colisao.

Um ataque pode ativar `Extended Arm Hitbox`. Nesse caso, cada Track ativa recebe uma Capsule adicional entre o centro real do player e o centro atual da hitbox. A grossura e global no moveset e a Capsule compartilha o Hit Group da Track, portanto nao duplica dano.

## Moveset inicial

- `L1`: dano 10, Poise 50, Windup 0,05 s, Active 0,08 s, Recovery 0,05 s e avanco 0,2. Usa uma Capsule varrida para representar uma estocada longa de lanca.
- `H1`: dano 120, Poise 60, Windup 0,5 s, Active 0,03 s, Recovery 0,35 s e avanco 1. Usa uma Sphere de raio 2, posicionada 2 unidades diante do centro do player.

Os valores numericos vieram dos valores serializados no Inspector da cena antes da remocao do sistema antigo. O H1 continua consumindo Heat no inicio, mesmo errando, e usa o multiplicador de poder e timing do sistema de Heat.
