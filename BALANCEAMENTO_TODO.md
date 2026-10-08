# Backlog de balanceamento — Heat, Cold e Superheat

## Decisões atuais

- Dano extremo dos pesados em Heat alto é intencional: o risco é acumular Superheat.
- Cold extremo deve tornar sobreviver a prioridade, não matar inimigos.
- Fontes de calor consumíveis serão adicionadas futuramente ao mapa para recuperação fora de combate.
- A geração mínima de Heat por pesado é intencional, para evitar que Cold extremo impeça qualquer recuperação.
- Heat ganho e resfriamento por inimigo atingido são intencionais.
- O dano do pesado usa o Heat existente antes do acerto; o Heat ganho fortalece ataques seguintes.

## Próximas correções

- Fazer `heatPowerReference` e `heatPowerExponent` afetarem de fato a curva de poder dos pesados.
- Reduzir levemente o bônus de poise damage concedido por Heat alto.
- Escalar Heat de `-1000 / 1000` para `-10000 / 10000`, mantendo valores inteiros para maior precisão.

## Próximos experimentos de design

- Revisar Superheat: considerar remover a geração passiva e converter Heat excedente acima de 850 diretamente em Superheat, com taxa crescente conforme o Heat aumenta.
- Fazer Cold recuperar passivamente fora de combate até `minHeat * 0,2` (hoje, -200).
- Fazer Heat positivo decair somente fora de combate, após 10 segundos.
- Definir formalmente o critério de "fora de combate" antes de implementar recuperação e decay.
