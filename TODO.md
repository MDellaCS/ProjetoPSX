# TODO

## Sistema de animação desacoplado

### Iteração 4 — Reações e interrupções

- Status: Pending
- Dependências: Iterações 1 e 2.
- Objetivo: apresentar Hurt/Stunned/Dead e futuras reações sem controlar sua duração.
- Tarefas: mapear interrupções, stagger, morte e recuperação; garantir que animações antigas não fiquem presas.
- Critérios de aceitação: reação visual respeita o estado real e nunca atrasa o retorno do controle.

### Iteração 5 — Taxa de atualização visual independente

- Status: Pending
- Dependências: Iteração 2.
- Objetivo: permitir animações visuais em frequência configurável, independente do gameplay.
- Tarefas: amostrar poses a 12/15/24 FPS; investigar comportamento do Animator; preservar input, física e resposta de interrupções.
- Critérios de aceitação: mudar FPS visual não quantiza gameplay nem altera resultados de combate ou movimento.

### Iteração 6 — Polimento e extensibilidade

- Status: Pending
- Dependências: iterações anteriores.
- Objetivo: preparar efeitos visuais avançados desacoplados.
- Tarefas: avaliar slow motion visual, squash/stretch, blends direcionais, animações aditivas, variações de Idle e reutilização para inimigos.
- Critérios de aceitação: qualquer efeito continua puramente de apresentação.

## Outros itens existentes

- Programação (Pending): refatorar `PlayerMovement` sem mudar gameplay. Manter `PlayerMovement` como fachada/orquestrador e único dono de `CharacterController.Move`; extrair responsabilidades para `PlayerMovementMotor`, `PlayerGrounding`, `PlayerLocomotion`, `PlayerDash`, `PlayerJump` e `PlayerCrouchSlide`. Migrar em etapas, preservando a API pública usada por combate e outros sistemas.
- Combate (Pending): avaliar pisão/bounce na cabeça do inimigo, inspirado em Super Mario e Crash. Deve devolver o player para cima, causar muito Poise e pouco dano.
- Combate (Pending): avaliar inimigos com ataque de grab anti-pisão, que prende o player pelo pé quando ele tenta cair sobre eles.
- Combate (Pending): implementar ataques realmente direcionais. Cada impacto deve carregar a direção física da `Hitbox Track`, calculada primeiro por `posição atual - posição anterior`. No primeiro frame, usar a trajetória planejada `End Local Position - Start Local Position`; para `Follow Anchors`, usar a última amostra disponível do Windup. Se não houver movimento válido, usar o forward local da hitbox e, por último, `player → ponto atingido`. Launch Forwards, Camera Shake, reações, knockback e futuros efeitos direcionais devem consumir essa direção. Validar especialmente hitboxes de ida e volta, como bumerangues, para que a volta lance na direção do player.
- Pending: revisar a reformulação de Heat/Cold/Superheat para uma barra de vida comum em que Heat alto aumenta dano causado e recebido.
- Pending: melhorias da câmera — revisar suavização de retorno após colisão e comportamento de rotação.
- Pending: revisar eventuais frames transitórios de `GROUNDED: NO` após dash; marcar como observado e ainda sem reprodução confiável.
- Pending: permitir configurações próprias para o segundo pulo.
- Pending: sistema de inimigo principal entre inimigos em aggro, com maior agressividade para um escolhido e menor para os demais.
- Pending: right shoulder camera offset, caso seja retomado após novos testes.
- Bloquear: não faz nada, apenas trava o player
