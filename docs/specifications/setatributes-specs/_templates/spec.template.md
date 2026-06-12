# SPEC — <Nome da feature>

> Fase 2 (Especificação). Plano de ação técnico gerado a partir do `prd.md`. É o prompt
> definitivo da implementação: a fase 3 não deve precisar de nenhuma outra fonte.

- **Baseado em:** `specs/NNN-slug/prd.md`
- **Data:** AAAA-MM-DD
- **Status:** `especificado` | `implementado`

## 1. Resumo técnico

<A solução em um parágrafo: abordagem escolhida e como as peças se conectam.>

## 2. Arquivos afetados

<Lista EXATA e completa. Nenhum arquivo fora desta tabela pode ser modificado na implementação.>

| # | Arquivo | Ação | Resumo da mudança |
|---|---|---|---|
| 1 | `Caminho/Arquivo.cs` | criar / alterar | <uma linha> |

## 3. Especificação por arquivo

<Uma seção por arquivo da tabela, isolada e auto-suficiente, na ordem de implementação.>

### 3.1 `Caminho/Arquivo.cs` — <ação>

**Objetivo:** <o que este arquivo passa a fazer.>

**Depende de:** <arquivos/padrões existentes que ele consome (do prd §4), ou "nada".>

**Lógica:**

1. <passo>
2. <passo>

**Código / contratos:**

```csharp
// Assinaturas, trechos novos ou diff conceitual (o que entra, o que sai, onde ancora).
// Para alterações: citar o trecho existente que serve de âncora.
```

## 4. Ordem de implementação

- [ ] 1. `Arquivo A` — <dependência satisfeita primeiro>
- [ ] 2. `Arquivo B`
- [ ] 3. Build: `dotnet build "Construct Sync Toolkit.csproj" -c Release`

## 5. Critérios de aceite

<Como saber que está pronto. Build limpo é obrigatório; o restante é roteiro de validação
manual no Navisworks (plugin não tem testes automatizados).>

- [ ] Build Release sem erros nem warnings novos
- [ ] No Navisworks: <passo de verificação manual — abrir janela X, executar Y, conferir Z>

## 6. Guard rails — o que NÃO fazer

<Transcrever/derivar do prd §7 e adicionar riscos identificados na especificação.
A implementação que esbarrar nestes limites deve PARAR e reportar, não improvisar.>

- Não refatorar: <lógica validada>
- Não tocar em arquivos fora da tabela da seção 2.
- <outros>
