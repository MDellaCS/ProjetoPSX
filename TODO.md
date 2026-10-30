# TODO

## Colisão de inimigo arremessado — expansões futuras

- Combate (Pending): avaliar modificadores de peso por classe (brutamonte/chefe) após testar o peso configurável atual por inimigo.
- Combate (Pending): avaliar interação com guarda e esquiva, além de imunidade curta a novos Body Impacts.
- Combate (Pending): avaliar dano no próprio inimigo arremessado e feedback audiovisual dedicado às colisões de corpos.

## Sistema de animação desacoplado

### Iteração 4 — Reações e interrupções

- Status: Pending
- Dependências: Iterações 1 e 2.
- Restante: adicionar uma intenção visual `Hurt` separada de `Stunned`, caso o gameplay mantenha essas reações distintas; validar interrupções e retorno visual sem controlar a duração do gameplay.

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

- Dialogo/Cutscene (Pending): integrar DialogueController a Timeline. Um Signal deve abrir uma fala modal, pausar somente o PlayableDirector, retomar a Timeline ao confirmar e concluir corretamente quando a cutscene for pulada. Nao implementar ate a proxima iteracao de dialogos em cutscene.

- Programação (Pending): refatorar `PlayerMovement` sem mudar gameplay. Manter `PlayerMovement` como fachada/orquestrador e único dono de `CharacterController.Move`; extrair responsabilidades para `PlayerMovementMotor`, `PlayerGrounding`, `PlayerLocomotion`, `PlayerDash`, `PlayerJump` e `PlayerCrouchSlide`. Migrar em etapas, preservando a API pública usada por combate e outros sistemas.
- Combate (Pending): avaliar pisão/bounce na cabeça do inimigo, inspirado em Super Mario e Crash. Deve devolver o player para cima, causar muito Poise e pouco dano.
- Combate (Pending): avaliar inimigos com ataque de grab anti-pisão, que prende o player pelo pé quando ele tenta cair sobre eles.
- Combate (Pending): completar os casos de direção de impacto que não têm amostra móvel anterior: preservar uma amostra de Windup para tracks `Follow Anchors` e usar `player → ponto atingido` como último fallback. Validar hitboxes que voltam, como bumerangues, para confirmar que a volta lança na direção do jogador.
- Pending: revisar a reformulação de Heat/Cold/Superheat para uma barra de vida comum em que Heat alto aumenta dano causado e recebido.
- Pending: melhorias da câmera — revisar suavização de retorno após colisão e comportamento de rotação.
- Pending: revisar eventuais frames transitórios de `GROUNDED: NO` após dash; marcar como observado e ainda sem reprodução confiável.
- Pending: permitir configurações próprias para o segundo pulo.
- Pending: right shoulder camera offset, caso seja retomado após novos testes.
