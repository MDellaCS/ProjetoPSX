## 2. Coletar um item do chão

1. Crie ou escolha um GameObject na cena para representar o item.
2. Adicione um Collider ao GameObject. Use uma forma e um tamanho que definam a área de interação. Ao adicionar `KeyItemPickup`, o Collider é configurado como Trigger.
3. Adicione **Projeto PSX > Itens > Coletável de item-chave** (`KeyItemPickup`).
4. Arraste a definição criada no passo 1 para o campo **Item**.
5. Ajuste **Interaction Message** se quiser mudar o texto do prompt, por exemplo, para “Examinar”.
6. Opcionalmente, atribua um objeto ao campo **Glint Visual**. Se deixar vazio, o sistema cria uma esfera dourada genérica durante o jogo. Ajuste **Glint Offset** para posicioná-la acima do objeto.
7. Posicione a área de interação para que o centro do jogador entre nela. O prompt contextual e o botão de interação são fornecidos pelo sistema geral de interação.

Ao interagir, o item é salvo no inventário, a modal é aberta e o coletável desaparece. Se o item já estiver no inventário, ele não será oferecido novamente.

## 3. Conceder como recompensa de diálogo

Use esta forma quando o item deve ser recebido depois que o jogador termina uma sequência de diálogo.

1. Na cena que contém o diálogo, crie um GameObject ativo para a recompensa ou escolha um objeto persistente apropriado.
2. Adicione **Projeto PSX > Itens > Recompensa por diálogo** (`KeyItemDialogueReward`).
3. Arraste a `DialogueSequence` correspondente para **Sequence**.
4. Arraste o item-chave para **Item**.
5. Deixe **Show Modal** marcado para apresentar a modal quando a recompensa for concedida.

A recompensa é concedida quando o fluxo ativo de falas termina por completo. Se a sequência tiver falas alternativas para saída antecipada, elas também fazem parte desse fluxo. Sair da área e encerrar o diálogo sem falas de saída não concede o item. Se a sequência for acionada novamente, a verificação de duplicatas impede uma segunda concessão.

## 4. Conceder durante ou depois de uma cutscene

`KeyItemGrantEvent` expõe o método público `Grant()` para UnityEvents e eventos de Timeline.

1. Na cena, crie um GameObject ativo para guardar a configuração da recompensa.
2. Adicione **Projeto PSX > Itens > Conceder item-chave** (`KeyItemGrantEvent`).
3. Atribua a definição ao campo **Item**.
4. Deixe **Grant On Start** desmarcado para conceder o item por evento. Mantenha **Show Modal** marcado se quiser exibir a modal.
5. Para conceder ao fim da cutscene, selecione o GameObject com `CutsceneController`, localize seu evento **On Finished** e adicione o GameObject da recompensa. Escolha `KeyItemGrantEvent > Grant()` no seletor de função.
6. Para conceder em um instante específico da Timeline, use um Signal/UnityEvent nesse instante e conecte-o a `KeyItemGrantEvent.Grant()`.

O evento **On Finished** é chamado quando a cutscene termina ou é pulada. Para conceder o item em ambos os casos, conecte a recompensa a esse evento.

## 5. Conceder no início de uma cena

1. Crie um GameObject ativo na cena que deve conceder o item.
2. Adicione **Projeto PSX > Itens > Conceder item-chave** (`KeyItemGrantEvent`).
3. Atribua a definição ao campo **Item**.
4. Marque **Grant On Start**.
5. Marque **Show Modal** para mostrar a modal quando a cena conceder o item.

O método roda uma vez quando o componente inicia. Se o jogador já tiver o item, a tentativa é ignorada e a modal não é aberta novamente.