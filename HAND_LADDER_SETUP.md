## Escada solta ou grudada

| Modo (`Attachment`) | Quando usar | Saída do topo |
|---|---|---|
| **Free** | escada solta no espaço, plataformas, escadas de pintor (marque `Climbable From Both Sides` para subir pelos dois lados) | pela frente |
| **Surface** | encostada em parede/objeto | por trás, sobre a borda (`Top Exit Side = Auto`) |

Para grudar numa parede: posicione a escada perto dela com a seta rosa apontando para fora da parede e clique **Encaixar na superfície**. O botão procura a superfície atrás da escada (`Surface Layers`, até `Surface Search Distance`), gira a escada em torno do eixo de subida para a normal bater com a parede e deixa a folga `Surface Standoff`. A linha verde/vermelha do gizmo (escada selecionada) mostra se há parede ao alcance.

Em `Surface`, a zona de entrada recua `Entry Back Depth` atrás do plano, então o jogador pode **descer pela borda** da parede e agarrar a escada por cima.

### Objetos curvos (rochas, colunas)

Em `Surface` com `Conform To Surface` (padrão), a escada mede a superfície em três faixas (esquerda, centro, direita) a cada `Curve Sample Spacing` e **acompanha a curvatura**: trilhos viram segmentos que seguem a superfície, cada degrau liga as duas faixas, e o jogador escala pela curva (posição, orientação, salto na normal local, zona de entrada e gizmos seguem o perfil). Paredes retas não geram perfil e nada muda. O perfil é gravado ao encaixar (Inspector, soltar o mouse com a escada selecionada, ou iniciar o jogo) e fica salvo em espaço local, então mover o objeto mantém a forma; mude a rocha e a escada reencaixa ao soltar o mouse. `Surface Search Distance` precisa cobrir o quanto a rocha se afasta do plano da escada.

## Visual: gerado ou seu

- **Sem perfil**: nenhum visual é gerado. Use sua própria malha (ou o prefab modelado) e ajuste Height/Width pelos gizmos. A escada funciona igual.
- **Com perfil** (`Hand Ladder Profile`): degraus e trilhos são gerados no filho `[Generated] Hand Ladder`. Crie o perfil pelo botão **Criar perfil**, escolha os eixos do prefab, espaçamento e largura padrão. O pivô dos prefabs fica no centro. Colliders dos prefabs são desativados; a escada não bloqueia o jogador.

## Subida em degraus

O jogador só para nos **vãos entre degraus** (quadradinhos amarelos no gizmo) e sobe de vão em vão com aceleração/desaceleração; segurar Cima/Baixo encadeia os degraus. O espaçamento vem do perfil (`Rung Spacing` + `Extra Gap`); sem perfil use `Custom Rung Spacing` (0 = subida contínua). `Step Phase` ajusta onde ele para no vão (0 = na altura de um degrau, 0,5 = no meio). Cima no último vão sai pelo topo; Baixo no primeiro sai pela base.

## Perfil dos prefabs `HandLadderRung`/`HandLadderRail`

Ambos são cilindros de 0,3 × 2 m com o comprimento no eixo Y: `Rung Width Axis` = Y, `Rail Length Axis` = Y. `Rung Thickness` e `Rail Thickness` ajustam a espessura nos dois eixos perpendiculares. Os trilhos ficam no mesmo plano dos degraus, que os atravessam.

## Controles do jogador

- **Interagir** agarra (o prompt aparece na zona ciano).
- **Cima/Baixo** sobem e descem. Em escadas largas, `Lateral Climb Speed > 0` permite andar para os lados.
- **Pular** salta na direção da normal da escada (`Jump Off Speed`, `Jump Off Height Multiplier`).
- **Agachar** solta a escada e cai, com um leve empurrão para fora (`Drop Away Speed`).
- Segurar Cima no topo / Baixo na base sai pelas saídas.

Agarrar e soltar são botões separados de propósito: ninguém cai da escada por reflexo (problema relatado em Dark Souls II/III, onde o mesmo botão soltava e descia).

## Encaixe e saída pré-feitos

- **Encaixe**: ao agarrar, o jogador é levado ao trilho por um movimento pré-feito (`Mount Speed`, `Mount Min Duration`), a partir de onde estiver: parado, no ar ou em alta velocidade. Pular ou Agachar já valem durante o encaixe.
- **Parada no topo**: o jogador para `Top Grip Clearance` (0,9 m) abaixo do topo, com o fim da escada na altura do meio do corpo. Cima nessa posição dispara a saída.
- **Saída do topo**: sobe primeiro e passa por cima da borda até o piso (`Exit Duration`), sem controle até chegar. Em escada solta (`Auto`) ela sai sobre uma caixa/plataforma se houver piso atrás do topo (procura até `Ledge Search Depth` além de `Exit Distance`); senão sai pela frente. Em `Surface` prefere sempre por trás.
- **Saída da base**: afasta-se da escada e desce até o chão, também pré-feita.

## Gancho: combate x movimento

O gancho tem duas configurações **globais e independentes** em `Assets/Resources/GrappleSettings.asset`:

- **Combat** (ataque de grapple em inimigo): `Range`, `Assist Angle`/`Assist Near Angle` (abertura da mira) e velocidades do puxão (`Lighter/Heavier Enemy Pull Speed`, `Pull Stop Distance`, `Post Pull Stagger Duration`). Escolhe o inimigo travado ou o melhor dentro do alcance e da mira próprios; não depende da assistência dos ataques normais.
- **Movement** (swing, escadas, barras): `Range` (padrão das âncoras e degraus), `Ladder Aim Angle` (cone da mira para degraus), `Anchor Aim Angle` (180 = sem limite) e `Pull Speed` (velocidade até escada/barras). Cada `PlayerSwingAnchor` pode ter `Interaction Range` próprio (0 = global); cada escada só liga/desliga `Allow Grapple`.

Prioridade do input: ponto de balanço > inimigo (ataque) > degrau de escada. O botão de gancho atraca no degrau mais próximo da mira (raio do centro do jogador na direção da câmera), onde o jogador fica no vão mais próximo. Escadas bloqueadas por geometria ou atrás do jogador (escada de um lado) não são alvo.

## Escada de teto (monkey bars)

Marque `Overhead`. Os eixos locais continuam valendo e a escada **acompanha a rotação do objeto** (yaw, inclinação, o que for): `Climb Axis` é a direção das barras e `Approach Axis` o lado de onde o jogador pende. Se os eixos ainda estiverem no padrão vertical (subida em Y), as barras seguem a frente do objeto e a normal aponta para baixo, sem alterar os valores salvos. O jogador fica pendurado `Hang Depth` abaixo das barras e anda ao longo delas para onde apontar o movimento (relativo à câmera), de degrau em degrau. **Agachar** solta; **Pular** salta para a frente; nas pontas ele se solta. O visual gerado funciona igual.

## Indicador do alvo do gancho

Sistema global e desacoplado: `GrappleTargetSystem` guarda os provedores (`IGrappleTargetProvider`) e `GrappleTargetHUD` (UI Toolkit, `Resources/GrappleTargetHUD`, criado sozinho) desenha um disco grande, preenchido e pulsante sobre o melhor alvo, com tamanho constante na tela e por cima da cena (visível de longe; fora da tela gruda na borda). Cores: ciano = ponto de balanço, vermelho = inimigo, amarelo = escada, roxo = outros. Hoje há três provedores: `PlayerSwingController` (balanço, prioridade 300, e escada, 100) e `PlayerTargetAssist` (inimigo, 200).

Para uma feature nova usar o indicador: implemente `IGrappleTargetProvider` (prioridade + `TryGetGrappleTarget`), chame `GrappleTargetSystem.Register(this)` em OnEnable e `Unregister(this)` em OnDisable. Não precisa tocar no HUD.

## Saídas

Calculadas por `Exit Distance`, `Top Exit Rise` e `Top Exit Side`; o ponto é encaixado no chão por um raycast curto (`Exit Ground Layers`). Para um ponto específico, atribua um Transform em `Top Exit`/`Bottom Exit`: ele substitui o cálculo.

## Zonas de entrada extras

Adicione `Hand Ladder Extra Entry Zone` a um filho com Collider (trigger) para criar outra área que também agarra a escada (um vão no chão, plataforma lateral). Desmarque `Use Automatic Entry Zone` para usar só as extras.

## Sistema de interação (genérico)

Escadas, lojas, cutscenes e a nova `Interaction Area` usam o mesmo sistema:

- `InteractableBase`: herde dela e implemente `ContainsPlayer` e `Interact`. Opcional: `CanInteract`, `InteractionPriority`, `DistanceSqrTo`.
- `InteractionSystem`/`InteractionDriver`: um único candidato por frame (maior prioridade; em empate, o mais próximo). O driver mostra o prompt, consome o input e respeita pausa, cutscene e diálogo. Sem objetos interativos na cena não existe driver.
- `Interaction Area` (**Projeto PSX > Interaction**): Collider trigger + prompt + `UnityEvent<GameObject>`, com `Interact Once`, `Cooldown`, `Require Grounded` e `Priority`. Serve para placas, painéis e qualquer "aperte E aqui" sem código.

## Limites atuais

- Sem clips, IK de mãos/pés ou animações de transição (usa o estado visual `LadderClimb`).
- A saída é um deslocamento direto; confira se há espaço livre onde o jogador vai parar.
