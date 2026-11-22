# TODO — Mensagens centrais de eventos

## Objetivo

Implementar um sistema desacoplado e extensível para exibir mensagens grandes, temporárias, centralizadas horizontalmente e posicionadas um pouco abaixo do centro da tela, acima da gameplay. O painel de pausa deve permanecer visualmente acima dessas mensagens.

Na primeira entrega, os eventos que solicitam mensagens são apenas:

- Derrota de chefe: `CHEFE DERROTADO` (texto final pode ser ajustado para o idioma/tom do jogo).
- Morte do jogador: `VOCE MORREU`.

As mensagens não devem aparecer simultaneamente. Solicitações devem ser processadas uma de cada vez em fila, com política explícita para duplicatas e validade de solicitações obsoletas (por exemplo, mensagem de chefe que só seria exibida depois de uma morte e de uma recarga de cena).

## Pesquisa e referência de design

- Em jogos Soulsborne, a mensagem grande `YOU DIED` é uma convenção recorrente exibida quando o personagem morre, como registrado no estudo acadêmico *Death Mechanics* (seção 533 e figura 2): https://uu.diva-portal.org/smash/get/diva2%3A1762862/FULLTEXT01.pdf
- A derrota do chefe é associada à mensagem `YOU DEFEATED`; uma referência secundária documenta o texto no contexto de Dark Souls Remastered: https://www.nexusmods.com/darksoulsremastered/mods/925?tab=description
- A pesquisa confirmou os tipos de evento e o destaque visual, mas não encontrou evidência confiável de que os jogos Souls implementem uma fila compartilhada que serializa estas duas mensagens. A fila deste projeto é um requisito próprio, não uma afirmação sobre a implementação interna dos jogos de referência.

## Investigação obrigatória antes de implementar

- Revisar `Assets/Scripts/UI/AreaTitleHUD.cs`, `GameOverHUD.cs`, `BossHealthHUD.cs`, `PlayerHUDToolkit.cs` e os respectivos UXML/USS como referências de UI Toolkit, `PanelRenderer`, ordem de desenho e ciclo de vida.
- Revisar `Assets/Scripts/Player/PlayerGameOver.cs`, `Assets/Scripts/Player/PlayerDeath.cs`, `Assets/Scripts/Enemy/EnemyHealth.cs`, `Assets/Scripts/Enemy/EnemyBase.cs` e os emissores de eventos de morte de chefe para localizar pontos de integração sem acoplar o apresentador da mensagem aos sistemas de gameplay.
- Revisar `Assets/Scripts/Systems/GamePauseController.cs` e `PauseMenuHUD.cs` para garantir que a mensagem não cubra o pause. Não modificar cenas `.unity` sem solicitação explícita.
- Confirmar se o projeto já dispõe de um evento/contrato reutilizável para morte do jogador e derrota de chefe; preferir esse contrato a buscas globais ou dependências diretas entre HUD e gameplay.
- Confirmar o asset/configuração existente de Crimson Text para UI Toolkit antes de escolher a referência tipográfica.

## Requisitos funcionais

- Expor uma API pequena para que qualquer produtor futuro solicite uma mensagem sem conhecer o elemento visual ou o `PanelRenderer`.
- Apresentar apenas uma solicitação por vez. A próxima começa depois que a atual termina ou é descartada por regra de validade definida.
- Definir comportamento para duplicatas, fila cheia (se houver limite), mensagens inválidas e reset ao trocar/recarregar cena. Evitar que mensagens antigas apareçam após a cena de destino já ter carregado.
- Mensagem de morte deve ser solicitada no fluxo de morte já existente, mantendo o timing de Game Over coerente e evitando duas mensagens concorrentes de morte.
- Mensagem de chefe deve ser solicitada uma única vez quando um chefe realmente morre, não quando um inimigo comum morre; considerar chefes com múltiplas fases ou morte diferida, conforme os padrões existentes.
- A pausa deve sempre renderizar acima da mensagem. Definir como a animação/tempo de permanência se comporta durante pause; a recomendação é congelar enquanto pausado, sem impedir que o pause abra imediatamente.
- Garantir ordenação visual acima da HUD de gameplay e abaixo do pause conforme os mecanismos existentes do `PanelRenderer`.
- Não iniciar Play Mode nem abrir cenas para teste sem autorização explícita.

## Requisitos visuais e de UI

- Seguir a arquitetura existente de UI Toolkit: `.uxml` para estrutura, `.uss` para estilo e `.cs` para comportamento; usar `PanelRenderer`.
- Usar Crimson Text e assets de fonte existentes.
- Texto grande, legível e centralizado, posicionado ligeiramente abaixo do centro vertical. Adaptar a diferentes resoluções e textos longos sem cortar.
- Criar transições discretas de entrada/saída e tempo de permanência configurável por tipo de mensagem; manter o visual compatível com a direção retrô/terror do projeto.
- Respeitar reload/recriação da árvore visual do `PanelRenderer` e limpar inscrições/eventos corretamente.
- Qualquer atributo novo exposto no Inspector deve ter `[Tooltip]` em português e manter referências/valores serializados existentes quando aplicável.

## Arquitetura sugerida (a validar com os padrões do repositório)

- Separar o serviço/roteador de mensagens (API e fila), o apresentador visual UI Toolkit e os produtores de eventos (morte/chefe).
- Usar payload enxuto com texto ou chave de mensagem, prioridade, duração e política de duplicata/expiração apenas se esses conceitos forem necessários para os usos futuros previstos. Evitar criar um framework genérico excessivo para dois eventos iniciais.
- Manter dependências unidirecionais: gameplay emite/solicita evento; sistema de apresentação enfileira e apresenta; UI não decide regras de combate ou morte.
- Preferir evento ou serviço já existente no projeto. Evitar singleton novo se `AreaTitleHUD`/outro sistema ou o root persistente já fornecer padrão apropriado.
- Documentar no código as garantias de ordem, pause e ciclo de vida, sem criar documentação Markdown adicional.

## Casos de validação

- Derrotar chefe durante gameplay: uma mensagem de vitória aparece e a HUD de chefe pode ser encerrada sem interferência.
- Morrer durante gameplay: a mensagem de morte aparece no momento apropriado e não duplica a mensagem existente de Game Over.
- Solicitar vitória e morte no mesmo intervalo: nunca sobrepor; processar segundo a política documentada e não exibir mensagem obsoleta após reinício/troca de cena.
- Abrir pausa durante entrada, permanência e saída: pause fica acima; mensagem congela ou segue a política escolhida de forma consistente.
- Eventos repetidos de morte/derrota não enfileiram cópias indevidas.
- Reconstrução/reload do UI Toolkit não perde permanentemente a mensagem ativa nem deixa callbacks duplicados.
- Verificar telas/resoluções e garantir legibilidade sem abrir cenas em Play Mode caso não haja autorização.

## Escopo futuro (não implementar nesta entrega)

Manter a API apta a servir, se o jogo vier a precisar, outros eventos curtos de alto destaque: início de chefe/entrada em arena, invasão ou emboscada, objetivo concluído, nova área descoberta, item-chave obtido, evolução/desbloqueio, retorno de uma corrida de cadáver, fase nova de chefe, interação narrativa decisiva e mensagens contextuais de falha/sucesso. Validar cada uso para não banalizar o destaque visual.

## TODO adicional — BossFightController e classificação de chefes

Criar um `BossFightController` responsável por coordenar entidades marcadas como chefes e o(s) HUD(s) de vida de chefe. Deve ser projetado desde o início para múltiplos chefes ativos na mesma luta, adicionando e gerenciando cada entidade sem pressupor que exista apenas um chefe.

### Classificação

- Permitir marcar um inimigo como chefe por configuração clara no Inspector, preferindo um componente/flag serializado compatível com as convenções existentes.
- Permitir configurar o nome apresentado no HUD; quando vazio, usar um fallback coerente com o nome da entidade.
- Não inferir classificação pelo nome do prefab ou por estar dentro de um `EnemyEncounter`.
- Manter os RagDolls comuns sem alteração de classificação; o clone de teste `RagDoll_Boss.prefab` deve poder ser marcado como chefe explicitamente.

### Responsabilidades e comportamento

- Garantir que cada chefe registrado mantenha visão/alvo do jogador e permaneça em estado de combate enquanto a luta estiver ativa, evitando o comportamento de sair do combate ou abandonar o alvo. Investigar o funcionamento de detecção, perda de alvo, busca e engajamento de `EnemyBrain` e `EnemyBase` antes de escolher o ponto de extensão; não duplicar nem sobrescrever a IA ofensiva.
- Registrar e remover chefes conforme habilitação, morte, destruição e troca de cena, sem inscrições duplicadas ou referências obsoletas.
- Gerenciar as entidades chefes ativas e exibir/atualizar o HUD de vida apropriado para múltiplos chefes. As barras devem ficar empilhadas verticalmente, uma acima da outra, com espaço para o nome de cada chefe. Não descartar silenciosamente nem ocultar informação de chefes ativos.
- Quando um chefe morrer, manter sua barra visível pelo delay configurado e então removê-la. Após a remoção, reorganizar a ordem/posição visual das barras restantes sem sobreposição ou salto de layout indevido. Enquanto aguarda o delay, a barra do chefe morto deve indicar claramente que ele foi derrotado (por exemplo, vida zerada).
- Responsabilizar-se pelo ciclo de vida do HUD de chefe: apresentação ao iniciar/entrar em combate, atualização de vida e encerramento ao morrer ou sair da luta.
- Solicitar uma única mensagem de vitória pelo sistema de mensagens centrais somente quando o último chefe da luta morrer, respeitando a fila e evitando mensagens duplicadas. A morte de chefes anteriores não deve exibir mensagem de vitória.
- Manter produtores de gameplay desacoplados do layout visual. O controller coordena dados e ciclo de vida; `BossHealthHUD` continua responsável pela apresentação UI Toolkit.
- Considerar pausa, morte do jogador, cenas e encontros sem um jogador resolvido. Não forçar combate nem manter HUD visível fora de uma luta válida.

### Investigação e validação específicas

- Revisar `EnemyBrain`, `EnemyBase`, `EnemyEncounter`, `EnemyEncounterWave` e `BossHealthHUD`, além dos pontos atuais que configuram alvo/estado de combate. Avaliar se `EnemyBase.SetEncounterEngaged` ou API similar atende, e identificar efeitos colaterais de mantê-lo ativo continuamente.
- Verificar como o projeto associa `BossHealthHUD` ao jogador/Canvas/`PanelRenderer` nas cenas e como a ordem visual deve coexistir com a HUD geral, mensagens centrais e pause.
- Validar dois ou mais chefes simultâneos: barras empilhadas com espaço para nomes, cada barra reflete a entidade correta, morte de um chefe mantém sua barra por todo o delay e depois reorganiza as restantes; a vitória só aparece após a morte do último chefe.
- Validar entrada/saída do jogador, chefe desabilitado/destruído, morte, recarga de cena, pausa e mais de uma fase de chefe.
- Manter cenas `.unity` somente leitura sem solicitação explícita; não testar em Play Mode sem autorização.

## Entrega do GPT-6.2 Astra

- Implementar apenas após revisar os pontos acima e confirmar os produtores reais de eventos.
- Preservar as restrições do `AGENTS.md`: cenas `.unity` somente leitura por padrão; não criar outros `.md`; testes automatizados e verificações fora de cena após mudanças de código; declarar validações não executadas.
- Ao concluir, resumir arquivos alterados, fluxo da fila, comportamento no pause e validações realizadas.
