# Backlog de balanceamento — Heat, Cold e Superheat

## Decisões atuais

- Dano extremo dos pesados em Heat alto é intencional: o risco é acumular Superheat.
- Cold extremo deve tornar sobreviver a prioridade, não matar inimigos.
- Fontes de calor consumíveis serão adicionadas futuramente ao mapa para recuperação fora de combate.
- A geração mínima de Heat por pesado é intencional, para evitar que Cold extremo impeça qualquer recuperação.
- Heat ganho e resfriamento por inimigo atingido são intencionais.
- O dano do pesado usa o Heat existente antes do acerto; o Heat ganho fortalece ataques seguintes.

## TODO — Possível simplificação do sistema

Avaliar substituir completamente Cold e Superheat por Vida comum + Heat apenas positivo.

- Vida volta a ser uma barra independente e clara para o jogador.
- Heat passa a ir apenas de zero ao máximo.
- Heat alto aumenta tanto o dano causado quanto o dano recebido pelo jogador.
- O risco deve crescer mais rápido perto do máximo: a vulnerabilidade no topo deve superar levemente o ganho ofensivo, para Heat máximo ser uma aposta e não o estado ideal permanente.
- Pesados geram Heat; leves podem reduzi-lo ou gerar menos Heat.
- Heat decai fora de combate.

Exemplo inicial para testes futuros:

| Heat | Dano causado | Dano recebido |
| ---: | ---: | ---: |
| 0% | 100% | 100% |
| 50% | 140% | 130% |
| 80% | 190% | 180% |
| 100% | 250% | 275% |

## TODO — Melhorias de câmera

- Revisitar a suavização de colisão com paredes e validar que não causa saltos, clipping ou atraso excessivo.
- Revisitar câmera em ombro direito: encontrar um enquadramento lateral que preserve leitura do player e da arena, sem apenas aumentar a distância aparente da câmera.

## TODO — Movimento

- Permitir configurar o segundo pulo separadamente do primeiro (velocidade inicial, aceleração ao segurar e duração máxima). Por enquanto, ambos usam os mesmos valores de pulo.

## Monitorar movimento

- Dash terrestre preserva contato com o chão para evitar ataques em buffer serem roteados como aéreos. Observar se o `CharacterController` ainda reporta `GROUNDED: NO` por alguns frames após o dash.
