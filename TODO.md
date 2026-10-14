# TODO

## Sistema de animação desacoplado

### Iteração 1 — Fundação da apresentação visual

- Status: Completed
- Dependências: sistemas atuais de `PlayerState`, `PlayerMovement` e `PlayerCombat`.
- Objetivo: observar o gameplay e resolver uma intenção visual sem controlar a simulação.
- Entregue: `PlayerAnimationPresenter`, `PlayerVisualState` e `IPlayerAnimationPlayback`.
- Critérios verificados: a resolução usa somente dados existentes; mudanças podem ser observadas por evento/log; não exige Animator, clips ou VisualRoot; não altera regras de gameplay.

### Iteração 2 — Integração com Unity Animator

- Status: Completed
- Dependências: Iteração 1 e um VisualRoot/Animator opcional.
- Objetivo: conectar a intenção visual ao Unity Animator de forma opcional.
- Entregue: `PlayerAnimatorPlayback`, bindings configuráveis por estado, transição visual sem reiniciar o estado atual, velocidade do clip Move derivada da velocidade real do player e root motion desabilitado por padrão.
- Critérios verificados: Animator ausente/inativo ou estados não configurados não geram exceção nem afetam o player; playback pode ficar no VisualRoot; troca de clip ou caminho de estado não exige alteração de gameplay.

### Iteração 3 — Animações de combate

- Status: Completed
- Dependências: Iteração 2 e identificação visual estável dos ataques.
- Objetivo: representar ataques terrestres, aéreos, dash attacks e suas fases reais.
- Entregue: `PlayerCombat` expõe ID, fase e progresso de ataque somente para leitura; `PlayerAnimatorPlayback` possui mapeamento visual por Attack Name e regiões de Windup/Active/Recovery no clip.
- Critérios verificados: o tempo real do combate seleciona a pose normalizada; sequências, combos e cancelamentos são observados sem Animation Events; hitboxes e dano permanecem independentes. Novos ataques exigem apenas um binding visual opcional.

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
- Critérios de aceitação: mudar FPS visual não quantiza gameplay nem altera resultados de combate/movimento.

### Iteração 6 — Polimento e extensibilidade

- Status: Pending
- Dependências: Iterações anteriores.
- Objetivo: preparar efeitos visuais avançados desacoplados.
- Tarefas: avaliar hitstop visual, slow motion visual, squash/stretch, blends direcionais, animações aditivas, variações de Idle e reutilização para inimigos.
- Critérios de aceitação: qualquer efeito continua puramente de apresentação.

## Outros itens existentes

- Pending: revisar a reformulação de Heat/Cold/Superheat para uma barra de vida comum em que Heat alto aumenta dano causado e recebido.
- Pending: reduzir levemente o bônus de poise damage concedido por Heat alto.
- Pending: melhorias da câmera — revisar suavização de retorno após colisão e comportamento de rotação.
- Pending: revisar eventuais frames transitórios de `GROUNDED: NO` após dash; marcar como observado e ainda sem reprodução confiável.
- Pending: reavaliar Lock On futuramente, após testes de combate em arenas maiores.
- Pending: adicionar hit stop corretamente como efeito de apresentação, sem depender de gameplay.
- Pending: permitir configurações próprias para o segundo pulo.
- Pending: sistema de inimigo principal entre aggroed enemies, com maior agressividade para um escolhido e menor para os demais.
- Pending: right shoulder camera offset, caso seja retomado após novos testes.
