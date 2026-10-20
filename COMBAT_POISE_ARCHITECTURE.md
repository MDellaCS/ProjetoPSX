# Combat Poise e Reactions

## Fluxo de um golpe

1. Confirma o acerto e resolve bloqueio ou esquiva.
2. Aplica dano de vida.
3. Aplica Poise Damage e reinicia o atraso global de regeneracao.
4. Se ainda existe Poise, nenhuma reacao de controle e aplicada.
5. Com Poise em zero ou negativo, resolve a reacao solicitada pelo ataque.
6. Uma reacao proibida pelo alvo usa Stagger como fallback.
7. Sem reacao especial, a quebra causa Stagger no chao ou Juggle no ar.

## Responsabilidades

- `CombatPoiseSystem`: configuracao global de regeneracao e movimentos compartilhados.
- `CombatPoiseState`: estado e calculo reutilizavel de Poise do Player e dos inimigos.
- `EnemyReactionController`: valida imunidades e escolhe Stagger, Juggle ou Launch.
- `EnemyHealth` e `PlayerHealth`: vida, Poise proprio e duracao individual de Stagger.
- `EnemyMovement` e `PlayerMovement`: executam o movimento forcado ja autorizado.

## Superstagger

Um golpe que atravessa o limite de `-100%` do Max Poise causa Superstagger.
O stun usa duas vezes a duracao individual de Stagger. Se o golpe tambem causa
um Launch valido, o Superstagger e aplicado quando o movimento forcado termina.

## Regeneracao

Todos os golpes conectados reiniciam o timer, mesmo com zero Poise Damage.
Depois do delay global e fora de uma reacao, a taxa por segundo e:

`Max Poise * Regen Max Percent Per Second + Regen Flat Per Second`
