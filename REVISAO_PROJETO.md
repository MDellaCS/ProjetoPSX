# Revisão completa do ProjetoPSX

Data: 03/11/2026 · Commit revisado: `085d5e8` · Unity `6000.6.4f1`

Nenhum arquivo do projeto foi alterado nesta revisão. Este documento é o único arquivo criado.

> **Atualização posterior:** o texto abaixo registra os achados da inspeção original. O código foi corrigido depois; consulte a seção final **Status após as correções** para não interpretar as listas históricas como estado atual.

**Escopo.** Foram lidos os 136 scripts de `Assets/Scripts`, todos os de `Assets/Editor`, todos os UXML e USS, além de `ProjectSettings`, `Packages/manifest.json`, `.gitignore` e `.gitattributes`. As cenas `.unity` foram consultadas por grep, sem edição. O jogo não foi executado e nenhuma cena foi aberta.

**Severidade.**
- **Crítico:** quebra o jogo ou a build.
- **Alto:** bug de gameplay visível ou risco ao jogador.
- **Médio:** bug pontual, custo de performance relevante ou violação de regra do projeto.
- **Baixo:** limpeza, má prática ou risco latente.

> **Contexto importante.** `ProjectSettings/EditorSettings.asset` tem `m_EnterPlayModeOptions: 3`, ou seja, **Domain Reload e Scene Reload estão desligados**. Todo estado `static` sobrevive entre sessões de Play no Editor. Vários itens abaixo só são problemas por causa disso.

---

## 1. Resumo: o que corrigir primeiro

| # | Sev. | Problema | Onde |
|---|---|---|---|
| 1 | Crítico | `1. MainMenu.unity` e `3. Room.unity` **não estão no Build Settings**, mas são carregadas por caminho. Na build o jogo fica preso no Bootstrap, sem menu. A cutscene de HoleHallways também não leva para Room. No Editor tudo funciona, porque o AssetDatabase mascara o erro. | `ProjectSettings/EditorBuildSettings.asset`, `GameSceneLoader.cs:119-127, 201-208, 243-250` |
| 2 | Alto | Atalhos de debug ativos na build de release, sem `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD`. Lista na seção 2.2. | vários |
| 3 | Alto | Segurar **Y por 3 s apaga todos os saves** (`PlayerPrefs.DeleteAll`), ativo por padrão na build. | `PlayerHealth.cs:46-49, 164-184` |
| 4 | Alto | Ao sair de combate com Heat negativo acima do limite, a "recuperação de Cold" **joga o Heat direto para o limite**. Exemplo: de -20 vai para -70, aproximando o jogador da morte por frio. | `PlayerHeat.cs:292-296` |
| 5 | Alto | O `OnDisable` de `FlyingPuppet` esconde o `OnDisable` privado de `EnemyBase`. Flyers nunca saem de `activeEnemies`, nem do Director, nem da fila de especiais. | `FlyingPuppet.cs:548`, `EnemyBase.cs:149` |
| 6 | Alto | A fonte **Crimson Text não é aplicada** em quase nenhum HUD: não há `-unity-font-definition` em USS/TSS e `PanelSettings.textSettings` está vazio. | `Assets/UI Toolkit/*` |
| 7 | Alto | `CombatParticleCollisionFilter.OnEnable` faz `FindObjectsByType<Collider>` **da cena inteira** a cada partícula reutilizada do pool. Causa pico de CPU e GC em toda morte e golpe. | `CombatParticleCollisionFilter.cs:10-21` |
| 8 | Alto | O hitbox do player consulta **todas as layers, incluindo triggers**, com `GetComponentsInParent<MonoBehaviour>()` (aloca) por collider, até 24 sweeps por frame. O buffer de 64 pode lotar e perder inimigos. | `PlayerHitboxController.cs:172-245` |
| 9 | Alto | `PlayerMovement.SetCrouching(false)` chama `CanStandUp()` antes do early-out. Resultado: `Physics.OverlapCapsule` com alocação **todo frame** com o jogador em pé. | `PlayerMovement.cs:588-597, 677-692` |
| 10 | Alto | O Director avalia `HasAvailableOpening` completo (lambdas, `GetComponent`, `Linecast`) 2 a 3×N vezes por frame. | `EnemyDirector.cs:94-101, 224-234` |
| 11 | Médio | Não há **nenhum teste automatizado real**: nenhum `asmdef` de testes e nenhum `[Test]`. As "regressões" são MenuItems, e uma delas aponta para uma cena que não existe. | `Assets/Editor/*Regression.cs` |
| 12 | Médio | O `Time.timeScale` tem dois donos (pausa e efeitos de slow-motion). Em certas sequências o jogo **fica preso a 5% de velocidade**. | `GamePauseController.cs:34,44`, `GameTimeScaleEffects.cs` |

---

## 2. Projeto, build e configuração

### 2.1 Build Settings
- **Crítico:** cenas faltando no build. Ver item 1 do resumo.
  - Além de adicionar as cenas, `GameSceneLoader.LoadScene` deveria registrar um `Debug.LogError` quando `ResolveScenePath` ou `CanLoadScene` falha. Hoje a falha é silenciosa (L204-208).
  - O `Start()` também desliga todos os `PanelRenderer` antes de saber se o load vai acontecer (L124).
- **Médio:** `999. TestArena.unity` está habilitada no Build Settings e vai para a build de release.
- **Baixo:** a pasta `Assets/Scenes/SampleScene/` (LightingData + ReflectionProbe) está órfã, porque não existe mais `SampleScene.unity`.
- **Baixo:** `companyName: DefaultCompany` em `ProjectSettings.asset:15`. Isso afeta o caminho de `PlayerPrefs` e de `persistentDataPath`, então mudar depois "perde" os saves dos jogadores.

### 2.2 Debug exposto em release (Alto)
Nenhum script usa `Debug.isDebugBuild`, `DEVELOPMENT_BUILD` ou guarda equivalente. Na build final o jogador tem acesso a:

| Atalho | Efeito | Local |
|---|---|---|
| Segurar **Y** | apaga todos os saves | `PlayerHealth.cs:164-184` |
| **U** | troca de moveset e **reseta vida e Heat** (cura total) | `PlayerMovesetController.cs:75-76, 107-108` |
| **F7** | god mode | `PlayerHealth.cs:157-161`, InputActions |
| **F8**, **F4/F5** | Heat máximo, Heat ± | `PlayerHealth.cs`, `PlayerHeat.cs:91` |
| **F11** | slow motion (lido mesmo com o F3 fechado) | `GameDebugTimeScale.cs`, `PlayerDebugToolkitHUD` |
| **F9** | pula de cena, inclusive com o jogo pausado ou em cutscene | `GameSceneLoader.cs:49, 137-144` (`enableDebugSceneShortcuts: 1` no Bootstrap) |
| override de combate | | `PlayerCombatStatus.cs:28-34` |

**Correção:** criar um mapa de input "Debug" habilitado só em `UNITY_EDITOR || DEVELOPMENT_BUILD` e mover todos os atalhos para um único componente de debug.

### 2.3 Input
- **Médio:** `HeavyAttack`, `LockOn`, `CycleTarget` e `Pause` (nos dois mapas) **não têm binding de gamepad**. Com controle não dá para usar ataque pesado, lock-on nem pausar.
- **Médio:** a órbita da câmera trata `Look` como delta de mouse (sem `deltaTime`). Com o analógico direito, a câmera praticamente não gira e o giro depende do FPS (`CameraFollow.cs:581-590`).
- **Médio:** várias teclas fixas ignoram o rebinding e o gamepad:
  - `E` da loja (`CurrencyShopInteractionArea.cs:84`, e o prompt mostra "E" fixo);
  - `F` do swing (`PlayerSwingController.cs:12, 218-223`);
  - skip de cutscene (`CutsceneController.cs:152-157`, que também não checa pausa);
  - `U`/`Y`, citadas acima.
- **Baixo:** ações `Previous`/`Next` sem uso e bindings de template (Touchscreen, XR, Joystick) sobrando no `.inputactions`.
- **Baixo:** `GameInput.FindAction(string)` + `EnsureInitialized` são chamados dezenas de vezes por frame. Fazer cache das `InputAction` (`GameInput.cs:110-132`).
- **Baixo:** `GameInputBindings.InitializeSession` faz `BindingsChanged = null` e derruba assinantes vivos da UI (L214-219). O JSON de overrides não tem versão.

### 2.4 Pacotes e assets
- **Médio:** pacotes instalados sem nenhum uso em código: `com.cesium.unity` (com scoped registry e `Assets/CesiumSettings`), `com.unity.ai.inference` e `com.unity.visualscripting`. `com.unity.ai.navigation` também não aparece em código. Isso aumenta o tempo de import e compilação e o tamanho da build.
- **Baixo:** `Assets/TextMesh Pro/Examples & Extras` (cerca de 30 scripts e fontes grandes) está versionado. O jogo usa UI Toolkit, então a pasta pode ser removida.
- **Baixo:** nomes de asset genéricos ou com erro: `Assets/Models/Untitled7.fbx`, `MAT_Untitled7.mat`, `StrenghLever.cs` ("Strength"). Para renomear a classe, use `[MovedFrom]` para não quebrar a serialização.
- **Baixo:** `NewUXMLTemplate.uxml` está sobrando e não é referenciado.
- **Baixo:** o GUID de `PlayerDpsMeter.cs.meta` foi escrito à mão (`e9000000000000000000000000000008`), o que traz risco de colisão.
- **Baixo (estética PS1/PS2):** as 118 texturas usam filtro bilinear (`filterMode: 1`), mipmaps e 2048 px. Não há shader com vertex snapping ou affine mapping. Para o visual retrô proposto, o esperado é Point filter, resolução baixa e, possivelmente, shader PSX.

### 2.5 Arquitetura e organização
- **Médio:** **não há nenhum `asmdef`** em `Assets/Scripts`: qualquer alteração recompila tudo e nada impede dependências cruzadas.
- **Médio:** **nenhum namespace** nos 136 scripts. Há risco de colisão com pacotes (`CameraController`, `GameInput`...).
- **Médio:** classes muito grandes:
  - `PlayerMovement` (1710 linhas, cerca de 40 campos de estado);
  - `EnemyMovement` (977);
  - `EnemyBrain` (918);
  - `FlyingPuppet` (651).

  O `TODO.md` já prevê a refatoração do `PlayerMovement`.
- **Médio:** componentes adicionados em runtime via `AddComponent` (`PlayerMovement.cs:190-222` adiciona Swing, SlideAttack, Footsteps e InputBuffer; `PlayerAttackController` adiciona TargetAssist). A composição real do prefab fica invisível e a ordem de `Awake` fica imprevisível.
- **Baixo:** cerca de 55 chamadas `Find*`/`FindGameObjectWithTag("Player")` espalhadas. Várias rodam por frame quando o player não existe (`EnemyBrain`, `PlayerHUDToolkit`, `EncounterTrigger`, `AreaTitleTrigger`, `DialogueTrigger`, `CutsceneTrigger`, `TemporaryWorldPromptTrigger`). **Correção:** um registro central do player (evento de spawn).
- **Baixo:** layers: só existem `CameraObstacle` e `CombatParticle`. Não há layer de Player, Enemy nem Hurtbox, por isso quase todas as queries físicas usam `AllLayers` e filtram com `GetComponentInParent` (causa de vários itens de performance abaixo).
- **Baixo:** `GetPlayerCenter()` está duplicado em 8 classes. Expor `PlayerCharacterController.WorldCenter`.
- **Baixo:** a lógica de pose de hitbox está duplicada entre Preview e runtime (Player e Enemy) e **já divergiu**. Ver as seções 3 e 4.
- **Baixo:** há quatro arquivos TODO na raiz (`TODO.md`, `BALANCEAMENTO_TODO.md`, `TODO_HUMANIDADE_MODO_HUMANO.md`, `TODO_TEMATICA.md`). Vale consolidar.

### 2.6 Padrão recorrente: `?.` / `??` / `??=` em objetos Unity (Médio)
No Editor, `GetComponent` devolve um objeto "fake null" quando o componente não existe, e os operadores C# não o tratam como nulo. Por isso **Editor e build se comportam de forma diferente**. Casos com efeito real:
- `CurrencyShopHUD.cs:79`: `GetComponent<PlayerCurrencyWallet>() ?? AddComponent(...)` nunca adiciona no Editor. O saldo aparece como 0 e as compras ficam bloqueadas.
- `PlayerProgression.cs:18, 66`: mesmo padrão.
- `PlayerTargetAssist.cs:39, 71`: `lockOn ??=` nunca busca de novo, e a assistência ignora o lock-on no Editor.
- `FlyingPuppet.cs:584-586`: o fallback de bones do Humanoid nunca é aplicado no Editor.
- `EnemyBodyPartDeathParticles.cs:28`: `GetComponent<MeshFilter>()?.sharedMesh` pode lançar `MissingComponentException`.

Também aparece em: `EnemyHealth:443`, `EnemyAttackController:46-49`, `EnemyAttackReflector:49`, `PauseMenuHUD:138`, `CutsceneController:259,266`, `PlayerHitboxController:76`, `PlayerGameOver:82-84`, `PlayerHeat:190,209-210`, `CurrencyGorePickup:56`, `CombatParticleCollisionFilter:12`, `CombatParticleSystem:310`, `CurrencyShopInteractionArea:23,42`.

**Correção:** usar `TryGetComponent` ou `== null` explícito.

### 2.7 Estado estático sem reset (Domain Reload desligado)
- Há reset correto em: `GamePauseController`, `GameDebugTimeScale`, `LoadingScreenHUD`, `GameInput`, `GameSettingsService`, `EnemySpecialAttackQueue`, `CurrencyShopHUD`.
- **Sem reset:**
  - `EnemyBase.activeEnemies` e o evento estático `EnemyBase.EnemyDied` (`EnemyBase.cs:34-35`);
  - `EnemyBrain.cachedVisibility*` e `nextGroupSeparationOrder` (L23-26);
  - `PlayerSwingAnchor.cachedPlayerMovement/Controller` (L12-13, não incluídos em `ResetRegistry`);
  - `GameSaveResetService.isResetting`;
  - `PlayerLockOn` (Texture2D estática nunca destruída, L20).
- Combinado com o bug do `FlyingPuppet.OnDisable`, os Flyers destruídos se acumulam no `HashSet` estático entre sessões de Play.

---

## 3. Player (`Assets/Scripts/Player/`)

### PlayerMovement.cs
- **Alto, L588-597:** overlap de crouch a cada frame (resumo, item 9). Fazer o early-out `isCrouching == value` primeiro e usar `OverlapCapsuleNonAlloc`.
- **Médio, L1317, L1630, L1377:** `controller.Move` roda até 3 vezes por frame: movimento, avanço de ataque e separação de entidades. Cada chamada refaz sweeps, ground snap e `SyncTransforms`, e sobrescreve `grounded`. Um avanço de ataque no ar pode "grudar" o jogador no chão. O próprio TODO exige um único `Move` por frame.
- **Médio, L427 + `PlayerCharacterController.cs:161-166`:** qualquer contato com `normal.y > 0.0001` vira `HasGroundContact`, inclusive paredes e quinas. Raspando numa parede no ar, **o pulo duplo é negado** e a parede é tratada como rampa íngreme (L1049).
- **Médio, L1505-1520:** `ApplyStaggerPush` não limpa `isDashJump`. Se o jogador sofre stagger durante um dash-jump e volta a Normal no ar, fica sem nenhum controle aéreo até pousar.
- **Médio, L815-879:** `CalculateHorizontalVelocity` não trata o estado Dead. Morto em dash-jump, o jogador ainda curva a trajetória com o input.
- **Médio, L1530-1543:** `InterruptForBurn` força `isCrouching = false` sem `CanStandUp()`, então a cápsula cresce dentro do teto. Também não limpa `isLaunched`, `isAerialDiving` nem `isJumpAscending`.
- **Baixo:**
  - `launchGravity` é escrito e nunca lido (L160, 1567, 1702).
  - Em L1644-1645, `verticalVelocity = 0` é sobrescrito na linha seguinte.
  - `BeginAerialDive` não limpa `isJumpAscending` (L1655-1667).
  - `Slerp(..., speed * dt)` depende do frame-rate (L1306-1310).
  - O tooltip de `crouchSpeedMultiplier` descreve um botão do mouse (L33-34).
  - `playerState` é usado sem checagem de nulo. Falta `[RequireComponent(typeof(PlayerState))]`.
  - `ResetForNewGame` está incompleto (L1546-1582).
  - Todos os parâmetros são `public` (L16-100).
  - Números mágicos: `12.8f`, `1.3f`, `-2f` (repetido 6 vezes) e `.12f`.

### PlayerCharacterController.cs
- **Médio, L434-452:** `IsIgnoredForMovement` faz 2 `GetComponentInParent` e 1 `GetComponent` por collider candidato. Isso se repete em cada sweep (até 5), na depenetração e no probe, e em até 3 `Move` por frame.
- **Baixo:** `Physics.SyncTransforms()` a cada `Move` (L188-189). `copyLegacyControllerSettings = true` sobrescreve em silêncio os valores do Inspector se houver `CharacterController` (L43, 85-86).

### PlayerHealth.cs
- **Alto, L164-184:** reset de saves com a tecla Y (ver seção 2.2).
- **Médio, L320-346:** ao morrer, não há `CancelAttack()`, e `PlayerAttackController.Update` (L61-85) não checa morte. **O ataque em andamento continua causando dano e gerando Heat depois da morte.** O mesmo acontece na morte por frio (`PlayerHeat.cs:608-614`).
- **Médio, L433-454:** `renderer.materials` instancia cópias de todos os materiais filhos, que nunca são destruídas. `normalColor` vem só de `materials[0]` e é aplicado a todos: depois do primeiro stagger, armas e VFX ficam todos da mesma cor. `PlayerState` também escreve nesses materiais (dois donos).
- **Baixo:** `currentHealth` só é inicializado no `Start`, então dano antes disso mata. Atalhos de debug misturados à lógica de vida.

### PlayerHeat.cs
- **Alto, L292-296:** bug da recuperação de Cold (resumo, item 4). **Correção:** `if (currentHeat >= recoveryLimit) return;`.
- **Médio, L130-131 / L427-447 vs L376-379:** o multiplicador mostrado no HUD usa o Heat com sinal (penalidade <1x em Cold), mas o golpe real usa `Max(0, heat)`, que é sempre ≥1x. **O HUD mostra um valor errado** e a penalidade de Cold é código morto.
- **Médio, L185-186:** `ResetForNewGame` limpa `rageGrants`, mas `PlayerProgression` não reaplica (`Initialize` retorna cedo, L23; `ApplyBonuses` só aplica efeitos novos, L101-105). **O Rage comprado some após o Game Over.**
- **Baixo:**
  - `-10` fixo em L456;
  - a lógica de 5 notches de 10 pressupõe `maxSuperheat = 50` (L124, 472-479);
  - multiplicadores mágicos `.1`, `.5` e `.05` (L435, 446);
  - `GetComponent` repetido em getters (L133-141);
  - comparação redundante em L264.

### PlayerState.cs
- **Médio, L24-36, 83-171:** as "Debug - State Colors" ficam sempre ativas. Já no `Awake`, pintam de branco todos os renderers filhos (armas, trails, VFX), instanciam materiais sem destruí-los e não respeitam o F3. Usar `MaterialPropertyBlock`, condicionar ao F3 e restaurar as cores originais.
- **Baixo:** campos sem `[Tooltip]` (L25, 27-33, 36).

### PlayerInputBuffer.cs / PlayerMovesetController.cs
- **Médio:** a expiração do buffer (`PlayerInputBuffer.cs:127, 156-157, 278-280`) e as janelas de continuação, held e PAUSE (`PlayerMovesetController.cs:259, 278, 286, 314`) usam `Time.time`. Esse relógio **continua andando durante o hitstop local**.
  - Exemplo: o hitstop pesado dura 0,2 s, o buffer dura 0,22 s e o `globalPauseDelay` dura 0,25 s. Inputs expiram durante o congelamento, e o clique seguinte cai indevidamente na rota PAUSE.
  - **Correção:** usar um relógio de gameplay que pare enquanto `IsGameplayFrozen`.
- **Baixo:**
  - `HasBuffered` parece só leitura, mas tem efeitos colaterais;
  - `bufferDuration` é `public`;
  - os eventos são assinados em `Awake`/`OnDestroy` (deveriam ser `OnEnable`/`OnDisable`);
  - um parâmetro chamado `_` é usado de fato (L314).
- **Baixo, resíduo de "Pause":** `requiresPause`, `globalPauseDelay` e `isPauseInput` continuam vivos (`PlayerMovesetDefinition` L26-27, 73, 137-160; `PlayerMovesetController` L286-288) e são usados por uma transição em `HumanMoveset.asset` e outra em `NormalMoveset.asset`. O `TODO.md` já prevê a remoção. O tooltip de `globalPauseDelay` descreve algo diferente do que o código mede.

### Combat/
- **Alto (perf), `PlayerHitboxController.cs:172-245`:** ver resumo, item 8.
- **Médio, `PlayerAttackController.cs:61-85`:** não checa morte (ver `PlayerHealth`).
- **Baixo, `PlayerAttackController`:**
  - `List.Find` com lambda alocando, e a busca é feita duas vezes (L239-267);
  - blocos duplicados com fallback `.25f`;
  - `AddComponent` morto por causa do `RequireComponent`.
- **Baixo, `PlayerAttackResolver`:**
  - o cálculo de dano, falloff e Heat está triplicado e o grapple já divergiu (L43-152 vs `PlayerGrappleAttackController` L195-226);
  - `GetComponent` a cada acerto (L72, 129);
  - fallbacks mágicos `18f`, `-60f`, `.85f`, `.35f`;
  - o falloff é medido a partir dos pés, não da origem do golpe.
- **Baixo:**
  - `PlayerAttackRuntime` tem um construtor com efeito colateral (consome Heat, L33-40);
  - `PlayerGrappleAttackController` faz `GetComponent` por frame com cache disponível (L295-297) e mantém campos legados com máscara mágica `243` (L26-30);
  - `PlayerAttackAudioController` consulta settings por frame e cria `new List` por track (L68-83);
  - `PlayerTargetAssist.CurrentBestTarget` não é usado.
- **Baixo, `PlayerAttackPreview` L184-242:** `GetPose` duplicado do hitbox e já divergente (não trata a trajetória TargetAssist).

### PlayerSwingController.cs / PlayerSwingAnchor.cs
- **Médio, L484, L209:** `EndSwingMotion(Vector3.zero, ...)` **descarta todo o momentum do balanço**, contrariando o próprio XML doc. Soltar a corda resulta em queda seca.
- **Baixo:**
  - `BeginSwing` faz snap visível para o arco;
  - `detachOnWorldCollision` solta inclusive com a flag `Below`;
  - largura e cor da corda são reaplicadas todo frame;
  - no Anchor: estático sem reset, `FindAnyObjectByType` no gizmo e gizmo fora do F3.

### PlayerSlideAttack.cs
- **Médio, L116-173:** `Physics.IgnoreCollision` não tem efeito. A cápsula do player é trigger e o motor usa queries, então `launchedEnemyCollisionIgnoreDuration` é uma feature morta.
- **Médio, L82-88:** `Physics.OverlapBox` com alocação por frame, `DefaultRaycastLayers` e triggers incluídos.
- **Baixo:** a caixa de acerto é simétrica e atinge inimigos atrás do jogador (L178-198). Campos `public`.

### PlayerLockOn.cs
- **Médio, L55-83:** o marcador usa `OnGUI` (viola o padrão de HUD) e chama `Camera.main` por evento. `GetEnemyCenter` faz `GetComponentInChildren<Renderer>()` em cada acesso a `LockPoint`, e pode pegar uma arma ou VFX.
- **Baixo:** `FindObjectsByType` 2 vezes + `Sort` com lambda a cada ciclo. `ValidateLockedTarget` roda pausado. Campos sem tooltip (L12, 15, 16).

### Demais
- **Médio, `PlayerCurrencyWallet.cs:74-87`:** `PlayerPrefs.Save()` a cada `Add`. Coletar gore em rajada causa escrita síncrona em disco por fragmento (hitches). Marcar como sujo e salvar em checkpoint ou ao sair.
- **Baixo, `PlayerGameOver`:**
  - flag `restartRequested` inútil;
  - o reset é incompleto para um player persistente (cooldowns, lock-on, `PlayerCombatStatus`, swing/grapple, Rage);
  - `showDelay` sem tooltip.
- **Baixo:** `PlayerDeath.cs` é um componente vazio.
- **Baixo, `Presentation/`:**
  - Swinging, GrapplePulling e Burn não são mapeados para animação;
  - `StringToHash` + `HasState` por frame (`PlayerAnimatorPlayback.cs:203-204`);
  - `GetComponent` por frame quando falta uma fonte;
  - tooltips ausentes.

---

## 4. Inimigos (`Assets/Scripts/Enemy/`)

### EnemyBase.cs / FlyingPuppet.cs
- **Alto:** `OnDisable` escondido (resumo, item 5). Tornar `OnEnable`/`OnDisable` da base `protected virtual` e chamar `base.OnDisable()`. As subclasses vazias `RagDoll`/`LimpRagDoll` correm o mesmo risco se ganharem métodos Unity. `GlassDoll` é `sealed` e as outras não (inconsistência).
- **Médio, `EnemyBase.cs:158`:** `ActiveEnemies` exposto como `IEnumerable` empacota o enumerador do `HashSet` (GC) em cada `foreach`. Isso roda por inimigo e por frame no Brain, 2 vezes no Director e no TargetAssist.
- **Baixo, `EnemyBase.cs:248-249`:** o taunt zera a velocidade vertical sem exigir chão. Um inimigo que começa o taunt no ar fica flutuando.
- **Médio, `FlyingPuppet.cs:257-293`:** raycast de altura por frame com 2 `GetComponentInParent` por hit, duplicando `TryGetGroundHeightAt` (L474-497).
- **Baixo, `FlyingPuppet`:**
  - `OnDisable` restaura o teleport incondicionalmente e pode desfazer estados de outros sistemas (L548-552);
  - `IgnoreOtherEnemiesDuringTeleportDive` é O(n·c²) e não cobre inimigos que surgem depois;
  - `AddComponent` redundante (L110-111);
  - checagem de pausa redundante (L163-164).

### EnemyMovement.cs
- **Médio, L639-657:** 3 `GetComponentInParent` por contato em `OnControllerColliderHit`, que dispara com o chão a cada `Move`. Uma das buscas é duplicada.
- **Médio, L798-806:** `OverlapCapsuleNonAlloc` sem layerMask nem `QueryTriggerInteraction` durante o arremesso. Chão e paredes lotam o buffer e inimigos atingidos podem ser ignorados.
- **Médio, L628-637:** `ApplyAerialDive` não define `launchGravity`, então a aceleração depende do último launch. Com 0, o inimigo fica parado no ar com `isLaunched`.
- **Baixo:** os timers de knockback só decrementam dentro de `Move` (L159-160), então o knockback dura mais que o configurado. `ResetForPracticeDummy` está incompleto (L219-239).

### EnemyBrain.cs
- **Médio, L579-616:** a separação de grupo é O(n²) por frame, com alocação.
- **Médio, L843-844 e L710-716:** `GetComponent<PlayerHealth>` por frame por inimigo. `SetEncounterEngaged` faz 2 `GetComponent` e é chamado por frame por `EnemyEncounterWave.Tick` (L91-94).
- **Baixo:**
  - `GeometryUtility.CalculateFrustumPlanes` aloca `Plane[6]` por frame (L885-886); usar o overload com buffer;
  - `FindGameObjectWithTag` por frame sem player;
  - `HasVisualContact` não testa linha de visão, então inimigos detectam através de paredes (L756-759);
  - números mágicos em L415-575.

### EnemyHealth.cs
- **Médio, L362-473:** `CacheEnemyRenderers()` (`GetComponentsInChildren`) a cada troca de estado (Windup, Active, Recovery, Stagger).
- **Baixo, L182-184:** calcula `finalHitDamage = Max(0, damage)`, mas subtrai `damage` cru. **Um dano negativo cura acima do máximo.**

### EnemyDirector.cs
- **Alto (perf), L94-101 / 224-234:** ver resumo, item 10.
- **Médio, L351-365:** o turno de Lead **nunca é liberado ao fim do ataque**, só por distância, morte ou Idle. A "alternância de turnos" descrita no tooltip não acontece.
- **Baixo:** com dois Directors na cena, desativar um zera o `activeDirector` mesmo com o outro ativo.

### EnemyAttackController.cs / Combat/
- **Médio, `EnemyAttackController.cs:204-210, 265-271`:** as continuações chamam `BeginAttack` direto e **pulam a fila de ataques exclusivos**. Dois inimigos podem executar o mesmo especial ao mesmo tempo.
- **Médio, `EnemyAttackController.cs:139-172` + `EnemyAttackMovement.cs:101-134`:** `approachTargetUntilPreferredRange` não tem timeout. Contra um alvo inalcançável, o inimigo fica em Windup para sempre, segurando o turno do Director.
- **Médio, `EnemyAttackController.cs:179-223`:** o progresso do movimento é calculado antes do decremento, então o último frame não amostra 1. **A distância do avanço depende do FPS** (cerca de 5% menor a 30 FPS).
- **Médio, `EnemyAttackHitboxController.cs:157-172`:** `HitConfirmed = true` mesmo quando `PlayerHealth.TakeDamage` descarta o golpe (player morto ou imune). As continuações que exigem acerto disparam sem ter acertado.
- **Médio, `EnemyAttackHitboxController.cs:122-158`:** overlaps com `AllLayers` + `Collide`, até 24 sweeps por track, `GetComponentInParent` por collider.
- **Médio:** `EnemyMovesetDefinition.FindNode` (L68-69) e `EnemyAttackDefinition.HasTag` (L203-205) usam lambdas com captura em caminho quente (alocação por chamada). `EnemyAttackSelector.IsValid` faz `GetComponent` a cada chamada (L171-185).
- **Baixo:**
  - `continuationWindow` nunca é aplicado em runtime, e a continuação não é revalidada antes de executar (`EnemyAttackSelector.cs:151-161`);
  - normalização duplicada (`EnemyAttackMovement.cs:30-35`);
  - o Preview duplica a pose do runtime e **já diverge**: usa `position + rotation*center` e ignora a escala, então mostra a hitbox no lugar errado em prefabs escalados (`EnemyAttackPreview.cs:166-251`);
  - `[ExecuteAlways]` é desnecessário.

### Outros
- **Médio, `EnemyGuard.cs:190-218`:** a "defesa frontal" **bloqueia golpes vindos de trás**, porque não há teste de ângulo. Isso contraria a diretriz de recompensar risco e posicionamento.
- **Médio, `EnemySpawnPresentation.cs:65-74`:** reiniciar o spawn não chama `DestroySphere()`, deixando esfera e `Material` órfãos.
- **Médio, `EnemySpawnPresentation.cs:168-171`:** `Shader.Find` em runtime. Se o shader for removido da build, `new Material(null)` lança exceção.
- **Médio, `EnemyEncounterWave.cs:141-146`:** `areaMultiplier` cresce sem teto a cada falha de spawn, então inimigos podem nascer fora da arena.
- **Baixo/Médio, `EncounterTrigger.cs:20-39`:** `GetComponent<Collider>` por frame, `FindAnyObjectByType` por frame sem player e `Update` ativo depois do encontro.
- **Médio, `EnemyDeathLoopTest.cs:48-64`:** com `playOnStart = false`, o loop roda mesmo assim no primeiro frame. O componente e `EnemyGore.SpawnTestGore` (L59-66) são código de teste dentro de runtime e vão para a build.
- **Baixo:**
  - `EncounterBarrier` (L9, 10, 13) e `EncounterTrigger` (L9) sem tooltips; `EncounterBarrier` também não checa nulos (L36/42);
  - `EnemyDebug.DrawGizmos()` nunca é chamado, então o gizmo de detecção não aparece no F3 (L109);
  - `PracticeDummy` faz `Instantiate(this)` e clona o estado moribundo (L97-104);
  - `EnemyDodge` checa nulo de forma redundante (L86-96).

---

## 5. Sistemas, câmera, áudio, partículas e mundo

### Cena, tempo e pausa
- **Médio, `GamePauseController.cs:26-46`:** os donos da pausa são `object`s sem limpeza. Um HUD destruído sem `ReleasePause` deixa `timeScale = 0` para sempre.
- **Médio, `GamePauseController` + `GameTimeScaleEffects`:** a pausa restaura um *snapshot* do timeScale em vez de pedir recálculo.
  - Exemplo: pausar durante o slow-motion (0.05) e resetar durante a pausa. Ao despausar, volta 0.05 e nada mais corrige: o jogo **fica a 5% de velocidade**.
  - **Correção:** `ReleasePause` chama `GameTimeScaleEffects.Reapply()`.
- **Médio, `GameSceneLoader.cs:201-241`:** a troca de cena não normaliza o tempo nem a pausa (só o Game Over faz isso).
- **Médio, `GameTimeScaleEffects.cs:146`:** `Time.fixedDeltaTime` nunca é escalado e nenhum Rigidbody usa interpolação. A 0.05x, a física roda um passo a cada ~0,4 s reais, e gore e currency "saltam" no slow-motion de fim de encontro.
- **Baixo:**
  - `GameSceneLoader.isLoading` trava se o objeto for desativado no load (L216-241);
  - `SetGameplayPanelsEnabled(true)` reativa painéis desligados de propósito (L449-457);
  - `ResetForGameRestart` não reseta `GameDebugTimeScale.IsSlowMotion` (L24-37);
  - `DontDestroyOnLoad` é aplicado em objeto não-raiz (L98-99).
- **Baixo, `PersistentGameRoot.cs:12-28`:** o duplicado é destruído com atraso. Os filhos rodam `Awake`, e `CameraFollow`/`CameraEffectStack` sobrescrevem `Instance` sem guarda. Hoje é latente.

### Save e settings
- **Baixo, `GameSaveResetService.cs`:**
  - `isResetting` não protege nada e não tem `try/finally`;
  - se o loader estiver ocupado, os saves são apagados e a cena não recarrega;
  - `PlayerPrefs.DeleteAll()` apaga também as chaves de resolução e tela da Unity.
  - **Correção:** apagar só as chaves com prefixo do projeto.
- **Baixo, `GameSettingsService.cs`:**
  - uma versão futura do save é descartada em silêncio (L106-113);
  - a chave se chama `V1` mas guarda v2;
  - `SetQualityLevel(..., true)` roda a cada passo do slider (L57-63, 145).

### LoadingScreenHUD.cs
- **Médio, L257-396:** o HUD é feito inteiro em UGUI por código, com a fonte `LegacyRuntime.ttf`. Viola duas regras do projeto: UXML/USS + `PanelRenderer`, e Crimson Text.
- **Médio, L206-229:** `SuppressCompetingUi` só trata `Canvas`, que nenhuma cena usa. O código é morto, custa `FindObjectsByType` e não esconde os HUDs UI Toolkit.
- **Baixo:** `LoadSceneWithoutSceneLoader` não tem trava de reentrância (L131-138).

### Combate
- **Médio, `CombatImpactSystem.cs:176-194, 302-353`:** `IsGameplayFrozen`/`IsPresentationFrozen` são consultados por frame em cerca de 12 sistemas. Cada chamada faz 2 `GetComponentInParent` e `List.Find`/`Exists` com closure. `FreezePresentation` faz `GetComponentsInChildren<Animator>` por golpe.
- **Baixo, `CombatImpactSystem`:**
  - `OnDisable` limpa listas estáticas mesmo no duplicado destruído (L134-143);
  - `hitstopDuration = .005f` é menor que um frame (L42);
  - campos sem tooltip (L40, 45, 63).
- **Baixo, `CombatPoiseSystem.cs:136`:** `Instance` pode criar um GameObject no teardown.
- **Médio, `GroundEntitySeparationSystem.cs:59-99`:** 2 `FindObjectsByType` por `LateUpdate` e `GetComponent` por inimigo. Usar um registro em `OnEnable`/`OnDisable`.
- **Baixo, `ScenePlayerSpawnPoint.cs:35-36`:** com vários `isDefault` ou `spawnId` inexistente, não há aviso e a ordem não é determinística.

### Câmera
- **Médio, `FixedCameraZone.cs:243-249`:** as margens de saída são triggers reais na layer Default e ficam visíveis para queries e `OnTrigger` de outros sistemas.
- **Baixo:**
  - `FixedCameraZone` reatribui tamanho e centro dos colliders todo frame (L267-297), forçando reconstrução no PhysX;
  - `CameraFollow`: clicar para travar o cursor também dispara um ataque leve (L593-606), e Damping/Priority são reatribuídos todo frame (L534-554);
  - `CameraEffectStack`: aloca um impulso por golpe, e o yaw é *unscaled* enquanto o Cinemachine é *scaled*, então em slow-motion os tremores dessincronizam (L86-137);
  - `CameraGameplayEffects`: `EnsureThermalProfile` roda `FindObjectsByType<Volume>` por frame sem Volume e não se recupera se o Volume for destruído (L129-134, 265-283).

### Áudio
- **Baixo, `GameAudioSystem`:**
  - `new List<AudioClip>` a cada som (L47-60);
  - `AudioCue` sem tooltips (L17-26);
  - casos duplicados entre `if` e `switch` (L311-332);
  - **não há AudioMixer**, e voz usa o volume de efeitos (L201-216, 284-285): o jogador não consegue separar voz e SFX.
- **Baixo:**
  - `GameplayAudioSystem.Play` aloca um handle por som, inclusive passos (L113-148);
  - em `GameplayFootstepEmitter`, um teleporte (spawn de cena) gera um deslocamento enorme num único frame e toca um passo falso (L27-47). Ignorar frames com deslocamento acima de um limite.

### Partículas
- **Alto, `CombatParticleCollisionFilter.cs:10-21`:** ver resumo, item 7. Usar a layer `CombatParticle` + matriz de colisão.
- **Médio, `CombatParticleSystem.cs:213-214, 268-272`:** com o pool cheio, `ReuseOldest` recicla partículas que **carregam currency**, e o dinheiro some antes de ser coletado.
- **Médio, `CurrencyGorePickup.cs:13-18`:** `attractionElapsed` não é zerado em `Initialize`. Partículas reutilizadas começam a atração já no fim da curva, e a currency "teleporta".
- **Médio, `CombatVisualParticle.cs`:** um `Material` próprio por partícula, com `Destroy` + `new Material` quando a origem muda. Não há batching (até 192 draw calls transparentes). O fade roda `SetPropertyBlock` todo frame, mesmo com alpha 1.
- **Baixo:**
  - `ContinuousDynamic` em até 192 detritos;
  - o pool de partes do corpo cresce sem limite;
  - `Shader.Find` exige shaders incluídos na build.

### Mundo
- **Médio, `StrenghLever.cs`:** com `singleUse = false`, qualquer golpe após a ativação **reativa** a alavanca e dispara `onActivated` de novo, ao contrário do que o tooltip descreve.
- **Médio, `CurrencyShopInteractionArea.cs:84`:** tecla E fixa (ver seção 2.3). A busca do mais próximo é O(n²) por frame.

---

## 6. UI, diálogo, cutscenes e editor

### Regras do projeto
- **Alto:** Crimson Text não é aplicada (resumo, item 6). Ela só aparece em `DialogueController` e `AreaTitleHUD`, via C#. **Correção:** `-unity-font-definition` em `:root` no `.tss`, ou configurar o Text Settings do `PanelSettings`.
- **Alto, `CurrencyShopHUD.cs:187-239, 322-328`:** a loja é **construída inteira em C#** com estilos inline e não tem UXML/USS próprios. O seletor `.currency-shop-selected` está "emprestado" do `PauseMenuHUD.uss:113`.
- **Médio:** HUDs fora do padrão UI Toolkit:
  - `LoadingScreenHUD` (UGUI por código);
  - `PlayerLockOn`, `DamageNumber`, `PlayerDpsMeter`, `PlayerAttackDebug`, `EnemyDebug` (IMGUI/`OnGUI`). `DamageNumber` cria um `GameObject` por acerto e `new GUIStyle` por evento.
- **Baixo:** `PauseMenuHUD.cs:425-456` cria linhas de opção com estilos inline. `PlayerHUDToolkit.cs:142-166` sobrescreve o USS com estilos inline.
- **Baixo:** gizmos fora do F3 em `DialogueInteraction` (`OnDrawGizmosSelected`, L213-249) e `PlayerSwingAnchor`.
- **Baixo, tooltips ausentes:**
  - `panelRenderer` em `TemporaryWorldPromptHUD:12`, `PlayerDebugToolkitHUD:16-20`, `BossHealthHUD:12` e `GameOverHUD:11`;
  - `TemporaryWorldPromptTrigger` (L30-32, 86).

### USS inválido (Médio)
Propriedades sem o hífen inicial são ignoradas pela Unity, com aviso:
- `unity-font-style` em:
  - `AreaTitleHUD.uss:30`: o subtítulo não fica itálico;
  - `BossHealthHUD.uss:22`;
  - `GameOverHUD.uss:29`: "GAME OVER" não fica em negrito;
  - `PlayerDebugHUD.uss:21, 62, 69, 93`;
  - `TemporaryWorldPrompt.uss:42, 55, 61`.
- `unity-text-align` em `TemporaryWorldPrompt.uss:53`.
- `PlayerHUD.uss:10`: `pointer-events` não existe em USS. Usar `picking-mode="Ignore"` no UXML.
- Seletores sem uso: `.thermometer-notch-major` (`PlayerHeatHUD.uss:70-73`) e `.thermometer-notch-fill` duplicado (L106-109).

### Bugs e performance de HUD
- **Médio, `BossHealthHUD.cs:40, 63`:** `Show`/`Hide` não têm nenhum chamador, então **o HUD de chefe nunca aparece**. A função `ValidateElement` está vazia.
- **Médio, `TemporaryWorldPromptHUD.cs:98-102`:** perto de um NPC, o prompt de tutorial congela, fica invisível e não aceita a tecla.
- **Médio, `TemporaryWorldPromptHUD.cs:40-75` + `DialogueInteraction.cs:91-93` + `CutsceneInteraction.cs:68-72`:** `ShowContextualPrompt` recria elementos todo frame dentro da área.
- **Médio, `PlayerHUDToolkit.cs:251, 349, 267-294, 377-399`:** interpolação de string por frame, mesmo com a barra oculta, e cerca de 25 notches recebendo 6 a 8 estilos inline por frame.
- **Médio, `DialogueController.cs:101`:** a duração das falas usa `Time.deltaTime`, então as falas ficam até 4x mais longas no slow-motion.
- **Baixo:**
  - `GameOverHUD.uxml:7` tem "PRESSIONE R" fixo e ignora o rebind;
  - `GameOverHUD.uss:1` e `BossHealthHUD.uss:1` são visíveis por padrão (deveriam começar com `display: none`);
  - `PlayerDebugToolkitHUD.OnDisable` dessincroniza `isVisible` (L67-70) e atualiza textos sem throttle;
  - `PlayerHUDToolkit` mistura `deltaTime` e `unscaledDeltaTime` (L327);
  - `CurrencyShopHUD.LateUpdate` chama `BringToFront()` todo frame (L185);
  - `DialogueController.Instance` faz `FindAnyObjectByType` a cada acesso enquanto não há instância (L43-44);
  - `CutsceneObjectAction` faz `FindAnyObjectByType<SignalReceiver>` e pode pegar o receiver errado (L42).
- **Baixo (mudanças recentes de diálogo):**
  - `.dialogue-text-block { align-items: center }` deixa o alinhamento do `DialogueSpeakerLibrary` sem efeito;
  - `using UnityEngine.UIElements` sem uso em `DialogueSpeakerLibrary.cs:4`;
  - o tooltip de alinhamento omite o eixo vertical;
  - `JulianeDormindo.asset` tem `voiceOverVolume: 0` em todas as linhas, então uma voz adicionada depois toca muda.
  - Observação: `DialogueSequence.asset` foi **renomeado** para `JulianeDormindo.asset` com o GUID preservado, e não há referência quebrada.

### Editor e testes
- **Médio:** não há testes NUnit nem `asmdef` de testes, apesar de `com.unity.test-framework` estar instalado. As "regressões" (`SelfLaunchDurationRegression`, `PlayerHeldBranchRegression`) são MenuItems que não aparecem no Test Runner e não rodam em CI.
- **Médio, `SelfLaunchDurationRegression.cs:56`:** abre `Assets/Scenes/TestArena.unity`, que **não existe** (o arquivo real é `999. TestArena.unity`). O teste nunca roda.
- **Médio, `SelfLaunchDurationRegression.cs:44-65`:** se o Play for interrompido, a flag `RunKey` fica no EditorPrefs e **sequestra o próximo Play comum**.
- **Médio, `SelfLaunchDurationRegression.cs:117, 310-316`:** o parse depende da cultura do sistema ("0,13"). Em en-US os valores viram `NaN` e o relatório marca **PASS falso**.
- **Baixo, `SelfLaunchDurationRegression`:**
  - `Fail()` não retorna e mascara a causa com uma NRE;
  - zera `playModeStartScene` sem restaurar o Bootstrap.
- **Baixo:**
  - `PlayerProgressionSetup` com `[InitializeOnLoad]` recria assets e prefab a cada recompilação, inclusive os apagados de propósito;
  - `PlayerAttackDefinitionEditor.cs:77` tem um `ApplyModifiedProperties` redundante;
  - o `#if UNITY_EDITOR` é redundante dentro de `Editor/`.

---

## 7. O que está correto (verificado)
- Os nomes de ações usados em `GameInput` existem no `.inputactions`, e todos os `Q<>()` de UI Toolkit existem nos UXML.
- O código de Editor fora de `Assets/Editor` está protegido por `#if UNITY_EDITOR`, então não quebra a build.
- Os `RegisterUIReloadCallback` têm o `Unregister` correspondente.
- Os eventos estáticos de `PauseMenuHUD` são desinscritos.
- `AreaTitleHUD`, `PauseMenuHUD` e `CurrencyShopHUD` usam tempo unscaled.
- `.gitignore` e `.gitattributes` (LFS) estão corretos. Não há `Library/`, `Temp/` ou `.csproj` versionados, e os binários grandes estão em LFS.
- Não há `.meta` órfão nem asset sem `.meta`.
- Não há `Debug.Log` solto nem uso do Input Manager legado (`activeInputHandler: 1`).

---

## 8. Status após as correções (03/11/2026)

### Corrigido

- Build Settings, rotas de carregamento e atalhos de debug em build de release; reset de saves agora apaga apenas dados do ProjetoPSX.
- Bugs de Heat/Cold, reaplicação de upgrades, dano/morte durante ataques, pausa/slow motion e estado estático com Domain Reload desativado.
- Bindings de controle, prompts de rebind, swing, skip de cutscene e sensibilidade de câmera por dispositivo.
- Alocações e consultas recorrentes da hitbox, separação de entidades, partículas, turnos do EnemyDirector e colisões de ataques de inimigos. Buffers não alocadores crescem quando lotam para não descartar alvos.
- Fila de ataques especiais também nas continuações, timeout de aproximação de inimigos, liberação dos turnos, término de ondas e limites de área de spawn.
- Bugs de guarda direcional, teleporte de spawn reiniciado, shader ausente em apresentação de spawn, moeda em objetos pooled e persistência de currency coletada.
- Erros de USS, fonte Crimson Text nos HUDs Toolkit, entrada do marcador de lock-on em UI Toolkit, conversão do LoadingScreenHUD para UI Toolkit, prompts contextuais e hint de retry rebindável.
- Busca de Volume global da câmera cacheada; buscas de Player sem sucesso em encontros agora têm intervalo; EncounterTrigger cacheia collider e para após iniciar; teletransportes não emitem passos falsos.
- Regressão de lançamento aponta à cena existente e restaura o ponto inicial de Play; o loop de teste não inicia quando desativado e os efeitos de teste ficam fora de builds de release.

### Adiado por escopo ou por depender de validação/decisão

- PlayerMovement agora acumula locomoção, avanço de ataque, separação, swing e grapple para um `Move` por frame em LateUpdate. A regressão isolada falhou (0 commits do jogador temporário; D1 não iniciou SelfLaunchUp), portanto a implementação está pendente de diagnóstico e validação de gameplay. Consulte `RETRABALHO_PLAYERMOVEMENT.md`.
- Corrigir o comportamento visual de cores em stagger/state sem instanciar materiais e escolher a estratégia de material por renderer. Requer conferir os materiais reais de personagens e VFX para não trocar a aparência.
- Converter `CurrencyShopHUD`, `DamageNumber`, `PlayerDpsMeter`, `PlayerAttackDebug` e `EnemyDebug` para UI Toolkit. O ganho depende de desenhar os UXML/USS e a forma de posicionamento mundial para cada elemento; o HUD de loja também é construído programaticamente e precisa migração completa.
- Conectar `BossHealthHUD`: a revisão não identifica quais inimigos/prefabs contam como chefes; precisa definir essa marcação antes de ligar o HUD ao ciclo de vida do boss.
- Mudar física de slow motion, volumes de saída de `FixedCameraZone`, mixer de áudio, batching de partículas, e política de limpeza de pacotes/assets/texturas. São mudanças de comportamento, autoria ou apresentação com impacto amplo; não foram feitas sem validação/decisão.
- Criar testes NUnit/CI. Os projetos gerados do Unity não contêm assemblies de teste; configurar isso é uma etapa de estrutura do projeto.
- A regressão de cena para o PlayerMovement foi executada numa cópia isolada e falhou com 0 commits no jogador temporário e sem iniciar SelfLaunchUp. Verifique o AudioListener durante Play e execute o MenuItem no Editor para diagnosticar. Consulte [PENDENCIAS_USUARIO.md](PENDENCIAS_USUARIO.md).
