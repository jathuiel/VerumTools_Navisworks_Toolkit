# UX Standards — Navisworks Toolkit

> **Versão:** 1.0 · **Data:** 2026-06-11 · **Aplicado em:** Navisworks Toolkit v2.1.0
> **Escopo:** naming, ribbon, títulos de janela, ícones e consistência visual.
> **Restrições respeitadas:** nenhuma funcionalidade modificada; nenhuma lógica de negócio alterada; apenas rótulos, organização e experiência.

## Princípios de naming

Nomes **claros, curtos, consistentes, orientados a resultado** e compreensíveis por usuários não técnicos. Palavras banidas em rótulos visíveis: *Tool, Manager, Utility, Process, Execute, Auto, Plugin, Addin, Module*.

Regra estrutural: **rótulo comercial ≠ identificador técnico.** Os `Id` de comando, nomes de classe e namespaces são contrato técnico estável e não acompanham renomeações de marketing. Renomear um botão custa 1 linha de XAML; renomear um `Id` quebraria ícones, dispatch e atalhos.

---

## 1. Renaming Table

### Botões (aplicado)

| Rótulo anterior | Novo rótulo | Id técnico (inalterado) | Racional |
|---|---|---|---|
| ViewBuilder | **Smart Views** | `ViewBuilder` | Resultado ("views inteligentes"), não mecanismo; remove jargão "Builder" |
| ModelCleaner | **Model Cleanup** | `ModelCleaner` | Ação-resultado; elimina sufixo de agente "Cleaner" |
| ImageExporter | **Image Capture** | `ImageExporter` | Verbo de resultado familiar (captura); evita "Exporter" |
| Inspecionar Seleção | **Selection Inspector** | `SelectionInspector` | Alinha idioma da suíte; nome de ferramenta reconhecível |
| Atributos | **Property Explorer** | `NativeAttrLab` | "Atributos" era vago; "Property Explorer" comunica exploração + edição de propriedades |
| Coloração de Sets | **Visual Sets** | `SetColoringRules` | Curto, orientado ao resultado visual |

### Outras superfícies renomeadas (aplicado)

| Superfície | Antes | Depois |
|---|---|---|
| Painéis do ribbon | Visualização · Modelo · Seleção e Atributos · Exportação | **Visualization · Selection · Data · Model** |
| `DisplayName` dos `[Command]` (aparece em customização do Navisworks) | nomes antigos mistos | = novo rótulo do botão |
| Nome da ferramenta em diálogos de erro do launcher | nomes antigos | = novo rótulo do botão |
| Headers internos das janelas (linha de 18px) | View Builder, Model Cleaner, Image Exporter, Inspeção da Seleção, Laboratório de Atributos, Regras de Coloração de Sets | = novo rótulo do botão |

### Relatório de inconsistências encontradas (estado anterior)

1. **Três idiomas misturados no mesmo ribbon** (ViewBuilder / Inspecionar Seleção / Coloração de Sets).
2. **CamelCase técnico exposto ao usuário** (ViewBuilder, ModelCleaner, ImageExporter).
3. **Títulos de janela em 3 formatos diferentes** ("X — descrição", "Marca - X", "X — descrição — Marca").
4. **Mesma ferramenta com nomes diferentes** por superfície (botão "Atributos", janela "Laboratório — Atributos Custom vs Nativos", header "Laboratório de Atributos").
5. Palavras banidas em uso: "Tool" (Auto ViewTool histórico), "Cleaner/Exporter/Builder" (sufixos de agente).

---

## 2. Ribbon Structure

Estrutura aplicada (tab **Navisworks Toolkit**), seguindo o padrão de grupos Visualization → Selection → Data → Model:

| Grupo | Comandos atuais | Reservado para futuro* |
|---|---|---|
| **Visualization** | Smart Views · Visual Sets · Image Capture | — |
| **Selection** | Selection Inspector | Search Sets · Filters |
| **Data** | Property Explorer | Object Data · Reports |
| **Model** | Model Cleanup | Validation · Review |
| **Tools*** | — | Settings · Logs · Updates |

\* Comandos reservados **não existem hoje** e não foram criados (restrição: não adicionar funcionalidade). O grupo *Tools* será criado apenas quando o primeiro desses comandos existir — não se cria painel vazio. Esta tabela fixa o "endereço" futuro de cada categoria para que a ribbon cresça sem reorganizações.

Racional do agrupamento: *Visualization* concentra o que muda o que se vê (views, cores, capturas); *Selection* o que responde "o que é isto que selecionei"; *Data* o que lê/escreve dados nos elementos; *Model* a saúde do modelo.

---

## 3. Icon Recommendations

**Estado atual:** os 6 ícones funcionam (16/32 px, PNG) mas vêm de duas famílias visuais distintas, e dois botões (Selection Inspector e Property Explorer) compartilham a mesma arte — herança dos projetos originais.

**Padrão-alvo** (estilo *Corporate Industrial*, referências Autodesk/Fluent/Power BI/ACC):

| Requisito | Especificação |
|---|---|
| Fonte de arte | **PNG** por ícone em `src/Resources/Icons/` (16/32 px; embutidos pelo build) |
| Rasterização | PNG 16×16, 32×32 e **64×64** (large ribbon em telas high-DPI), fundo transparente |
| Grade | 24×24 de desenho com 2px de margem de segurança; traço 1.5px; cantos arredondados raio 2 |
| Cores | Monocromático Ink `#292929` para estado normal; acento Azul `#2563EB` em no máximo 1 elemento por ícone; sem texto embutido; alto contraste sobre o cinza do ribbon |
| Nomenclatura | `<IdTécnico>_<tamanho>.png` (mantida — ex.: `ViewBuilder_32.png`) |

**Conceitos sugeridos por ícone:**

| Comando | Conceito visual |
|---|---|
| Smart Views | Câmera isométrica sobre cubo (viewpoint + 3D) |
| Visual Sets | Três camadas/folhas com uma colorida (override por set) |
| Image Capture | Moldura de foto com obturador |
| Selection Inspector | Cursor de seleção + lupa |
| Property Explorer | Etiqueta/tag com linhas de dados |
| Model Cleanup | Cubo com vassoura/faísca de limpeza |

> **Nota técnica:** o ribbon do Navisworks (`NWRibbonButton`) consome `ImageSource` bitmap — SVG não é suportado nativamente. O SVG é a **fonte de arte**; o pipeline rasteriza para PNG. A produção dos novos ícones é entrega de design (não foi feita nesta rodada para não substituir arte sem aprovação visual).

---

## 4. Window Titles

Formato permitido: **`{ToolName}`** (ou `{ToolName} Settings` para futuras janelas de configuração). Aplicado:

| Janela | Título anterior | Título novo |
|---|---|---|
| MainWindow | "View Builder — Criador de Viewpoints" | **Smart Views** |
| CleanupWindow | "Model Cleaner — Limpeza de Search Sets e Viewpoints" | **Model Cleanup** |
| ExportWindow | "Image Exporter — Exportar imagens de Viewpoints" | **Image Capture** |
| SelectionInspectorWindow | "Navisworks Toolkit - Inspeção da Seleção" | **Selection Inspector** |
| NativeAttrLabWindow | "Laboratório — Atributos Custom vs Nativos — Navisworks Toolkit" | **Property Explorer** |
| SetColoringRulesWindow | "Regras de Coloração de Sets — Navisworks Toolkit" | **Visual Sets** |

A descrição longa que antes vivia no título migrou de responsabilidade: continua disponível no **subtítulo do header interno** (12px, pt-BR), que foi **mantido** em todas as janelas.

---

## 5. UX Improvements

**Aplicado nesta rodada:**
1. Identidade 1:1 — botão, título de janela, header e diálogos de erro usam o **mesmo nome** para a mesma ferramenta (antes: até 3 nomes diferentes).
2. Idioma único nos rótulos de ferramenta (inglês comercial); descrições, tooltips e conteúdo das janelas permanecem em **pt-BR** (público-alvo), padrão comum em software profissional no Brasil.
3. Ribbon reorganizado por modelo mental do usuário (ver §2) em vez de "origem do código".
4. Títulos de janela curtos → legíveis na barra de tarefas e no Alt-Tab.

**Backlog recomendado (não aplicado — exigiria mudanças além de naming):**
1. Família única de ícones (ver §3) — maior ganho visual restante.
2. Unificar `Theme.xaml` + `DesignSystem.xaml` num só dicionário (mesma paleta, estilos divergentes) — janelas hoje têm pequenas diferenças de botão/checkbox entre as duas gerações.
3. Tooltips estendidos do ribbon com atalho e resultado ("Cria um viewpoint por Selection Set — sem abrir cada vista manualmente").
4. Janela "About/Settings" no futuro grupo *Tools* (versão, licença, link de suporte) — porta de entrada comercial.
5. Empty states orientados a ação nas listas (ex.: "Nenhum Selection Set no modelo. Crie sets em Home → Sets para usar o Smart Views").

---

## 6. Implementation Notes

**Arquivos alterados** (somente strings de UI — zero mudança de lógica, confirmado por build):

| Arquivo | O que mudou |
|---|---|
| `src/en-US/NavisworksToolkit.xaml` | `Text` dos 6 botões; títulos e ordem dos painéis (Visualization/Selection/Data/Model) |
| `src/Core/NavisworksToolkitPlugin.cs` | `DisplayName` dos 6 `[Command]`; nomes exibidos nos diálogos de erro do launcher |
| 6 × `src/Modules/**/*Window.xaml` | `Title` da janela + 1ª linha do header interno |

**Invariantes preservados (não tocar em renomeações futuras):**
- `Id` dos comandos no XAML do ribbon e no `ExecuteCommand` (contrato de dispatch).
- Nomes dos arquivos de ícone (`<IdTécnico>_16/32.png`) e o mecanismo `../Nome.png`.
- `[Plugin("NavisworksToolkit", "VRM")]` e `[RibbonTab("NavisworksToolkitTab")]` — identidade de registro no Navisworks.
- Tooltips em pt-BR; subtítulos descritivos dos headers em pt-BR.
- Classes, namespaces, x:Class — intactos.

**Processo para renomear um botão no futuro:** alterar apenas (1) `Text` no ribbon XAML, (2) `DisplayName` no `[Command]`, (3) `Title` + header da janela, (4) nome no launcher — e registrar a mudança na Renaming Table deste documento.
