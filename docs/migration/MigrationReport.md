# MigrationReport — Consolidação Navisworks Toolkit

> **Data:** 2026-06-11
> **Escopo:** Consolidação dos projetos `Auto_ViewTool` (Verum Toolkit v1.1.2) e `SetAtributesToolkit` (Construct Sync Toolkit v0.1.8) na solução única `NavisworksToolkit` v2.0.0.
> **Inventário completo da auditoria:** [`INVENTORY.md`](./INVENTORY.md)
> **Script de migração (reproduzível):** [`migrate-src.ps1`](./migrate-src.ps1)
> **Checklist de validação:** [`VALIDATION_CHECKLIST.md`](./VALIDATION_CHECKLIST.md)
>
> **Nota (reorganização de 2026-06-11):** os projetos originais `Auto_ViewTool/` e `SetAtributesToolkit/` foram movidos da raiz para `Archive/`; os caminhos de origem citados neste relatório referem-se à localização na época da migração.

---

## 1. Arquivos migrados

**Total: 46 arquivos de código** (cópias transformadas — os originais permanecem intactos) + temas, assets, ícones e template.

### 1.1 De `Auto_ViewTool` (24 arquivos)

| Origem | Destino | Transformação |
|---|---|---|
| `src/Core/NavisworksInterop.cs`, `SavedItemTree.cs`, `PerfLog.cs` | `src/Core/` | namespace → `NavisworksToolkit.Core` |
| `src/Core/XlsxFile.cs` | `src/Shared/` | namespace → `NavisworksToolkit.Shared` |
| `src/Models/ISelectableItem.cs`, `src/UI/UiCommon.cs`, `src/UI/SelectableListUi.cs` | `src/Shared/` | namespace → `NavisworksToolkit.Shared`; em `UiCommon`, assembly do pack URI `AutoViewTool` → `NavisworksToolkit` |
| `src/Core/{SelectionSetManager,IsolationHandler,ViewpointManager,TemplateManager}.cs` + `src/Models/{SelectionSetData,ViewpointData,ViewpointTemplateRow}.cs` + `src/UI/MainWindow.xaml(.cs)` | `src/Modules/ViewBuilder/` | namespace → `NavisworksToolkit.Modules.ViewBuilder`; XAML `x:Class` atualizado |
| `src/Core/CleanupManager.cs` + `src/Models/CleanupItemData.cs` + `src/UI/CleanupWindow.xaml(.cs)` | `src/Modules/ModelCleaner/` | namespace → `...Modules.ModelCleaner`; merge do tema → `/NavisworksToolkit;component/UI/Theme.xaml` |
| `src/Core/ExportManager.cs` + `src/Models/ExportItemData.cs` + `src/UI/ExportWindow.xaml(.cs)` | `src/Modules/ImageExporter/` | namespace → `...Modules.ImageExporter`; merge do tema atualizado |
| `src/UI/Theme.xaml` + `THEME_MAP.md` | `src/UI/` | cópia sem alteração |
| `assets/Icone.png`, `assets/logo_partners.png` | `src/Assets/` | cópia (embutidos como Resource) |
| `Resources/Icons/*.png` (6) | `src/Resources/Icons/` | cópia |
| `templates/AutoViewTool_Template.xlsx` | `templates/` | cópia |

### 1.2 De `SetAtributesToolkit` (24 arquivos)

| Origem | Destino | Transformação |
|---|---|---|
| `Helpers/{AsyncRelayCommand,ExceptionHelper,PropertyExtractionHelper,SelectionSetCache,VerumSchema}.cs` + `Models/{CheckableItem,SelectionSetItem}.cs` | `src/Shared/` | namespace flat `SetAtributesToolkit` → `NavisworksToolkit.Shared` |
| `Models/{AtributoCustom,AttributeEntry,LabCatNode,LabPropNode,SetAssignment}.cs` + `Services/{AtributoService,AttributeTemplateSerializer}.cs` + `Views/NativeAttrLabWindow.xaml(.cs)` | `src/Modules/AttributeLab/` | namespace → `...Modules.AttributeLab`; XAML `x:Class`, `xmlns:local` e merge do DesignSystem atualizados |
| `Models/InspectorModels.cs` + `Views/SelectionInspectorWindow.xaml(.cs)` | `src/Modules/SelectionInspector/` | idem |
| `Models/ColoringRule.cs` + `Services/SetColoringService.cs` + `ViewModels/SetColoringRulesViewModel.cs` + `Views/SetColoringRulesWindow.xaml(.cs)` | `src/Modules/SetColoring/` | idem |
| `Themes/DesignSystem.xaml` | `src/UI/DesignSystem.xaml` | cópia sem alteração interna (pack URI das janelas atualizado para o novo local) |
| `Resources/{atributos_icon,ler_atributo_icon}.png` (512²) | `assets/images/*_512.png` + derivados 16/32 em `src/Resources/Icons/` | redimensionamento bicúbico de alta qualidade |

### 1.3 Arquivos novos (não existiam em nenhum projeto)

| Arquivo | Papel |
|---|---|
| `NavisworksToolkit.sln`, `src/NavisworksToolkit.csproj` | Solução/projeto SDK-style unificado (net48, x64, WPF, v2.0.0) |
| `src/Core/NavisworksToolkitPlugin.cs` | Entry point único: `CommandHandlerPlugin` com os 6 comandos |
| `src/Core/ToolLauncher.cs` | Launcher unificado (ver §3, conflito 2) |
| `src/en-US/NavisworksToolkit.xaml` | Ribbon única (4 painéis, 6 botões) |
| `build.ps1`, `deploy.ps1` | Build e instalação da nova solução |
| `config/config.json` + `config/README.md` | Configuração central documentada |
| `docs/**` (incl. `docs/migration/`), `assets/**`, `README.md`, este relatório | Documentação/identidade/auditoria consolidadas |

### 1.4 Arquivos NÃO migrados (substituídos ou obsoletos — preservados nos originais e no Archive)

| Arquivo | Motivo |
|---|---|
| `Auto_ViewTool/src/VerumToolkitPlugin.cs` | Substituído por `NavisworksToolkitPlugin.cs` (mesmos 3 comandos + 3 novos) |
| `SetAtributesToolkit/Plugin/Construct Sync Toolkit.cs` (3 `AddInPlugin`) | Substituído pelos comandos `SelectionInspector`, `NativeAttrLab`, `SetColoringRules` do plugin unificado |
| `SetAtributesToolkit/Plugin/PluginHelpers.cs` | Lógica incorporada ao `ToolLauncher` unificado (comportamento preservado, incluindo a inicialização do esquema pack URI) |
| `SetAtributesToolkit/SetAtributesToolkit.addin` + `SetAtributesToolkit.xaml` | Mecanismo de registro substituído pela ribbon `[RibbonLayout]` |
| `Auto_ViewTool/en-US/VerumToolkit.xaml` | Substituído por `src/en-US/NavisworksToolkit.xaml` |
| `Auto_ViewTool/App.config` | `appSettings` nunca eram lidos em runtime; valores preservados em `config/config.json` (seção `legacy`) |
| `Auto_ViewTool/Properties/AssemblyInfo.cs` | Atributos gerados pelo csproj SDK-style (Title/Product/Company/Version) |
| Scripts antigos (`build/deploy/Package-Plugin/Snapshot-Version.ps1`, `.iss`, `PackageContents.xml`) | Específicos das estruturas antigas; novos `build.ps1`/`deploy.ps1` criados. Empacotamento bundle/instalador para a nova DLL listado como pendência (§6) |

---

## 2. Arquivos duplicados (entre os dois projetos)

| Item | Verificação | Resolução |
|---|---|---|
| `assets/Icone.png` | SHA-256 idêntico nos dois projetos | 1 cópia em `assets/logos/` e `src/Assets/` |
| `assets/logo_partners.png` | SHA-256 idêntico | idem |
| `.claude/skills/ui-design-system/` (SKILL.md + script) | SHA-256 idêntico | 1 cópia em `docs/skills/ui-design-system/` |
| Templates prd/spec (`spec-driven/_templates` × `specs/_templates`) | Versões **diferentes** do mesmo fluxo | Ambas preservadas em `docs/specifications/{auto-viewtool-spec-driven,setatributes-specs}/` |

---

## 3. Arquivos/pontos conflitantes e como foram resolvidos

| # | Conflito | Resolução | Comportamento alterado? |
|---|---|---|---|
| 1 | **Mecanismos de registro diferentes**: `CommandHandlerPlugin`+`[RibbonLayout]` (A) vs 3×`AddInPlugin`+`.addin`+`PluginRibbon` (B) | Padrão A adotado para todos (mecanismo oficial da API .NET para tab própria, já comprovado) | Não — cada botão abre a mesma janela; muda apenas onde o botão mora (tab unificada) |
| 2 | **Dois `ToolLauncher<T>`** com o mesmo papel e detalhes diferentes (A: factory com `NavisworksInterop` + dispose; B: construtor simples + `UriParser.IsKnownScheme("pack")`) | Classe única em `src/Core/ToolLauncher.cs` com os dois construtores; o check do esquema pack passou a valer para todas as ferramentas (idempotente e inofensivo) | Não |
| 3 | **Dois temas WPF** com a mesma paleta e estilos diferentes | Ambos mantidos em `src/UI/`; cada janela continua usando o seu | Não |
| 4 | **Namespace flat do B** (`SetAtributesToolkit` único) × namespaces hierárquicos do A | Tudo remapeado para `NavisworksToolkit.{Core,Shared,Modules.*,UI}`; `SelectionSetItem`/`CheckableItem` foram para `Shared` porque `PropertyExtractionHelper` (Shared) os referencia | Não |
| 5 | **Nomenclatura do B inconsistente** (csproj/sln "Construct Sync Toolkit" ≠ assembly `SetAtributesToolkit`) | Nome único `NavisworksToolkit` em solução, projeto, assembly e plugin | Não |
| 6 | **HintPaths divergentes** (`C:\Program Files` × `C:\Arquivos de Programas`) | Padronizado `C:\Program Files\...` (alias pt-BR resolve para o mesmo diretório; verificado nesta máquina) | Não |
| 7 | **Versões divergentes** (1.1.2 × 0.1.8) | Nova série **2.0.0** | — |
| 8 | **Títulos de janela** do B diziam "Construct Sync" | Atualizados para "Navisworks Toolkit" (3 títulos) | **Sim — cosmético, justificado**: identidade única da plataforma; nenhuma lógica alterada |
| 9 | **Ícones 512² usados direto no ribbon** do B | Derivados 16/32 px gerados (nomenclatura `Comando_<tam>.png`); originais preservados em `assets/images/` | Visual do botão mais nítido; mesma arte |
| 10 | `OutputPath` do B apontava direto para `%AppData%\...\Plugins\` (build = instalação) | Novo projeto usa `bin\` padrão + `deploy.ps1` explícito | Fluxo de dev: deploy passou a ser passo explícito (instalação não acontece mais como efeito colateral do build) |

---

## 4. Funcionalidades encontradas × incorporadas

Todas as **6 funcionalidades** encontradas na auditoria foram incorporadas — **nenhum comando ficou fora da ribbon**:

| Funcionalidade | Origem | Comando na ribbon única | Painel | Status |
|---|---|---|---|---|
| ViewBuilder (viewpoints isométricos em lote) | A | `ViewBuilder` | Visualização | ✅ Incorporada |
| ModelCleaner (limpeza de sets/viewpoints) | A | `ModelCleaner` | Modelo | ✅ Incorporada |
| ImageExporter (exportação JPG) | A | `ImageExporter` | Exportação | ✅ Incorporada |
| Inspecionar Seleção (propriedades BIM → CSV/Excel/TSV) | B | `SelectionInspector` | Seleção e Atributos | ✅ Incorporada |
| Laboratório de Atributos Nativos (COM API, merge, templates) | B | `NativeAttrLab` | Seleção e Atributos | ✅ Incorporada |
| Regras de Coloração de Sets (overrides cor/transparência) | B | `SetColoringRules` | Modelo | ✅ Incorporada |

Funcionalidades de suporte preservadas: isolamento hierárquico otimizado, câmera isométrica por bounding box, templates Excel (XlsxFile sem dependências), busca em tempo real, PerfLog, acesso defensivo via reflexão (`ComBridgeHelper`, `SetColoringService`, `PropertyExtractionHelper`), schema legado (`VerumSchema`: `Autis_*`, `AWP_*`, `Sets`), keyboard interop em janelas modeless.

## 5. Funcionalidades pendentes (planejadas nos originais, não implementadas — herdadas como backlog)

| Item | Fonte |
|---|---|
| Relatórios de viewpoints (HTML + XLSX com imagem + CSV) | README do A ("próxima etapa") |
| Exportação CSV de metadados de viewpoints | README do A |
| Custom properties por viewpoint (disciplina, revisor etc.) | README do A |
| Unit tests para os managers | CLAUDE.md do A ("Next Tasks") |

## 6. Dependências

| Dependência | Versão | Resolução |
|---|---|---|
| `Autodesk.Navisworks.Api` / `ComApi` / `Interop.ComApi` | 26.0 (Navisworks 2026) | HintPath `C:\Program Files\Autodesk\Navisworks Simulate 2026\`, `Private=false` |
| `AdWindows` | idem | idem (usado pelo XAML do ribbon; referência mantida por paridade com o B) |
| WPF / WinForms / `WindowsFormsIntegration` | .NET Framework 4.8 | `UseWPF` + `UseWindowsForms` |
| `System.IO.Compression` | GAC | Referência explícita (XlsxFile) |
| Pacotes NuGet | — | **Nenhum** (igual aos originais) |

Build verificado com .NET SDK (`dotnet build`) e compatível com MSBuild do VS2022.

## 7. Riscos identificados

| # | Risco | Severidade | Mitigação |
|---|---|---|---|
| 1 | **Validação em runtime pendente**: a carga da ribbon, ícones e comandos só pode ser confirmada dentro do Navisworks (plugins não têm teste automatizado; era assim nos originais) | Alta | Checklist de validação manual em `VALIDATION_CHECKLIST.md`; mecanismo de ribbon idêntico ao do A (que funcionava) |
| 2 | **Persistência de sets do NativeAttrLab**: dados gravados nos modelos com schema `Verum_Attributes` por instalações antigas continuam legíveis (VerumSchema preservado), mas conviver com o plugin antigo instalado em paralelo duplicaria botões | Média | Desinstalar os plugins antigos (`AutoViewTool`, `SetAtributesToolkit`) ao instalar o consolidado |
| 3 | **`%AppData%\...\Plugins\SetAtributesToolkit\`** pode conter a versão antiga do B (o build antigo instalava direto lá) | Média | Remover a pasta antiga ao adotar o consolidado (manualmente — este processo não apaga nada) |
| 4 | Renomeação de namespaces em massa: erro residual apareceria como falha de compilação ou de binding XAML | Baixa | Compilação Release sem erros nem warnings; BAMLs e recursos verificados na DLL |
| 5 | Log do PerfLog continua em `AutoViewTool_perf.log` (nome herdado) | Baixa | Intencional (zero mudança de comportamento); renomear é melhoria futura |
| 6 | HintPaths fixos por máquina (sem variável de ambiente) | Baixa | Igual aos originais; melhoria futura sugerida (§8) |

## 8. Recomendações

1. **Validar no Navisworks 2026** seguindo `VALIDATION_CHECKLIST.md` (abrir cada ferramenta, executar um fluxo mínimo por módulo).
2. **Desinstalar os plugins antigos** das máquinas que receberem o consolidado (evita tabs e botões duplicados).
3. **Iniciar repositório git** na raiz consolidada (o histórico antigo do B está preservado em `Archive/ProjectB/.git`).
4. **Criar instalador/bundle** para a nova DLL (adaptar `VerumToolkit-Setup.iss` e/ou `Package-Plugin.ps1`/`PackageContents.xml` preservados no Archive).
5. **Parametrizar o HintPath** do SDK (ex.: propriedade MSBuild `NavisworksSdkDir` com default) para builds em máquinas com SKU/idioma diferentes.
6. **Unificar os dois temas WPF** num único dicionário (hoje há `Theme.xaml` + `DesignSystem.xaml` com a mesma paleta) — mudança visual, fazer com revisão de UI.
7. **Artes próprias** para `SelectionInspector` e `NativeAttrLab` (hoje compartilham a mesma arte de origem, herança do projeto B).
8. **Renomear o log do PerfLog** para `NavisworksToolkit_perf.log` quando for aceitável quebrar scripts que o consumam.
9. **Estender o PerfLog** aos módulos do ex-B (hoje só os módulos do ex-A o utilizam) e introduzir leitura real do `config/config.json` em runtime, se desejado (ambas são mudanças de comportamento — por isso não foram feitas agora).
10. **Retomar o fluxo spec-driven** (`docs/specifications/`) para as funcionalidades pendentes do §5.
