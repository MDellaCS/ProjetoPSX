# Level Up e loja compartilhada

Unity conferido: **6000.6.4f1**.

## Primeira configuracao

1. Aguarde a compilacao dos scripts. O instalador de Editor cria apenas assets e prefab, sem editar cenas.
2. Caso os assets ainda nao aparecam, execute **Tools > Projeto PSX > Progression > Create Shop Assets and Prefab**.
3. Arraste **Assets/Prefabs/World/UpgradeShop.prefab** para quantos pontos do mapa desejar.
4. Mantenha o HUD de prompts existente e o PauseMenuHUD no GameRoot. O PauseMenuHUD instala automaticamente o CurrencyShopHUD no mesmo painel.
5. Entre na area da loja e pressione **E**. Compre clicando e feche pelo botao, Q (pause configurado) ou cancelamento da UI. O cursor e a pausa sao restaurados ao fechar.

Todos os terminais usam **Assets/Resources/PlayerUpgradeCatalog.asset**. Nao existe configuracao de atributos ou precos por terminal.

## Estrutura dos dados

- Catalogo global: titulo e lista ordenada de atributos.
- **Assets/GameData/Player/Progression/Charge.asset**: Carga.
- **TBD1.asset**, **TBD2.asset**, **TBD3.asset**: atributos ainda indefinidos, exibidos e sem compras habilitadas.
- PlayerProgression: niveis persistentes, validacao de compra, pagamento e aplicacao dos beneficios. Cada atributo define seu proprio limite pela quantidade de entradas em Levels.
- CurrencyShopHUD: unica tela global; nenhum terminal precisa de um painel proprio.

O Player recebe PlayerProgression automaticamente por PlayerHeat. A carteira tambem e criada quando necessaria.

## Configurar um atributo

Crie **Create > Projeto PSX > Progression > Attribute**, configure e inclua na lista do catalogo. Remover um atributo dessa lista remove sua oferta e seus beneficios na proxima inicializacao, sem apagar o nivel salvo.

Cada atributo possui:

| Campo | Significado |
| --- | --- |
| ID | Identificador unico e persistente; nao renomear depois de distribuir saves |
| Display Name / Description | Texto da loja |
| Purchasable | Habilita compras; mantenha desligado nos TBD |
| Stat ID | Status que recebe o bonus; Carga usa max-heat |
| Levels | Lista ordenada: Element 0 = nivel 1; Element 1 = nivel 2; etc. Seu tamanho e o nivel maximo do atributo |

Dentro de **cada entrada de Levels**:

| Campo | Significado |
| --- | --- |
| Price | Preco inteiro para comprar este nivel |
| Gain | Incremento bruto concedido por esta compra |
| Retained Gain | Fracao mantida desse incremento, limitada a 0–1 |
| Unlocks | Especiais concedidos neste nivel; deixe vazio para nao desbloquear nada |

Cada entrada de **Unlocks** tem ID, Display Name, Description e uma lista **Effects** de assets PlayerUpgradeEffect. Um nivel pode conceder varios especiais e varios efeitos por especial; os desbloqueios anteriores permanecem ativos.

Para mudar a quantidade de niveis, adicione ou remova entradas de Levels. Nao e preciso modificar codigo, loja ou saves. Remover niveis limita temporariamente o nivel efetivo, preservando o nivel comprado no save.

O nivel 1 e sempre o estado inicial: zero bonus e nenhuma compra.

O bonus total e a soma dos incrementos comprados:

    bonus(nivel) = soma de Levels[i].Gain * clamp01(Levels[i].RetainedGain), dos niveis 2 ate nivel

Exemplo: ganho bruto 10 em todos os niveis e retorno retido 1, 0.8, 0.6, 0.4, 0.2 produz bonus totais 0, 10, 18, 24, 28, 30. A reducao afeta cada incremento, sem retirar ganhos anteriores.

Nao mude os IDs nem use IDs duplicados. O menu **Tools > Projeto PSX > Progression > Validate Global Catalog** verifica IDs e regras basicas dos dados.

## Carga padrao

Os valores sao dados iniciais editaveis no asset:

| Nivel | Incremento | Bonus total | Max Heat com base 100 |
| --- | --- | --- | --- |
| 1 | 0 | 0 | 100 |
| 2 | +0.5 notch | +5 Heat | 105 |
| 3 | +0.5 notch | +10 Heat | 110 |
| 4 | +0.5 notch | +15 Heat | 115 |
| 5 | +0.5 notch | +20 Heat | 120 |
| 6 | +1 notch | +30 Heat | 130 |

O ganho do nivel 6 substitui o incremento de 0.5 desse nivel, resultando em 3 notches extras no total. O nivel 6 desbloqueia **Rage**, ID **rage**, usando o efeito **Assets/GameData/Player/Progression/Rage.asset**.

O HUD representa cada notch com 10 Heat e exibe o ultimo bloco pela metade quando necessario. Cold continua usando sua propria capacidade.

O poder pesado usa os notches extras acima de 100. Quando a capacidade termina em meio notch, encher a barra permite gastar e aproveitar tambem essa fracao; abaixo do teto continuam os marcos completos de 10 Heat. Ex.: teto 105 cheio concede 10.5 notches de carga, multiplicador de dano 11.5x e consome 105 Heat.

Superheat so recebe excedente acima da nova capacidade. Carga nao aumenta Cold ou Superheat.

## Efeitos especiais e novos status

Um sistema pode consultar o PlayerProgression no Player:

    progression.Level(attribute);
    progression.StatBonus("max-heat");
    progression.StatBonus("outro-status");
    progression.HasSpecial("rage");

Para acoes configuradas no Inspector, adicione **PlayerUpgradeUnlockActions** ao Player. Em cada Binding informe o Special ID e ligue On Unlocked / On Locked a metodos publicos ou ativacao de objetos/habilidades. Saves ja desbloqueados disparam a aplicacao na inicializacao.

Para comportamentos especializados, derive um ScriptableObject de **PlayerUpgradeEffect**, implemente Apply(PlayerProgression) e Remove(PlayerProgression), e adicione o asset em Levels > Unlocks > Effects. Isso permite trocar movesets, habilitar ataques, instalar componentes ou aplicar regras de gameplay sem acoplar o sistema de compras a essas mecanicas.

## Rage

O fluxo com Rage desbloqueado e:

    Pre-Explosion -> Explosao + stun inicial -> Rage -> Burning -> stun de recuperacao

Sem o desbloqueio, a etapa Rage e pulada.

Em **Assets/GameData/Player/Progression/Rage.asset**, configure **Duration**, inicialmente 5 segundos de gameplay. Pause e hitstop local suspendem a contagem como no restante do sistema de Burn. Duration 0 desativa essa etapa.

Durante Rage:

- Heat permanece no Max Heat atual, incluindo os upgrades de Carga.
- Pesados usam o poder maximo sem gastar Heat ou a reserva de Superheat.
- Dano recebido nao reduz Heat; dano de Poise e reacoes normais continuam funcionando.
- Movimentos, dash, pulo, slide e ataques ficam liberados.
- Encerrar um ataque ou dash devolve o Player a Rage enquanto o tempo estiver ativo.
- O F3 mostra o desbloqueio e os segundos restantes.

Ao expirar, o sistema encerra ataques e traversal, inicia a caminhada lenta de Burning e reduz o Heat ate o Cold configurado, seguido da recuperacao normal.

Para conceder Rage em outro nivel ou atributo, mova ou adicione o desbloqueio e o mesmo asset de efeito na entrada desejada. Se varios efeitos de Rage estiverem ativos, vale a maior duracao concedida.

Assets sao compartilhados: guarde estado mutavel no Player/componente, nunca no asset. Apply e Remove devem ser simetricos. Remover um atributo/efeito do catalogo remove seu beneficio ao reaplicar a progressao; nao apaga o progresso salvo.

## Prefab da loja

    UpgradeShop
      Collider fisico + CurrencyShopTerminal + EnemyGore + WorldLockOnTarget
      VisualRoot
      InteractionArea
        SphereCollider (Is Trigger) + CurrencyShopInteractionArea

Substitua o modelo em VisualRoot e ajuste o collider fisico de forma independente. A esfera de InteractionArea define o alcance para abrir a loja e usa o HUD de prompts existente, com E ou icone opcional. Sua indicacao e reutilizavel e nao e consumida como uma dica de tutorial.

CurrencyShopTerminal configura **Required Damage** e dois modos de destruicao: **Accumulate Within Window** soma golpes enquanto o intervalo entre eles nao ultrapassar **Damage Window**; **Single Hit Only** exige todo o dano em um unico golpe. Golpes insuficientes continuam gerando Heat e impacto. EnemyGore configura perfil visual, quantidade de fragmentos coletaveis, intervalo de currency por fragmento e Overkill, exatamente como nos inimigos.

A area verifica o centro do Player diretamente dentro do volume do trigger, inclusive quando ele nasce dentro dela. A abertura tambem recupera a raiz da interface ja carregada pelo menu de pausa. Falta de painel ou catalogo gera um aviso explicativo no Console ao apertar E.

O Player pode travar mira no terminal, acertar ataques e gerar Heat, hitstop e tremor via o fluxo existente de IPlayerAttackTarget. O trigger de interacao e ignorado pela hitbox de ataque; apenas o collider fisico recebe os golpes.

Ao destruir, o objeto emite gore coletavel e fica desativado. As outras lojas continuam funcionando. Essa destruicao vale para a instancia carregada da cena; nao ha persistencia de destruicao de objetos de mapa.

## Saves e testes

Os niveis novos usam **ProjetoPSX.Progression.v2.ID** em PlayerPrefs. O prototipo antigo de Carga foi removido; seus niveis e bonus antigos nao sao importados. Currency usa a carteira existente.

Teste no Unity:

1. Comece no nivel 1: Max Heat base sem bonus; quatro atributos visiveis.
2. Entre/sai da area: prompt E aparece e some; duas areas sobrepostas escolhem o terminal mais proximo.
3. Abra: jogo pausado, cursor livre; Fechar/Q devolve movimento e camera.
4. Saldo insuficiente: compra indisponivel e sem debito.
5. Compre Carga: apenas um nivel por clique, preco correto, carteira descontada e Max Heat/HUD atualizados.
6. Va a outra instancia e troque de cena: o nivel e o mesmo. Pare e inicie Play: o nivel deve ser restaurado.
7. Chegue ao nivel 6: teto 130 com base 100, especial marcado e compra bloqueada.
8. Encha Heat em 105/115: pesado aproveita a meia carga; em 130 supera o poder do teto antigo.
9. Teste uma curva de retorno decrescente e confira os incrementos mostrados na loja.
10. Trave mira na loja e destrua-a: gore com currency, alvo invalidado, terminal desativado e demais lojas disponiveis.
11. Adicione um nivel 7 a um atributo e configure preco, ganho, retorno e desbloqueios: a loja deve mostrar o novo limite e os dados corretos.
12. Com Carga abaixo de 6, use F8: deve ir da explosao para Burning sem Rage. Com Carga 6, use F8: deve liberar movimentos apos a explosao/stun e manter Heat cheio durante Rage.
13. Durante Rage, ataque, de dash, pule e receba golpes: os controles funcionam, o Heat nao cai e a contagem continua. Ao fim, volta a caminhada lenta e o Heat comeca a cair.

Verificacao automatica realizada fora do Editor: compilacao dos projetos de runtime e Editor. Interacao visual e fisica precisam ser verificadas no Play Mode.
