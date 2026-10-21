# Moveset atual do jogador — dano, DPS e configurações

Snapshot técnico dos assets em `Assets/GameData/Player/Moveset` e dos valores
efetivamente serializados na `SampleScene`. Atualizado manualmente em
21/10/2026.

Este arquivo descreve o gameplay, não a animação. Hitboxes, movimentos,
Heat, Poise e reações funcionam independentemente do Animator.

## Como ler os números

- **Dano por impacto** é o dano de um acerto de um `Strike`, antes de defesa,
  esquiva e limite de vida do alvo.
- **Dano potencial** assume que o mesmo inimigo permanece dentro de todas as
  hitboxes/grupos permitidos pelo ataque. Não é garantia: alcance, movimento,
  frames, esquiva, guarda, morte do alvo e cancelamentos podem diminuir o
  resultado.
- **DPS de ciclo** = dano potencial / (Windup + Active + Recovery). É uma
  medida comparável entre ataques completos sem cancelamento. Não inclui tempo
  para alcançar o alvo, troca entre ataques nem buffer.
- Os dados abaixo são contra um alvo sem guarda e sem esquiva, com vida
  suficiente. O dano real retornado por `EnemyHealth` é o que alimenta Heat,
  números de dano, DPS Meter, partículas, Push e impacto.
- Todas as hitboxes atuais são `Sphere`. `Sweep` impede que elas atravessem um
  alvo entre frames. Uma hitbox com outro `Hit Group` pode atingir o mesmo alvo
  novamente; `Repeat` permite novo acerto depois do intervalo indicado.

## Pipeline real de dano

1. O `PlayerAttackRuntime` cria uma fotografia do ataque ao iniciá-lo.
2. Pesados gastam todo o Heat positivo **no início**, inclusive se errarem.
3. Cada hitbox ativa chama o Strike correspondente. O dano é
   `RoundToInt(dano do runtime × multiplicador do Strike)`.
4. O inimigo pode esquivar: nesse caso o ataque não conecta e não há dano,
   Heat, Push ou impacto.
5. Se a guarda frontal aplicável estiver ativa, ela reduz o dano e Poise; só o
   dano depois dessa redução conta como dano realmente causado.
6. `EnemyHealth` limita o dano à vida restante. Esse valor final é enviado ao
   DPS Meter e, em ataques leves, gera Heat.
7. O Poise é aplicado junto com o dano. Apenas quando o ataque quebra Poise a
   reação solicitada (Stagger/Juggle/Launch) é efetivada.

Arquivos responsáveis: `PlayerAttackRuntime`, `PlayerAttackResolver`,
`PlayerHitboxController`, `PlayerHeat`, `EnemyHealth`, `EnemyGuard`,
`EnemyDodge` e `CombatPoiseSystem`.

## Heat e pesados

Valores efetivos no componente `PlayerHeat` da cena:

| Regra | Valor atual |
|---|---:|
| Heat máximo / Cold máximo | `100` / `-100` |
| Tamanho de um notch | `10 Heat` |
| Heat por dano realmente causado por leves | `0.3` |
| Bônus aditivo de poder pesado | `6` |
| Superheat máximo | `50` |
| Conversão de excesso Heat → Superheat | `0.5` |

Para um pesado, com `n` sendo o notch consumido:

```text
Multiplicador de dano M
  n >= 0: 1 + n
  n < 0 : max(0, 1 + n × 0.1)

Dano pesado final = RoundToInt((DanoBase + M × 6) × M)
Poise pesado final = RoundToInt(PoiseBase × P)
  n >= 0: P = 1 + n × 0.5
  n < 0 : P = max(0, 1 + n × 0.05)

Multiplicador de Windup e Recovery = 1 / Lerp(1, M, 0.5)
Active não muda com Heat.
```

Assim, `H1` e `AirH1` (base `120 dano / 80 Poise`) têm estes marcos:

| Notch / Heat consumido | M | Dano | Poise | Windup + Recovery | Duração total | DPS de ciclo (1 impacto) |
|---|---:|---:|---:|---:|---:|---:|
| `0` / `0–9.999` | 1x | 126 | 80 | 0.850 s | 0.930 s | 135.484 |
| `1` / `10–19.999` | 2x | 264 | 120 | 0.567 s | 0.647 s | 408.247 |
| `5` / `50–59.999` | 6x | 936 | 280 | 0.243 s | 0.323 s | 2,899.115 |
| `10` / `100` | 11x | 2,046 | 480 | 0.142 s | 0.222 s | 9,230.075 |

O notch `0` já é `1x`; por isso o cálculo atual também adiciona `6` ao dano
base de um pesado sem Heat. Em Cold, o multiplicador cai gradualmente; em
`-10` o dano pesado chega a `0`.

## Tabela resumida de ataques

`W/A/R` = Windup/Active/Recovery. `Mov.` é a distância programada no período
Windup + Active. `Hit.` é o máximo previsível de impactos por alvo parado,
antes de considerar Repeat dependente de frame.

| Ataque | Contexto / tipo | W / A / R (s) | Dano / Poise por impacto | Hit. | Dano potencial / Poise | DPS de ciclo | Heat se todo dano conectar | Mov. | Efeitos |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|
| L1 | chão / leve | .050 / .150 / .100 | 10 / 20 | 2 | 20 / 40 | 66.667 | 6.0 | .4 | — |
| L2 | chão / leve | .050 / .150 / .100 | 10 / 20 | 2 | 20 / 40 | 66.667 | 6.0 | .4 | — |
| L3 | chão / leve | .100 / .400 / .150 | 7 / 5 | 5 | 35 / 25 | 53.846 | 10.5 | 1.2 | — |
| L3p | chão / leve | .050 / .400 / .150 | 25 / 30 | até 3* | até 75 / 90 | até 125.000 | até 22.5 | 2 | órbita expansiva |
| L4 | chão / leve | .010 / .150 / .225 | 32 / 60 | 2 | 64 / 120 | 166.234 | 19.2 | .8 | — |
| L4p | chão / leve | .100 / .672 / .220 | 45 / 90 | até 5* | até 225 / 450 | até 226.815 | até 67.5 | 3 | Launch Up + Launch Forwards |
| H1 | chão / pesado | .500 / .080 / .350 | 120 / 80 base | 1 | ver Heat | ver Heat | 0 | 1 | Launch Forwards |
| D1 | dash no chão / leve | .030 / .150 / .070 | 15 / 50 | 1 | 15 / 50 | 60.000 | 4.5 | .5 | Launch Up + Self Launch Up |
| AirL1 | ar / leve | .040 / .100 / .040 | 6 / 9 | 2 | 12 / 18 | 66.667 | 3.6 | .4 | Juggle + auto-Juggle |
| AirL2 | ar / leve | .040 / .100 / .040 | 5 / 9 | 2 | 10 / 18 | 55.556 | 3.0 | .4 | Juggle + auto-Juggle |
| AirL3 | ar / leve | .040 / .100 / .040 | 6 / 9 | 2 | 12 / 18 | 66.667 | 3.6 | .4 | Juggle + auto-Juggle |
| AirL4 | ar / leve | .040 / .100 / .040 | 5 / 9 | 2 | 10 / 18 | 55.556 | 3.0 | .4 | Juggle + auto-Juggle |
| AirL5 | ar / leve | .040 / .100 / .040 | 6 / 9 | 2 | 12 / 18 | 66.667 | 3.6 | .4 | Juggle + auto-Juggle |
| AirL6 | ar / leve | .040 / .100 / .040 | 5 / 9 | 2 | 10 / 18 | 55.556 | 3.0 | .4 | Juggle + auto-Juggle |
| AirL7 | ar / leve | .040 / .100 / .040 | 6 / 9 | 2 | 12 / 18 | 66.667 | 3.6 | .4 | Juggle + auto-Juggle |
| AirL8 | ar / leve | .080 / .200 / .080 | 30 / 50 | 1 | 30 / 50 | 83.333 | 9.0 | 0 | Launch Down + Self Launch Down |
| AirH1 | ar / pesado | .500 / .080 / .350 | 120 / 80 base | 1 | ver Heat | ver Heat | 0 | 1 | Launch Down |

\* O número é o máximo temporal aproximado de hits repetidos se o alvo fica
dentro de tudo: L3p tem 2 oportunidades na parte orbital e 1 na volta;
L4p tem 4 na órbita e 1 na volta. Como o Repeat depende de `Time.time`,
taxa de quadros e contato contínuo, trate esse DPS como teto, não promessa.

## Routes do moveset

Entradas iniciais:

```text
Chão: Light → L1        Heavy → H1
Dash de chão: Light → D1
Ar: Light → AirL1      Heavy → AirH1
```

Rotas:

```text
L1 → L2 → L3 → L4
          └─ (PAUSE) → L3p → L4p

AirL1 → AirL2 → AirL3 → AirL4 → AirL5 → AirL6 → AirL7 → AirL8
```

Configuração global do `StarterMoveset`:

| Regra | Valor |
|---|---:|
| Global Pause Delay | .4 s após o fim do Windup |
| Janela de continuação leve | .6 s após o fim do Windup |
| Janela de continuação pesada | .6 s após o fim do Windup |
| Delay mínimo de continuação | 0 s |
| Giro Windup + Active, leves | 1500°/s |
| Giro Windup + Active, pesados | 1500°/s |
| Raio de Extended Arm Hitbox | .15 |

Cancelamentos globais de leves:

- Jump e Dash: Active/Recovery, mas espera terminar o Active.
- Heavy: Windup/Active/Recovery, cancela imediatamente.

Cancelamentos globais de pesados:

- Jump e Dash: apenas Recovery.

## Geometria detalhada das hitboxes

Coordenadas são locais ao **centro real** do `CharacterController`; `z`
positivo é frente. Todos os tracks usam Sphere e `Sweep` de `.18`, exceto H1
e AirH1, que não fazem Sweep.

| Ataque | Track(s), janela dentro do Active | Geometria | Grupo / Repeat |
|---|---|---|---|
| L1 | Vai `0–.5`; Volta `.5–1` | `(+.36,.36,.2) → (0,.36,3.5) → (+.36,.36,.2)`, raio `.5` | `0`, `1` / não |
| L2 | Vai `0–.5`; Volta `.5–1` | `(-.36,.36,.2) → (0,.36,3.5) → (-.36,.36,.2)`, raio `.5` | `0`, `1` / não |
| L3 | cinco tracks: `0–.2`, `.2–.4`, `.4–.6`, `.6–.8`, `.8–1` | cada um `(+.36,.36,.2) → (0,.36,2.25)`, raio `.5` | `0..4` / não |
| L3p | Gira `0–.9`; Volta `.9–1` | órbita `180°→400°`, distância `.25→3.5`, raio `.5→1`, altura `.36`; volta `(2.48,.36,2.58)→(0,.36,.2)`, raio `.8` | ambos `0` / sim, `.2 s` |
| L4 | Esquerda e Direita, ambas `0–1` | `(+.36,.36,.2)→(+.48,.36,3.5)` e espelho negativo, raio `.6` | `0`, `1` / não |
| L4p | Gira `0–.9`; Volta `.9–1` | órbita `180°→450°`, distância `.25→5.5`, raio `.25→1.5`, altura `.36`; volta `(5.74,.36,-.43)→(0,.36,.2)`, raio `.8` | ambos `0` / sim, `.2 s` |
| H1 / AirH1 | esfera `0–1` | `(+.36,-.36,1.15) → (0,0,1.75)`, raio `1.75` | `0` / não |
| D1 | `0–1` | `(+.36,0,.8) → (+.36,1.5,.8)`, raio `1.5` | `0` / não |
| AirL1/3/5/7 | Vai `0–.5`; Volta `.5–1` | `(+.36,.36,.2) → (0,.36,2.8) → (+.36,.36,.2)`, raio `.5` | `0`, `1` / não |
| AirL2/4/6 | Vai `0–.5`; Volta `.5–1` | `(-.36,.36,.2) → (0,.36,2.8) → (-.36,.36,.2)`, raio `.5` | `0`, `1` / não |
| AirL8 | Dive `0–1` | `(+.36,1.3,.8) → (+.36,-.8,.8)`, raio `1.5` | `0` / não |

`Extended Arm Hitbox` está ligada em todos os leves, inclusive os aéreos e
D1; está desligada em H1 e AirH1. Quando ligada, cria uma cápsula extra do
centro do player até a posição de cada track ativa, com raio global `.15`.

## Movimento próprio dos ataques

O deslocamento só é aplicado em Windup + Active. A janela abaixo é
normalizada sobre a soma dessas duas fases; o Recovery nunca desloca o player.
As curvas atuais são progressivas (começam suaves e terminam mais rápidas).

| Ataque | Distância | Janela normalizada de movimento |
|---|---:|---|
| L1, L2, L4 | .4 / .4 / .8 | `0 → .5` |
| L3 | 1.2 | `0 → .9` |
| L3p | 2 | `0 → 1` |
| L4p | 3 | `0 → .5` |
| H1, AirH1 | 1 | `0 → 1` |
| D1 | .5 | `0 → 1` |
| AirL1–AirL7 | .4 cada | `0 → .5` |
| AirL8 | 0 | `0 → 0` (sem deslocamento próprio) |

## Poise, Launch e Juggle

Valores efetivos do `CombatPoiseSystem` na cena:

| Regra global | Valor |
|---|---:|
| Início da regeneração de Poise | 3 s sem golpe |
| Regeneração de Poise | 10% do máximo/s + 5/s |
| Push/Launch Forwards por dano realmente causado | .08 força por dano |
| Força mínima / máxima de Launch Forwards e Down | 16 / 44 |
| Desaceleração de Launch Forwards | 37 por segundo |
| Lift de Juggle | 1.5 |
| Multiplicador de gravidade em Juggle | .3 |
| Duração do Juggle | .15 s |

Push/Launch Forwards e Launch Down usam
`Clamp(dano realmente causado × .08, 16, 44)`. Portanto H1 com 126 de dano
tem força 16; em Heat alto, H1 chega rapidamente ao teto 44. O ataque só
produz reação se o Poise for quebrado naquele golpe; sem quebra, o dano ainda
acontece, mas não há Launch/Stagger/Juggle.

O `Juggle` dos AirL1–7 pede Juggle no inimigo e também chama o mesmo efeito no
player quando o Strike começa, preservando os dois no ar. `AirL8` pede Launch
Down no inimigo e Self Launch Down no player. `D1` pede Launch Up e Self
Launch Up. L4p combina Launch Up e Launch Forwards na mesma reação.

## Defesa inimiga que altera o resultado

- **Dodge:** se ativada, o hit retorna como não conectado: dano, Poise, Heat,
  DPS Meter, impacto e reação não acontecem.
- **Guard frontal:** por configuração padrão do componente, reduz 70% do dano
  e 50% do Poise; pesados causam 3× o dano de guarda baseado no Poise bruto.
  Esses valores podem variar por prefab do inimigo.
- **Vida restante:** dano realmente causado nunca excede a vida atual. É esse
  valor reduzido que gera Heat e força de Launch Forwards.
- **Imunidades do inimigo:** Launch Up, Down, Forwards e Juggle podem ser
  recusados pela configuração do alvo; quando não houver reação válida, a
  quebra de Poise cai no Stagger padrão.

## Burn: dano fora do moveset

Não é um ataque de input, mas é fonte de dano do jogador e entra no DPS Meter
se acertar. Na `SampleScene`, ao alcançar Superheat máximo:

- Pre-explosion: `1.5 s`, movimento a 30%.
- Explosão: raio `6`, Active `0.7 s`, `950` dano e `200` Poise por inimigo;
  Push usa as mesmas regras globais acima.
- Stun da explosão: `3 s`; Burning: `5 s`; Recovery stun: `1 s`.
- Durante Burning, Heat vai para `-70` e Superheat zera ao explodir.

## Observações de leitura

- `requiresHitConfirm` está desligado em todas as transições atuais: acertar
  não é necessário para continuar os combos.
- As entradas D1 só existem para Light durante dash de chão; não há Dash Heavy.
- Todos os leves que aparecem na tabela têm `generatesHeatOnHit` ligado. Os
  dois pesados não geram Heat.
- O DPS dos pesados depende de um snapshot do Heat no início: mudanças de Heat
  posteriores não mudam aquele ataque já iniciado.
- `L3p` e `L4p` são particularmente sensíveis à permanência do alvo dentro da
  hitbox; o número de hits repetidos não é uma constante serializada.
