---
description: "Fase 2 (Especificação) — consome o prd.md e gera o spec.md (plano de ação técnico)"
argument-hint: <NNN ou slug da feature>
---

Você está na **Fase 2 (Especificação)** do fluxo spec-driven deste projeto. Sua única entrega é o `spec.md` — o plano de ação técnico definitivo que a fase de implementação executará sem nenhuma outra fonte. **Nesta fase é proibido implementar qualquer coisa ou editar arquivos do projeto fora de `specs/`.**

Feature: $ARGUMENTS

## Passos

1. **Localizar e ler o PRD.** Encontre `specs/*$ARGUMENTS*/prd.md` (o argumento pode ser o número `NNN` ou parte do slug; se ambíguo, liste as opções e pergunte; se o status do prd indicar perguntas em aberto bloqueantes, resolva-as com o usuário antes de especificar). Leia o `prd.md` inteiro.

2. **Disciplina de contexto.** O prd é sua fonte primária. Você pode abrir **somente os arquivos citados no prd**, e apenas para confirmar assinaturas, contratos e pontos de ancoragem exatos das alterações. Não explore o repositório além disso — se o prd estiver incompleto a ponto de exigir nova pesquisa, pare e reporte que ele precisa voltar à Fase 1.

3. **Escrever o `spec.md`** na mesma pasta do prd, usando a estrutura de `specs/_templates/spec.template.md`:
   - **§2 Arquivos afetados:** lista EXATA — todo arquivo criado/alterado aparece aqui; nada fora dela poderá ser tocado na implementação;
   - **§3 Especificação por arquivo:** uma seção isolada e auto-suficiente por arquivo, com lógica passo a passo e trechos de código/assinaturas; para alterações, cite o trecho existente que serve de âncora;
   - **§4 Ordem de implementação:** checklist em ordem de dependência, terminando no build;
   - **§5 Critérios de aceite:** build Release limpo + roteiro de validação manual no Navisworks;
   - **§6 Guard rails:** transcreva o "Não tocar" do prd (§7) e acrescente riscos que você identificou.

## Regras

- **Reaproveite os padrões catalogados no prd (§4)** — o spec não pode reinventar helper que já existe nem refatorar lógica validada de carona.
- Respeite as restrições do prd §6 (registro de plugin em 3 arquivos, `ToolLauncher<T>`, MVVM, `VerumSchema`, DesignSystem/THEME_MAP, pt-BR).
- Cada seção do §3 deve ser executável isoladamente: quem implementa lê o spec de cima a baixo, sem precisar deduzir nada.
- Marque o status do spec como `especificado` e atualize o status do prd se necessário.

## Ao finalizar

Informe ao usuário: o caminho do `spec.md`, a tabela de arquivos afetados (resumida), e o próximo passo do fluxo:

```
/clear
/implement NNN
```
