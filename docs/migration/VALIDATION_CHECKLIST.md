# Checklist de Validação Final — Navisworks Toolkit v2.0.0

> Gerado em 2026-06-11 (ETAPA 10 da consolidação).
> Parte A executada automaticamente nesta máquina; Parte B requer o Navisworks aberto (plugins não têm teste automatizado — validação funcional é manual, como nos projetos originais).

## Parte A — Verificações automatizadas ✅ (todas PASS)

| # | Verificação | Resultado |
|---|---|---|
| 1 | Compilação Release sem erros e **sem warnings** (`dotnet build`, clean build) | ✅ PASS |
| 2 | Referências resolvidas (API Navisworks 2026 via HintPath verificado na máquina) | ✅ PASS |
| 3 | `NavisworksToolkit.dll` gerada (x64, net48, versão 2.0.0) | ✅ PASS |
| 4 | Ribbon `en-US\NavisworksToolkit.xaml` copiada ao output | ✅ PASS |
| 5 | 12 ícones (6 comandos × 16/32 px) copiados à raiz do output | ✅ PASS |
| 6 | Cada um dos 6 comandos: atributo `[Command]` + `case` no `ExecuteCommand` + botão na ribbon + par de ícones | ✅ PASS |
| 7 | 51 tipos presentes na DLL nos namespaces `NavisworksToolkit.{Core,Shared,Modules.*}` | ✅ PASS |
| 8 | Recursos embutidos: `assets/icone.png`, `assets/logo_partners.png` + 8 BAMLs (6 janelas + 2 temas) | ✅ PASS |
| 9 | Pack URIs atualizados (temas e branding apontam para `NavisworksToolkit`) | ✅ PASS |
| 10 | Backup íntegro: `Archive/ProjectA` (117 arquivos) e `Archive/ProjectB` (449 arquivos) — bytes idênticos às origens | ✅ PASS |
| 11 | Projetos originais intactos (98 e 50 arquivos relevantes, inalterados) | ✅ PASS |
| 12 | Nenhum arquivo excluído em todo o processo | ✅ PASS |

## Parte B — Validação manual no Navisworks ⬜ (pendente)

Instalação: `./deploy.ps1` como Administrador, com o Navisworks fechado; depois abrir o Navisworks Simulate 2026.

| # | Verificação | Como testar |
|---|---|---|
| 1 | ⬜ Tab **"Navisworks Toolkit"** aparece no ribbon | Abrir o Navisworks e localizar a tab |
| 2 | ⬜ 4 painéis visíveis: Visualização · Modelo · Seleção e Atributos · Exportação | Inspeção visual |
| 3 | ⬜ 6 botões com ícones carregados (sem placeholder em branco) | Inspeção visual |
| 4 | ⬜ **ViewBuilder** abre, lista Selection Sets e cria viewpoints isométricos | Abrir modelo com sets → marcar → Criar Viewpoints |
| 5 | ⬜ Template Excel: gerar `.xlsx` e importar de volta | Botões "Gerar modelo" / "Importar template" |
| 6 | ⬜ **ModelCleaner** lista sets/viewpoints e remove os selecionados (com confirmação) | Marcar item de teste → remover → Ctrl+Z restaura |
| 7 | ⬜ **ImageExporter** exporta JPG com resolução/qualidade/prefixo/markups | Selecionar viewpoints → exportar para pasta de teste |
| 8 | ⬜ **Inspecionar Seleção** monta a grade de propriedades e exporta CSV/Excel XML/TSV | Selecionar elementos → abrir → exportar |
| 9 | ⬜ **Atributos** lê categorias nativas, grava categoria custom (merge) e remove | Selecionar elementos → importar categoria → gravar → verificar no Properties |
| 10 | ⬜ Sets gravados anteriormente (schema `Verum_Attributes`/legados) são reconhecidos | Abrir modelo já atributado pela versão antiga |
| 11 | ⬜ **Coloração de Sets** aplica e remove overrides de cor/transparência; import/export XML | Habilitar regra → Aplicar → Remover tudo |
| 12 | ⬜ Janelas reativam (não duplicam) ao clicar de novo no botão | Clicar 2× no mesmo botão |
| 13 | ⬜ Teclado funciona nas janelas (digitação em campos de busca) | Digitar nos campos |
| 14 | ⬜ Ícone e logo aparecem nas janelas do ex-Auto_ViewTool | Inspeção visual (header/título) |
| 15 | ⬜ Plugins antigos desinstalados (sem tabs "Verum Toolkit"/"Construct Sync" duplicadas) | Remover pastas antigas de `Plugins\` se existirem |

## Ambiente de referência

- Windows 11 Pro · Navisworks Simulate 2026 (2025 e 2027 também presentes)
- .NET Framework 4.8 · dotnet CLI + MSBuild VS2022 Community
- HintPath: `C:\Program Files\Autodesk\Navisworks Simulate 2026\`
