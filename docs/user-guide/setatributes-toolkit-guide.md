# Construct Sync Toolkit

Plugin para **Autodesk Navisworks Simulate 2026** que permite inspecionar propriedades BIM, gravar atributos customizados, aplicar coloração por Selection Set e gerenciar a timeline de projeto diretamente no ambiente Navisworks.

---

## Funcionalidades

### Inspecionar Seleção

Inspeciona e exporta propriedades dos elementos selecionados em formato tabular.

- Filtro interativo por categorias de propriedades via checkboxes
- Grade dinâmica com cabeçalho em dois níveis (Categoria / Propriedade)
- Exportação para **CSV** com separador `;` e BOM UTF-8 (compatível com Excel)
- Exportação para **Excel XML** (`.xml` — abre diretamente no Excel com formatação de células)
- Cópia para área de transferência em formato **TSV** (para colar em planilhas)
- Processamento em background para não travar a interface

---

### Atributos Customizados

Escreve atributos customizados nos elementos selecionados, com detecção de Selection Sets e validação via COM API.

O fluxo é guiado por quatro seções na interface:

**1 — Atributos Nativos**
- Árvore de categorias e propriedades lidas diretamente da seleção atual
- Importa qualquer categoria nativa para o laboratório com um clique (preenche nome e valores)

**2 — Nome da categoria custom**
- Campo livre para definir a categoria que receberá os atributos
- Preenchido automaticamente ao importar uma categoria nativa

**3 — Selection Sets detectados**
- Lista os sets aos quais os elementos selecionados pertencem, com contagem de elementos por set
- Checkboxes para escolher quais sets serão gravados como atributo `_sets` na categoria
- Seleção persiste entre sessões: na próxima abertura, os sets previamente gravados são marcados automaticamente
- Botões para selecionar todos, limpar e recarregar

**4 — Atributos extras a gravar**
- Grade editável com colunas Nome / Valor / Tipo (`string`, `int`, `double`, `boolean`)
- Linhas adicionadas manualmente ou importadas de CSV/XML
- Suporte a importação e exportação de templates de atributos (`.csv` e `.xml`)

**Gravação:**
- Grava categoria customizada via COM API com merge: preserva atributos existentes, atualiza apenas os editados
- Inclui os sets selecionados como propriedade composta (`nome_a | nome_b | ...`)
- Log de verificação exibe o estado antes e depois: flags UserDefined vs Native, props por categoria, coexistência de nomes iguais

**Exclusão:**
- Remove a categoria customizada (e categorias legadas) de todos os elementos selecionados

---

### Regras de Coloração de Sets

Aplica substituições de cor aos elementos do modelo baseadas em Selection Sets.

- Carrega todos os Selection Sets do documento ativo
- **Auto-seleção inteligente:** se houver elementos selecionados na cena, os sets que os contêm são marcados automaticamente como habilitados
- Grade editável: cor (color picker), transparência (0–100%) e ativo/inativo por set
- Importação e exportação de regras em formato XML
- Aplica overrides de cor e transparência via API do Navisworks
- Remove todas as substituições com um clique

**Formato XML de regras:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<ColoringRules version="1.0">
  <Rule SelectionSet="Estrutura"  Color="#FF0000" Transparency="0"  Enabled="true"/>
  <Rule SelectionSet="Hidraulica" Color="#0000FF" Transparency="30" Enabled="true"/>
</ColoringRules>
```

---

## Ribbon

| Painel | Botões |
|---|---|
| **Leitura e Atributos** | Inspecionar Seleção · Atributos Customizados |
| **Coloração de Sets** | Regras de Coloração |

---

## Tecnologias

| Camada | Tecnologia |
|---|---|
| Linguagem | C# (.NET Framework 4.8) |
| Interface | WPF + XAML (padrão MVVM) |
| API BIM | Autodesk Navisworks API 2026 |
| COM Bridge | `Autodesk.Navisworks.Interop.ComApi` |
| Build | MSBuild (SDK-style csproj) |
| Plataforma | Windows x64 |

---

## Estrutura do Projeto

```
SetAtributesToolkit 0.1.8/
├── Plugin/
│   ├── Construct Sync Toolkit.cs      # 3 classes AddInPlugin (entry points)
│   └── PluginHelpers.cs               # Abertura segura de janelas WPF no host Win32
├── Views/
│   ├── SelectionInspectorWindow.xaml(.cs)
│   ├── NativeAttrLabWindow.xaml(.cs)
│   └── SetColoringRulesWindow.xaml(.cs)
├── ViewModels/
│   └── SetColoringRulesViewModel.cs
├── Services/
│   └── SetColoringService.cs
├── Models/
│   ├── AttributeEntry.cs
│   ├── CheckableItem.cs
│   ├── ColoringRule.cs
│   ├── InspectorModels.cs             # ColDef, BuildResult, ColumnHeaderData
│   ├── LabCatNode.cs
│   ├── LabPropNode.cs
│   └── SelectionSetItem.cs
├── Helpers/
│   ├── AsyncRelayCommand.cs           # ICommand assíncrono para ViewModels
│   ├── ExceptionHelper.cs             # UnwrapMessage centralizado
│   ├── PropertyExtractionHelper.cs    # Extração de propriedades e selection sets
│   └── SelectionSetCache.cs           # Cache tipado de selection/search sets
├── Themes/
│   └── DesignSystem.xaml              # Tokens de cor e estilos globais (WPF)
├── Resources/
│   ├── atributos_icon.png
│   └── ler_atributo_icon.png
├── Docs/
│   └── Figma_UI_Brief.md
├── SetAtributesToolkit.addin          # Manifesto do plugin
└── SetAtributesToolkit.xaml           # Definição do ribbon
```

---

## Instalação

### Pré-requisitos
- Autodesk Navisworks Simulate 2026
- .NET Framework 4.8
- Visual Studio 2022

### Compilar e instalar

1. Abra `SetAtributesToolkit.sln` no Visual Studio 2022
2. Compile em `Release | x64`
3. Os arquivos são copiados automaticamente para:
   ```
   %AppData%\Autodesk\Navisworks Simulate 2026\Plugins\SetAtributesToolkit\
   ```
4. Reinicie o Navisworks — o tab **"Construct Sync"** aparecerá no ribbon com os 2 painéis

> As referências ao Navisworks API são resolvidas via path local (`C:\Arquivos de Programas\Autodesk\Navisworks Simulate 2026\`). Certifique-se de ter o Navisworks 2026 instalado antes de compilar.

---

## Licença

Projeto proprietário — todos os direitos reservados.
