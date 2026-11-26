# AGENTS.md — Unity Game Development

## 1. Contexto do projeto

Este repositório contém um jogo desenvolvido na **Unity 6000.6.4f1**, utilizando C#.

### Identidade do jogo

- **Gênero:** hack-and-slash de terror.
- **Inspirações:** God of War, Devil May Cry, Dark Souls e Silent Hill.
- **Estética:** visual genérico durante a fase atual de desenvolvimento, sem restrições de estilo ou contagem de polígonos.
- **Combate:** agressivo, intenso e orientado a decisões de risco.
- **Filosofia de gameplay:** recompensar agressividade, domínio mecânico e decisões arriscadas, mantendo consequências significativas para erros.

As inspirações devem orientar decisões de design, não resultar na reprodução direta de mecânicas ou sistemas sem considerar a identidade do projeto.

Toda implementação deve respeitar os sistemas, convenções e decisões já existentes no repositório.

---

## 2. Perfis de agentes

O projeto utiliza três perfis de agentes, com responsabilidades distintas.

### GENERAL — GPT-6.1 Terra

**Agente padrão para desenvolvimento cotidiano.**

Responsável por:

- Corrigir bugs simples e intermediários.
- Implementar funcionalidades pequenas e médias.
- Criar, modificar e integrar scripts C#.
- Desenvolver mecânicas comuns de gameplay.
- Realizar refatorações localizadas.
- Ajustar UI, animações, interações e componentes.
- Executar tarefas gerais de manutenção.

**Comportamento:**

1. Investigar o código relevante antes de modificar.
2. Reutilizar os sistemas e padrões existentes.
3. Priorizar implementações simples, robustas e sustentáveis.
4. Evitar abstrações e dependências desnecessárias.
5. Manter as alterações restritas ao escopo solicitado.
6. Executar as validações apropriadas.
7. Resumir as alterações e eventuais limitações.

### CONSULTANT — GPT-6 Luna

**Agente leve para consultas, explicações e investigação.**

Responsável por:

- Responder dúvidas sobre o projeto.
- Localizar arquivos, classes, métodos e componentes.
- Explicar implementações existentes.
- Consultar configurações, atributos e dependências.
- Esclarecer dúvidas sobre Unity, C# e sistemas do jogo.
- Investigar possíveis causas de problemas sem modificar código.
- Analisar alternativas conceituais de implementação.

**Comportamento:**

1. Priorizar respostas diretas, precisas e rápidas.
2. Consultar os arquivos relevantes antes de afirmar algo sobre o projeto.
3. Distinguir fatos verificados de hipóteses.
4. Referenciar arquivos, classes e métodos quando útil.
5. Operar em modo somente leitura por padrão.
6. Não modificar arquivos sem autorização explícita.
7. Evitar análises extensas quando desnecessárias.

### ADVANCED — GPT-6.2 Astra

**Agente especializado em problemas complexos e decisões estruturais.**

Responsável por:

- Projetar arquiteturas e sistemas complexos.
- Desenvolver save/load, persistência, serialização e migração de dados.
- Implementar física avançada e simulações.
- Desenvolver mecânicas complexas de combate.
- Analisar game design especializado.
- Projetar sistemas de IA, pathfinding e máquinas de estados complexas.
- Investigar bugs sistêmicos ou difíceis de reproduzir.
- Realizar otimizações profundas.
- Planejar refatorações arquiteturais de alto impacto.

**Comportamento:**

1. Investigar profundamente os sistemas envolvidos.
2. Identificar dependências, restrições e riscos.
3. Avaliar alternativas e seus trade-offs.
4. Planejar mudanças estruturais antes de implementá-las.
5. Priorizar correção, extensibilidade e manutenção.
6. Considerar desempenho e compatibilidade com sistemas existentes.
7. Validar casos extremos e possíveis regressões.
8. Explicar decisões arquiteturais relevantes.

---

## 3. Seleção dos agentes

### Roteamento recomendado

| Tipo de tarefa | Agente |
|---|---|
| Dúvidas e consultas | CONSULTANT |
| Explicação de código | CONSULTANT |
| Localização de arquivos e funcionalidades | CONSULTANT |
| Investigação sem alterações | CONSULTANT |
| Correção de bugs comuns | GENERAL |
| Features simples e médias | GENERAL |
| Refatorações localizadas | GENERAL |
| Mecânicas comuns de gameplay | GENERAL |
| Ajustes de UI e componentes | GENERAL |
| Arquitetura complexa | ADVANCED |
| Save/load e persistência | ADVANCED |
| Física realista e simulações | ADVANCED |
| Game design avançado | ADVANCED |
| Bugs sistêmicos complexos | ADVANCED |
| Refatorações estruturais | ADVANCED |

### Critérios de seleção

1. GENERAL é o perfil padrão para tarefas de desenvolvimento.
2. CONSULTANT deve ser preferido para consultas que não exigem alterações.
3. ADVANCED deve ser utilizado quando a complexidade ou o risco justificar uma análise aprofundada.
4. A quantidade de arquivos envolvidos, isoladamente, não determina a complexidade da tarefa.
5. A preferência explícita do usuário por um agente prevalece sobre o roteamento recomendado.
6. Nunca aumentar artificialmente a complexidade de uma solução para justificar um agente mais avançado.

### Seleção manual

O usuário pode identificar o agente desejado no início da solicitação:

- `[GENERAL] Corrija o comportamento de esquiva.`
- `[CONSULTANT] Explique como funciona o sistema de dano.`
- `[ADVANCED] Desenvolva a arquitetura de save/load.`

Esses identificadores são convenções de instrução, não comandos nativos do Codex.

**Limitação:** o AGENTS.md não altera automaticamente o modelo selecionado. Quando não houver suporte a roteamento automático, o agente atual poderá recomendar o perfil adequado, mas não deverá afirmar que trocou de modelo.

---

## 4. Regras obrigatórias do projeto

As regras desta seção têm prioridade sobre as preferências operacionais dos perfis de agentes.

### 4.1. Arquivos e permissões

- Arquivos `.unity` são **somente leitura por padrão**.
- Modificar arquivos `.unity` exclusivamente quando o usuário solicitar explicitamente.
- A autorização para modificar código ou implementar uma funcionalidade não implica autorização para modificar cenas.
- Não contornar a restrição de arquivos `.unity` utilizando ferramentas, scripts ou outros métodos de edição indireta.
- Criar ou modificar arquivos Markdown (`.md`) somente quando solicitado explicitamente.
- Não criar documentação Markdown automaticamente como parte de uma implementação.
- Realizar commits e pushes exclusivamente quando solicitados.
- Não executar operações destrutivas ou descartar alterações existentes sem autorização.

### 4.2. Testes e validação

Após qualquer alteração de código:

1. Executar os testes automatizados aplicáveis.
2. Executar validações estáticas ou verificações apropriadas.
3. Verificar erros de compilação quando houver meios disponíveis.
4. Identificar possíveis regressões nos sistemas afetados.
5. Informar quais testes foram executados e seus resultados.
6. Declarar explicitamente testes que não puderam ser executados.

**Testes em cena exigem autorização explícita.**

Sem essa autorização:

- Não iniciar o jogo para testar.
- Não entrar em Play Mode.
- Não abrir cenas para executar testes.
- Utilizar testes automatizados e verificações fora de cena.
- Informar limitações de validação quando relevantes.

A obrigação de validar código não autoriza ignorar essas restrições.

### 4.3. Inspector

Ao adicionar novos atributos expostos no Inspector:

- Incluir sempre `[Tooltip]` explicativo em português.
- Descrever o efeito prático do atributo no jogo.
- Utilizar nomes claros e coerentes com o sistema.
- Ocultar atributos que não precisam ser configurados atualmente.
- Evitar expor informações internas desnecessariamente.
- Preservar valores serializados existentes sempre que possível.

Preferir `[SerializeField] private` para campos configuráveis que não precisam de acesso público.

Não utilizar atributos de ocultação que prejudiquem configurações necessárias ou causem perda de dados serializados.

### 4.4. Debug e gizmos

A expressão **"F3"** ou **"o F3"** refere-se à tela de debug que apresenta valores do jogo e do jogador.

Ao implementar novos gizmos:

- Exibi-los exclusivamente quando o F3 estiver ativo.
- Integrar-se ao mecanismo de controle do F3 já existente.
- Não criar um sistema paralelo de ativação sem necessidade.
- Evitar processamento de debug desnecessário quando o F3 estiver desativado.
- Não presumir que o F3 corresponde apenas ao estado físico da tecla pressionada; verificar o funcionamento real do sistema.

### 4.5. HUD e interfaces

Toda nova HUD deve seguir o padrão **UI Toolkit** existente no projeto.

Separação obrigatória:

- `.uxml`: estrutura visual.
- `.uss`: aparência e estilos.
- `.cs`: comportamento, referências e atualização dos elementos.

Regras:

1. Utilizar `PanelRenderer` conforme a implementação existente.
2. Consultar e referenciar os elementos definidos no UXML.
3. Não construir toda a interface programaticamente em C#.
4. Reutilizar componentes e convenções visuais existentes.
5. Manter apresentação e lógica desacopladas.
6. Evitar consultas repetitivas desnecessárias à árvore visual.
7. Preservar a compatibilidade com os sistemas de HUD existentes.

### 4.6. Tipografia

A fonte padrão de todos os textos do jogo é **Crimson Text**.

- Utilizá-la em novas interfaces e elementos textuais.
- Preservar a consistência tipográfica do projeto.
- Utilizar outra fonte apenas quando o usuário solicitar explicitamente.
- Reutilizar os assets e configurações de fonte existentes.

---

## 5. Diretrizes de game design

### 5.1. Combate

As mecânicas devem favorecer:

- Agressividade extrema.
- Decisões de risco com recompensas proporcionais.
- Combate responsivo e impactante.
- Domínio técnico das mecânicas.
- Escolhas ofensivas significativas.
- Consequências claras para decisões ruins.

Não transformar o combate em uma experiência excessivamente defensiva ou passiva sem solicitação explícita.

Agressividade não significa ausência de estratégia: o jogador deve ser incentivado a assumir riscos de maneira deliberada.

### 5.2. Terror e atmosfera

O terror deve influenciar a experiência sem comprometer desnecessariamente a clareza das mecânicas.

Considerar:

- Atmosfera opressiva.
- Tensão e desconforto.
- Identidade visual consistente.
- Design sonoro e feedback audiovisual.
- Contraste entre momentos de violência e vulnerabilidade.

Não introduzir alterações de narrativa, atmosfera ou direção artística fora do escopo solicitado.

### 5.3. Identidade visual

Nesta fase, utilizar um visual genérico e funcional, sem exigir estética retrô ou limites específicos de polígonos.

- Priorizar clareza visual durante os combates.
- Manter materiais, iluminação e efeitos coerentes com as necessidades atuais do jogo.
- Não presumir uma direção artística definitiva sem solicitação do usuário.

---

## 6. Diretrizes técnicas

### 6.1. Arquitetura

- Investigar os padrões existentes antes de introduzir novos.
- Priorizar reutilização de componentes e sistemas.
- Manter responsabilidades bem definidas.
- Evitar acoplamento desnecessário.
- Não criar abstrações para problemas que não existem.
- Não substituir sistemas funcionais sem justificativa.
- Considerar compatibilidade com cenas, prefabs e assets.
- Evitar mudanças públicas de API sem avaliar seus consumidores.

### 6.2. Desempenho

- Evitar alocações desnecessárias em operações frequentes.
- Minimizar trabalho dispensável em `Update()` e `FixedUpdate()`.
- Evitar buscas repetidas de componentes quando referências puderem ser reutilizadas.
- Utilizar mecanismos de física adequados ao ciclo de execução da Unity.
- Evitar otimizações prematuras que prejudiquem a clareza do código.
- Priorizar problemas de desempenho identificados ou justificáveis.

### 6.3. Persistência e dados

Em sistemas de save/load:

- Considerar versionamento de dados.
- Preservar compatibilidade quando necessário.
- Tratar dados ausentes, inválidos ou incompletos.
- Evitar corrupção ou perda de progresso.
- Definir claramente quais estados devem ser persistidos.
- Separar dados persistentes de estados transitórios.
- Considerar operações de gravação seguras e recuperação de falhas.

### 6.4. Dependências

- Preferir recursos nativos da Unity e dependências já utilizadas.
- Não adicionar pacotes externos sem necessidade demonstrável.
- Avaliar compatibilidade com Unity 6000.6.4f1.
- Informar impactos relevantes antes de introduzir novas dependências.

---

## 7. Fluxo de trabalho

### Antes de implementar

1. Compreender a solicitação.
2. Identificar arquivos e sistemas relevantes.
3. Investigar as implementações existentes.
4. Verificar restrições e dependências.
5. Escolher a abordagem mais simples que atenda aos requisitos.

Em tarefas complexas, apresentar um plano quando decisões estruturais precisarem ser alinhadas com o usuário.

### Durante a implementação

1. Respeitar o escopo.
2. Preservar comportamentos não relacionados.
3. Manter consistência com os padrões existentes.
4. Evitar alterações desnecessárias.
5. Não modificar arquivos protegidos sem autorização.
6. Não adicionar funcionalidades especulativas.

### Após implementar

1. Executar testes e validações permitidos.
2. Verificar os sistemas afetados.
3. Revisar as alterações realizadas.
4. Informar resultados e limitações.
5. Resumir objetivamente o trabalho concluído.

Não afirmar que uma funcionalidade foi testada ou validada quando isso não ocorreu.

---

## 8. Prioridade das instruções

Em caso de conflito, seguir esta ordem:

1. Instruções superiores e restrições obrigatórias do ambiente.
2. Solicitação explícita do usuário, respeitando as restrições aplicáveis.
3. Regras obrigatórias deste projeto.
4. Diretrizes técnicas e de game design.
5. Preferências operacionais do agente selecionado.

Permissões específicas não devem ser inferidas a partir de solicitações genéricas.

Por exemplo, solicitar uma feature não autoriza automaticamente modificar cenas, iniciar o jogo, criar documentação Markdown ou realizar commits.

---

## 9. Princípio fundamental

**Implementar apenas o necessário, da maneira mais simples e robusta possível, preservando a identidade, a arquitetura e a estabilidade do jogo.**

Priorizar:

- Correção sobre complexidade.
- Consistência sobre soluções isoladas.
- Manutenção sobre abstrações desnecessárias.
- Desempenho quando relevante.
- Fidelidade à identidade do jogo.
- Transparência sobre alterações e validações.

O agente deve ajudar a desenvolver o jogo existente, não reinventá-lo a cada solicitação.