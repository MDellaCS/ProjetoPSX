Abra a Timeline do `NewGameIntro` e adicione os tracks que compõem a sequência. Por exemplo:

1. **Cinemachine Track** para os cortes de câmera.
2. **Animation Track** para animações de objetos presentes na cena, como portas, luzes ou elementos do cenário.
3. **Activation Track** para ativar ou ocultar objetos durante a intro.
4. Tracks de áudio ou Signals para música, efeitos e ações pontuais.

### Cortes de câmera

1. Crie uma ou mais **Cinemachine Cameras** na cena, por exemplo `CM_Intro_Wide` e `CM_Intro_Close`.
2. Posicione cada câmera para os enquadramentos desejados. Para câmeras fixas, deixe `Follow` e `Look At` vazios.
3. Na Timeline, adicione um **Cinemachine Track** e crie Cinemachine Shot clips para cada câmera, ajustando seus tempos e transições.

Ao carregar `MainMenu`, o `GameSceneLoader` liga automaticamente o Cinemachine Track ao Brain da câmera persistente. Quando a intro começa, a prioridade de `MenuCameraPose` cai; a Timeline pode então comandar os cortes. No fim, a câmera de gameplay volta a acompanhar o jogador.

### Animação do jogador

O jogador é persistente e não pertence ao arquivo da cena `MainMenu`, então uma Animation Track não pode receber sua referência diretamente no Inspector. Para fazer a ligação automática:

1. Adicione um **Animation Track** à Timeline.
2. Renomeie o track para conter a palavra `Player`, por exemplo `Player Animation`.
3. Adicione o clip de animação ao track.

Quando a cena carrega, o `GameSceneLoader` associa esse track ao `Animator` encontrado nos filhos do jogador persistente. Durante a cutscene, o `PlayerAnimationPresenter` deixa de sobrepor o estado animado pela Timeline. Root Motion continua desativado: a animação apresenta o personagem, enquanto o gameplay permanece responsável pela física e movimento.

Para animar a posição física do jogador, use uma solução de movimento explicitamente preparada para cutscenes. Não dependa de Root Motion: o `CharacterController` e o sistema de movimento do jogador continuam sendo a autoridade da simulação.

### Objetos do cenário

Objetos que pertencem à cena `MainMenu` podem ser ligados diretamente aos tracks pelo Inspector da Timeline. Para uma ação pontual, use Signals e um `Signal Receiver`; o projeto já tem ações reutilizáveis como `CutsceneObjectAction` para mostrar ou ocultar objetos.

## 4. O que acontece ao apertar Novo Jogo

O fluxo fica assim:

```text
MainMenu aberto e pausado
  → clique em NOVO JOGO
  → menu fecha e a simulação volta
  → MenuCameraPose perde prioridade
  → NewGameIntro toca e bloqueia controles
  → Timeline termina ou é pulada
  → controles e HUDs de gameplay voltam
  → gameplay continua na própria cena MainMenu
```

Não é preciso adicionar um evento final para “despausar”: o `CutsceneController` libera o jogador quando a Timeline para, e o `GameSceneLoader` reativa os HUDs nesse momento. Configure a duração da Timeline e evite um último clip que a mantenha tocando indefinidamente.

Se a cena `MainMenu` ainda não tiver inimigos ou encontros, adicione nela o conteúdo que deve existir quando a intro terminar. O fluxo não carrega `Fase1` automaticamente.

## 5. Verificação no Editor

1. Salve `MainMenu` e confirme que `Bootstrap` está configurado para iniciar no Play Mode.
2. Entre em Play. O jogo deve chegar ao menu usando `MenuCameraPose`.
3. Confirme que mover o jogador na Hierarchy não altera o enquadramento da câmera do menu.
4. Clique em **NOVO JOGO**. O menu deve fechar sem trocar de cena.
5. Confirme que a Timeline toca e que os controles do jogador estão bloqueados durante a sequência.
6. Ao terminar ou pular a Timeline, confirme que o jogador pode agir e que os HUDs reaparecem.

Se o botão mostrar a mensagem de configuração ausente, confira o nome `NewGameIntro`, se o objeto está ativo na cena `MainMenu`, e se ele tem `PlayableDirector`, uma Timeline atribuída e `CutsceneController`.
