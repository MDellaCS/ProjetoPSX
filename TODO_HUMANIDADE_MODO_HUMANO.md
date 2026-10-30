# TODO — Humanidade e Modo Humano

Status geral: **Pending — a progressão de Humanidade, o aumento de dificuldade e a morte permanente ainda não foram implementados. O moveset com recurso Health já existe como base para o Modo Humano.**

## Objetivo

Adicionar **Humanidade** como atributo opcional de progressão com seis níveis. Cada nível aumenta a dificuldade global do jogo e tem custo alto. O nível 6 desbloqueia o **Modo Humano**, uma forma de jogar mais vulnerável e com regras de combate próprias.

## Progressão de Humanidade — pendente

- [ ] Adicionar Humanidade ao catálogo de upgrades com seis níveis configurados em seus dados, preços altos e desbloqueio especial no nível 6.
- [ ] Definir e balancear os modificadores globais de dificuldade por nível. Candidatos: vida e dano dos inimigos, agressividade e composição dos encontros.
- [ ] Centralizar a aplicação dos modificadores para que inimigos e encontros usem os mesmos valores.
- [ ] Vincular o desbloqueio do nível 6 ao Modo Humano.

## Ativação do Modo Humano — pendente

- [ ] Adicionar o toggle de teste **F12**, habilitado somente após Humanidade nível 6, para entrar e sair do moveset Human.
- [ ] Garantir que a troca pelo toggle aplique o moveset e o recurso Health juntos, sem deixar regras de Heat ativas.

## Morte permanente e dados salvos

- [ ] Separar dados de campanha de opções e configurações do jogador, caso ainda compartilhem armazenamento.
- [ ] Ao morrer no Modo Humano, apagar apenas os dados da campanha e preservar as opções/configurações.
- [ ] Validar o fluxo de morte e reinício antes de habilitar a regra fora de testes.

## Interface e configuração

- [ ] Exibir na loja os modificadores de dificuldade e o desbloqueio do Modo Humano; preço e nível já são exibidos pelo sistema de upgrades.
- [ ] Adicionar tooltips em português para todos os novos campos configuráveis no Inspector.

## Próxima ordem de implementação

1. Configurar Humanidade e seus modificadores de dificuldade no catálogo.
2. Vincular o desbloqueio ao nível 6 e adicionar o toggle F12.
3. Isolar o save de campanha e implementar morte permanente.
4. Exibir os efeitos de Humanidade na loja e validar o fluxo em uma arena.

## Critérios de conclusão

- Humanidade possui seis níveis configuráveis e aumenta a dificuldade global conforme os valores definidos.
- O nível 6 desbloqueia o Modo Humano e F12 alterna o modo para testes.
- Morrer no Modo Humano apaga somente o save de campanha, preservando as configurações do jogador.
- Todos os novos campos do Inspector têm tooltips explicativos em português.
