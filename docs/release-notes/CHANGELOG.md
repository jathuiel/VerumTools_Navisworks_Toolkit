# CHANGELOG — Navisworks Toolkit

Histórico consolidado da plataforma. Versões anteriores à consolidação referem-se aos projetos de origem.

## [2.1.2] — 2026-06-12 — Ghosting no Smart Views

> **Snapshot:** `NavisworksToolkit_v2.1.2_src.zip`

Substituição do comportamento de isolamento no **Smart Views**: elementos fora do Selection Set deixam de ser ocultados e passam a receber um override de cor cinza semi-transparente (*ghosting*), mantendo o contexto espacial do modelo durante a criação de viewpoints.

### Alterado
- `IsolationHandler`: troca `SetHidden(offPath, true)` por `OverridePermanentColor` + `OverridePermanentTransparency` (cinza 0.88 / 75 % de transparência) nos itens off-path.
- Reset de isolamento (`Restaurar Visibilidade`) agora também limpa overrides de cor/transparência via `ResetAllPermanentMaterials()`.
- O ghost é capturado automaticamente pelo `CaptureRuntimeOverrides()` — viewpoints salvos incluem o efeito sem alterações no `ViewpointManager`.

### Parâmetros ajustáveis
```csharp
// IsolationHandler.cs
private static readonly Color GhostColor = new Color(0.88, 0.88, 0.88);
private const float GhostTransparency = 0.75f; // 0.0 opaco … 1.0 invisível
```

## [2.1.0] — 2026-06-11 — Padronização UX (naming)

> **Snapshot:** `NavisworksToolkit_v2.1.0_src.zip` (151 arquivos · 0,42 MB)
> SHA-256: `2DD4B7D52D80C9B6B6873847DDC051EE3885074CBDAFC9BD80F4486B89892A10`
> Inclui: código-fonte, docs, assets, config, templates. Exclui: bin/, obj/, Archive/, .vscode/.

Aplicação dos UX Standards ([docs/architecture/ux-standards.md](../architecture/ux-standards.md)) — **somente strings de UI; nenhuma funcionalidade ou lógica alterada**. IDs técnicos dos comandos preservados.

### Alterado
- Botões do ribbon renomeados para rótulos comerciais em inglês: ViewBuilder → **Smart Views** · ModelCleaner → **Model Cleanup** · ImageExporter → **Image Capture** · Inspecionar Seleção → **Selection Inspector** · Atributos → **Property Explorer** · Coloração de Sets → **Visual Sets**.
- Painéis do ribbon reorganizados: **Visualization** (Smart Views, Visual Sets, Image Capture) · **Selection** (Selection Inspector) · **Data** (Property Explorer) · **Model** (Model Cleanup).
- Títulos das 6 janelas padronizados no formato `{ToolName}` e headers internos alinhados ao nome do botão (descrições longas permanecem nos subtítulos, em pt-BR).
- `DisplayName` dos `[Command]` e nomes nos diálogos de erro alinhados aos novos rótulos.

## [2.0.0] — 2026-06-11 — Consolidação

Unificação dos projetos **Auto_ViewTool (Verum Toolkit) v1.1.2** e **SetAtributesToolkit (Construct Sync Toolkit) v0.1.8** em uma única solução `NavisworksToolkit`.

### Adicionado
- Solução única `NavisworksToolkit.sln` com projeto SDK-style (`net48`, x64, WPF).
- Ribbon única **"Navisworks Toolkit"** com os 6 comandos organizados por grupos:
  - **Visualização**: ViewBuilder
  - **Modelo**: ModelCleaner, Coloração de Sets
  - **Seleção e Atributos**: Inspecionar Seleção, Laboratório de Atributos
  - **Exportação**: ImageExporter
- Entry point unificado `NavisworksToolkitPlugin` (`CommandHandlerPlugin`) substituindo os mecanismos paralelos de registro (3 `AddInPlugin` + `.addin` do SetAtributesToolkit).
- `ToolLauncher` unificado (janelas modeless singleton com keyboard interop) consolidando as duas implementações existentes.
- Namespaces padronizados: `NavisworksToolkit.Core`, `NavisworksToolkit.Shared`, `NavisworksToolkit.Modules.*`, `NavisworksToolkit.UI`.
- Documentação consolidada em `/docs`; assets consolidados em `/assets`; configuração centralizada em `/config`.
- Ícones de ribbon padronizados 16/32 px para as ferramentas do SetAtributesToolkit (derivados dos originais 512², preservados).

### Preservado (sem alteração funcional)
- Toda a lógica das 6 ferramentas (managers, services, helpers, viewmodels) migrada sem mudança de comportamento.
- Os dois temas WPF (`Theme.xaml` do Auto_ViewTool e `DesignSystem.xaml` do SetAtributesToolkit), ambos com a mesma paleta de 5 cores.
- Backup integral dos dois projetos em `/Archive/ProjectA` e `/Archive/ProjectB`.

---

## Histórico anterior — Auto_ViewTool (Verum Toolkit)

> Fonte: arquivo `VERSION` (1.1.2) e snapshots zip gerados por `Snapshot-Version.ps1` (registrados em `..\Auto_ViewTool_releases\RELEASES.md`, fora deste repositório).

- **1.1.2** — versão final antes da consolidação (instalador `VerumToolkit_v1.1.2_Setup.exe` em `dist/`).
- **1.1.0** — bundle `AutoViewTool_v1.1.0_bundle.zip` (distribuição via ApplicationPlugins).
- **1.0.3** — versão documentada no README: 3 ferramentas (ViewBuilder, ModelCleaner, ImageExporter) numa única DLL com tab própria "Verum Toolkit".
- Marcos anteriores: migração de 3 `AddInPlugin` independentes (faixa Add-Ins) para `CommandHandlerPlugin` único com ribbon própria; isolamento hierárquico otimizado; templates Excel sem dependências externas; exportação JPG com markups.

## Histórico anterior — SetAtributesToolkit (Construct Sync Toolkit)

> Fonte: README (v0.1.8) e histórico git em `/Archive/ProjectB/.git`.

- **0.1.8** — versão final antes da consolidação: 3 ferramentas (Inspecionar Seleção, Laboratório de Atributos Nativos, Regras de Coloração de Sets) na tab "Construct Sync".
- **001-remover-timeline** — remoção da ferramenta DocumentTimeliner (spec em `/docs/specifications/setatributes-specs/001-remover-timeline/`); resquícios permanecem apenas em artefatos de build antigos (`obj/`).
- Marcos anteriores: design system de 5 cores (`DesignSystem.xaml`); migração da janela de coloração para MVVM; schema `VerumSchema` com suporte a nomes legados (`Autis_*`, `AWP_*`, `Sets`).
