# Ações pendentes do usuário

Atualizado em 03/11/2026.

## Teste em cena

O projeto proíbe o agente de modificar arquivos `.unity`. A cena `Assets/Scenes/999. TestArena.unity` não declara um `AudioListener`; a tentativa anterior ficou presa repetindo avisos de áudio, sem emitir resultado final. Uma nova tentativa saiu com código 1 sem iniciar o teste, enquanto já havia processos do Unity Editor abertos. Nenhuma cena foi salva ou alterada.

No Unity `6000.6.4f1`:

1. Abra `Assets/Scenes/999. TestArena.unity` e verifique a câmera ativa de gameplay.
2. Garanta que ela tenha um `AudioListener` ativo e que não exista outro `AudioListener` ativo na cena. Salve a cena.
3. Execute **Tools > Tests > Run Self Launch Duration Regression**.
4. Confirme no Console um resultado `PASS` para as durações testadas e para o pulo duplo. Se falhar, envie o erro completo do Console.

## Escopo de código ainda aberto

- A revisão identifica uma refatoração grande de `PlayerMovement` para fazer um único `CharacterController.Move` por frame. Ela afeta movimento de ataque, separação, grapple, launch e snap de chão; deixei-a registrada como trabalho futuro que requer uma etapa dedicada de implementação e validação.
- Os testes existentes são ações de menu do Editor, não testes NUnit/CI. A criação de uma suíte automatizada permanece como melhoria futura.

Os demais itens executáveis foram corrigidos ou registrados em `AGENTS.md`; a lista técnica completa e o estado atual estão em `REVISAO_PROJETO.md`.
