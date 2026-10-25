# TODO — Humanidade e Modo Humano

Status geral: **Pending — proposta de design, ainda não implementada**.

## Objetivo

Adicionar **Humanidade** como atributo opcional de progressão com seis níveis. Cada nível aumenta a dificuldade global do jogo e tem custo alto. O nível 6 desbloqueia o **Modo Humano**, uma forma de jogar mais vulnerável e com regras de combate próprias.

## Progressão de Humanidade

- [ ] Adicionar Humanidade como atributo independente no catálogo de upgrades, configurável sem acoplamento aos outros atributos.
- [ ] Configurar até seis níveis, com preço e efeitos próprios por nível.
- [ ] Tornar os níveis caros, com curva de preço configurável.
- [ ] Cada nível deve aumentar a dificuldade global por meio de modificadores configuráveis.
- [ ] Definir e balancear os modificadores globais de dificuldade. Candidatos para teste: vida dos inimigos, dano causado por inimigos, agressividade e composição dos encontros.
- [ ] Manter cada modificador centralizado para que encontros e inimigos recebam os valores de forma consistente.
- [ ] No nível 6, desbloquear o Modo Humano.

## Modo Humano

- [ ] Adicionar um toggle de teste em **F12**, disponível apenas após desbloquear Humanidade nível 6.
- [ ] Criar um controlador de modo que aplique e remova todas as regras do Modo Humano em conjunto.
- [ ] Permitir configurar um moveset próprio para o Modo Humano.
- [ ] O moveset humano deve ter alcance significativamente menor.
- [ ] Desativar geração de Heat no Modo Humano.
- [ ] Desativar o multiplicador de dano dos ataques pesados baseado em Heat.
- [ ] Implementar vida convencional e uma barra de vida comum para o jogador nesse modo.
- [ ] Manter as regras do Modo Humano independentes do moveset normal e do sistema de Heat, facilitando balanceamento e expansão.

## Morte permanente e dados salvos

- [ ] Ao morrer no Modo Humano, apagar o save de campanha para cumprir a regra de morte permanente.
- [ ] Separar dados de campanha de opções e configurações do jogador.
- [ ] A morte permanente deve apagar apenas os dados da campanha; opções e configurações devem permanecer.
- [ ] Validar o fluxo de morte e reinício antes de habilitar a regra fora de testes.

## Interface e configuração

- [ ] A loja deve exibir preço, nível atual, modificadores de dificuldade e desbloqueios de Humanidade.
- [ ] Exibir claramente que o nível 6 desbloqueia o Modo Humano.
- [ ] Adicionar tooltips em português para todos os novos campos configuráveis no Inspector.

## Etapas sugeridas

1. Definir quais modificadores compõem a dificuldade global e seus valores por nível.
2. Implementar Humanidade na progressão e na loja.
3. Implementar o controlador do Modo Humano, toggle F12, moveset, Heat e vida.
4. Integrar HUD de vida e validar entradas/saídas do modo.
5. Implementar morte permanente com dados de campanha isolados.
6. Testar cada etapa em uma cena de arena antes de usar em campanha.

## Critérios de conclusão

- Humanidade possui seis níveis configuráveis e aumenta a dificuldade global conforme os valores definidos.
- O nível 6 desbloqueia o Modo Humano e F12 alterna o modo para testes.
- No Modo Humano, o jogador usa moveset próprio, ataques curtos, não gera Heat e não usa multiplicador de Heat nos pesados.
- Vida e HUD convencionais funcionam no Modo Humano.
- Morrer no Modo Humano apaga somente o save de campanha, preservando as configurações do jogador.
- Todos os novos campos do Inspector têm tooltips explicativos em português.
