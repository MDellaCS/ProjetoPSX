# Instruções do projeto

- Versão do Unity: `6000.6.4f1`.
- Pode ler, mas nunca modificar arquivos `.unity`.
- Fazer commits e pushes apenas quando solicitado.
- O jogo é um hack-and-slash inspirado em God of War, Devil May Cry e Dark Souls.
- O estilo visual é retrô de PS1-PS2, com modelos low/mid-poly.
- As mecânicas devem priorizar agressividade extrema e recompensar decisões de risco.
- "F3" ou "o F3" referencia a tela de debug com valores do jogo/jogador. Quando criar um gizmo, exiba-o quando em F3.
- Ao adicionar novos atributos expostos no Inspector, sempre incluir tooltips explicativos em portugues, descrevendo seu efeito no jogo.
- Sempre executar os testes automatizados e validacoes apropriados depois de alterar codigo.
- Executar testes em cena apenas quando o usuario pedir explicitamente. Sem esse pedido, nao iniciar o jogo nem abrir cenas para testar; usar testes automatizados ou verificacoes fora de cena e informar limitacoes relevantes.
- HUDs novos devem seguir o padrao UI Toolkit do projeto: estrutura visual em arquivo `.uxml`, estilos em arquivo `.uss` e script C# separado para comportamento e atualizacao dos elementos. Usar `PanelRenderer` e consultar os elementos definidos no UXML, sem construir toda a interface programaticamente no script.
- A fonte padrao de todos os textos do jogo deve ser Crimson Text, exceto quando uma solicitacao explicitar outra fonte.

## Padroes de manutencao confirmados

- Atalhos que alteram estado de jogo, moveset, Heat, combate, tempo ou cena devem funcionar somente no Editor ou em builds de desenvolvimento. Em builds de release, verificacoes de debug devem retornar falso sem ler os bindings.
- Reset de progresso deve apagar somente chaves pertencentes ao ProjetoPSX; nunca usar `PlayerPrefs.DeleteAll`, pois isso tambem apaga preferencias da Unity. Ao adicionar uma nova chave persistente, incluir sua limpeza no fluxo explicito de novo jogo.
- Como Domain Reload e Scene Reload estao desligados, estado `static` mutavel precisa de reset em `RuntimeInitializeOnLoadMethod(SubsystemRegistration)`. Eventos estaticos devem ser limpos e registros devem ser reconstruiveis por `OnEnable`/`OnDisable`.
- Componentes Unity devem ser comparados com `== null`/`!= null` ou verificados com `TryGetComponent`; nao confiar em `?.`, `??` ou `??=` para detectar componentes destruidos ou ausentes.
- Evitar consultas de cena inteira em callbacks de objetos pooled e em caminhos por frame. Para colisao de particulas, manter a layer `CombatParticle` e a filtragem por colisao sem enumerar todos os colliders da cena.
- Ao ajustar uma hitbox ou collider em resposta a input, fazer o early-out antes de consultas fisicas. Preferir consultas `NonAlloc` quando a operacao puder ocorrer repetidamente.
- `EnemyBase.OnDisable` e um ponto comum de liberacao dos registros do Director e da fila de ataques. Classes derivadas que implementem `OnDisable` devem sobrescrever o metodo base.
- A UI Toolkit deve corrigir propriedades USS com os nomes suportados (`-unity-font-style`, `-unity-text-align`) e ignorar picking no UXML com `picking-mode="Ignore"`, nao com propriedades USS inexistentes.
- Testes de cena devem abrir a cena correta sem salvar alteracoes; quando o projeto for configurado para iniciar Play pelo Bootstrap, um teste que precise de outra cena deve definir explicitamente sua cena inicial e restaurar a configuracao anterior ao terminar.

## Registro da revisao do projeto (2026-11-03)

### Feito nesta revisao

- Corrigidos os Build Settings para incluir `1. MainMenu` e `3. Room`, e remover `999. TestArena` da build de release. Falhas de resolucao/carregamento agora sao reportadas.
- Atalhos de debug foram limitados a builds de desenvolvimento; removido o reset de saves por tecla. O reset explicito apaga somente as chaves de progresso do ProjetoPSX e bloqueia reentrada durante carregamento.
- Corrigidos bugs de Heat/Cold, reaplicacao de Rage apos Game Over, dano de inimigos e confirmacao de acerto, morte durante ataque, pausa/slow motion, persistencia de registros estaticos e limpeza de inimigos derivados.
- Corrigidos alvos de input e feedback: bindings de gamepad para ataque pesado, lock-on, troca de alvo e pausa; acoes configuraveis para swing e skip de cutscene; sensibilidade de camera separada para mouse e gamepad; prompts consultam o binding atual.
- Reduzidas alocacoes/consultas recorrentes na hitbox do jogador, Director, filtro de particulas, hitstop e separacao de entidades. Corrigidos reinicio de currency em pool, protecao da currency no pool visual e limites de spawn/lever.
- Corrigidos erros de sintaxe USS/picking mode e aplicada Crimson Text nos estilos UI Toolkit existentes. Corrigida a cena e a restauracao do ponto de entrada no teste `SelfLaunchDurationRegression`.
- `LoadingScreenHUD` usa UI Toolkit com UXML/USS e `PanelRenderer`; entrada de gameplay/UI fica bloqueada durante o carregamento. O marcador de lock-on tambem foi migrado de IMGUI para UI Toolkit.
- Corrigidos novos casos de movimento apos morte/stagger/burn, consultas de colisao de inimigos/slide com buffers nao alocadores, timeout de aproximacao e liberacao de turno nas continuacoes de ataques, busca de Volume global cacheada, e comportamento de pausa/skip de cutscene e prompts.
- `EnemyDeathLoopTest` e gore de teste ficam restritos ao Editor/build de desenvolvimento; a regressao nao inicia o loop automaticamente quando configurada para nao iniciar.
- Validacao: builds `Assembly-CSharp` e `Assembly-CSharp-Editor` passaram com 0 avisos/erros; JSON de Input Actions valido e IDs unicos; `git diff --check` sem erros.

### Bloqueado para o agente; acao do usuario no Editor

- `AGENTS.md` proibe editar qualquer `.unity`. A cena `Assets/Scenes/999. TestArena.unity` nao declara `AudioListener`; tentativas em Play geraram avisos repetidos sem relatorio, e a execucao automatizada mais recente saiu com codigo 1 sem iniciar o teste enquanto ja havia processos do Unity Editor abertos. Adicione/verifique um `AudioListener` na camera de gameplay da TestArena (garanta que nao haja dois ativos) e rode **Tools > Tests > Run Self Launch Duration Regression**. A cena nao foi salva nem alterada pelo agente.
- Qualquer ajuste de objetos, componentes, referencias serializadas ou posicionamento que exista somente dentro de uma cena `.unity` precisa ser feito pelo usuario no Editor. Nenhuma cena `.unity` foi modificada.

### Ainda pendente

- Repetir a validacao de gameplay em cena apos verificar o AudioListener; a regressao nao esta aprovada ate obter `PASS` no log.
- A refatoracao de `PlayerMovement` para um unico `Move` por frame e extensa e requer validação de gameplay; os varios caminhos atuais mudam estado de grounded e colisao. Rever itens remanescentes em `REVISAO_PROJETO.md` antes de expandir o escopo.
- A revisao ainda nao foi convertida em testes NUnit/CI; os testes existentes continuam sendo validacoes por MenuItem.
- A lista concreta do que depende do usuario esta em [PENDENCIAS_USUARIO.md](PENDENCIAS_USUARIO.md).
