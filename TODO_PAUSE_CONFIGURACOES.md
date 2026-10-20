# TODO — Pause e configurações

Plano incremental para o menu de pausa e as opções do jogo. A ordem permite testar cada etapa antes de ampliar a interface.

## Etapa 1 — Núcleo de pausa [Concluída em código; validar no Unity]

- [x] Centralizar os pedidos de pausa de menu e prompts, restaurando o tempo somente após o último pedido terminar.
- [x] Deixar o `PauseMenuHUD` responsável apenas pelo atalho, botões, cursor e apresentação.
- [x] Bloquear input de gameplay e descartar ações em buffer durante a pausa, sem interferir no input exigido por prompts.
- [x] Preservar o menu atual com `Q`, Continuar, Configurações como placeholder e Sair.
- [x] Validar no Play Mode: pausa isolada; menu sobre prompt que pausa; saída de Play Mode pausado; nenhuma ação executada ao retomar.

## Etapa 2 — Entradas configuráveis [Concluída em código; validar no Unity]

- [x] Reaproveitar o asset `InputSystem_Actions.inputactions` definido como Project-wide Actions; mapas `Gameplay` e `UI`.
- [x] Centralizar movimento, combate, câmera, lock-on, pause e atalhos de debug em `GameInput`.
- [x] Alternar os mapas pela pausa e manter o buffer sem entradas residuais.
- [x] Preservar leitura direta somente para prompts de sequência arbitrária de teclas/botões e detecção da presença do mouse; esses inputs são dinâmicos.
- [x] Validar sintaxe do asset e compilação C# (0 erros, 0 warnings).
- [x] Validar no Play Mode: WASD, cliques, Space, Shift, botão lateral, câmera, lock-on, F3–F8, Q, navegação/clique no menu e retorno sem input preso.
- [x] O menu lê `UI` pelo `GameInput` e resolve clique/foco por código; não depende da troca do asset já referenciado pelo EventSystem da cena.

## Etapa 3 — Serviço de configurações [Concluída em código; validar no Unity]

- [x] Definir valores padrão e limites para áudio, vídeo, câmera e acessibilidade; controles usam os padrões do asset de ações.
- [x] Aplicar prévia, salvar/carregar preferências em PlayerPrefs e restaurar valores ao cancelar.
- [x] Restaurar padrões por seção e normalizar dados inválidos, incluindo NaN e valores fora dos limites.
- [x] Validar persistência e comportamento de tela cheia/qualidade em uma build.

## Etapa 4 — Tela de configurações [Concluída em código; validar no Unity]

- [x] Criar páginas em UI Toolkit para Áudio, Vídeo, Câmera, Controles e Acessibilidade.
- [x] Ligar as opções ao serviço com prévia, Aplicar, Cancelar e padrões por seção.
- [x] Permitir mouse e navegação/seleção por teclado ou controle; o menu usa coordenadas de painel para o fallback de clique do PanelRenderer.
- [x] Validar legibilidade e rolagem dos controles no Play Mode e em resoluções menores.

## Etapa 5 — Rebinding e acabamento [Concluída em código; validar no Unity]

- [x] Capturar novos bindings pelo Input System, rejeitar conflitos no mesmo mapa, cancelar com Esc e restaurar padrões.
- [x] Persistir overrides, espelhar Pause/F3 nos dois mapas e atualizar dicas de ataque/dash no PlayerHUD e de pause no menu.
- [x] Testar no Play Mode e em build: pausa simples, pause sobre prompt, game over, troca de cena, saída durante pause, rebinding, cancelamento e retorno sem ação residual.