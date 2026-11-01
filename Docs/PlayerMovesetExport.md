# Export completo dos Movesets do Player

> Gerado a partir dos ScriptableObjects atualmente salvos em `Assets/GameData/Player/Moveset`. Os blocos YAML preservam os valores serializados completos, incluindo curvas e configuracoes detalhadas de cada hitbox.

## Indice rapido

- [Moveset Normal](#moveset-normal)
- [Moveset Human](#moveset-human)
- [Valores completos dos ataques](#valores-completos-dos-ataques)
- [Contexto simples](#contexto-simples)

## Moveset Normal

Arquivo: `Assets/GameData/Player/Moveset/Normal/NormalMoveset.asset`

### Entradas

| Botao | Contexto | Node inicial |
|---|---|---|
| Light | Ground | `L1` |
| Heavy | Ground | `H1` |
| Light | Dash | `D1` |
| Light | Air | `AirL1` |
| Heavy | Air | `AirH1` |

### Nodes e transicoes

| Node | Asset de ataque | Transicoes |
|---|---|---|
| `L1` | `L1` (`Assets/GameData/Player/Moveset/Normal/L1.asset`) | Light -> `L2`; Light -> `L1h` (hold do mesmo clique) |
| `L1h` | `L1h` (`Assets/GameData/Player/Moveset/Normal/L1h.asset`) | nenhuma |
| `L2` | `L2` (`Assets/GameData/Player/Moveset/Normal/L2.asset`) | Light -> `L3`; Light -> `L3p` (Pause) |
| `L3` | `L3` (`Assets/GameData/Player/Moveset/Normal/L3.asset`) | Light -> `L4`; Light -> `L3h` (hold do mesmo clique) |
| `L3h` | `L3h` (`Assets/GameData/Player/Moveset/Normal/L3h.asset`) | Light -> `L4` |
| `L3p` | `L3p` (`Assets/GameData/Player/Moveset/Normal/L3p.asset`) | Light -> `L4p` |
| `L4` | `L4` (`Assets/GameData/Player/Moveset/Normal/L4.asset`) | nenhuma |
| `L4p` | `L4p` (`Assets/GameData/Player/Moveset/Normal/L4p.asset`) | nenhuma |
| `H1` | `H1` (`Assets/GameData/Player/Moveset/Normal/H1.asset`) | nenhuma |
| `D1` | `D1` (`Assets/GameData/Player/Moveset/Normal/D1.asset`) | nenhuma |
| `AirL1` | `AirL1` (`Assets/GameData/Player/Moveset/Normal/AirL1.asset`) | Light -> `AirL2` |
| `AirL2` | `AirL2` (`Assets/GameData/Player/Moveset/Normal/AirL2.asset`) | Light -> `AirL3` |
| `AirL3` | `AirL3` (`Assets/GameData/Player/Moveset/Normal/AirL3.asset`) | Light -> `AirL4` |
| `AirL4` | `AirL4` (`Assets/GameData/Player/Moveset/Normal/AirL4.asset`) | Light -> `AirL5` |
| `AirL5` | `AirL5` (`Assets/GameData/Player/Moveset/Normal/AirL5.asset`) | Light -> `AirL6` |
| `AirL6` | `AirL6` (`Assets/GameData/Player/Moveset/Normal/AirL6.asset`) | Light -> `AirL7` |
| `AirL7` | `AirL7` (`Assets/GameData/Player/Moveset/Normal/AirL7.asset`) | Light -> `AirL8` |
| `AirL8` | `AirL8` (`Assets/GameData/Player/Moveset/Normal/AirL8.asset`) | nenhuma |
| `AirH1` | `AirH1` (`Assets/GameData/Player/Moveset/Normal/AirH1.asset`) | nenhuma |

### Configuracao serializada completa

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: b6c0923b5d764d3894c72c934ddb8589, type: 3}
  m_Name: NormalMoveset
  m_EditorClassIdentifier: Assembly-CSharp::PlayerMovesetDefinition
  displayName: Normal
  resourceMode: 0
  globalPauseDelay: 0.4
  movementSpeedMultiplier: 1
  allowDash: 1
  allowAirJump: 1
  allowAirDash: 1
  jumpHeightMultiplier: 1
  jumpDurationMultiplier: 1
  dashImpulse: 12.8
  dashCooldown: 1
  lightWindupActiveRotationSpeed: 450
  heavyWindupActiveRotationSpeed: 350
  heldAttackBranchThreshold: 0.3
  targetAssistRangePadding: 0.25
  lightTransitionTiming:
    minimumDelayAfterWindup: 0
    continuationWindow: 0.9
  heavyTransitionTiming:
    minimumDelayAfterWindup: 0
    continuationWindow: 0.9
  extendedArmHitboxRadius: 0.25
  extendedArmHitboxHeightOffset: 0.45
  lightCancelRules:
  - action: 2
    allowedPhases: 6
    finishActiveBeforeCancelling: 1
  - action: 3
    allowedPhases: 6
    finishActiveBeforeCancelling: 1
  - action: 1
    allowedPhases: 7
    finishActiveBeforeCancelling: 0
  - action: 4
    allowedPhases: -1
    finishActiveBeforeCancelling: 0
  heavyCancelRules:
  - action: 2
    allowedPhases: 4
    finishActiveBeforeCancelling: 1
  - action: 3
    allowedPhases: 4
    finishActiveBeforeCancelling: 1
  - action: 4
    allowedPhases: 4
    finishActiveBeforeCancelling: 1
  entries:
  - input: 0
    context: 0
    targetNodeId: L1
  - input: 1
    context: 0
    targetNodeId: H1
  - input: 0
    context: 2
    targetNodeId: D1
  - input: 0
    context: 1
    targetNodeId: AirL1
  - input: 1
    context: 1
    targetNodeId: AirH1
  nodes:
  - nodeId: L1
    attack: {fileID: 11400000, guid: 54d1fc2271d160849b7c6cfc939fd6ef, type: 2}
    transitions:
    - input: 0
      targetNodeId: L2
      requiresHeldInput: 0
      requiresPause: 0
      requiresHitConfirm: 0
    - input: 0
      targetNodeId: L1h
      requiresHeldInput: 1
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: L1h
    attack: {fileID: 11400000, guid: db7e686f168096f489b9adcbf6f25a88, type: 2}
    transitions: []
  - nodeId: L2
    attack: {fileID: 11400000, guid: b5c584aa6e940a44caa09e31d4925dea, type: 2}
    transitions:
    - input: 0
      targetNodeId: L3
      requiresHeldInput: 0
      requiresPause: 0
      requiresHitConfirm: 0
    - input: 0
      targetNodeId: L3p
      requiresHeldInput: 0
      requiresPause: 1
      requiresHitConfirm: 0
  - nodeId: L3
    attack: {fileID: 11400000, guid: 0521bd37a88a5e84a9d37f42db85c475, type: 2}
    transitions:
    - input: 0
      targetNodeId: L4
      requiresHeldInput: 0
      requiresPause: 0
      requiresHitConfirm: 0
    - input: 0
      targetNodeId: L3h
      requiresHeldInput: 1
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: L3h
    attack: {fileID: 11400000, guid: 1602d7647da607f46ad944461f452088, type: 2}
    transitions:
    - input: 0
      targetNodeId: L4
      requiresHeldInput: 0
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: L3p
    attack: {fileID: 11400000, guid: 06ff9d088f5a4af4e90452ef7653392b, type: 2}
    transitions:
    - input: 0
      targetNodeId: L4p
      requiresHeldInput: 0
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: L4
    attack: {fileID: 11400000, guid: 25871bd56eaedcc4da477ff5382d1d40, type: 2}
    transitions: []
  - nodeId: L4p
    attack: {fileID: 11400000, guid: 726d7b2a73827a149a94f2a00ac0fe3c, type: 2}
    transitions: []
  - nodeId: H1
    attack: {fileID: 11400000, guid: 8a9407bd5f8cbaa45b8e5b21f924c8ea, type: 2}
    transitions: []
  - nodeId: D1
    attack: {fileID: 11400000, guid: 89836aba85f921d4e8a7891a54c750ea, type: 2}
    transitions: []
  - nodeId: AirL1
    attack: {fileID: 11400000, guid: e7b1b0ff71f7839479012180d069aa73, type: 2}
    transitions:
    - input: 0
      targetNodeId: AirL2
      requiresHeldInput: 0
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: AirL2
    attack: {fileID: 11400000, guid: f56c932af45fe8d4ca6b29f134a6729c, type: 2}
    transitions:
    - input: 0
      targetNodeId: AirL3
      requiresHeldInput: 0
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: AirL3
    attack: {fileID: 11400000, guid: dd2e98d891c8fb248b41a9d1e11a3e3c, type: 2}
    transitions:
    - input: 0
      targetNodeId: AirL4
      requiresHeldInput: 0
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: AirL4
    attack: {fileID: 11400000, guid: a76ca13646650bf4a8f2755e05b49ea0, type: 2}
    transitions:
    - input: 0
      targetNodeId: AirL5
      requiresHeldInput: 0
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: AirL5
    attack: {fileID: 11400000, guid: 1b6c98b8d19cbfc4c8460f94a0388957, type: 2}
    transitions:
    - input: 0
      targetNodeId: AirL6
      requiresHeldInput: 0
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: AirL6
    attack: {fileID: 11400000, guid: 2c5cfa9cdb4398349858171fe4330323, type: 2}
    transitions:
    - input: 0
      targetNodeId: AirL7
      requiresHeldInput: 0
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: AirL7
    attack: {fileID: 11400000, guid: aaec7f07e29e7054c9651fa6014127bf, type: 2}
    transitions:
    - input: 0
      targetNodeId: AirL8
      requiresHeldInput: 0
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: AirL8
    attack: {fileID: 11400000, guid: df3b5c96e8b99da4e998203a701c3e53, type: 2}
    transitions: []
  - nodeId: AirH1
    attack: {fileID: 11400000, guid: 2f2381b2b926b3644816e32e4242ce22, type: 2}
    transitions: []
```

## Moveset Human

Arquivo: `Assets/GameData/Player/Moveset/Human/HumanMoveset.asset`

### Entradas

| Botao | Contexto | Node inicial |
|---|---|---|
| Light | Ground | `L1` |
| Heavy | Ground | `H1` |
| Light | Dash | `D1` |
| Light | Air | `AirL1` |
| Heavy | Air | `AirH1` |

### Nodes e transicoes

| Node | Asset de ataque | Transicoes |
|---|---|---|
| `L1` | `L1` (`Assets/GameData/Player/Moveset/Human/L1.asset`) | Light -> `L2` |
| `L2` | `L2` (`Assets/GameData/Player/Moveset/Human/L2.asset`) | Light -> `L3`; Light -> `L3p` (Pause) |
| `L3` | `L3` (`Assets/GameData/Player/Moveset/Human/L3.asset`) | Light -> `L4` |
| `L3p` | `L3p` (`Assets/GameData/Player/Moveset/Human/L3p.asset`) | Light -> `L4p` |
| `L4` | `L4` (`Assets/GameData/Player/Moveset/Human/L4.asset`) | nenhuma |
| `L4p` | `L4p` (`Assets/GameData/Player/Moveset/Human/L4p.asset`) | nenhuma |
| `H1` | `H1` (`Assets/GameData/Player/Moveset/Human/H1.asset`) | nenhuma |
| `D1` | (sem asset: fileID 0) | nenhuma |
| `AirL1` | `AirL1` (`Assets/GameData/Player/Moveset/Human/AirL1.asset`) | Light -> `AirL2` |
| `AirL2` | `AirL2` (`Assets/GameData/Player/Moveset/Human/AirL2.asset`) | Light -> `AirL3` |
| `AirL3` | (sem asset: fileID 0) | Light -> `AirL4` |
| `AirL4` | (sem asset: fileID 0) | Light -> `AirL5` |
| `AirL5` | (sem asset: fileID 0) | Light -> `AirL6` |
| `AirL6` | (sem asset: fileID 0) | Light -> `AirL7` |
| `AirL7` | (sem asset: fileID 0) | Light -> `AirL8` |
| `AirL8` | (sem asset: fileID 0) | nenhuma |
| `AirH1` | `AirH1` (`Assets/GameData/Player/Moveset/Human/AirH1.asset`) | nenhuma |

### Configuracao serializada completa

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: b6c0923b5d764d3894c72c934ddb8589, type: 3}
  m_Name: HumanMoveset
  m_EditorClassIdentifier: Assembly-CSharp::PlayerMovesetDefinition
  displayName: Human
  resourceMode: 1
  globalPauseDelay: 0.5
  movementSpeedMultiplier: 0.85
  allowDash: 0
  allowAirJump: 0
  allowAirDash: 0
  jumpHeightMultiplier: 1
  jumpDurationMultiplier: 0.85
  dashImpulse: 11
  dashCooldown: 1.3
  lightWindupActiveRotationSpeed: 1500
  heavyWindupActiveRotationSpeed: 800
  lightTransitionTiming:
    minimumDelayAfterWindup: 0
    continuationWindow: 0.6
  heavyTransitionTiming:
    minimumDelayAfterWindup: 0
    continuationWindow: 0.6
  extendedArmHitboxRadius: 0.15
  extendedArmHitboxHeightOffset: 0.45
  lightCancelRules:
  - action: 2
    allowedPhases: 6
    finishActiveBeforeCancelling: 1
  - action: 3
    allowedPhases: 6
    finishActiveBeforeCancelling: 1
  - action: 1
    allowedPhases: 7
    finishActiveBeforeCancelling: 0
  heavyCancelRules:
  - action: 2
    allowedPhases: 4
    finishActiveBeforeCancelling: 1
  - action: 3
    allowedPhases: 4
    finishActiveBeforeCancelling: 1
  entries:
  - input: 0
    context: 0
    targetNodeId: L1
  - input: 1
    context: 0
    targetNodeId: H1
  - input: 0
    context: 2
    targetNodeId: D1
  - input: 0
    context: 1
    targetNodeId: AirL1
  - input: 1
    context: 1
    targetNodeId: AirH1
  nodes:
  - nodeId: L1
    attack: {fileID: 11400000, guid: 895f992d1dbb8fa43bf23a28ef8c3c13, type: 2}
    transitions:
    - input: 0
      targetNodeId: L2
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: L2
    attack: {fileID: 11400000, guid: 118d9b41db4de3246a77cbb35ac6f33f, type: 2}
    transitions:
    - input: 0
      targetNodeId: L3
      requiresPause: 0
      requiresHitConfirm: 0
    - input: 0
      targetNodeId: L3p
      requiresPause: 1
      requiresHitConfirm: 0
  - nodeId: L3
    attack: {fileID: 11400000, guid: 31c2010bba316b141823847018791753, type: 2}
    transitions:
    - input: 0
      targetNodeId: L4
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: L3p
    attack: {fileID: 11400000, guid: ebcae8a04f5bb0844bae7fe97c6d897c, type: 2}
    transitions:
    - input: 0
      targetNodeId: L4p
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: L4
    attack: {fileID: 11400000, guid: f880ab7b17f2eea459267ed79dc025cd, type: 2}
    transitions: []
  - nodeId: L4p
    attack: {fileID: 11400000, guid: abbe91d9563134b419fb59634d46e1ed, type: 2}
    transitions: []
  - nodeId: H1
    attack: {fileID: 11400000, guid: 95941554b61b46d45aa574e36b69c975, type: 2}
    transitions: []
  - nodeId: D1
    attack: {fileID: 0}
    transitions: []
  - nodeId: AirL1
    attack: {fileID: 11400000, guid: c10c703d59c260d4d8d92d1060e29a1f, type: 2}
    transitions:
    - input: 0
      targetNodeId: AirL2
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: AirL2
    attack: {fileID: 11400000, guid: 94f11a5cf075c8d4a9b656ad5fc3abc9, type: 2}
    transitions:
    - input: 0
      targetNodeId: AirL3
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: AirL3
    attack: {fileID: 0}
    transitions:
    - input: 0
      targetNodeId: AirL4
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: AirL4
    attack: {fileID: 0}
    transitions:
    - input: 0
      targetNodeId: AirL5
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: AirL5
    attack: {fileID: 0}
    transitions:
    - input: 0
      targetNodeId: AirL6
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: AirL6
    attack: {fileID: 0}
    transitions:
    - input: 0
      targetNodeId: AirL7
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: AirL7
    attack: {fileID: 0}
    transitions:
    - input: 0
      targetNodeId: AirL8
      requiresPause: 0
      requiresHitConfirm: 0
  - nodeId: AirL8
    attack: {fileID: 0}
    transitions: []
  - nodeId: AirH1
    attack: {fileID: 11400000, guid: b6b8243c8fe6d6047b1498a87ac990f3, type: 2}
    transitions: []
```

## Valores completos dos ataques

Todos os ataques `.asset` presentes nas pastas `Normal` e `Human` estao listados abaixo. Isso inclui o Grapple (habilidade separada das rotas de combo) e ataques auxiliares que nao aparecem como node atual.

### Ataques Normal

| ID | Tipo | Contexto | Windup | Active | Recovery | Cooldown | Dano | Poise | Avanco | Strikes | Tracks | Hitgroups | Asset |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| AirH1 | Pesado | Aereo | 0.5 | 0.03 | 0.35 | 0 | 120 | 80 | 1 | 1 | 1 | 1 | `Assets/GameData/Player/Moveset/Normal/AirH1.asset` |
| AirL1 | Leve | Aereo | 0.02 | 0.15 | 0.05 | 0 | 7 | 9 | 0.2 | 1 | 2 | 2 | `Assets/GameData/Player/Moveset/Normal/AirL1.asset` |
| AirL2 | Leve | Aereo | 0.02 | 0.15 | 0.05 | 0 | 7 | 9 | 0.2 | 1 | 2 | 2 | `Assets/GameData/Player/Moveset/Normal/AirL2.asset` |
| AirL3 | Leve | Aereo | 0.02 | 0.15 | 0.05 | 0 | 7 | 9 | 0.2 | 1 | 2 | 2 | `Assets/GameData/Player/Moveset/Normal/AirL3.asset` |
| AirL4 | Leve | Aereo | 0.02 | 0.15 | 0.08 | 0 | 18 | 9 | 0.2 | 1 | 2 | 2 | `Assets/GameData/Player/Moveset/Normal/AirL4.asset` |
| AirL5 | Leve | Aereo | 0.02 | 0.15 | 0.08 | 0 | 18 | 9 | 0.2 | 1 | 2 | 2 | `Assets/GameData/Player/Moveset/Normal/AirL5.asset` |
| AirL6 | Leve | Aereo | 0.02 | 0.15 | 0.08 | 0 | 18 | 9 | 0.2 | 1 | 2 | 2 | `Assets/GameData/Player/Moveset/Normal/AirL6.asset` |
| AirL7 | Leve | Aereo | 0.02 | 0.15 | 0.08 | 0 | 18 | 9 | 0.2 | 1 | 2 | 2 | `Assets/GameData/Player/Moveset/Normal/AirL7.asset` |
| AirL8 | Leve | Aereo | 0.02 | 0.05 | 0.08 | 0 | 30 | 50 | 0 | 1 | 1 | 1 | `Assets/GameData/Player/Moveset/Normal/AirL8.asset` |
| BurnExplosion | Pesado | Chao | 0 | 0.2 | 0 | 0 | 2000 | 270 | 0 | 1 | 1 | 1 | `Assets/GameData/Player/Moveset/Normal/BurnExplosion.asset` |
| D1 | Leve | Dash | 0.06 | 0.15 | 0.06 | 0 | 20 | 50 | 2 | 1 | 1 | 1 | `Assets/GameData/Player/Moveset/Normal/D1.asset` |
| Grapple | Leve | Chao | 0.03 | 0.4 | 0 | 0.65 | 5 | 10 | 0.4 | 1 | 1 | 1 | `Assets/GameData/Player/Moveset/Normal/Grapple.asset` |
| H1 | Pesado | Chao | 0.5 | 0.03 | 0.35 | 0 | 120 | 80 | 2 | 1 | 1 | 1 | `Assets/GameData/Player/Moveset/Normal/H1.asset` |
| L1 | Leve | Chao | 0.02 | 0.2 | 0.08 | 0 | 10 | 20 | 0.7 | 1 | 2 | 2 | `Assets/GameData/Player/Moveset/Normal/L1.asset` |
| L1h | Leve | Chao | 0.4 | 0.05 | 0 | 0 | 25 | 60 | 0 | 1 | 1 | 1 | `Assets/GameData/Player/Moveset/Normal/L1h.asset` |
| L2 | Leve | Chao | 0.02 | 0.2 | 0.08 | 0 | 10 | 20 | 0.7 | 1 | 2 | 2 | `Assets/GameData/Player/Moveset/Normal/L2.asset` |
| L3 | Leve | Chao | 0.08 | 0.4 | 0.07 | 0 | 7 | 5 | 1.1 | 1 | 5 | 5 | `Assets/GameData/Player/Moveset/Normal/L3.asset` |
| L3h | Leve | Chao | 0.3 | 0.9 | 0.1 | 0 | 3 | 5 | 2.3 | 1 | 15 | 15 | `Assets/GameData/Player/Moveset/Normal/L3h.asset` |
| L3p | Leve | Chao | 0.05 | 0.8 | 0.1 | 0 | 30 | 55 | 2 | 1 | 2 | 1 | `Assets/GameData/Player/Moveset/Normal/L3p.asset` |
| L4 | Leve | Chao | 0.22 | 0.2 | 0.3 | 0 | 25 | 80 | 3 | 1 | 2 | 2 | `Assets/GameData/Player/Moveset/Normal/L4.asset` |
| L4p | Leve | Chao | 0.15 | 1 | 0.35 | 0 | 50 | 110 | 4 | 1 | 2 | 1 | `Assets/GameData/Player/Moveset/Normal/L4p.asset` |

#### Serializacao integral dos assets

##### AirH1

Arquivo: `Assets/GameData/Player/Moveset/Normal/AirH1.asset`  
GUID: `2f2381b2b926b3644816e32e4242ce22`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: AirH1
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: AirH1
  displayName: AirH1
  kind: 1
  context: 1
  windupDuration: 0.5
  activeDuration: 0.03
  recoveryDuration: 0.35
  holdActiveUntilGrounded: 1
  baseDamage: 120
  basePoiseDamage: 80
  movement:
    forwardDistance: 1
    startNormalized: 0
    endNormalized: 1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: AirH1
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 0
    effects:
      launchDown: 1
      selfLaunchDown: 1
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      juggle: 0
    hitboxes:
    - trackName: Heavy Sphere
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.4, z: 1.15}
      endLocalPosition: {x: 0, y: -0.8, z: 1.15}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 2
      capsuleHeight: 1
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 1
      sweep: 0
      sweepStepDistance: 0.25
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.1
  usesExtendedArmHitbox: 0
```

##### AirL1

Arquivo: `Assets/GameData/Player/Moveset/Normal/AirL1.asset`  
GUID: `e7b1b0ff71f7839479012180d069aa73`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: AirL1
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: AirL1
  displayName: AirL1
  kind: 0
  context: 1
  windupDuration: 0.02
  activeDuration: 0.15
  recoveryDuration: 0.05
  holdActiveUntilGrounded: 0
  baseDamage: 7
  basePoiseDamage: 9
  movement:
    forwardDistance: 0.2
    startNormalized: 0
    endNormalized: 1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: AirL1
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      launchBackwards: 0
      juggle: 1
    hitboxes:
    - trackName: AirL1_Vai
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 3.5}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 0.5
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: AirL1_Volta
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0, y: 0.36, z: 3.5}
      endLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.5
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 1
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### AirL2

Arquivo: `Assets/GameData/Player/Moveset/Normal/AirL2.asset`  
GUID: `f56c932af45fe8d4ca6b29f134a6729c`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: AirL2
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: AirL2
  displayName: AirL2
  kind: 0
  context: 1
  windupDuration: 0.02
  activeDuration: 0.15
  recoveryDuration: 0.05
  holdActiveUntilGrounded: 0
  baseDamage: 7
  basePoiseDamage: 9
  movement:
    forwardDistance: 0.2
    startNormalized: 0
    endNormalized: 1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: AirL2
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      launchBackwards: 0
      juggle: 1
    hitboxes:
    - trackName: AirL2_Vai
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 3.5}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 0.5
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: AirL2_Volta
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0, y: 0.36, z: 3.5}
      endLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.5
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 1
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### AirL3

Arquivo: `Assets/GameData/Player/Moveset/Normal/AirL3.asset`  
GUID: `dd2e98d891c8fb248b41a9d1e11a3e3c`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: AirL3
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: AirL3
  displayName: AirL3
  kind: 0
  context: 1
  windupDuration: 0.02
  activeDuration: 0.15
  recoveryDuration: 0.05
  holdActiveUntilGrounded: 0
  baseDamage: 7
  basePoiseDamage: 9
  movement:
    forwardDistance: 0.2
    startNormalized: 0
    endNormalized: 1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: AirL3
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      launchBackwards: 0
      juggle: 1
    hitboxes:
    - trackName: AirL3_Vai
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 3.5}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 0.5
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: AirL3_Volta
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0, y: 0.36, z: 3.5}
      endLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.5
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 1
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### AirL4

Arquivo: `Assets/GameData/Player/Moveset/Normal/AirL4.asset`  
GUID: `a76ca13646650bf4a8f2755e05b49ea0`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: AirL4
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: AirL4
  displayName: AirL4
  kind: 0
  context: 1
  windupDuration: 0.02
  activeDuration: 0.15
  recoveryDuration: 0.08
  holdActiveUntilGrounded: 0
  baseDamage: 18
  basePoiseDamage: 9
  movement:
    forwardDistance: 0.2
    startNormalized: 0
    endNormalized: 1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: AirL4
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      launchBackwards: 0
      juggle: 1
    hitboxes:
    - trackName: AirL4_Vai
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.76, y: -0.4, z: -0.6}
      endLocalPosition: {x: 0.58, y: 1.11, z: 3}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.6
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 0.8
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: AirL4_Volta
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.58, y: 1.11, z: 3}
      endLocalPosition: {x: -0.76, y: 0, z: 0.2}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.6
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.8
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 1
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### AirL5

Arquivo: `Assets/GameData/Player/Moveset/Normal/AirL5.asset`  
GUID: `1b6c98b8d19cbfc4c8460f94a0388957`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: AirL5
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: AirL5
  displayName: AirL5
  kind: 0
  context: 1
  windupDuration: 0.02
  activeDuration: 0.15
  recoveryDuration: 0.08
  holdActiveUntilGrounded: 0
  baseDamage: 18
  basePoiseDamage: 9
  movement:
    forwardDistance: 0.2
    startNormalized: 0
    endNormalized: 1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: AirL5
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      launchBackwards: 0
      juggle: 1
    hitboxes:
    - trackName: AirL5_Vai
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.76, y: -0.4, z: -0.6}
      endLocalPosition: {x: -0.58, y: 1.11, z: 3}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.6
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 0.8
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: AirL5_Volta
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.58, y: 1.11, z: 3}
      endLocalPosition: {x: 0.76, y: 0, z: 0.2}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.6
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.8
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 1
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### AirL6

Arquivo: `Assets/GameData/Player/Moveset/Normal/AirL6.asset`  
GUID: `2c5cfa9cdb4398349858171fe4330323`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: AirL6
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: AirL6
  displayName: AirL6
  kind: 0
  context: 1
  windupDuration: 0.02
  activeDuration: 0.15
  recoveryDuration: 0.08
  holdActiveUntilGrounded: 0
  baseDamage: 18
  basePoiseDamage: 9
  movement:
    forwardDistance: 0.2
    startNormalized: 0
    endNormalized: 1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: AirL6
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      launchBackwards: 0
      juggle: 1
    hitboxes:
    - trackName: AirL6_Vai
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.76, y: -0.4, z: -0.6}
      endLocalPosition: {x: 0.58, y: 1.11, z: 3}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.6
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 0.8
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: AirL6_Volta
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.58, y: 1.11, z: 3}
      endLocalPosition: {x: -0.76, y: 0, z: 0.2}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.6
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.8
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 1
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### AirL7

Arquivo: `Assets/GameData/Player/Moveset/Normal/AirL7.asset`  
GUID: `aaec7f07e29e7054c9651fa6014127bf`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: AirL7
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: AirL7
  displayName: AirL7
  kind: 0
  context: 1
  windupDuration: 0.02
  activeDuration: 0.15
  recoveryDuration: 0.08
  holdActiveUntilGrounded: 0
  baseDamage: 18
  basePoiseDamage: 9
  movement:
    forwardDistance: 0.2
    startNormalized: 0
    endNormalized: 1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: AirL7
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      launchBackwards: 0
      juggle: 1
    hitboxes:
    - trackName: AirL7_Vai
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.76, y: -0.4, z: -0.6}
      endLocalPosition: {x: -0.58, y: 1.11, z: 3}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.6
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 0.8
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: AirL7_Volta
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.58, y: 1.11, z: 3}
      endLocalPosition: {x: 0.76, y: 0, z: 0.2}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.6
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.8
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 1
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### AirL8

Arquivo: `Assets/GameData/Player/Moveset/Normal/AirL8.asset`  
GUID: `df3b5c96e8b99da4e998203a701c3e53`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: AirL8
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: AirL8
  displayName: AirL8
  kind: 0
  context: 1
  windupDuration: 0.02
  activeDuration: 0.05
  recoveryDuration: 0.08
  cooldownDuration: 0
  holdActiveUntilGrounded: 1
  baseDamage: 30
  basePoiseDamage: 50
  movement:
    forwardDistance: 0
    startNormalized: 0
    endNormalized: 0
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: AirL8
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 1
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      launchBackwards: 0
      juggle: 1
    hitboxes:
    - trackName: AirL8_Dive
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0, y: 0.2, z: 0.8}
      endLocalPosition: {x: 0, y: 0.2, z: 0.8}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 1.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### BurnExplosion

Arquivo: `Assets/GameData/Player/Moveset/Normal/BurnExplosion.asset`  
GUID: `c43e4c5dbbe741f98d30da582a2619c9`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: BurnExplosion
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: BurnExplosion
  displayName: Burn Explosion
  kind: 1
  context: 0
  windupDuration: 0
  activeDuration: 0.2
  recoveryDuration: 0
  baseDamage: 2000
  basePoiseDamage: 270
  movement:
    forwardDistance: 0
    startNormalized: 0
    endNormalized: 1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: Burn Blast
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 1
      startDistance: 2
      endDistance: 8
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 0.35
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 0
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 1
      selfLaunchUp: 0
      launchForwards: 1
      juggle: 0
    hitboxes:
    - trackName: Burn Blast Sphere
      shape: 0
      anchorMode: 0
      trajectoryMode: 1
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0, y: 0, z: 0}
      endLocalPosition: {x: 0, y: 0, z: 0}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 0
      orbitStartDistance: 0
      orbitEndDistance: 0
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 1
      orbitEndRadius: 9
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve: []
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 6
      capsuleHeight: 1
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.25
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.01
  usesExtendedArmHitbox: 0
```

##### D1

Arquivo: `Assets/GameData/Player/Moveset/Normal/D1.asset`  
GUID: `89836aba85f921d4e8a7891a54c750ea`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: D1
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: D1
  displayName: D1
  kind: 0
  context: 2
  windupDuration: 0.06
  activeDuration: 0.15
  recoveryDuration: 0.06
  cooldownDuration: 0
  holdActiveUntilGrounded: 0
  baseDamage: 20
  basePoiseDamage: 50
  movement:
    forwardDistance: 2
    startNormalized: 0
    endNormalized: 1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: D1
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 1
      launchBackwards: 0
      juggle: 0
    hitboxes:
    - trackName: L1_Vai
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.3}
      endLocalPosition: {x: 0, y: 0.36, z: 1.5}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 1.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### Grapple

Arquivo: `Assets/GameData/Player/Moveset/Normal/Grapple.asset`  
GUID: `f985f7c3642c44b4c8f83801acf329a3`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: Grapple
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: Grapple
  displayName: Grapple
  kind: 0
  context: 0
  windupDuration: 0.03
  activeDuration: 0.4
  recoveryDuration: 0
  cooldownDuration: 0.65
  holdActiveUntilGrounded: 0
  baseDamage: 5
  basePoiseDamage: 10
  movement:
    forwardDistance: 0.4
    startNormalized: 0
    endNormalized: 0.1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: Grapple
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      launchBackwards: 0
      juggle: 1
    hitboxes:
    - trackName: Grapple_Vai
      shape: 0
      anchorMode: 0
      trajectoryMode: 2
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0}
      endLocalPosition: {x: 0, y: 0.36, z: 15}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 1
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 0.5
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### H1

Arquivo: `Assets/GameData/Player/Moveset/Normal/H1.asset`  
GUID: `8a9407bd5f8cbaa45b8e5b21f924c8ea`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: H1
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: H1
  displayName: H1
  kind: 1
  context: 0
  windupDuration: 0.5
  activeDuration: 0.03
  recoveryDuration: 0.35
  holdActiveUntilGrounded: 0
  baseDamage: 120
  basePoiseDamage: 80
  movement:
    forwardDistance: 2
    startNormalized: 0
    endNormalized: 1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: H1
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 0
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 1
      juggle: 0
    hitboxes:
    - trackName: Heavy Sphere
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: -0.36, z: 1.15}
      endLocalPosition: {x: 0, y: 0, z: 1.75}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 2
      capsuleHeight: 1
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 1
      sweep: 0
      sweepStepDistance: 0.25
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.1
  usesExtendedArmHitbox: 0
```

##### L1

Arquivo: `Assets/GameData/Player/Moveset/Normal/L1.asset`  
GUID: `54d1fc2271d160849b7c6cfc939fd6ef`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: L1
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: L1
  displayName: L1
  kind: 0
  context: 0
  windupDuration: 0.02
  activeDuration: 0.2
  recoveryDuration: 0.08
  baseDamage: 10
  basePoiseDamage: 20
  movement:
    forwardDistance: 0.7
    startNormalized: 0
    endNormalized: 0.8
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: L1
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      juggle: 0
    hitboxes:
    - trackName: L1_Vai
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 3.5}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 0.5
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L1_Volta
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0, y: 0.36, z: 3.5}
      endLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.5
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 1
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### L1h

Arquivo: `Assets/GameData/Player/Moveset/Normal/L1h.asset`  
GUID: `db7e686f168096f489b9adcbf6f25a88`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: L1h
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: L1h
  displayName: L1h
  kind: 0
  context: 0
  windupDuration: 0.4
  activeDuration: 0.05
  recoveryDuration: 0
  cooldownDuration: 0
  holdActiveUntilGrounded: 0
  baseDamage: 25
  basePoiseDamage: 60
  movement:
    forwardDistance: 0
    startNormalized: 0
    endNormalized: 1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: L1h
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 1
      selfLaunchUp: 0
      launchForwards: 0
      launchBackwards: 0
      juggle: 0
    hitboxes:
    - trackName: L1h
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0, z: 1}
      endLocalPosition: {x: 0, y: 1, z: 1}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 1.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### L2

Arquivo: `Assets/GameData/Player/Moveset/Normal/L2.asset`  
GUID: `b5c584aa6e940a44caa09e31d4925dea`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: L2
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: L2
  displayName: L2
  kind: 0
  context: 0
  windupDuration: 0.02
  activeDuration: 0.2
  recoveryDuration: 0.08
  baseDamage: 10
  basePoiseDamage: 20
  movement:
    forwardDistance: 0.7
    startNormalized: 0
    endNormalized: 0.8
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: L2
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      juggle: 0
    hitboxes:
    - trackName: L2_Vai
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 3.5}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 0.5
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L2_Volta
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0, y: 0.36, z: 3.5}
      endLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.5
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 1
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### L3

Arquivo: `Assets/GameData/Player/Moveset/Normal/L3.asset`  
GUID: `0521bd37a88a5e84a9d37f42db85c475`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: L3
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: L3
  displayName: L3
  kind: 0
  context: 0
  windupDuration: 0.08
  activeDuration: 0.4
  recoveryDuration: 0.07
  cooldownDuration: 0
  holdActiveUntilGrounded: 0
  baseDamage: 7
  basePoiseDamage: 5
  movement:
    forwardDistance: 1.1
    startNormalized: 0
    endNormalized: 0.3
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: L3
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      launchBackwards: 0
      juggle: 0
    hitboxes:
    - trackName: L3_1
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 0.2
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3_2
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.2
      activeEndNormalized: 0.4
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 1
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3_3
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.4
      activeEndNormalized: 0.6
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 2
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3_4
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.6
      activeEndNormalized: 0.8
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 3
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3_5
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.8
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 4
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### L3h

Arquivo: `Assets/GameData/Player/Moveset/Normal/L3h.asset`  
GUID: `1602d7647da607f46ad944461f452088`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: L3h
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: L3h
  displayName: L3h
  kind: 0
  context: 0
  windupDuration: 0.3
  activeDuration: 0.9
  recoveryDuration: 0.1
  cooldownDuration: 0
  holdActiveUntilGrounded: 0
  baseDamage: 3
  basePoiseDamage: 5
  movement:
    forwardDistance: 2.3
    startNormalized: 0.1
    endNormalized: 0.8
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 1
        outSlope: 1
        tangentMode: 34
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 1
        outSlope: 1
        tangentMode: 34
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: L3h
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      launchBackwards: 0
      juggle: 0
    hitboxes:
    - trackName: L3h_1
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 0.06666667
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3h_2
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.06666667
      activeEndNormalized: 0.13333333
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 1
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3h_3
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.13333333
      activeEndNormalized: 0.2
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 2
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3h_4
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.2
      activeEndNormalized: 0.26666668
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 3
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3h_5
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.26666668
      activeEndNormalized: 0.33333334
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 4
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3h_6
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.33333334
      activeEndNormalized: 0.4
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 5
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3h_7
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.4
      activeEndNormalized: 0.46666667
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 6
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3h_8
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.46666667
      activeEndNormalized: 0.5333333
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 7
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3h_9
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.5333333
      activeEndNormalized: 0.6
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 8
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3h_10
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.6
      activeEndNormalized: 0.6666667
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 9
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3h_11
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.6666667
      activeEndNormalized: 0.73333335
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 10
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3h_12
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.73333335
      activeEndNormalized: 0.8
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 11
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3h_13
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.8
      activeEndNormalized: 0.8666667
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 12
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3h_14
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.8666667
      activeEndNormalized: 0.93333334
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 13
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3h_15
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2.25}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.93333334
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 14
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### L3p

Arquivo: `Assets/GameData/Player/Moveset/Normal/L3p.asset`  
GUID: `06ff9d088f5a4af4e90452ef7653392b`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: L3p
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: L3p
  displayName: L3p
  kind: 0
  context: 0
  windupDuration: 0.05
  activeDuration: 0.8
  recoveryDuration: 0.1
  cooldownDuration: 0
  holdActiveUntilGrounded: 0
  baseDamage: 30
  basePoiseDamage: 55
  movement:
    forwardDistance: 2
    startNormalized: 0
    endNormalized: 0.4
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: L3p
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      launchBackwards: 0
      juggle: 0
    hitboxes:
    - trackName: L3p_Gira
      shape: 0
      anchorMode: 0
      trajectoryMode: 1
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 4}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 180
      orbitEndAngleDegrees: 360
      orbitStartDistance: 0.1
      orbitEndDistance: 5
      orbitHeight: 0.36
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 2
          outSlope: 2
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 1
          outSlope: 1
          tangentMode: 34
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 1
          tangentMode: 34
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.6
      orbitEndRadius: 0.6
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 0.5
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.01
    - trackName: L3p_Gira2
      shape: 0
      anchorMode: 0
      trajectoryMode: 1
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 4}
      endLocalPosition: {x: 0, y: 0.36, z: 0.2}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 360
      orbitEndAngleDegrees: 560
      orbitStartDistance: 5
      orbitEndDistance: 0.1
      orbitHeight: 0.36
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 2
          outSlope: 2
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 1
          outSlope: 1
          tangentMode: 34
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 1
          tangentMode: 34
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.6
      orbitEndRadius: 0.6
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.5
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.01
  usesExtendedArmHitbox: 1
```

##### L4

Arquivo: `Assets/GameData/Player/Moveset/Normal/L4.asset`  
GUID: `25871bd56eaedcc4da477ff5382d1d40`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: L4
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: L4
  displayName: L4
  kind: 0
  context: 0
  windupDuration: 0.22
  activeDuration: 0.2
  recoveryDuration: 0.3
  holdActiveUntilGrounded: 0
  baseDamage: 25
  basePoiseDamage: 80
  movement:
    forwardDistance: 3
    startNormalized: 0
    endNormalized: 1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: L4
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 1
      launchBackwards: 0
      juggle: 0
    hitboxes:
    - trackName: L4_Vai_Esquerda
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0.48, y: 0.36, z: 3.5}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.6
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L4_Vai_Direita
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: -0.48, y: 0.36, z: 3.5}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.6
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 1
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### L4p

Arquivo: `Assets/GameData/Player/Moveset/Normal/L4p.asset`  
GUID: `726d7b2a73827a149a94f2a00ac0fe3c`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: L4p
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: L4p
  displayName: L4p
  kind: 0
  context: 0
  windupDuration: 0.15
  activeDuration: 1
  recoveryDuration: 0.35
  cooldownDuration: 0
  holdActiveUntilGrounded: 0
  baseDamage: 50
  basePoiseDamage: 110
  movement:
    forwardDistance: 4
    startNormalized: 0
    endNormalized: 0.45
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: L4p
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 1
      selfLaunchUp: 0
      launchForwards: 1
      launchBackwards: 0
      juggle: 0
    hitboxes:
    - trackName: L4p_Gira
      shape: 0
      anchorMode: 0
      trajectoryMode: 1
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 6}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 180
      orbitEndAngleDegrees: 360
      orbitStartDistance: 0.1
      orbitEndDistance: 7
      orbitHeight: 0.36
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 2
          outSlope: 2
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 1
          outSlope: 1
          tangentMode: 34
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 1
          tangentMode: 34
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.6
      orbitEndRadius: 0.6
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 0.5
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.01
    - trackName: L4p_Gira2
      shape: 0
      anchorMode: 0
      trajectoryMode: 1
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0, y: 0.36, z: 6}
      endLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 360
      orbitEndAngleDegrees: 580
      orbitStartDistance: 7
      orbitEndDistance: 0.1
      orbitHeight: 0.36
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 2
          outSlope: 2
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 1
          outSlope: 1
          tangentMode: 34
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 1
          tangentMode: 34
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.6
      orbitEndRadius: 0.6
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.5
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.5
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.01
  usesExtendedArmHitbox: 1
```

### Ataques Human

| ID | Tipo | Contexto | Windup | Active | Recovery | Cooldown | Dano | Poise | Avanco | Strikes | Tracks | Hitgroups | Asset |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| AirH1 | Pesado | Aereo | 0.5 | 0.03 | 0.35 | 0 | 45 | 20 | 1 | 1 | 1 | 1 | `Assets/GameData/Player/Moveset/Human/AirH1.asset` |
| AirL1 | Leve | Aereo | 0.02 | 0.15 | 0.05 | 0 | 7 | 3 | 0.7 | 1 | 1 | 1 | `Assets/GameData/Player/Moveset/Human/AirL1.asset` |
| AirL2 | Leve | Aereo | 0.02 | 0.15 | 0.05 | 0 | 7 | 3 | 0.7 | 1 | 1 | 1 | `Assets/GameData/Player/Moveset/Human/AirL2.asset` |
| AirL3 | Leve | Aereo | 0.08 | 0.2 | 0.08 | 0 | 20 | 3 | 0 | 1 | 1 | 1 | `Assets/GameData/Player/Moveset/Human/AirL8.asset` |
| H1 | Pesado | Chao | 0.5 | 0.03 | 0.35 | 0 | 45 | 20 | 2 | 1 | 1 | 1 | `Assets/GameData/Player/Moveset/Human/H1.asset` |
| L1 | Leve | Chao | 0.02 | 0.2 | 0.08 | 0 | 10 | 5 | 0.7 | 1 | 1 | 1 | `Assets/GameData/Player/Moveset/Human/L1.asset` |
| L2 | Leve | Chao | 0.02 | 0.2 | 0.08 | 0 | 10 | 5 | 0.7 | 1 | 1 | 1 | `Assets/GameData/Player/Moveset/Human/L2.asset` |
| L3 | Leve | Chao | 0.08 | 0.4 | 0.1 | 0 | 7 | 3 | 1.1 | 1 | 3 | 3 | `Assets/GameData/Player/Moveset/Human/L3.asset` |
| L3p | Leve | Chao | 0.05 | 0.8 | 0.1 | 0 | 20 | 12 | 2 | 1 | 2 | 1 | `Assets/GameData/Player/Moveset/Human/L3p.asset` |
| L4 | Leve | Chao | 0.22 | 0.2 | 0.3 | 0 | 25 | 15 | 1.05 | 1 | 1 | 1 | `Assets/GameData/Player/Moveset/Human/L4.asset` |
| L4p | Leve | Chao | 0.15 | 1 | 0.35 | 0 | 35 | 24 | 4 | 1 | 2 | 1 | `Assets/GameData/Player/Moveset/Human/L4p.asset` |

#### Serializacao integral dos assets

##### AirH1

Arquivo: `Assets/GameData/Player/Moveset/Human/AirH1.asset`  
GUID: `b6b8243c8fe6d6047b1498a87ac990f3`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: AirH1
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: AirH1
  displayName: AirH1
  kind: 1
  context: 1
  windupDuration: 0.5
  activeDuration: 0.03
  recoveryDuration: 0.35
  holdActiveUntilGrounded: 1
  baseDamage: 45
  basePoiseDamage: 20
  movement:
    forwardDistance: 1
    startNormalized: 0
    endNormalized: 1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: AirH1
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 0
    effects:
      launchDown: 1
      selfLaunchDown: 1
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      juggle: 0
    hitboxes:
    - trackName: Heavy Sphere
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.4, z: 0.7}
      endLocalPosition: {x: 0, y: -0.8, z: 0.7}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 1.2
      capsuleHeight: 1
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 1
      sweep: 0
      sweepStepDistance: 0.25
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.1
  usesExtendedArmHitbox: 0
```

##### AirL1

Arquivo: `Assets/GameData/Player/Moveset/Human/AirL1.asset`  
GUID: `c10c703d59c260d4d8d92d1060e29a1f`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: AirL1
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: AirL1
  displayName: AirL1
  kind: 0
  context: 1
  windupDuration: 0.02
  activeDuration: 0.15
  recoveryDuration: 0.05
  holdActiveUntilGrounded: 0
  baseDamage: 7
  basePoiseDamage: 3
  movement:
    forwardDistance: 0.7
    startNormalized: 0
    endNormalized: 0.8
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: AirL1
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      juggle: 1
    hitboxes:
    - trackName: AirL1
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 1.8}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.3
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### AirL2

Arquivo: `Assets/GameData/Player/Moveset/Human/AirL2.asset`  
GUID: `94f11a5cf075c8d4a9b656ad5fc3abc9`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: AirL2
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: AirL2
  displayName: AirL2
  kind: 0
  context: 1
  windupDuration: 0.02
  activeDuration: 0.15
  recoveryDuration: 0.05
  holdActiveUntilGrounded: 0
  baseDamage: 7
  basePoiseDamage: 3
  movement:
    forwardDistance: 0.7
    startNormalized: 0
    endNormalized: 0.8
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: AirL2
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      juggle: 1
    hitboxes:
    - trackName: AirL2
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 1.8}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.3
      orbitEndRadius: 0.3
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.3
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### AirL3

Arquivo: `Assets/GameData/Player/Moveset/Human/AirL8.asset`  
GUID: `4225092576fa2664bbf57c74754189ba`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: AirL8
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: AirL3
  displayName: AirL3
  kind: 0
  context: 1
  windupDuration: 0.08
  activeDuration: 0.2
  recoveryDuration: 0.08
  holdActiveUntilGrounded: 1
  baseDamage: 20
  basePoiseDamage: 3
  movement:
    forwardDistance: 0
    startNormalized: 0
    endNormalized: 0
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: AirL8
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 1
    effects:
      launchDown: 1
      selfLaunchDown: 1
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      juggle: 0
    hitboxes:
    - trackName: AirL8_Dive
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 1.3, z: 0.8}
      endLocalPosition: {x: 0.36, y: -0.8, z: 0.8}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.3
      orbitEndRadius: 0.3
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 1
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### H1

Arquivo: `Assets/GameData/Player/Moveset/Human/H1.asset`  
GUID: `95941554b61b46d45aa574e36b69c975`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: H1
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: H1
  displayName: H1
  kind: 1
  context: 0
  windupDuration: 0.5
  activeDuration: 0.03
  recoveryDuration: 0.35
  holdActiveUntilGrounded: 0
  baseDamage: 45
  basePoiseDamage: 20
  movement:
    forwardDistance: 2
    startNormalized: 0
    endNormalized: 1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: H1
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 0
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      juggle: 0
    hitboxes:
    - trackName: Heavy Sphere
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: -0.36, z: 0.7}
      endLocalPosition: {x: 0, y: 0, z: 1.2}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 1.2
      capsuleHeight: 1
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 1
      sweep: 0
      sweepStepDistance: 0.25
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.1
  usesExtendedArmHitbox: 0
```

##### L1

Arquivo: `Assets/GameData/Player/Moveset/Human/L1.asset`  
GUID: `895f992d1dbb8fa43bf23a28ef8c3c13`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: L1
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: L1
  displayName: L1
  kind: 0
  context: 0
  windupDuration: 0.02
  activeDuration: 0.2
  recoveryDuration: 0.08
  holdActiveUntilGrounded: 0
  baseDamage: 10
  basePoiseDamage: 5
  movement:
    forwardDistance: 0.7
    startNormalized: 0
    endNormalized: 0.8
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: L1
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 0
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      juggle: 0
    hitboxes:
    - trackName: L1
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 1.8}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.3
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### L2

Arquivo: `Assets/GameData/Player/Moveset/Human/L2.asset`  
GUID: `118d9b41db4de3246a77cbb35ac6f33f`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: L2
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: L2
  displayName: L2
  kind: 0
  context: 0
  windupDuration: 0.02
  activeDuration: 0.2
  recoveryDuration: 0.08
  holdActiveUntilGrounded: 0
  baseDamage: 10
  basePoiseDamage: 5
  movement:
    forwardDistance: 0.7
    startNormalized: 0
    endNormalized: 0.8
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: L2
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 0
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      juggle: 0
    hitboxes:
    - trackName: L2
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 1.8}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.3
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### L3

Arquivo: `Assets/GameData/Player/Moveset/Human/L3.asset`  
GUID: `31c2010bba316b141823847018791753`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: L3
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: L3
  displayName: L3
  kind: 0
  context: 0
  windupDuration: 0.08
  activeDuration: 0.4
  recoveryDuration: 0.1
  holdActiveUntilGrounded: 0
  baseDamage: 7
  basePoiseDamage: 3
  movement:
    forwardDistance: 1.1
    startNormalized: 0
    endNormalized: 0.3
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: L3
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 0
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      juggle: 0
    hitboxes:
    - trackName: L3_1
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 1.8}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.3
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 0.3333
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3_2
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 1.8}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.3
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.3333
      activeEndNormalized: 0.6666
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 1
      allowRepeatHits: 0
      repeatHitInterval: 0.05
    - trackName: L3_3
      shape: 0
      anchorMode: 0
      trajectoryMode: 0
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 1.8}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 0
      orbitEndAngleDegrees: 360
      orbitStartDistance: 1
      orbitEndDistance: 1
      orbitHeight: 0
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.5
      orbitEndRadius: 0.5
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.3
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.6666
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 2
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### L3p

Arquivo: `Assets/GameData/Player/Moveset/Human/L3p.asset`  
GUID: `ebcae8a04f5bb0844bae7fe97c6d897c`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: L3p
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: L3p
  displayName: L3p
  kind: 0
  context: 0
  windupDuration: 0.05
  activeDuration: 0.8
  recoveryDuration: 0.1
  holdActiveUntilGrounded: 0
  baseDamage: 20
  basePoiseDamage: 12
  movement:
    forwardDistance: 2
    startNormalized: 0
    endNormalized: 1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: L3p
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 0
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      juggle: 0
    hitboxes:
    - trackName: L3p_Gira
      shape: 0
      anchorMode: 0
      trajectoryMode: 1
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 2}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 180
      orbitEndAngleDegrees: 360
      orbitStartDistance: 0.1
      orbitEndDistance: 2
      orbitHeight: 0.36
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 2
          outSlope: 2
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 1
          outSlope: 1
          tangentMode: 34
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 1
          tangentMode: 34
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.3
      orbitEndRadius: 0.3
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.3
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 0.5
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.01
    - trackName: L3p_Gira2
      shape: 0
      anchorMode: 0
      trajectoryMode: 1
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0.36, y: 0.36, z: 4}
      endLocalPosition: {x: 0, y: 0.36, z: 0.2}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 360
      orbitEndAngleDegrees: 560
      orbitStartDistance: 2
      orbitEndDistance: 0.1
      orbitHeight: 0.36
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 2
          outSlope: 2
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 1
          outSlope: 1
          tangentMode: 34
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 1
          tangentMode: 34
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.3
      orbitEndRadius: 0.3
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.3
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.5
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.01
  usesExtendedArmHitbox: 1
```

##### L4

Arquivo: `Assets/GameData/Player/Moveset/Human/L4.asset`  
GUID: `f880ab7b17f2eea459267ed79dc025cd`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: L4
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: L4
  displayName: L4
  kind: 0
  context: 0
  windupDuration: 0.22
  activeDuration: 0.2
  recoveryDuration: 0.3
  holdActiveUntilGrounded: 0
  baseDamage: 25
  basePoiseDamage: 15
  movement:
    forwardDistance: 1.05
    startNormalized: 0
    endNormalized: 0.85
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: L4
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 0
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      juggle: 0
    hitboxes:
    - trackName: L4
      shape: 0
      anchorMode: 0
      trajectoryMode: 1
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 1.8}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: -150
      orbitEndAngleDegrees: 50
      orbitStartDistance: 0.2
      orbitEndDistance: 1.8
      orbitHeight: 0.36
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.3
      orbitEndRadius: 0.3
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.3
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.05
  usesExtendedArmHitbox: 1
```

##### L4p

Arquivo: `Assets/GameData/Player/Moveset/Human/L4p.asset`  
GUID: `abbe91d9563134b419fb59634d46e1ed`

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5b98f1fcdf334852af096c0b0bb97bcb, type: 3}
  m_Name: L4p
  m_EditorClassIdentifier: Assembly-CSharp::PlayerAttackDefinition
  attackId: L4p
  displayName: L4p
  kind: 0
  context: 0
  windupDuration: 0.15
  activeDuration: 1
  recoveryDuration: 0.35
  holdActiveUntilGrounded: 0
  baseDamage: 35
  basePoiseDamage: 24
  movement:
    forwardDistance: 4
    startNormalized: 0
    endNormalized: 1
    movementCurve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 0
        value: 0
        inSlope: 0
        outSlope: 0
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      - serializedVersion: 3
        time: 1
        value: 1
        inSlope: 2
        outSlope: 2
        tangentMode: 0
        weightedMode: 0
        inWeight: 0
        outWeight: 0
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
  strikes:
  - strikeName: L4p
    activeStartNormalized: 0
    activeEndNormalized: 1
    damageMultiplier: 1
    poiseMultiplier: 1
    damageFalloff:
      enabled: 0
      startDistance: 0
      endDistance: 1
      damageMultiplier:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
    generatesHeatOnHit: 0
    effects:
      launchDown: 0
      selfLaunchDown: 0
      launchUp: 0
      selfLaunchUp: 0
      launchForwards: 0
      juggle: 0
    hitboxes:
    - trackName: L4p_Gira
      shape: 0
      anchorMode: 0
      trajectoryMode: 1
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      endLocalPosition: {x: 0, y: 0.36, z: 3}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 180
      orbitEndAngleDegrees: 360
      orbitStartDistance: 0.1
      orbitEndDistance: 2.6
      orbitHeight: 0.36
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 2
          outSlope: 2
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 1
          outSlope: 1
          tangentMode: 34
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 1
          tangentMode: 34
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.3
      orbitEndRadius: 0.3
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.3
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0
      activeEndNormalized: 0.5
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.01
    - trackName: L4p_Gira2
      shape: 0
      anchorMode: 0
      trajectoryMode: 1
      primaryAnchorId: 
      secondaryAnchorId: 
      startLocalPosition: {x: 0, y: 0.36, z: 3}
      endLocalPosition: {x: -0.36, y: 0.36, z: 0.2}
      startLocalEuler: {x: 0, y: 0, z: 0}
      endLocalEuler: {x: 0, y: 0, z: 0}
      orbitStartAngleDegrees: 360
      orbitEndAngleDegrees: 580
      orbitStartDistance: 2.6
      orbitEndDistance: 0.1
      orbitHeight: 0.36
      orbitAngleCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 2
          outSlope: 2
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 0
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitDistanceCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 1
          outSlope: 1
          tangentMode: 34
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 1
          tangentMode: 34
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      orbitStartRadius: 0.3
      orbitEndRadius: 0.3
      orbitRadiusCurve:
        serializedVersion: 2
        m_Curve:
        - serializedVersion: 3
          time: 0
          value: 0
          inSlope: 0
          outSlope: 1
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        - serializedVersion: 3
          time: 1
          value: 1
          inSlope: 1
          outSlope: 0
          tangentMode: 0
          weightedMode: 0
          inWeight: 0
          outWeight: 0
        m_PreInfinity: 2
        m_PostInfinity: 2
        m_RotationOrder: 4
      radius: 0.3
      capsuleHeight: 3.12
      capsuleAxis: 2
      boxSize: {x: 1, y: 1, z: 1}
      activeStartNormalized: 0.5
      activeEndNormalized: 1
      sweep: 1
      sweepStepDistance: 0.18
      hitGroup: 0
      allowRepeatHits: 0
      repeatHitInterval: 0.01
  usesExtendedArmHitbox: 1
```

## Contexto simples

- **Entries** escolhem o primeiro ataque conforme o botao e o contexto do jogador (chao, ar ou dash).
- **Nodes** definem os ataques disponiveis e para onde cada cadeia pode continuar. Uma transicao `hold do mesmo clique` exige manter pressionado o clique que iniciou o ataque atual pelo tempo configurado no moveset.
- Cada ataque define windup, active, recovery, cooldown, dano, Poise, movimento e um ou mais **Strikes**. As **Hitbox Tracks** determinam forma, posicao, janela ativa, hitgroup e curvas de movimento.
- Tracks no mesmo hitgroup compartilham o limite de um acerto por inimigo. Hitgroups distintos podem acertar o mesmo inimigo novamente; por isso varios hitgroups podem aumentar o dano/Poise total de um ataque.
- Campos ausentes do YAML serializado usam os valores padrao declarados nas classes C# do projeto. `fileID: 0` indica node sem asset de ataque associado.
- Este export cobre os assets de moveset e ataque nessas pastas. Valores globais dos componentes do Player (por exemplo, movimento, pulo, grapple pull e assistencia de alvo) continuam configurados nos respectivos componentes e nao sao parte dos ScriptableObjects de moveset.
