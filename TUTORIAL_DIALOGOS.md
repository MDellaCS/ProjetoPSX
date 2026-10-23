# Tutorial — Diálogos de gameplay

Este é o fluxo do sistema de diálogo atual para Unity `6000.6.4f1`. Ele usa UI Toolkit e funciona durante gameplay. A integração com Timeline/cutscenes é propositalmente uma pendência no `TODO.md`.

## Recursos disponíveis

- Caixa de texto branca e discreta no centro inferior, sobre fundo escuro em camadas.
- Uma ou várias páginas em uma mesma sequência.
- Som opcional por página usando `AudioCue`.
- Fechamento por tempo, confirmação, ou ambos.
- Clique esquerdo ou `Enter` para avançar quando permitido.
- Fila global: mensagens concorrentes não se sobrepõem.
- Início por área (`DialogueTrigger`) ou por qualquer `UnityEvent` (`DialogueEvent`).

## 1. Instalar o HUD na cena

Faça uma vez por cena jogável:

1. Crie um GameObject vazio na Hierarchy chamado `DialogueHUD_UI`.
2. Adicione `Panel Renderer (UI Toolkit)` e `Dialogue Controller`.
3. No `Panel Renderer`, atribua o mesmo `Panel Settings` usado pelos HUDs existentes.
4. Em `Source Asset`, arraste [DialogueHUD.uxml](Assets/UI%20Toolkit/DialogueHUD.uxml).
5. Não adicione `UI Document`: neste projeto o `Panel Renderer` renderiza a UI.

O HUD fica invisível até uma sequência ser solicitada.

No `Dialogue Controller`, configure também `Minimum First Page Display Duration`. Esse é o tempo global de proteção contra pular sem querer a primeira página de qualquer diálogo; ele não afeta as páginas seguintes e usa o maior valor entre ele e o `Minimum Display Duration` da página.

## 2. Criar conteúdo de diálogo

1. Crie a pasta `Assets/Dialogues`.
2. No Project, clique com o botão direito:

   ```text
   Create > Projeto PSX > Dialogue Sequence
   ```

3. Nomeie, por exemplo, `DoorOpened_Dialogue`.
4. Em `Lines`, aumente a lista para criar uma ou mais páginas.

### Campos de uma página

| Campo | Função |
|---|---|
| `Text` | Texto exibido. |
| `Speaker Name` | Opcional; vazio mantém mensagem sem locutor. |
| `Voice Over Clip` | `AudioClip` opcional, tocado ao abrir a página. |
| `Voice Over Volume` | Volume da voz; não há variação aleatória. |
| `Finish Mode` | Como a página fecha. |
| `Display Duration` | Tempo em tela para modos temporizados. |
| `Allow Early Advance` | Permite clique esquerdo ou `Enter` fechar antes. |
| `Minimum Display Duration` | Evita fechar com o mesmo clique que abriu a fala. |

`Finish Mode`:

- `Timed`: fecha no fim de `Display Duration`.
- `Confirm`: só fecha por clique/`Enter`.
- `Timed Or Confirm`: encerra pelo primeiro dos dois.

Para uma mensagem comum, use:

```text
Finish Mode: Timed Or Confirm
Display Duration: 3
Allow Early Advance: ligado
Minimum Display Duration: 0.15
```

Para aviso importante, use `Confirm`.

## 3. Som opcional

Arraste o arquivo de voz para `Voice Over Clip` e ajuste apenas `Voice Over Volume`. A voz é sempre 2D, sem variação aleatória de volume ou pitch. Deixar o clip vazio é válido. O som toca apenas quando a página abre, não a cada frame.

## 4. Exibir ao entrar em uma área

1. Crie `DoorOpened_DialogueTrigger` na Hierarchy.
2. Adicione um `Box Collider`, com `Is Trigger` ligado.
3. Adicione `Dialogue Trigger`.
4. Arraste `DoorOpened_Dialogue` para `Sequence`.
5. Para uso único, deixe `Play Once` e `Disable After Play` ligados.

Ao entrar, o player enfileira a sequência. Se outra fala já estiver visível, ela espera.

## 5. Exibir por alavanca, encounter ou evento

`DialogueEvent` é uma ponte limpa para UnityEvents.

1. Crie `DoorOpened_DialogueEvent`.
2. Adicione `Dialogue Event`.
3. Atribua `DoorOpened_Dialogue` em `Sequence`.
4. No emissor — por exemplo `StrenghLever > On Activated` — clique em `+`.
5. Arraste `DoorOpened_DialogueEvent` para o listener e escolha:

   ```text
   DialogueEvent > Show()
   ```

Exemplo de organização:

```text
DoorLever
  └─ StrenghLever
       └─ On Activated → DoorOpened_DialogueEvent.Show()

DoorOpened_DialogueEvent
  └─ DialogueEvent
       └─ Sequence: DoorOpened_Dialogue

DialogueHUD_UI
  ├─ Panel Renderer
  │    └─ Source Asset: DialogueHUD.uxml
  └─ DialogueController
```

## 6. Teste recomendado

1. Dê Play com apenas `DialogueHUD_UI` configurado: nenhuma caixa deve aparecer.
2. Teste uma sequência de uma página por `DialogueTrigger`.
3. Teste timeout, clique e `Enter`.
4. Teste duas áreas em sequência para confirmar que a fila funciona.
5. Ligue `DialogueEvent.Show()` ao `On Activated` da `StrenghLever`.

## Limites desta versão

- Ainda não há diálogo dentro de cutscene/Timeline.
- Não há retratos, escolhas, typewriter ou localização.
- Diálogo de gameplay não bloqueia player nem pausa o jogo.
- Clique esquerdo também pode coincidir com o ataque. Para evitar fechar mensagens sem querer, prefira `Timed` ou aumente `Minimum Display Duration`.

Esses limites mantêm a primeira versão simples e desacoplada para a próxima etapa de integração com cutscenes.
