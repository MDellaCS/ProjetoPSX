# Design de Save do Jogo

## Decisoes confirmadas

- O jogo tera **5 slots manuais de campanha**.
- Cada campanha manual fica associada ao slot escolhido quando iniciada. O Santuario atualiza esse slot, em vez de pedir um slot novo a cada visita.
- Salvar no Santuario exige uma **confirmacao simples** antes de sobrescrever o progresso atual daquele slot. A interface deve identificar qual slot sera atualizado.
- O Santuario oferece somente a acao **Salvar**. Nao deve oferecer carregar jogo.
- **Carregar Jogo** fica no menu de pause.
- Havera **1 slot separado e exclusivo para save automatico de checkpoint**. Checkpoints sobrescrevem esse slot automaticamente, sem confirmacao, e nunca sobrescrevem os cinco slots manuais.

Assim, existem cinco espacos manuais escolhidos pelo jogador e um espaco automatico reservado ao checkpoint. O slot automatico nao e selecionavel como campanha manual no Santuario.

## Fluxo de uso

### Novo Jogo

O jogador escolhe um dos cinco slots manuais vazios. Se escolher um slot ocupado, o jogo informa que a campanha existente sera substituida e pede confirmacao.

### Santuario

Ao interagir, a interface mostra o slot ativo e um resumo curto do destino do save, como capitulo ou ultimo ponto de descanso. A opcao de salvar pede confirmacao simples; confirmar atualiza o slot ativo, cancelar retorna sem salvar. Nao ha opcao de carregar nesta tela.

### Checkpoint

Ao atingir um checkpoint, o jogo grava o progresso recente no slot automatico reservado. A gravacao ocorre sem interromper o fluxo de jogo e sem solicitar confirmacao.

### Carregar Jogo

No menu de pause, **Carregar Jogo** permite escolher uma campanha manual ou continuar a partir do progresso automatico mais recente. A UI deve identificar claramente os saves manuais e o checkpoint automatico, incluindo slots vazios.

## Informacoes da lista de saves

Para ajudar o jogador a reconhecer uma campanha, cada entrada deve priorizar:

- numero do slot e tipo (manual ou checkpoint automatico);
- capitulo ou area e local do ultimo save;
- tempo de jogo;
- data e hora da gravacao;
- indicacao de slot vazio, quando aplicavel.

## Em aberto para implementacao

- Definir quais dados de campanha e progresso entram em cada arquivo de save.
- Definir quais checkpoints gravam automaticamente e se o slot automatico pode ser usado para continuar apos morte ou retorno ao menu.
- Definir o comportamento de Carregar Jogo quando o slot automatico for mais recente que uma campanha manual.
- Definir layout visual final e se cada entrada tera uma imagem da area.

Estas decisoes de implementacao nao alteram os requisitos confirmados acima: cinco slots manuais, um slot automatico isolado, confirmacao simples para salvar no Santuario e carregamento pelo menu de pause.
