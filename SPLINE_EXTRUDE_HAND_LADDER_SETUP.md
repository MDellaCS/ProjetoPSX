# Escada na parede de um tubo Spline Extrude

O componente `SplineExtrudeHandLadderPlacer` gera uma escada `HandLadder` curva ao longo da parede de uma extrusao circular. A ferramenta usa o comprimento real do trecho, respeita o `Range` do `SplineExtrude` e nao depende de colliders para encaixar a escada.

## Configuracao

1. Selecione o objeto que possui o componente `Spline Extrude`.
2. Adicione **Add Component > Projeto PSX > Level Design > Spline Extrude Hand Ladder Placer**. Se o `Spline Extrude` estiver no mesmo objeto, ele sera selecionado como origem automaticamente. Caso contrario, arraste-o para o campo **Source**.
3. Use uma extrusao de perfil circular. A ferramenta usa o `Radius` do `Spline Extrude`; perfis quadrados ou personalizados nao correspondem a esse calculo.
4. Ajuste **Spline Index** se o container tiver mais de uma spline.
5. Escolha o lado da parede:
   - **Inside Tube** ligado: escada na face interna; o jogador fica virado para o interior do tubo.
   - **Inside Tube** desligado: escada na face externa.
   - **Wall Angle Degrees** gira a posicao ao redor do tubo a partir do vetor Up da spline. A previa verde ajuda a escolher a parede.
6. Ajuste **Wall Clearance** para afastar os trilhos da parede e **Ladder Width** para definir a largura.
7. Ajuste **Start Trim** e **End Trim** para afastar as pontas da escada das extremidades do trecho extrudido. Ambos sao medidos em metros ao longo da curva.
8. Ajuste **Sample Spacing** para controlar a fidelidade da curva. Valores menores acompanham curvas mais fechadas, mas geram mais segmentos.
9. Atribua um **Ladder Profile** para gerar degraus, trilhos e configurar o comportamento de escalada. Sem perfil, a escada continua interativa, mas a ferramenta nao gera as pecas visuais.
10. Pressione **Gerar / Atualizar escada**. A ferramenta cria uma escada curva filha do objeto de autoria. Para remover o resultado, pressione **Remover escada gerada**.

## Atualizar depois de editar a spline

A ferramenta nao atualiza a escada automaticamente. Depois de alterar a spline, o `Range`, o `Radius` ou os valores do posicionador, pressione **Gerar / Atualizar escada** novamente. A previa verde representa a configuracao atual antes da geracao.

## Como a escada e posicionada

A ferramenta amostra o comprimento em espaco de mundo para distribuir os pontos mesmo quando a spline tem velocidade variavel ou transformacao. Em cada ponto, usa a tangente para acompanhar a direcao do tubo e o vetor Up da spline para definir o angulo ao redor da secao circular. O raio e o recuo posicionam a superficie da escada; a largura segue a circunferencia. O resultado e gravado como espinha curva de uma `HandLadder` no modo `Surface`, com encaixe automatico por raycast desligado.

## Observacoes

- O `Spline Extrude` precisa ter `Spline Container`, ao menos uma spline valida e um `Range` com comprimento.
- O trecho util depois de aplicar os recuos precisa ter pelo menos 0,3 m.
- A espinha gerada tem no maximo 1024 amostras. Em trajetos muito longos, o espacamento efetivo pode ficar maior que `Sample Spacing` para manter o custo de consulta da escada limitado.
- Escala nao uniforme do container e considerada ao medir o comprimento; a geometria gerada e mantida em escala de mundo unitaria.
- A escada e gerada no Editor, nunca reconstruida por frame nem automaticamente durante Play.
- O topo usa saida pela frente do plano, adequada para continuar pelo interior do tubo. Revise a saida no layout do nivel, especialmente se o topo terminar numa plataforma.
- Regressao matematica: **Tools > Tests > Run Spline Extrude Hand Ladder Regression**.
- Teste com um tubo da cena de corredores: **Tools > Tests > Run Spline Extrude Hand Ladder Scene Test**. O teste abre `2. HoleHallways` em modo aditivo, gera uma escada temporaria, valida a espinha, a quantidade de degraus e a zona de entrada, e fecha a copia da cena sem salvar.

## Resultado da revisao em cena

O teste automatizado foi executado em uma copia isolada do projeto com a cena `2. HoleHallways`. A spline circular selecionada (`Spline (3)`) produziu uma escada interna de 351,51 m, com 1024 amostras e 586 degraus do perfil padrao. A zona geometrica de entrada tambem reconheceu um ponto de teste na metade do percurso. A copia da cena foi fechada sem salvar alteracoes.
