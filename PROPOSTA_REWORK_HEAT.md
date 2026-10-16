# Rework de Heat por Notches

Status: implementado. Esta e a base atual de balanceamento.

## Escala

- Heat: `float` de `-100` a `100`, com 10 notches negativos, neutro `0` e 10 positivos.
- Superheat: `float` de `0` a `50`, com 5 notches de 10 pontos.
- Cada notch de Heat equivale a 10 pontos.

## Poder de pesados

O pesado captura o Heat positivo no inicio da animacao e o consome imediatamente. O dano e o poise daquele golpe usam o snapshot capturado, portanto acertar ou errar nao muda o custo.

| Heat consumido | Dano pesado | Poise Damage pesado |
| --- | --- | --- |
| Notch 0 | `1x` | `1x` |
| Notch positivo N | `1 + N x` | `1 + N * 0.5 x` |
| Notch negativo N | `1 - |N| * 0.1 x` | `1 - |N| * 0.05 x` |

Exemplo: a `60 Heat`, um pesado usa `7x` dano e `4x` poise, depois consome os 60 pontos.

Heat negativo nao e consumido por pesados. Em `-100`, o dano pesado chega a `0x`, mas Cold extremo mata o jogador.

## Geracao de Heat

- Leves geram Heat quando acertam.
- Os valores existentes de leves sao convertidos por `Attack Heat Value Scale`; valor inicial `0.01`, portanto `300` vira `3 Heat`.
- Pesados nao geram Heat.
- Cold nao reduz a geracao dos leves.

## Superheat

- Superheat so nasce quando uma geracao de Heat excederia `100 Heat`.
- O excedente e convertido pelo `Excess Heat To Superheat Multiplier`; valor inicial `0.5`.
- Nao existe mais geracao passiva de Superheat por manter Heat alto.
- Quando um pesado consome Heat positivo, Superheat e convertido instantaneamente em Heat, ponto a ponto, ate acabar a reserva ou atingir o limite de Heat.
- Superheat continua caindo fora de combate e em Cold.

## Burn

Ao atingir `50 Superheat`, a penalidade maxima continua ativa: o jogador entra em Burn.

1. Burn Stun inicial: nenhum controle.
2. Burning: apenas caminhada lenta; ataque, dash, pulo, crouch e slide ficam bloqueados.
3. Burn Stun final: nenhum controle antes do retorno.

Durante a sequencia, Heat e Superheat drenam para `-90 Heat` e `0 Superheat`.

Valores iniciais: `1.25 s` de stun inicial, `4 s` Burning, `1.25 s` de stun final e `30%` de velocidade de caminhada.

## HUD

- Exibe valores internos de Heat e Superheat.
- Dez quadrados frios a esquerda, neutro no centro e dez quentes a direita.
- Cinco quadrados vermelhos mostram Superheat.
- O texto `1x`, `4x` etc. mostra o multiplicador do proximo pesado, usando o Heat atualmente disponivel.
