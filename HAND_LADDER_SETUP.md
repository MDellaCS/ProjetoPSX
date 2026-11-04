# Escadas de mão

## Perfil visual

1. Crie prefabs de degrau e, opcionalmente, de trilho. O pivô de cada prefab deve ficar no centro.
2. No Inspector de `Hand Ladder`, clique em **Create Profile** para criar e atribuir um asset em `Assets/Profiles/HandLadders`. Também é possível criar pelo menu `Create > Projeto PSX > Level Design > Hand Ladder Profile`.
3. No perfil, escolha qual eixo local do prefab corresponde à largura do degrau. Para os trilhos, escolha o eixo longitudinal e o eixo de espessura. O gerador mede a malha e ajusta esses eixos às dimensões desejadas; os campos de giro permitem orientar a aparência em torno deles.
4. Ajuste o espaçamento dos degraus, largura padrão e espessura dos trilhos. O mesmo perfil pode ser usado em várias escadas.

## Criar uma escada

1. Crie um GameObject para a escada. **Climb Axis** define a direção local da base ao topo; **Approach Axis** aponta para fora, no lado por onde o jogador se aproxima. Os padrões são Y e Z. Rotacione o GameObject ou edite os vetores para configurar escadas inclinadas e outras orientações.
2. Adicione `Projeto PSX > Level Design > Hand Ladder` e atribua o perfil.
3. Ajuste altura, largura e espaço adicional. O objeto filho `[Generated] Hand Ladder` é atualizado no Editor quando os parâmetros mudam e contém os módulos visuais e a área contextual de interação. Editar o perfil atualiza as escadas que o utilizam nas cenas carregadas.
4. `TopExit` e `BottomExit` são criados automaticamente como transforms filhos nos extremos. Mova-os na Scene View ou atribua outros transforms para ajustar as saídas. Pontos automáticos acompanham alterações de altura; pontos movidos manualmente mantêm sua posição nas regenerações.
5. Os colliders dos prefabs visuais são desativados para evitar colisões duplicadas. A escada não bloqueia fisicamente o jogador.

## Jogar

- A área da escada mostra o prompt contextual genérico. Pressione **Interagir** para subir.
- Segure cima/baixo no eixo de movimento para escalar; solte para parar.
- Pressione Interagir ou Agachar para sair. Pulo permite soltar-se para o lado de aproximação.
- Nos extremos, continue segurando na direção de saída para usar `TopExit` ou `BottomExit`.
- Um estado visual `LadderClimb` pode ser associado a uma animação/blend tree no Animator. Sem binding, o sistema usa Idle como pose neutra.

## Limites atuais

- A geração cria objetos editáveis separados e não combina malhas. Para muitas escadas, considere bakear a geometria.
- O sistema ainda não fornece clips, IK de mãos/pés ou animações de transição.
- Confira se há espaço livre nos pontos de saída; reposicione-os quando necessário.
