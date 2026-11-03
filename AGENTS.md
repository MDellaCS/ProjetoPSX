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
