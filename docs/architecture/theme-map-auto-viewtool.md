# Theme Map — AutoViewTool Plugin WPF

> Documento de referência para reusar o tema em outros projetos WPF do mesmo ecossistema.
> Fonte canônica: `src/UI/Theme.xaml`. A `MainWindow.xaml` ainda carrega os recursos inline
> (não migrada por estabilidade). As demais janelas usam `MergedDictionaries`.

---

## 1. Identidade Visual

| Atributo       | Valor                                     |
|----------------|-------------------------------------------|
| Estilo         | Dark Chrome + Card Claro (2 superfícies)  |
| Personalidade  | Industrial, técnico, profissional         |
| Contexto       | Plugin BIM / desktop engineering tool     |
| Conformidade   | WCAG AA — contraste ≥ 4.5:1 em todos os pares verificados |

---

## 2. Paleta (5 cores fixas — sem 6ª cor)

| Token        | Hex       | Papel                                  |
|--------------|-----------|----------------------------------------|
| **Ink**      | `#292929` | Janela, header, footer (chrome escuro) |
| **Gray**     | `#585757` | Divisores, bordas, texto secundário    |
| **Cream**    | `#F6F6F6` | Cards, fundo de lista (claro)          |
| **Orange**   | `#FC6A0A` | CTA, acento, seleção, foco             |
| **Deep Orange** | `#E74504` | Hover CTA, ações de perigo          |

> Variações de tom são obtidas exclusivamente por **opacidade** das 5 cores acima.

### Mapa completo de brushes

| Resource Key   | Color    | Opacity | Uso                                          |
|----------------|----------|---------|----------------------------------------------|
| `Bg`           | `#292929`| 1.0     | Fundo da janela                              |
| `Chrome`       | `#292929`| 1.0     | Header e footer                              |
| `Card`         | `#F6F6F6`| 1.0     | Fundo de ListView / card claro               |
| `HeaderBg`     | `#585757`| 0.10    | Cabeçalho de colunas da GridView             |
| `Line`         | `#585757`| 1.0     | Bordas estruturais (header/footer border)    |
| `RowLine`      | `#585757`| 0.22    | Linha divisória entre rows                   |
| `SearchBg`     | `#585757`| 0.18    | Fundo do input de busca / campo de texto     |
| `Text`         | `#292929`| 1.0     | Texto principal sobre superfície clara       |
| `Muted`        | `#585757`| 1.0     | Texto secundário sobre superfície clara      |
| `TextLight`    | `#F6F6F6`| 1.0     | Texto principal sobre superfície escura      |
| `MutedLight`   | `#F6F6F6`| 0.70    | Texto secundário / placeholder sobre escuro  |
| `Primary`      | `#FC6A0A`| 1.0     | Acento laranja: CTA, foco, ícone de status   |
| `Slate`        | `#585757`| 1.0     | Botão secundário (rest)                      |
| `SlateHover`   | `#F6F6F6`| 1.0     | Botão secundário (hover) — inverte para cream|
| `SlatePress`   | `#1F1F1F`| 1.0     | Botão secundário (press)                     |
| `SlateBorder`  | `#585757`| 1.0     | Borda de checkbox                            |
| `Cta`          | `#FC6A0A`| 1.0     | Botão CTA (rest)                             |
| `CtaHover`     | `#E74504`| 1.0     | Botão CTA (hover)                            |
| `CtaPress`     | `#E74504`| 1.0     | Botão CTA (press)                            |
| `Danger`       | `#E74504`| 1.0     | Botão destrutivo (rest)                      |
| `DangerHover`  | `#C53A03`| 1.0     | Botão destrutivo (hover) — escurecimento     |
| `RowHover`     | `#FC6A0A`| 0.14    | Highlight de row ao hover                    |
| `RowSelect`    | `#FC6A0A`| 0.26    | Highlight de row selecionada                 |

---

## 3. Tipografia

| Atributo         | Valor                                        |
|------------------|----------------------------------------------|
| Font stack       | `Segoe UI, San Francisco PRO, Helvetica Neue`|
| Monospace        | `Cascadia Mono, Consolas`                    |
| Título (header)  | 18px Bold, `TextLight`                       |
| Subtítulo        | 12px Regular, `MutedLight`                   |
| Rótulos (CAPS)   | 11px SemiBold, `MutedLight`                  |
| Botões           | 13px SemiBold / Bold (CTA)                   |
| Dados / stats    | 12px Monospace, `TextLight` ou `Primary`     |
| Texto de corpo   | 13px Regular, `Text` (sobre card) / `TextLight` (sobre chrome) |

---

## 4. Geometria e Espaçamento

| Elemento          | Valor             |
|-------------------|-------------------|
| Border radius padrão | `6px` (botões, inputs, cards) |
| Border radius pequeno | `4px` (checkbox, progress bar) |
| Border radius circular | `11px` (botão ClearBtn) |
| Padding botão     | `12,9`            |
| Padding header/footer | `20,14` / `20,12` |
| Padding content   | `20` (todos os lados) |
| Altura do input   | `36px` (busca), `34px` (FieldBox) |
| Altura do progress bar | `6px`        |
| Checkbox          | `18×18px`         |
| Barra de acento (header) | `4px` largura, CornerRadius `2` |
| Separador horizontal | `Height="1"`, `Line` brush |

---

## 5. Estrutura de Layout (3 linhas)

```
┌─────────────────────────────────────────┐  ← Header (Auto)
│  [accent bar] [Título / Subtítulo] [Logo]│    Background=Chrome
│                                         │    BorderBrush=Line (bottom)
├─────────────────────────────────────────┤
│  Content area (*)                        │    Margin="20"
│  ┌────────────────────┐ ┌─────────────┐ │
│  │ [label CAPS]        │ │ [label CAPS]│ │
│  │ [SearchBox]         │ │ [BtnSecond.]│ │
│  │ [Card / ListView *] │ │ [BtnCta]    │ │
│  └────────────────────┘ │ [Stats mono]│ │
│                          └─────────────┘ │
├─────────────────────────────────────────┤  ← Footer (Auto)
│  [● status] [mensagem]       [XX%]       │    Background=Chrome
│  [ProgressBar 6px]                       │    BorderBrush=Line (top)
└─────────────────────────────────────────┘
```

---

## 6. Componentes e Estilos

### Botões

| Key             | Rest BG    | Rest FG    | Hover BG   | Hover FG   | Press BG   |
|-----------------|------------|------------|------------|------------|------------|
| `BtnBase`       | (binding)  | (binding)  | —          | —          | —          |
| `BtnSecondary`  | `Slate`    | `TextLight`| `SlateHover`| `Text`   | `SlatePress`|
| `BtnCta`        | `Cta`      | `#292929`  | `CtaHover` | `#F6F6F6`  | `CtaPress` |
| `BtnDanger`     | `Danger`   | `#F6F6F6`  | `DangerHover`| —       | `DangerHover`|

Todos: CornerRadius=6, BorderThickness=0, Cursor=Hand, FontSize=13, disabled Opacity=0.45.

### Inputs

| Key         | Fundo       | Borda rest  | Borda foco | Texto      |
|-------------|-------------|-------------|------------|------------|
| `SearchBox` | `SearchBg`  | `Line` 1.5  | `Primary`  | `TextLight`|
| `FieldBox`  | `SearchBg`  | `Line` 1.5  | `Primary`  | `TextLight`|

Ambos: CornerRadius=6, CaretBrush=Primary, SelectionBrush=Primary.

`SearchBox` inclui ícone de lupa (Path) e placeholder colapsável.

### ListView / GridView

- Container: `Card` background, CornerRadius=8, `Line` border 1px
- Header: `HeaderBg` + `RowLine` border-bottom/right, Foreground=`Muted`, 11px SemiBold
- Row: Foreground=`Text`, hover=`RowHover`, selected=`RowSelect`, divisor=`RowLine` (bottom)

### Checkbox (`ChkCell`)

- Rest: BG=`Card`, border=`SlateBorder` 1.5px
- Checked: BG=`Primary`, border=`Primary`, checkmark stroke=`#292929`
- Indeterminate: BG=`Primary`, dash fill=`#292929`
- Hover: border=`Primary`

### ProgressBar (`ProgressDark`)

- Track: CornerRadius=4, BG=`SearchBg`
- Fill: BG=`Primary`, CornerRadius=4, HorizontalAlignment=Left

---

## 7. Padrões de Uso por Superfície

| Superfície   | Background | Texto principal | Texto secundário | Acento  |
|--------------|------------|-----------------|------------------|---------|
| Chrome (dark)| `#292929`  | `TextLight`     | `MutedLight`     | `Primary` |
| Card (light) | `#F6F6F6`  | `Text`          | `Muted`          | `Primary` |

> **Regra de ouro**: nunca use texto claro sobre card claro, nem texto escuro sobre chrome.

---

## 8. Como Aplicar em Outro Projeto WPF

### Passo 1 — Copiar o arquivo de tema
```
src/UI/Theme.xaml  →  SeuProjeto/UI/Theme.xaml
```
Ajuste o assembly name no Source se necessário.

### Passo 2 — Referenciar no App.xaml (global) ou por janela
```xml
<!-- App.xaml (global) -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="UI/Theme.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>

<!-- Ou por janela -->
<Window.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="/SeuAssembly;component/UI/Theme.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Window.Resources>
```

### Passo 3 — Configurar a Window raiz
```xml
<Window Background="#292929"
        FontFamily="Segoe UI, San Francisco PRO, Helvetica Neue"
        UseLayoutRounding="True"
        TextOptions.TextFormattingMode="Ideal">
```

### Passo 4 — Usar os tokens nos seus controles
```xml
<!-- Botão de ação principal -->
<Button Content="Executar" Style="{StaticResource BtnCta}"/>

<!-- Botão secundário -->
<Button Content="Cancelar" Style="{StaticResource BtnSecondary}"/>

<!-- Ação destrutiva -->
<Button Content="Excluir tudo" Style="{StaticResource BtnDanger}"/>

<!-- Card claro com lista -->
<Border Background="{StaticResource Card}" CornerRadius="8"
        BorderBrush="{StaticResource Line}" BorderThickness="1">
    ...
</Border>

<!-- Rótulo de seção em caps -->
<TextBlock Text="SEÇÃO" FontWeight="SemiBold" FontSize="11"
           Foreground="{StaticResource MutedLight}"/>

<!-- Separador horizontal -->
<Border Height="1" Background="{StaticResource Line}" Margin="0,8"/>
```

### Passo 5 — Adicionar estilos específicos da janela (se necessário)
Janelas novas podem adicionar estilos locais logo após o MergedDictionaries,
como `FieldLabel`, `FieldBox`, `CheckOption` (ver ExportWindow.xaml como exemplo).

---

## 9. Anti-padrões a Evitar

- ❌ Introduzir uma 6ª cor — use opacidade das 5 existentes
- ❌ Texto `#F6F6F6` sobre card `#F6F6F6` — contraste 1:1
- ❌ Texto `#292929` sobre chrome `#292929` — contraste 1:1
- ❌ Laranja como fundo de texto de corpo — reservado para acento/CTA
- ❌ `RowHover` / `RowSelect` sobre chrome escuro — tints são para superfícies claras
- ❌ Bordas visíveis em botões — `BorderThickness="0"` é a regra base
