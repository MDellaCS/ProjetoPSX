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
- `PlayerMovement` acumula locomocao e deslocamentos externos e executa um unico `PlayerCharacterController.Move` em `LateUpdate`; manter esse contrato ao adicionar novas fontes de movimento. Consumidores de colisao devem usar os flags do commit/callback, nunca assumir resultado sincrono ao enfileirar deslocamento.
- A separacao de entidades do jogador deve ocorrer antes do commit, em `Update` com ordem 1000. Ground snap so pode ocorrer se o jogador iniciou o movimento apoiado e nao esta subindo; parede nao conta como suporte de chao.
- A regressao de movimento deve consultar o frame do ultimo commit, pois callbacks de Editor podem rodar antes do `LateUpdate` do frame atual.