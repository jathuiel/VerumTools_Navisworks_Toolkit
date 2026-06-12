# Arquitetura Consolidada — Navisworks Toolkit

> Documento de arquitetura da solução unificada criada em 2026-06-11.
> Para a arquitetura dos projetos originais, ver os guias em `/docs/developer-guide/`.

## Visão geral

Uma única DLL (`NavisworksToolkit.dll`) expõe **seis ferramentas** numa tab própria do ribbon ("Navisworks Toolkit"), usando o mecanismo oficial da API .NET do Navisworks: um `CommandHandlerPlugin` com atributos `[Command]`, `[RibbonLayout]` e `[RibbonTab]`.

| Grupo (painel) | Ferramenta | Origem |
|---|---|---|
| Visualização | ViewBuilder | Auto_ViewTool |
| Modelo | ModelCleaner | Auto_ViewTool |
| Modelo | Coloração de Sets | SetAtributesToolkit |
| Seleção e Atributos | Inspecionar Seleção | SetAtributesToolkit |
| Seleção e Atributos | Laboratório de Atributos | SetAtributesToolkit |
| Exportação | ImageExporter | Auto_ViewTool |

## Decisão: mecanismo de registro único

Os projetos originais usavam mecanismos diferentes:

- **Auto_ViewTool**: `CommandHandlerPlugin` único + `[RibbonLayout("VerumToolkit.xaml")]` (schema AdWindows, arquivo em `en-US\` ao lado da DLL). Tab própria, 3 comandos.
- **SetAtributesToolkit**: 3 classes `AddInPlugin` + manifesto `.addin` + `PluginRibbon` XAML (schema 2009).

A consolidação adota o **padrão do Auto_ViewTool** (CommandHandlerPlugin), por ser o mecanismo documentado da API .NET para tabs próprias e já comprovado no projeto. Os três `AddInPlugin` do SetAtributesToolkit viram comandos do plugin unificado; o `.addin` e o `PluginRibbon` XAML deixam de ser necessários (preservados no Archive).

**Comportamento preservado:** cada botão continua abrindo a mesma janela, como modeless singleton (reativa se já aberta), com keyboard interop habilitado.

## Estrutura da solução

```
NavisworksToolkit.sln
src/
├── NavisworksToolkit.csproj      # SDK-style, net48, x64, UseWPF + UseWindowsForms
├── Core/                          # Infra de plugin (namespace NavisworksToolkit.Core)
│   ├── NavisworksToolkitPlugin.cs # Entry point: CommandHandlerPlugin com 6 comandos
│   ├── ToolLauncher.cs            # Launcher unificado (modeless singleton + interop)
│   ├── NavisworksInterop.cs       # Wrapper da API (ex-Auto_ViewTool)
│   ├── SavedItemTree.cs           # Travessia de saved items (ex-Auto_ViewTool)
│   └── PerfLog.cs                 # Log de diagnóstico (ex-Auto_ViewTool)
├── Shared/                        # Utilitários sem dependência de módulo (NavisworksToolkit.Shared)
│   ├── XlsxFile.cs                # Leitor/escritor XLSX (ex-A)
│   ├── UiCommon.cs                # Helpers de UI (ex-A)
│   ├── SelectableListUi.cs        # Lista com checkbox/busca (ex-A)
│   ├── ISelectableItem.cs         # (ex-A)
│   ├── AsyncRelayCommand.cs       # ICommand assíncrono (ex-B)
│   ├── ExceptionHelper.cs         # UnwrapMessage (ex-B)
│   ├── PropertyExtractionHelper.cs# Extração de propriedades/sets (ex-B)
│   ├── SelectionSetCache.cs       # Cache tipado de sets (ex-B)
│   ├── VerumSchema.cs             # Fonte única de nomes de schema custom (ex-B)
│   └── CheckableItem.cs           # (ex-B)
├── Modules/                       # Um subdiretório por ferramenta (NavisworksToolkit.Modules.*)
│   ├── ViewBuilder/               # SelectionSetManager, IsolationHandler, ViewpointManager,
│   │                              # TemplateManager, DTOs, MainWindow
│   ├── ModelCleaner/              # CleanupManager, CleanupItemData, CleanupWindow
│   ├── ImageExporter/             # ExportManager, ExportItemData, ExportWindow
│   ├── SelectionInspector/        # InspectorModels, SelectionInspectorWindow
│   ├── AttributeLab/              # AtributoService, AttributeTemplateSerializer, DTOs,
│   │                              # NativeAttrLabWindow
│   └── SetColoring/               # SetColoringService, ColoringRule,
│                                  # SetColoringRulesViewModel, SetColoringRulesWindow
├── UI/                            # Recursos visuais compartilhados (NavisworksToolkit.UI)
│   ├── Theme.xaml                 # Tema das janelas ex-Auto_ViewTool
│   └── DesignSystem.xaml          # Tema das janelas ex-SetAtributesToolkit
├── Assets/                        # Imagens embutidas na DLL (Resource)
│   ├── Icone.png
│   └── logo_partners.png
├── Resources/Icons/               # Ícones do ribbon (copiados ao output no build)
└── en-US/
    └── NavisworksToolkit.xaml     # Layout do ribbon (RibbonControl, schema AdWindows)
```

## Namespaces

| Namespace | Conteúdo |
|---|---|
| `NavisworksToolkit.Core` | Entry point, launcher, interop, logging |
| `NavisworksToolkit.Shared` | Utilitários reutilizáveis entre módulos |
| `NavisworksToolkit.Modules.ViewBuilder` | Criação de viewpoints |
| `NavisworksToolkit.Modules.ModelCleaner` | Limpeza de sets/viewpoints |
| `NavisworksToolkit.Modules.ImageExporter` | Exportação de imagens |
| `NavisworksToolkit.Modules.SelectionInspector` | Inspeção de propriedades |
| `NavisworksToolkit.Modules.AttributeLab` | Atributos customizados (COM API) |
| `NavisworksToolkit.Modules.SetColoring` | Coloração por sets |
| `NavisworksToolkit.UI` | Temas e recursos visuais |

## Regras de carregamento do Navisworks (não óbvias — não quebrar)

1. **DLL em subpasta homônima**: `Plugins\NavisworksToolkit\NavisworksToolkit.dll`.
2. **Ribbon XAML em `en-US\`** relativo à DLL, como arquivo físico (não pode ser embutido).
3. **Ícones do ribbon** na pasta da DLL, referenciados como `../Nome.png` no XAML.
4. **Pack URIs** de temas e imagens embutidas usam o novo assembly: `/NavisworksToolkit;component/...`.
5. Plugins não recarregam a quente — reiniciar o Navisworks após cada build.

## Dois temas, uma paleta

Ambos os temas usam a mesma paleta de 5 cores (Ink `#292929`, Gray `#585757`, Cream `#F6F6F6`, Orange `#FC6A0A`, Deep Orange `#E74504`, variações apenas por opacidade):

- `UI/Theme.xaml` — estilos das janelas ex-Auto_ViewTool (mapa em `/docs/architecture/theme-map-auto-viewtool.md`).
- `UI/DesignSystem.xaml` — tokens e estilos das janelas ex-SetAtributesToolkit (`BtnCta`, `BtnSecondary`, `BtnDanger`, `SearchBox`, `FieldBox`, `ChkCell`, `ProgressDark`).

A fusão dos dois temas em um único dicionário **não** foi feita na consolidação (mudaria aparência/comportamento); está listada como melhoria futura no MigrationReport.

## Acesso defensivo à API (preservado intencionalmente)

Os pontos de reflexão do SetAtributesToolkit foram migrados sem simplificação, pois a superfície da API varia entre versões/SKUs:

- `ComBridgeHelper` localiza o `ComApiBridge` em namespaces candidatos.
- `SetColoringService` testa nomes candidatos para métodos de override de aparência.
- `PropertyExtractionHelper.LoadSelectionSetsViaReflection` tem fast path tipado e fallback de reflexão.
