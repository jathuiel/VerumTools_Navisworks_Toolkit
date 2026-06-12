# Inventário Completo — Consolidação VerumTools

> **Data da auditoria:** 2026-06-11
> **Auditor:** Claude Code (ETAPA 1 do plano de consolidação)
> **Projetos auditados:**
> - **Projeto A** — `Auto_ViewTool` (Verum Toolkit) — v1.1.2
> - **Projeto B** — `SetAtributesToolkit` (Construct Sync Toolkit) — v0.1.8

---

## 1. Inventário de Estrutura

### 1.1 Projeto A — Auto_ViewTool

| Item | Valor |
|---|---|
| Solução | `Auto_ViewTool.sln` |
| Projeto | `Auto_ViewTool.csproj` (formato legado, ToolsVersion 15.0) |
| AssemblyName | `AutoViewTool` |
| RootNamespace | `AutoViewTool` |
| Target | .NET Framework 4.8, x64, WPF + WinForms interop |
| Tipo de plugin | `CommandHandlerPlugin` único com 3 `[Command]` + `[RibbonLayout]`/`[RibbonTab]` |
| Ribbon | Tab própria **"Verum Toolkit"** via `en-US\VerumToolkit.xaml` (schema AdWindows) |
| Versão atual | **1.1.2** (arquivo `VERSION`; README desatualizado menciona v1.0.3) |
| Git | Não é repositório git |

Árvore relevante (excluindo `bin/`, `obj/`):

```
Auto_ViewTool/
├── Auto_ViewTool.sln / .csproj
├── App.config
├── VERSION (1.1.2)
├── PackageContents.xml          # manifesto bundle Autodesk
├── VerumToolkit-Setup.iss       # instalador Inno Setup
├── build.ps1 / deploy.ps1 / Package-Plugin.ps1 / Snapshot-Version.ps1
├── CLAUDE.md / README.md / SETUP.md / USER_GUIDE.md / EMPACOTAMENTO.md
├── .claude/  (settings + skills: ui-design-system, ui-ux-pro-max)
├── .vscode/settings.json
├── assets/   (Icone.png 512², logo_partners.png 844x321)
├── en-US/VerumToolkit.xaml      # layout do ribbon
├── Properties/AssemblyInfo.cs
├── Resources/Icons/  (6 PNGs: ViewBuilder|ModelCleaner|ImageExporter _16/_32)
├── spec-driven/  (README + templates prd/spec)
├── templates/AutoViewTool_Template.xlsx
├── dist/  (bundle e setup compilados: AutoViewTool.bundle, zips, exe)
└── src/
    ├── VerumToolkitPlugin.cs    # entry point (CommandHandlerPlugin)
    ├── Core/      (10 arquivos)
    ├── Models/    (6 arquivos)
    └── UI/        (8 arquivos + THEME_MAP.md)
```

Contagem de arquivos relevantes (sem `bin/obj/.git/.vs/__pycache__`): **98**

### 1.2 Projeto B — SetAtributesToolkit

| Item | Valor |
|---|---|
| Solução | `Construct Sync Toolkit.sln` |
| Projeto | `Construct Sync Toolkit.csproj` (SDK-style) |
| AssemblyName | `SetAtributesToolkit` (⚠ difere do nome do csproj/sln) |
| RootNamespace | `SetAtributesToolkit` |
| Target | net48, x64, `UseWPF` + `UseWindowsForms` |
| Tipo de plugin | 3 classes `AddInPlugin` + manifesto `.addin` + `PluginRibbon` XAML (schema 2009) |
| Ribbon | Tab **"Construct Sync"** via `SetAtributesToolkit.xaml` |
| OutputPath | direto em `%AppData%\...\Navisworks Simulate 2026\Plugins\SetAtributesToolkit\` |
| Versão | **0.1.8** (mencionada no README; sem arquivo VERSION) |
| Git | Repositório git completo (histórico em `.git/`) |

Árvore relevante (excluindo `bin/`, `obj/`, `.git/`, `.vs/`):

```
SetAtributesToolkit/
├── Construct Sync Toolkit.sln / .csproj
├── SetAtributesToolkit.addin    # manifesto do plugin
├── SetAtributesToolkit.xaml     # ribbon (PluginRibbon, schema 2009)
├── .gitignore
├── CLAUDE.md / README.md
├── .claude/  (settings, commands: prd/spec/implement, skill ui-design-system)
├── assets/   (Icone.png, logo_partners.png — IDÊNTICOS aos do Projeto A)
├── Build/Script para gerar instalador.iss
├── Helpers/      (5 arquivos)
├── Models/       (9 arquivos)
├── Plugin/       (2 arquivos)
├── Resources/    (atributos_icon.png 512², ler_atributo_icon.png 512²)
├── Services/     (3 arquivos)
├── specs/        (README, templates, 001-remover-timeline)
├── Themes/DesignSystem.xaml
├── ViewModels/   (1 arquivo)
└── Views/        (3 janelas .xaml + .xaml.cs)
```

Contagem de arquivos relevantes (sem `bin/obj/.git/.vs`): **50**

> ⚠ Divergência: o README do Projeto B menciona `Docs/Figma_UI_Brief.md`, mas a pasta `Docs/` **não existe** no projeto.

---

## 2. Inventário de Funcionalidades

### Tabela-mestre

| Projeto | Funcionalidade | Classe principal | Janela | Ribbon atual | Botão atual | Ícone | Status |
|---|---|---|---|---|---|---|---|
| A | **ViewBuilder** — cria viewpoints isométricos em lote a partir de Selection Sets (template Excel opcional) | `VerumToolkitPlugin` (cmd `ViewBuilder`) → `ViewpointManager`, `IsolationHandler`, `SelectionSetManager`, `TemplateManager` | `MainWindow` | Tab "Verum Toolkit" / painel "Automation Tools" | ViewBuilder | `ViewBuilder_16/32.png` | ✅ Funcional |
| A | **ModelCleaner** — remove Search/Selection Sets e Viewpoints indesejados | `VerumToolkitPlugin` (cmd `ModelCleaner`) → `CleanupManager` | `CleanupWindow` | Tab "Verum Toolkit" / painel "Automation Tools" | ModelCleaner | `ModelCleaner_16/32.png` | ✅ Funcional |
| A | **ImageExporter** — exporta JPG dos viewpoints (resolução/qualidade/markups/prefixo) | `VerumToolkitPlugin` (cmd `ImageExporter`) → `ExportManager` | `ExportWindow` | Tab "Verum Toolkit" / painel "Automation Tools" | ImageExporter | `ImageExporter_16/32.png` | ✅ Funcional |
| B | **Inspecionar Seleção** — inspeção tabular de propriedades BIM, exportação CSV/Excel XML/TSV | `SelectionInspectorPlugin` → `PropertyExtractionHelper` | `SelectionInspectorWindow` | Tab "Construct Sync" / painel "Leitura e Laboratório" | Inspecionar Seleção | `ler_atributo_icon.png` | ✅ Funcional |
| B | **Laboratório de Atributos Nativos** — grava/remove atributos customizados via COM API (merge, sets como propriedade, templates CSV/XML) | `NativeAttrLabPlugin` → `AtributoService`, `VerumSchema`, `AttributeTemplateSerializer` | `NativeAttrLabWindow` | Tab "Construct Sync" / painel "Leitura e Laboratório" | Laboratório de Atributos Nativos | `ler_atributo_icon.png` | ✅ Funcional |
| B | **Regras de Coloração de Sets** — overrides de cor/transparência por Selection Set, import/export XML | `SetColoringRulesPlugin` → `SetColoringRulesViewModel` → `SetColoringService` | `SetColoringRulesWindow` | Tab "Construct Sync" / painel "Coloração de Sets" | Regras de Coloração | `atributos_icon.png` | ✅ Funcional (MVVM) |

### Funcionalidades de suporte (não expostas em botão)

| Projeto | Item | Arquivo | Função |
|---|---|---|---|
| A | `NavisworksInterop` | `src/Core/NavisworksInterop.cs` | Wrapper robusto da API (IDisposable) |
| A | `SavedItemTree` | `src/Core/SavedItemTree.cs` | Travessia da hierarquia de saved items |
| A | `PerfLog` | `src/Core/PerfLog.cs` | Log de diagnóstico em `%TEMP%\AutoViewTool_perf.log` |
| A | `XlsxFile` | `src/Core/XlsxFile.cs` | Leitor/escritor XLSX sem dependências externas |
| A | `UiCommon` / `SelectableListUi` | `src/UI/` | Helpers de UI compartilhados das 3 janelas |
| A | `ToolLauncher<T>` (interno) | `src/VerumToolkitPlugin.cs` | Janela modeless singleton + keyboard interop |
| B | `ToolLauncher<T>` | `Plugin/PluginHelpers.cs` | Janela modeless singleton + keyboard interop (mesmo padrão, implementação distinta) |
| B | `VerumSchema` | `Helpers/VerumSchema.cs` | Fonte única de nomes de categoria/propriedade custom + nomes legados |
| B | `SelectionSetCache` | `Helpers/SelectionSetCache.cs` | Cache tipado de selection/search sets |
| B | `PropertyExtractionHelper` | `Helpers/PropertyExtractionHelper.cs` | Extração de propriedades com fast path + fallback de reflexão |
| B | `ComBridgeHelper` | `Services/AtributoService.cs` | Localização defensiva do ComApiBridge (reflexão intencional) |
| B | `AsyncRelayCommand` | `Helpers/AsyncRelayCommand.cs` | ICommand assíncrono para MVVM |
| B | `ExceptionHelper` | `Helpers/ExceptionHelper.cs` | UnwrapMessage centralizado |

---

## 3. Inventário de Dependências

### 3.1 Referências de assembly

| Dependência | Projeto A | Projeto B | HintPath |
|---|---|---|---|
| `Autodesk.Navisworks.Api` 26.0 | ✔ | ✔ | A: `C:\Program Files\Autodesk\Navisworks Simulate 2026\` · B: `C:\Arquivos de Programas\...` (alias pt-BR do mesmo diretório — **ambos resolvem nesta máquina**) |
| `Autodesk.Navisworks.ComApi` | ✔ | ✔ | idem |
| `Autodesk.Navisworks.Interop.ComApi` | ✔ | ✔ | idem |
| `AdWindows` | – (usado só no XAML do ribbon) | ✔ | idem |
| WPF (PresentationCore/Framework, WindowsBase, System.Xaml) | ✔ | ✔ (via `UseWPF`) | GAC |
| WinForms + `WindowsFormsIntegration` | ✔ | ✔ (via `UseWindowsForms`) | GAC |
| `System.Drawing` | ✔ (GenerateImage) | via SDK | GAC |
| `System.IO.Compression` | ✔ (XlsxFile) | – | GAC |
| `System.Xml` / `System.Xml.Linq` | ✔ | via SDK | GAC |

**Nenhum pacote NuGet em nenhum dos projetos.** Sem dependências externas além do SDK do Navisworks e do .NET Framework 4.8.

### 3.2 Ambiente verificado nesta máquina (2026-06-11)

| Item | Status |
|---|---|
| Navisworks Simulate 2026 (`C:\Program Files\Autodesk\Navisworks Simulate 2026\`) | ✅ instalado (também 2025 e 2027) |
| Alias pt-BR `C:\Arquivos de Programas\...` | ✅ resolve |
| MSBuild (VS2022 Community) | ✅ `C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe` |
| dotnet CLI | ✅ `C:\Program Files\dotnet\dotnet.exe` |

### 3.3 Ferramentas auxiliares / scripts

| Projeto | Script | Função |
|---|---|---|
| A | `build.ps1` | Compila via MSBuild (auto-localiza VS2022) |
| A | `deploy.ps1` | Copia DLL para `Plugins\AutoViewTool\` do Navisworks (requer admin) |
| A | `Package-Plugin.ps1` | Gera `dist\AutoViewTool.bundle` (+ zip; `-Install` p/ ApplicationPlugins) |
| A | `Snapshot-Version.ps1` | Snapshot versionado em zip + RELEASES.md (SemVer no arquivo VERSION) |
| A | `VerumToolkit-Setup.iss` | Instalador Inno Setup |
| A | `PackageContents.xml` | Manifesto de bundle Autodesk (ApplicationPlugins) |
| B | `Build/Script para gerar instalador.iss` | Instalador Inno Setup (instala em `%ProgramData%\Autodesk\Navisworks <SKU>\Plugins\`) |

### 3.4 Mecanismos de registro de plugin (DIFERENTES — ponto crítico da consolidação)

| Aspecto | Projeto A | Projeto B |
|---|---|---|
| Classe base | `CommandHandlerPlugin` (1 classe, 3 comandos) | `AddInPlugin` (3 classes) |
| Tab própria | Sim — `[RibbonLayout("VerumToolkit.xaml")]` + `[RibbonTab]` | Sim — `PluginRibbon` XAML + `.addin` |
| Schema do ribbon XAML | `clr-namespace:Autodesk.Windows;assembly=AdWindows` (RibbonControl) | `http://schemas.autodesk.com/navisworks/2009/xaml/commands` (PluginRibbon) |
| Localização do XAML | `en-US\` relativo à DLL (obrigatório) | raiz do output, nome = AssemblyName |
| Ícones do ribbon | PNGs na raiz do output, ref. `../Nome.png` | PNGs em `Resources\`, ref. relativa |
| Instalação | `Plugins\AutoViewTool\AutoViewTool.dll` (subpasta = nome da DLL) | `%AppData%\...\Plugins\SetAtributesToolkit\` |

---

## 4. Inventário de Assets Visuais

| Asset | Origem | Dimensões | Tamanho | Uso | Observação |
|---|---|---|---|---|---|
| `Icone.png` | A: `assets/` · B: `assets/` | 512×512 | 12,1 KB | Ícone de janela (embutido como Resource na DLL do A) | **Byte-idêntico nos dois projetos** |
| `logo_partners.png` | A: `assets/` · B: `assets/` | 844×321 | 10,7 KB | Logo no header das janelas (embutido no A) | **Byte-idêntico nos dois projetos** |
| `ViewBuilder_16.png` / `_32.png` | A: `Resources/Icons/` | 16×16 / 32×32 | 0,4 / 0,8 KB | Botão ribbon ViewBuilder | Padrão correto 16+32 |
| `ModelCleaner_16.png` / `_32.png` | A: `Resources/Icons/` | 16×16 / 32×32 | 0,4 / 0,8 KB | Botão ribbon ModelCleaner | Padrão correto 16+32 |
| `ImageExporter_16.png` / `_32.png` | A: `Resources/Icons/` | 16×16 / 32×32 | 0,4 / 0,7 KB | Botão ribbon ImageExporter | Padrão correto 16+32 |
| `ler_atributo_icon.png` | B: `Resources/` | 512×512 | 14,4 KB | Botões SelectionInspector e NativeAttrLab | ⚠ Fora de padrão (512² usado direto no ribbon; compartilhado por 2 botões) |
| `atributos_icon.png` | B: `Resources/` | 512×512 | 10,8 KB | Botão SetColoringRules | ⚠ Fora de padrão (512²) |
| Cópias em `dist/AutoViewTool.bundle/Contents/` | A | 16/32 | – | Bundle compilado (artefato de release, não fonte) | Preservar em Archive |
| `AutoViewTool_Template.xlsx` | A: `templates/` | – | 3,1 KB | Template Excel do ViewBuilder | Asset funcional |

### Paleta de cores (idêntica nos dois projetos — base da identidade visual)

| Token | Hex | Uso |
|---|---|---|
| Ink | `#292929` | Texto principal / chrome escuro |
| Gray | `#585757` | Texto secundário |
| Cream | `#F6F6F6` | Superfícies claras |
| Orange | `#FC6A0A` | Acento / CTA |
| Deep Orange | `#E74504` | CTA hover/pressed |

Regra dos dois temas: variações **somente por opacidade**; nunca introduzir 6ª cor.
Arquivos: A: `src/UI/Theme.xaml` (mapeado em `src/UI/THEME_MAP.md`) · B: `Themes/DesignSystem.xaml`.

---

## 5. Inventário de Documentação

| Projeto | Documento | Tamanho | Conteúdo | Destino na consolidação |
|---|---|---|---|---|
| A | `README.md` | 10,0 KB | Visão geral, arquitetura, build, instalação, uso, troubleshooting | `/docs` (raiz) + user-guide + developer-guide |
| A | `CLAUDE.md` | 5,4 KB | Padrões de código, build, versionamento | `/docs/developer-guide` |
| A | `SETUP.md` | 6,8 KB | Verificação de SDK e build | `/docs/developer-guide` |
| A | `USER_GUIDE.md` | 12,5 KB | Guia do usuário das 3 ferramentas | `/docs/user-guide` |
| A | `EMPACOTAMENTO.md` | 5,5 KB | Bundle de distribuição Autodesk | `/docs/developer-guide` |
| A | `src/UI/THEME_MAP.md` | 10,9 KB | Mapa do tema WPF | `/docs/architecture` |
| A | `spec-driven/README.md` + templates | 9,1 KB | Fluxo spec-driven (prd/spec) | `/docs/specifications` |
| A | `.claude/skills/ui-design-system/SKILL.md` | 1,0 KB | Skill de design system | `/docs/skills` |
| A | `.claude/skills/ui-ux-pro-max/SKILL.md` (+ data/scripts) | 13,2 KB | Skill UI/UX com CSVs de referência | `/docs/skills` |
| B | `README.md` | 6,1 KB | Visão geral, funcionalidades, ribbon, instalação | `/docs` (raiz) + user-guide |
| B | `CLAUDE.md` | 6,6 KB | Arquitetura, cadeia de registro, design system | `/docs/developer-guide` + architecture |
| B | `specs/README.md` + templates | 7,2 KB | Fluxo spec-driven (prd/spec/implement) | `/docs/specifications` |
| B | `specs/001-remover-timeline/` (prd + spec) | 14,5 KB | Spec da remoção do DocumentTimeliner | `/docs/specifications` |
| B | `.claude/commands/` (prd, spec, implement) | 7,7 KB | Comandos do fluxo spec-driven | `/docs/skills` |
| B | `.claude/skills/ui-design-system/SKILL.md` | 1,0 KB | Skill de design system (duplicada do A) | `/docs/skills` (deduplicar) |

**Documentos ausentes em ambos:** `CHANGELOG.md`, `ROADMAP.md`, `TODO.md`, `SKILLS.md` formal.
Conteúdo de roadmap existe embutido no README do A ("Planejadas 🔄") e no CLAUDE.md do A ("Next Tasks").

---

## 6. Duplicações e conflitos identificados

| # | Item | Detalhe | Resolução proposta |
|---|---|---|---|
| 1 | `assets/Icone.png` e `assets/logo_partners.png` | Byte-idênticos nos dois projetos | Manter 1 cópia em `/assets`, originais preservados no Archive |
| 2 | `ToolLauncher<T>` | Duas implementações do mesmo padrão (modeless singleton + keyboard interop) | Unificar em `NavisworksToolkit.Core` preservando os dois comportamentos (com/sem interop factory) |
| 3 | Skill `ui-design-system` | Duplicada nos dois `.claude/skills/` | Consolidar 1 cópia |
| 4 | Templates prd/spec | `spec-driven/_templates` (A) ≈ `specs/_templates` (B) — versões diferentes do mesmo fluxo | Consolidar em `/docs/specifications/_templates`, preservando ambas as versões |
| 5 | Mecanismo de plugin | `CommandHandlerPlugin` (A) vs `AddInPlugin`+`.addin` (B) | Migrar tudo para `CommandHandlerPlugin` único com tab própria (padrão A, oficial da API .NET) |
| 6 | Dois temas WPF | `Theme.xaml` (A) vs `DesignSystem.xaml` (B), mesma paleta, estilos diferentes | Manter ambos no novo assembly (cada janela usa o seu) — unificação visual fica como melhoria futura |
| 7 | Nomenclatura inconsistente no B | csproj/sln "Construct Sync Toolkit" ≠ AssemblyName "SetAtributesToolkit" | Resolvido pela nova solução `NavisworksToolkit` |
| 8 | Versões divergentes | A = 1.1.2, B = 0.1.8 | Nova numeração unificada iniciando em 2.0.0 (sugestão) |
| 9 | README do B cita `Docs/Figma_UI_Brief.md` | Arquivo não existe | Registrar no MigrationReport |
| 10 | Ícones 512² no ribbon do B | Fora do padrão 16/32 | Gerar derivados 16/32 padronizados, preservando originais |
