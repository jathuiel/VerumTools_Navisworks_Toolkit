---
description: "Fase 3 (Implementação) — executa o spec.md arquivo por arquivo"
argument-hint: <NNN ou slug da feature>
---

Você está na **Fase 3 (Implementação)** do fluxo spec-driven deste projeto. O plano já foi pesquisado (prd) e especificado (spec); seu papel agora é **executar o `spec.md` à risca**, não redescobrir nem replanejar.

Feature: $ARGUMENTS

## Passos

1. **Localizar e ler o spec.** Encontre `specs/*$ARGUMENTS*/spec.md` (número `NNN` ou parte do slug; se ambíguo, liste e pergunte). Leia o `spec.md` inteiro antes de tocar em qualquer arquivo. Não releia o `prd.md` — se o spec não bastar, ele é que está incompleto (ver regra de divergência abaixo).

2. **Implementar na ordem do §4**, arquivo por arquivo, seguindo a especificação isolada de cada um (§3). Antes de alterar um arquivo existente, leia-o e ancore a mudança no trecho citado pelo spec.

3. **Buildar e reportar:**
   ```powershell
   dotnet build "Construct Sync Toolkit.csproj" -c Release
   ```
   Lembrete do projeto: o build já instala na pasta de Plugins do Navisworks; se falhar ao copiar a DLL, o Navisworks provavelmente está aberto — reporte isso ao usuário em vez de tentar contornar.

4. **Fechar o ciclo:** marque os checkboxes do §4 e §5 do `spec.md` conforme concluídos, atualize o status para `implementado` e informe o que resta de validação manual no Navisworks (§5).

## Regras invioláveis

- **Só toque nos arquivos listados na tabela §2 do spec.** Nenhuma refatoração oportunista, nenhuma "melhoria de passagem" em código validado — os guard rails do §6 têm precedência sobre qualquer instinto de limpeza.
- **Divergência volta para o spec, não vira improviso.** Se o spec se mostrar inviável, incompleto ou em conflito com o código real (assinatura diferente, âncora inexistente), PARE essa parte, descreva a divergência ao usuário e proponha a correção do `spec.md`. Só prossiga após o ajuste — silenciosamente "dar um jeito" é exatamente o que este fluxo existe para impedir.
- Respeite as convenções do projeto (CLAUDE.md): pt-BR, tokens do DesignSystem, reflexão defensiva intencional nos services.
- Não faça commit a menos que o usuário peça.

## Ao finalizar

Reporte: arquivos criados/alterados, resultado do build (com output em caso de erro), checklist do spec atualizado e o roteiro de validação manual pendente no Navisworks.
