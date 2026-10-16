# Heat como carga e consumo de pesados

Status: implementado como parte do rework atual.

## Fluxo

1. Leves acertam e acumulam Heat.
2. Um pesado captura todo Heat positivo no inicio do ataque.
3. O golpe usa o bonus por notches da carga capturada, mesmo se errar.
4. Heat volta a zero.
5. Se houver Superheat, ele e convertido instantaneamente em Heat para deixar uma nova carga disponivel.

## Decisoes implementadas

- Formula de dano: notches.
- Momento de consumo: inicio do pesado.
- Pesados que erram: consomem a carga normalmente.
- Conversao de Superheat: `1 Superheat = 1 Heat` enquanto houver espaco.
- Superheat maximo: mantem Burn como penalidade maxima.

## Consequencia de design

O sistema cria um ciclo claro de construir com leves, escolher quando explodir com pesado e assumir risco para guardar Heat suficiente para produzir Superheat. Manter Superheat exige permanecer no limite, pois gastar pesados cedo impede o excedente de existir.
