# Ações pendentes do usuário

Atualizado em 03/11/2026.

## Diagnosticar e validar o movimento do jogador

O retrabalho foi implementado e compila, mas a regressão `SelfLaunchDurationRegression` falhou ao rodar numa cópia isolada do projeto: o PlayerMovement temporário reportou 0 commits e o ataque D1 não iniciou `SelfLaunchUp`. Não há aprovação de gameplay ainda. Nenhuma cena `.unity` foi alterada.

No Unity 6000.6.4f1:

1. Abra `Assets/Scenes/999. TestArena.unity` no Editor e confirme que a cena tenha exatamente um `AudioListener` ativo durante Play.
2. Execute **Tools > Tests > Run Self Launch Duration Regression**.
3. Se falhar, envie o erro completo do Console. Verifique por que o jogador criado pela regressão não registra um commit e por que D1 não inicia o lançamento.
4. Depois de obter `PASS`, valide em gameplay pulo, pulo duplo, dash, dash-jump, slide, launch, dive, swing, grapple, separação e ataques terrestres/aéreos.

## Definição do HUD de chefe

- Informe quais inimigos/prefabs devem ser tratados como chefes para conectar `BossHealthHUD` ao inimigo correto.

## Não iniciado por exigir decisão ou validação visual

- Corrigir cores/materiais de stagger após conferir os materiais de personagem, armas e VFX.
- Converter `CurrencyShopHUD`, `DamageNumber`, `PlayerDpsMeter`, `PlayerAttackDebug` e `EnemyDebug` para UI Toolkit; requer desenho e posicionamento próprios.
- Ajustar física de slow motion, volumes de `FixedCameraZone`, mixer de áudio, batching de partículas e limpeza de assets/pacotes/texturas.
- Configurar estrutura NUnit/CI; os testes atuais continuam sendo ações de menu do Editor.

Detalhes técnicos e status estão em `REVISAO_PROJETO.md` e `RETRABALHO_PLAYERMOVEMENT.md`.

## Validação de código (03/11/2026): feito e restante

Feito: revisão estática dos itens corrigidos, com correções em Build Settings (Bootstrap volta a ser a cena 0), `GameSceneLoader`, lock-on com D-pad, `?.` em componentes Unity, `EnemyDeathLoopTest` (inerte fora de build de desenvolvimento), prompts que mostram o binding do dispositivo usado por último e `SelfLaunchDurationRegression`. `Assembly-CSharp` e `Assembly-CSharp-Editor` compilam com 0 erros e 0 avisos. Nada foi executado em cena.

Precisa de você:

- Conferir em Play que o Bootstrap abre o MainMenu e que o menu aparece mesmo se a cena configurada falhar ao carregar.
- Conferir que o lock-on troca de alvo uma vez por toque no D-pad.
- Os prompts de binding só atualizam quando o texto é reconstruído; trocar de teclado para gamepad com o prompt já aberto não o atualiza na hora.
- `ResetAllSavesAndReload` não tem chamador; decida em qual menu ele deve aparecer.
- `OnGUI` restante (`PlayerDpsMeter`, `DamageNumber`, `EnemyDebug`, `PlayerAttackDebug`) e `CurrencyShopHUD` programático: exigem desenho de UXML/USS e posicionamento, já listados acima.
- A worktree `C:\Users\mathe\ProjetoPSX_validacao` (branch `revisao-validacao`) pode ser removida com `git worktree remove ../ProjetoPSX_validacao` e `git branch -D revisao-validacao`.
