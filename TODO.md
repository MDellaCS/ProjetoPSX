## Traversal

- Ledge grab (Pending): refazer do zero, sem reutilizar a implementação removida. Definir uma forma explícita e fácil de autorizar superfícies agarráveis, incluindo bordas de `MeshCollider`; permitir deslocamento lateral contínuo pelas bordas, com validação segura de extremidades e cantos. Incluir soltura e salto direcional, animações/alinhamento, ferramentas de configuração no Editor, custo baixo de física e testes de movimento em cena. Revisar a integração com `HandLadder` para evitar disputa de controle.

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

- Audio (Pending): implementar reverberacao dinamica leve. Usar um ambiente global como base, volumes de regiao com presets e transicao suave, e raycasts periodicos entre fontes 3D relevantes e o jogador para detectar oclusao (atenuar som direto e agudos). Escalonar/limitar os raycasts por frame e ignorar fontes distantes ou de baixa prioridade; manter UI e dialogos 2D fora do processamento. Regioes sobrepostas devem resolver por prioridade. Nao tentar estimar reverberacao completa por raycast; medir o custo no Profiler antes de aumentar frequencia ou quantidade de raios.

- Combate (Pending): remover completamente os ataques de Pause das cadeias e todo o codigo residual de Pause Input, incluindo regras de transicao, temporizadores, campos de Inspector, assets e testes relacionados. Fazer essa limpeza como uma tarefa dedicada, preservando as demais rotas dos movesets.

- Dialogo/Cutscene (Pending): integrar DialogueController a Timeline. Um Signal deve abrir uma fala modal, pausar somente o PlayableDirector, retomar a Timeline ao confirmar e concluir corretamente quando a cutscene for pulada. Nao implementar ate a proxima iteracao de dialogos em cutscene.

- Combate (Pending): avaliar pisão/bounce na cabeça do inimigo, inspirado em Super Mario e Crash. Deve devolver o player para cima, causar muito Poise e pouco dano. Avaliar inimigos com ataque de grab anti-pisão, que prende o player pelo pé quando ele tenta cair sobre eles.
- Combate (Pending): completar os casos de direção de impacto que não têm amostra móvel anterior: preservar uma amostra de Windup para tracks `Follow Anchors` e usar `player → ponto atingido` como último fallback. Validar hitboxes que voltam, como bumerangues, para confirmar que a volta lança na direção do jogador.
