# TODO — GIFs para a tela de loading

## Objetivo

Futuramente, substituir o spinner atual da tela de loading por uma lista de GIFs animados. A cada carregamento, escolher um GIF aleatoriamente e evitar que o mesmo GIF seja exibido em dois carregamentos consecutivos, sempre que houver mais de uma opção disponível.

## Possibilidades de animação

Estas ideias são opções visuais para avaliar; a escolha final dos GIFs fica para depois.

1. **Agulha de máquina de costura** — favorita. Uma agulha sobe e desce continuamente, atravessando um pequeno pedaço de tecido. Movimento mecânico, hipnótico e fácil de repetir perfeitamente. Loop sugerido: 0,6–1 segundo.
2. **Roda de locomotiva** — uma pequena roda ferroviária girando, com a biela fazendo o movimento de vai e vem. Animação industrial que funciona muito bem em silhueta. Loop sugerido: 1–1,5 segundo.
3. **Carretel de linha** — um carretel girando lentamente, com um fio saindo pela lateral e desaparecendo na escuridão. Minimalista e com uma silhueta reconhecível. Loop sugerido: 1,5–2 segundos.
4. **Sinal ferroviário mecânico** — uma pequena haste de sinalização alterna entre duas posições, com uma breve pausa em cada uma. Pode sugerir que o caminho está sendo liberado. Loop sugerido: 2–3 segundos.

## Requisitos a considerar na implementação

- Manter uma lista configurável de GIFs disponíveis para a tela de loading.
- Sortear uma opção a cada carregamento.
- Guardar a opção anterior e excluí-la do sorteio seguinte para impedir repetição consecutiva, inclusive quando o carregamento atual termina rapidamente.
- Se houver apenas um GIF disponível, permitir sua seleção normalmente.
- Preservar o comportamento atual da tela de loading enquanto não houver GIFs configurados ou até esta tarefa ser implementada.
- Avaliar a importação e reprodução de GIFs na Unity e escolher uma solução compatível com os recursos e dependências já usados no projeto.
