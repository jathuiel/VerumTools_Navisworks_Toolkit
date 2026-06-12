# SPEC — Remover a funcionalidade Timeline de Projeto (DocumentTimeliner)

> Fase 2 (Especificação). Plano de ação técnico gerado a partir do `prd.md`. É o prompt
> definitivo da implementação: a fase 3 não deve precisar de nenhuma outra fonte.

- **Baseado em:** `specs/001-remover-timeline/prd.md`
- **Data:** 2026-06-11
- **Status:** `implementado` (pendente apenas a validação manual no Navisworks — §5)

## 1. Resumo técnico

Remoção completa de uma das 4 ferramentas do plugin. Cinco arquivos de uso exclusivo do Timeliner são excluídos; a cadeia de registro (entry point C#, manifesto `.addin`, ribbon XAML) perde a ponta do `DocumentTimeliner` nas 3 pontas de forma consistente; README.md e CLAUDE.md passam a descrever 3 ferramentas e 2 painéis. O csproj é SDK-style (globbing automático) e não muda. Não há código novo.

## 2. Arquivos afetados

| # | Arquivo | Ação | Resumo da mudança |
|---|---|---|---|
| 1 | `Views/DocumentTimelinerWindow.xaml` | excluir | Janela WPF do Timeliner |
| 2 | `Views/DocumentTimelinerWindow.xaml.cs` | excluir | Code-behind da janela |
| 3 | `ViewModels/DocumentTimelinerViewModel.cs` | excluir | ViewModel exclusivo |
| 4 | `Services/TimelinerService.cs` | excluir | Service exclusivo (reflexão sobre Timeliner) |
| 5 | `Models/TimelinerTaskData.cs` | excluir | Model exclusivo |
| 6 | `Plugin/Construct Sync Toolkit.cs` | alterar | Remover classe `DocumentTimelinerPlugin` |
| 7 | `SetAtributesToolkit.addin` | alterar | Remover bloco `<Plugin>` do DocumentTimeliner |
| 8 | `SetAtributesToolkit.xaml` | alterar | Remover `RibbonPanel "Timeline"` |
| 9 | `README.md` | alterar | Remover seção, linha da tabela Ribbon, 3 entradas da árvore, ajustar contagens |
| 10 | `CLAUDE.md` | alterar | Ajustar contagem de ferramentas e remover menções a Timeliner |

## 3. Especificação por arquivo

### 3.1 Arquivos 1–5 — excluir

**Objetivo:** eliminar todo o código de uso exclusivo do Timeliner.

**Lógica:** deletar os 5 arquivos do disco. Confirmado no prd (§4) que `TimelinerService`, `DocumentTimelinerViewModel` e `TimelinerTaskData` não têm consumidores fora deles. Artefatos `obj/*/Views/DocumentTimelinerWindow.g.cs` são regenerados pelo build — não tocar.

### 3.2 `Plugin/Construct Sync Toolkit.cs` — alterar

**Objetivo:** remover o entry point `DocumentTimelinerPlugin`, mantendo as 3 classes restantes.

**Código / contratos** — remover o bloco abaixo (a classe e a linha em branco que a separa da `SetColoringRulesPlugin`), preservando o fechamento do namespace:

```csharp
    [Plugin("SetAtributesToolkit.DocumentTimeliner", "JCA", DisplayName = "Timeline de Projeto")]
    public class DocumentTimelinerPlugin : AddInPlugin
    {
        public override int Execute(params string[] parameters)
        {
            ToolLauncher<DocumentTimelinerWindow>.Launch();
            return 0;
        }
    }
```

### 3.3 `SetAtributesToolkit.addin` — alterar

**Objetivo:** desregistrar o plugin no manifesto.

**Código / contratos** — remover o bloco (e a linha em branco adjacente), preservando `</Plugins>`:

```xml
  <Plugin>
    <Id>SetAtributesToolkit.DocumentTimeliner</Id>
    <FileName>SetAtributesToolkit</FileName>
    <ClassName>SetAtributesToolkit.DocumentTimelinerPlugin</ClassName>
    <DisplayName>Timeline de Projeto</DisplayName>
    <ToolTip>Gerencia tarefas da timeline do projeto</ToolTip>
  </Plugin>
```

### 3.4 `SetAtributesToolkit.xaml` — alterar

**Objetivo:** remover o painel "Timeline" do ribbon, deixando 2 painéis.

**Código / contratos** — remover o bloco inteiro (e a linha em branco adjacente), preservando `</RibbonTab>`:

```xml
    <RibbonPanel PanelName="Timeline">
      <Button DisplayName="Timeline de Projeto"
              Id="SetAtributesToolkit.DocumentTimeliner"
              ToolTip="Gerencia tarefas da timeline do projeto"
              IsLarge="True"
              Icon="Resources/ler_atributo_icon.png"
              LargeIcon="Resources/ler_atributo_icon.png" />
    </RibbonPanel>
```

### 3.5 `README.md` — alterar

**Objetivo:** documentação reflete o plugin com 3 ferramentas e 2 painéis.

**Lógica (5 edições pontuais):**

1. Remover a seção `### Timeline de Projeto` inteira (título, descrição "Gerencia tarefas do Timeliner do Navisworks." e os 4 bullets), junto com um dos separadores `---` adjacentes — restando um único `---` entre a seção de Coloração e `## Ribbon`.
2. Na tabela `## Ribbon`, remover a linha `| **Timeline** | Timeline de Projeto |`.
3. Na árvore de estrutura: trocar o comentário `# 4 classes AddInPlugin (entry points)` por `# 3 classes AddInPlugin (entry points)`.
4. Na árvore: remover `│   └── DocumentTimelinerWindow.xaml(.cs)` (promovendo `SetColoringRulesWindow.xaml(.cs)` a `└──`), remover `│   └── DocumentTimelinerViewModel.cs` (promovendo `SetColoringRulesViewModel.cs` a `└──`) e remover `│   └── TimelinerTaskData.cs` (promovendo `SelectionSetItem.cs` a `└──`).
5. No passo 4 da instalação: `com os 3 painéis` → `com os 2 painéis`.

### 3.6 `CLAUDE.md` — alterar

**Objetivo:** orientação das futuras instâncias sem a ferramenta removida.

**Lógica (3 edições pontuais):**

1. Linha de abertura: `com 4 ferramentas ... Regras de Coloração de Sets e Timeline de Projeto.` → `com 3 ferramentas ... e Regras de Coloração de Sets.`
2. Seção "Duas gerações de UI": `\`SetColoringRulesWindow\` e \`DocumentTimelinerWindow\` usam ViewModel` → `\`SetColoringRulesWindow\` usa ViewModel`.
3. Seção "Acesso defensivo": remover o bullet `- \`TimelinerService\` acessa tarefas do Timeliner inteiramente por reflexão (\`AddNew\`/\`Add\`, propriedades por nome).`

## 4. Ordem de implementação

- [x] 1. Excluir os 5 arquivos (`Views/DocumentTimelinerWindow.xaml{,.cs}`, `ViewModels/DocumentTimelinerViewModel.cs`, `Services/TimelinerService.cs`, `Models/TimelinerTaskData.cs`)
- [x] 2. `Plugin/Construct Sync Toolkit.cs` — remover classe
- [x] 3. `SetAtributesToolkit.addin` — remover bloco
- [x] 4. `SetAtributesToolkit.xaml` — remover painel
- [x] 5. `README.md` — 5 edições
- [x] 6. `CLAUDE.md` — 3 edições
- [x] 7. Build: `dotnet build "Construct Sync Toolkit.csproj" -c Release`

## 5. Critérios de aceite

- [x] Build Release sem erros nem warnings novos
- [x] `grep -i "timeliner|timeline"` no repositório (fora `obj/`, `.git/`, `specs/`) sem ocorrências
- [ ] No Navisworks (reiniciar após o build): tab "Construct Sync" exibe apenas 2 painéis ("Leitura e Laboratório" e "Coloração de Sets"); as 3 ferramentas restantes abrem normalmente

## 6. Guard rails — o que NÃO fazer

- Não excluir `Helpers/AsyncRelayCommand.cs` nem `Helpers/ExceptionHelper.cs` — compartilhados com Coloração de Sets e demais janelas (prd §4).
- Não refatorar as 3 ferramentas restantes nem seus services/models de carona.
- Não tocar em `Construct Sync Toolkit.csproj`, `.sln`, `Build/Script para gerar instalador.iss` nem artefatos de `obj/`.
- Não editar manualmente arquivos gerados (`*.g.cs`).
