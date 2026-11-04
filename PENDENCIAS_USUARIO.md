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
