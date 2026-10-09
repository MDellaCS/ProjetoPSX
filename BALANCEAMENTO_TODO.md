# Backlog de balanceamento — Heat, Cold e Superheat

## Decisões atuais

- Dano extremo dos pesados em Heat alto é intencional: o risco é acumular Superheat.
- Cold extremo deve tornar sobreviver a prioridade, não matar inimigos.
- Fontes de calor consumíveis serão adicionadas futuramente ao mapa para recuperação fora de combate.
- A geração mínima de Heat por pesado é intencional, para evitar que Cold extremo impeça qualquer recuperação.
- Heat ganho e resfriamento por inimigo atingido são intencionais.
- O dano do pesado usa o Heat existente antes do acerto; o Heat ganho fortalece ataques seguintes.

## TODO — Possivel simplificacao do sistema

Avaliar substituir completamente Cold e Superheat por Vida comum + Heat apenas positivo.

- Vida volta a ser uma barra independente e clara para o jogador.
- Heat passa a ir apenas de zero ao maximo.
- Heat alto aumenta tanto o dano causado quanto o dano recebido pelo jogador.
- O risco deve crescer mais rapido perto do maximo: a vulnerabilidade no topo deve superar levemente o ganho ofensivo, para Heat maximo ser uma aposta e nao o estado ideal permanente.
- Pesados geram Heat; leves podem reduzi-lo ou gerar menos Heat.
- Heat decai fora de combate.

Exemplo inicial para testes futuros:

| Heat | Dano causado | Dano recebido |
| ---: | ---: | ---: |
| 0% | 100% | 100% |
| 50% | 140% | 130% |
| 80% | 190% | 180% |
| 100% | 250% | 275% |

## TODO — Melhorias de camera

- Revisitar a suavizacao de colisao com paredes e validar que nao causa saltos, clipping ou atraso excessivo.

## TODO — Movimento

- Permitir configurar o segundo pulo separadamente do primeiro (velocidade inicial, aceleracao ao segurar e duracao maxima). Por enquanto, ambos usam os mesmos valores de pulo.

## Resolvido - Monitorar movimento

- Dash terrestre agora preserva contato com o chao para evitar ataques em buffer serem roteados como aereos. Observar se o CharacterController ainda reporta `GROUNDED: NO` por alguns frames apos o dash.

## TODO - Lock-on

- Reimplementar Lock-on: selecao e troca de alvo, rotacao do player e enquadramento de camera devem ser validados juntos antes de entrar novamente no prototipo.

## TODO - Hit Stop

- Reintroduzir Hit Stop como um sistema de impacto completo: deve suportar intensidades e duracoes por acao, funcionar com ataques do jogador e inimigos, preservar UI/input necessarios e restaurar corretamente o tempo mesmo com impactos consecutivos. O antigo sistema global de `Time.timeScale = 0` foi removido por ser simples demais para o combate final.
