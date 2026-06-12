# Assets — Navisworks Toolkit

Identidade visual consolidada dos dois projetos de origem. Nenhuma versão anterior foi apagada (originais preservados em `/Archive`).

## Estrutura e padrão

| Pasta | Conteúdo | Padrão |
|---|---|---|
| `logos/` | `Icone.png` (512×512, ícone de janela) e `logo_partners.png` (844×321, header das janelas) | Eram byte-idênticos nos dois projetos — mantida 1 cópia |
| `icons/` | Ícones de botão do ribbon | PNG, `<NomeDoComando>_<tamanho>.png`, tamanhos **16** e **32** px, PascalCase |
| `images/` | Arte-fonte em alta resolução | `<nome-original>_512.png` |

## Ícones do ribbon (6 ferramentas × 2 tamanhos)

| Comando | 16px | 32px | Origem da arte |
|---|---|---|---|
| ViewBuilder | `ViewBuilder_16.png` | `ViewBuilder_32.png` | Auto_ViewTool (original) |
| ModelCleaner | `ModelCleaner_16.png` | `ModelCleaner_32.png` | Auto_ViewTool (original) |
| ImageExporter | `ImageExporter_16.png` | `ImageExporter_32.png` | Auto_ViewTool (original) |
| SelectionInspector | `SelectionInspector_16.png` | `SelectionInspector_32.png` | Derivado de `ler_atributo_icon_512.png` |
| NativeAttrLab | `NativeAttrLab_16.png` | `NativeAttrLab_32.png` | Derivado de `ler_atributo_icon_512.png` |
| SetColoringRules | `SetColoringRules_16.png` | `SetColoringRules_32.png` | Derivado de `atributos_icon_512.png` |

> Os derivados 16/32 foram gerados em 2026-06-11 com redimensionamento bicúbico de alta qualidade.
> SelectionInspector e NativeAttrLab compartilham a mesma arte de origem (era assim no projeto original); criar artes distintas é melhoria sugerida no MigrationReport.

## Paleta oficial (5 cores — variações somente por opacidade)

| Token | Hex |
|---|---|
| Ink | `#292929` |
| Gray | `#585757` |
| Cream | `#F6F6F6` |
| Orange | `#FC6A0A` |
| Deep Orange | `#E74504` |
