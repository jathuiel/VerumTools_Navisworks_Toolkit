---
description: "Fase 1 (Pesquisa) — mapeia a base de código e gera o prd.md da feature"
argument-hint: <descrição da feature ou mudança>
---

Você está na **Fase 1 (Pesquisa)** do fluxo spec-driven deste projeto (leia `specs/README.md` se precisar do contexto do fluxo). Sua única entrega é um `prd.md` — repositório bruto de informações. **Nesta fase é proibido escrever código novo, propor a solução em detalhe ou editar qualquer arquivo do projeto fora de `specs/`.**

Feature solicitada: $ARGUMENTS

Se a descrição acima estiver vazia ou vaga demais para pesquisar, pergunte ao usuário antes de prosseguir.

## Passos

1. **Criar a pasta da feature.** Liste `specs/` e determine o próximo número sequencial `NNN` (3 dígitos, começando em `001`). Derive um slug kebab-case curto da descrição. A pasta é `specs/NNN-slug/`.

2. **Mapear a base de código.** Use Grep/Glob/Read (chamadas paralelas) para identificar:
   - Todos os arquivos que a mudança deve tocar e os que ela consome (entram na tabela do §3 do template);
   - Padrões, helpers e services existentes que devem ser **reaproveitados** (§4) — cite com `arquivo:linha` e cole a assinatura ou trecho exato, pois a fase de spec trabalhará apenas com o prd;
   - Restrições aplicáveis do `CLAUDE.md` (cadeia de registro de plugin em 3 arquivos, `ToolLauncher<T>`, MVVM para UI nova, `VerumSchema`, reflexão defensiva intencional) e, se houver UI, do `Themes/THEME_MAP.md` (§6);
   - Lógicas validadas que ficam **fora de escopo / não tocar** (§7).

3. **Documentação externa (se a feature depender de API).** Consulte as fontes disponíveis (MCP Autodesk Product Help para Navisworks, Context7, WebSearch) e **cole os recortes relevantes** no §5 com a fonte — o spec será escrito sem nova pesquisa. Se o usuário forneceu links/docs no prompt, processe-os. Se algo ficar sem confirmação, registre no §8 (perguntas em aberto).

4. **Escrever o `prd.md`.** Use a estrutura de `specs/_templates/prd.template.md` (copie as seções, substitua os placeholders). Grave em `specs/NNN-slug/prd.md` com status `pronto para spec` (ou `pesquisa`, se houver perguntas em aberto bloqueantes).

## Regras

- O prd registra **o que existe e o que se quer**, nunca **como implementar**. Sem trechos de código novo; código citado é sempre código existente do repositório ou recorte de documentação.
- Prefira colar trechos exatos a parafrasear — o consumidor do prd não terá o repositório "fresco" no contexto.
- Tudo em pt-BR, como o restante do projeto.

## Ao finalizar

Informe ao usuário: o caminho do `prd.md` gerado, um resumo de 3–5 linhas do que foi mapeado, as perguntas em aberto (se houver) e o próximo passo do fluxo:

```
/clear
/spec NNN
```
