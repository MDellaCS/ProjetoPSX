# Retrabalho do PlayerMovement

Atualizado em 03/11/2026. Implementação concluída; validação de gameplay em cena ainda bloqueada pela regressão automatizada descrita abaixo. Nenhum arquivo `.unity` foi modificado.

## Implementado

- Locomoção, avanço de ataque, separação de entidades, swing e grapple agora acumulam deslocamentos. `PlayerMovement.LateUpdate` faz o único commit com `PlayerCharacterController.Move`, depois dos Updates de gameplay e da separação (agora em `Update`, ordem 1000).
- O `CollisionFlags` desse commit processa pouso/teto/parede/slide; swing recebe o resultado por callback. Grapple mantém o movimento acumulado no mesmo commit.
- O snap ao chão só é permitido se o jogador iniciou o movimento grounded e não está subindo. Contato de suporte exige normal não vertical; a classificação de superfície caminhável usa o limite de inclinação.
- Classificação de colliders ignorados é cacheada durante cada `Move`; cache é descartado ao terminar. `Physics.SyncTransforms` permanece uma vez ao fim do movimento, não por sweep.
- Parâmetros do Inspector viraram campos privados serializados mantendo nomes e dados existentes, com propriedades de leitura para consumidores externos. Adicionado contador de commits visível no F3.
- Corrigidos os itens de estado e manutenção listados na seção 4 do plano original: crouch NonAlloc com early-out, stagger/dash-jump, steering em Dead, estados de burn, dive, rotação independente de frame rate, tooltip, componente requerido, reset completo e constantes nomeadas.
- Adicionada regressão de Editor para matemática de deslocamento, snap e classificação de superfície em `PlayerHeldBranchRegression`.

## Validação

- `dotnet build Assembly-CSharp.csproj --no-restore`: passou, 0 avisos e 0 erros.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: passou, 0 avisos e 0 erros.
- `git diff --check`: passou.
- Inspeção: há uma chamada a `controller.Move` no código de `PlayerMovement`, em `CommitMovement`.
- A regressão de cena `SelfLaunchDurationRegression` foi executada numa cópia isolada do projeto para não competir com os Editors abertos no projeto do usuário. Ela falhou: o jogador temporário reportou 0 commits e o ataque não iniciou SelfLaunchUp. Portanto, pouso real, game feel, swing/grapple e demais combinações físicas não estão aprovados. O log completo está no diretório temporário da cópia usada no teste; a cópia será removida ao encerrar a validação.

## Ação necessária no Editor

No Unity 6000.6.4f1, abra `Assets/Scenes/999. TestArena.unity` e rode **Tools > Tests > Run Self Launch Duration Regression**. Se falhar, envie o erro completo do Console; investigue especialmente por que o PlayerMovement criado pelo teste não registra commit e por que o ataque D1 não inicia. Depois do PASS, faça a validação manual de pulo, pulo duplo, dash, dash-jump, slide, launch, dive, swing, grapple, separação e ataques terrestres/aéreos. Não salve mudanças de cena sem revisar as referências serializadas.

## Revisao (03/11/2026)

- Revisado `CommitMovement`, acumuladores, ordem de execucao (PlayerMovement -1000, CameraFollow -100, separacao 1000: o commit em `LateUpdate` ocorre antes da camera) e callbacks de swing/grapple. Nenhum segundo `controller.Move` foi encontrado.
- Causa provavel da falha "0 commits": o `LateUpdate` zerava `movementCommitsThisFrame` em todo frame sem commit (hitstop, pausa, estados que pulam o `Update`), e a regressao lia esse 0 como violacao. Removido o reset; o contador agora vale para o ultimo frame que fez commit. `Assembly-CSharp` e `Assembly-CSharp-Editor` compilam com 0 erros e 0 avisos.
- Nao foi possivel rodar a regressao em cena (so a pedido do usuario e com Editors abertos); a hipotese acima continua nao confirmada. Se a regressao ainda falhar, envie o log do Console.
- Observacao de projeto: `MoveDuringSwing` devolve o `CollisionFlags` do commit anterior (um frame de atraso); o resultado do frame atual chega por `OnMovementCommitted`.
