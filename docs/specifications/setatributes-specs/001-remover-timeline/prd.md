# PRD — Remover a funcionalidade Timeline de Projeto (DocumentTimeliner)

> Fase 1 (Pesquisa). Repositório bruto de informações: consolida tudo o que a fase de
> especificação vai precisar, sem propor solução nem escrever código novo.

- **Pasta:** `specs/001-remover-timeline/`
- **Data:** 2026-06-11
- **Status:** `pronto para spec`

## 1. Problema e objetivo

A ferramenta **Timeline de Projeto** (gerência de tarefas do Timeliner do Navisworks) sai do escopo do plugin. Hoje ela é uma das 4 ferramentas registradas no ribbon "Construct Sync". O objetivo é removê-la por completo — entry point, registro, ribbon, janela, ViewModel, service e model — e atualizar a documentação (README.md e CLAUDE.md) para refletir o plugin com 3 ferramentas, sem deixar resíduo de código morto.

## 2. Comportamento esperado

- RF1 — O ribbon "Construct Sync" passa a exibir somente 2 painéis: "Leitura e Laboratório" e "Coloração de Sets" (o painel "Timeline" desaparece).
- RF2 — O plugin `SetAtributesToolkit.DocumentTimeliner` deixa de ser registrado no Navisworks (sem entrada no `.addin`, sem classe `AddInPlugin`).
- RF3 — Nenhum arquivo de código do Timeliner permanece no repositório; o projeto compila limpo em Release.
- RF4 — README.md e CLAUDE.md não mencionam mais Timeline/Timeliner (funcionalidade, tabela do ribbon, estrutura de pastas, contagem de ferramentas/painéis).

## 3. Mapeamento da base de código — arquivos impactados

| Arquivo | Impacto provável | Papel no contexto da feature |
|---|---|---|
| `Views/DocumentTimelinerWindow.xaml` | **excluir** | Janela WPF da ferramenta (uso exclusivo do Timeliner) |
| `Views/DocumentTimelinerWindow.xaml.cs` | **excluir** | Code-behind da janela; consome `DocumentTimelinerViewModel` e `TimelinerTaskData` |
| `ViewModels/DocumentTimelinerViewModel.cs` | **excluir** | ViewModel; consome `TimelinerService` e `TimelinerTaskData` (uso exclusivo) |
| `Services/TimelinerService.cs` | **excluir** | Acesso ao Timeliner via reflexão (uso exclusivo) |
| `Models/TimelinerTaskData.cs` | **excluir** | Model `INotifyPropertyChanged` de tarefa (uso exclusivo — ver §4) |
| `Plugin/Construct Sync Toolkit.cs` | alterar | Remover a classe `DocumentTimelinerPlugin` (linhas 35–43) |
| `SetAtributesToolkit.addin` | alterar | Remover o bloco `<Plugin>` do DocumentTimeliner (linhas 28–34) |
| `SetAtributesToolkit.xaml` | alterar | Remover o `RibbonPanel "Timeline"` (linhas 31–38) |
| `README.md` | alterar | Seção "Timeline de Projeto", linha da tabela Ribbon, 3 entradas na árvore de estrutura, contagens ("4 classes AddInPlugin", "3 painéis") |
| `CLAUDE.md` | alterar | Linha 7 (4 ferramentas), linha 53 (exemplo MVVM), linha 61 (bullet do `TimelinerService`) |
| `Construct Sync Toolkit.csproj` | só leitura | SDK-style com globbing automático de `.cs`/`.xaml` — **não requer mudança** ao excluir arquivos |
| `obj/**` (ex.: `obj/*/Views/DocumentTimelinerWindow.g.cs`) | só leitura | Artefatos gerados; são regenerados/limpos pelo build — não editar manualmente |

### Trechos exatos a remover (âncoras para o spec)

`Plugin/Construct Sync Toolkit.cs:35-43`:

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

`SetAtributesToolkit.addin:28-34`:

```xml
  <Plugin>
    <Id>SetAtributesToolkit.DocumentTimeliner</Id>
    <FileName>SetAtributesToolkit</FileName>
    <ClassName>SetAtributesToolkit.DocumentTimelinerPlugin</ClassName>
    <DisplayName>Timeline de Projeto</DisplayName>
    <ToolTip>Gerencia tarefas da timeline do projeto</ToolTip>
  </Plugin>
```

`SetAtributesToolkit.xaml:31-38`:

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

### Ocorrências no README.md (verificadas por grep)

- Linha 79–86: seção `### Timeline de Projeto` (com bullets e o separador `---` adjacente);
- Linha 96: linha da tabela Ribbon — `| **Timeline** | Timeline de Projeto |`;
- Linha 118: comentário da árvore — `# 4 classes AddInPlugin (entry points)` → passa a 3;
- Linhas 124, 127, 138: entradas da árvore — `DocumentTimelinerWindow.xaml(.cs)`, `DocumentTimelinerViewModel.cs`, `TimelinerTaskData.cs`;
- Linha 172: `o tab **"Construct Sync"** aparecerá no ribbon com os 3 painéis` → passa a 2 painéis.

### Ocorrências no CLAUDE.md (verificadas por grep)

- Linha 7: `...com 4 ferramentas expostas no ribbon "Construct Sync": Inspecionar Seleção, Laboratório de Atributos Nativos (gravação de atributos customizados), Regras de Coloração de Sets e Timeline de Projeto.`
- Linha 53: `- **MVVM** (padrão para código novo): \`SetColoringRulesWindow\` e \`DocumentTimelinerWindow\` usam ViewModel...`
- Linha 61: `- \`TimelinerService\` acessa tarefas do Timeliner inteiramente por reflexão (\`AddNew\`/\`Add\`, propriedades por nome).`

## 4. Padrões e funções existentes a reaproveitar

Não há código novo nesta mudança. O ponto crítico é o inverso — **helpers compartilhados que o Timeliner usa mas que têm outros consumidores e DEVEM permanecer**:

- `Helpers/AsyncRelayCommand.cs:7` — `public class AsyncRelayCommand : ICommand` — usado por `ViewModels/SetColoringRulesViewModel.cs:56-60` (5 commands) e `:296-300` (`RaiseCanExecuteChanged`).
- `Helpers/ExceptionHelper.cs:5` — `internal static class ExceptionHelper` — usado por `SetColoringRulesViewModel`, `SetColoringService`, `SelectionInspectorWindow` e `NativeAttrLabWindow`.

Confirmado por grep: `TimelinerService`, `DocumentTimelinerViewModel` e `TimelinerTaskData` **não têm consumidores fora** dos 5 arquivos a excluir.

## 5. Documentação externa

Nenhuma necessária — a mudança é remoção de código interno, sem nova superfície de API.

## 6. Restrições do projeto aplicáveis

- **Cadeia de registro do plugin (CLAUDE.md):** cada ferramenta existe em 3 arquivos acoplados por nome (`Plugin/Construct Sync Toolkit.cs`, `SetAtributesToolkit.addin`, `SetAtributesToolkit.xaml`). A remoção deve tirar as 3 pontas de forma consistente, sob pena de o Navisworks registrar botão sem plugin (ou vice-versa).
- **Build = instalação:** o output vai para `%AppData%\Autodesk\Navisworks Simulate 2026\Plugins\SetAtributesToolkit\`. Validar com `dotnet build "Construct Sync Toolkit.csproj" -c Release`; com o Navisworks aberto o build pode falhar (DLL bloqueada). É preciso reiniciar o Navisworks para o ribbon refletir a mudança.
- **Idioma:** documentação e mensagens em pt-BR.

## 7. Fora de escopo / Não tocar

- `Helpers/AsyncRelayCommand.cs` e `Helpers/ExceptionHelper.cs` — compartilhados com Coloração de Sets e demais janelas (ver §4).
- As 3 ferramentas restantes (`SelectionInspector`, `NativeAttrLab`, `SetColoringRules`) e seus services/models — nenhuma refatoração de carona.
- `Construct Sync Toolkit.csproj` / `.sln` — sem mudanças (globbing automático).
- `Build/Script para gerar instalador.iss` — não referencia arquivos do Timeliner individualmente (empacota a DLL + manifesto + ribbon); fica como está.
- Artefatos em `obj/` — gerados; não editar manualmente.

## 8. Perguntas em aberto

Nenhuma — a remoção foi ordenada explicitamente pelo autor do projeto.
