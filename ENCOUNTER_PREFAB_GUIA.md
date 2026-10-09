# Guia — Prefab de Enemy Encounter

## Hierarquia recomendada

```text
EnemyEncounter_Template
├── ActivationTrigger
├── Barriers
│   ├── EntranceBarrier
│   │   └── BarrierVisual
│   └── ExitBarrier
│       └── BarrierVisual
├── Wave_01
│   ├── Enemy_01
│   └── Enemy_02
├── Wave_02
│   ├── Enemy_03
│   └── Enemy_04
└── Wave_03
    └── Enemy_05
```

## Configuracao

1. No objeto raiz, adicionar `EnemyEncounter`.
2. Em `Waves`, criar uma entrada para cada onda.
3. Em cada onda, preencher `Enemies`, `Spawn Mode` e `Spawn Delay`.
4. Em `Barriers`, referenciar todos os `EncounterBarrier` da arena.
5. Em `ActivationTrigger`, adicionar um Collider marcado como `Is Trigger` e o componente `EncounterTrigger`.
6. No campo `Encounter` do trigger, referenciar o `EnemyEncounter` do objeto raiz.
7. Arrastar o objeto raiz para uma pasta de Prefabs. Criar variantes para cada encontro da fase.

## Regras

- Um inimigo deve pertencer a somente uma onda do mesmo encounter.
- Todos os inimigos configurados ficam inativos antes do trigger.
- Ondas vazias sao ignoradas.
- `Sequential`: esta onda so agenda depois que todos os inimigos das ondas iniciadas forem derrotados; entao respeita seu `Spawn Delay`.
- `Overlap`: o delay desta onda comeca assim que a onda anterior nasce, permitindo sobreposicao de inimigos.
- `Sustain`: nasce ate `Max Alive` inimigos de sua lista e repoe os slots perdidos apos seu `Spawn Delay`, ate toda a lista ser invocada e derrotada.
- Uma onda `Sustain` deve terminar antes da proxima onda ser iniciada.
