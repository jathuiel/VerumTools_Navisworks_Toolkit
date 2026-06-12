# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## O que é

Plugin WPF para **Autodesk Navisworks Simulate 2026** (.NET Framework 4.8, x64) com 3 ferramentas expostas no ribbon "Construct Sync": Inspecionar Seleção, Laboratório de Atributos Nativos (gravação de atributos customizados) e Regras de Coloração de Sets.

Atenção à nomenclatura: o csproj/sln se chamam `Construct Sync Toolkit`, mas o **AssemblyName é `SetAtributesToolkit`** — e esse nome amarra todo o registro do plugin (ver abaixo). Código, UI, comentários e mensagens de commit são em **português (pt-BR)**; commits usam prefixos convencionais (`feat:`, `fix:`, `refactor:`, `chore:`).

## Build e deploy

```powershell
dotnet build "Construct Sync Toolkit.csproj" -c Release
```

Ou abrir `Construct Sync Toolkit.sln` no Visual Studio 2022. Não há testes nem lint — a única verificação automatizada é compilar; a validação funcional é manual, dentro do Navisworks.

- **Build = instalação**: o `OutputPath` aponta direto para `%AppData%\Autodesk\Navisworks Simulate 2026\Plugins\SetAtributesToolkit\`. O target `CopyAddin` copia o `.addin` junto.
- Plugins não recarregam a quente: é preciso **reiniciar o Navisworks** após cada build. Com o Navisworks aberto, o build pode falhar ao copiar a DLL (arquivo bloqueado).
- As referências da API (`Autodesk.Navisworks.Api`, `AdWindows`, ComApi/Interop.ComApi) são resolvidas por HintPath local em `C:\Arquivos de Programas\Autodesk\Navisworks Simulate 2026\` (Windows pt-BR), com `Private=false` — exigem o Navisworks 2026 instalado na máquina.
- O csproj fixa `PlatformTarget=x64`; a configuração "Any CPU" da sln ainda produz x64.
- Instalador para distribuição: `Build/Script para gerar instalador.iss` (Inno Setup), que instala em `%ProgramData%\Autodesk\Navisworks <SKU>\Plugins\`.

## Fluxo de desenvolvimento (spec-driven)

Features novas seguem o fluxo em 3 fases documentado em `specs/README.md`, com limpeza de contexto (`/clear`) entre elas:

1. `/prd <descrição>` — pesquisa: mapeia arquivos impactados, padrões a reaproveitar e docs externas → `specs/NNN-slug/prd.md`
2. `/spec NNN` — especificação: consome **apenas** o prd e produz o plano técnico definitivo → `specs/NNN-slug/spec.md`
3. `/implement NNN` — implementação: executa **apenas** o spec, arquivo por arquivo

Regras de ouro: cada fase produz só o seu artefato; a implementação não toca arquivos fora da tabela do spec nem refatora lógica validada de carona; divergência encontrada na implementação volta para o `spec.md` antes de virar código.

## Arquitetura

### Cadeia de registro do plugin (3 arquivos acoplados por nome)

Cada ferramenta existe em três lugares que precisam concordar:

1. `Plugin/Construct Sync Toolkit.cs` — classe `AddInPlugin` com `[Plugin("SetAtributesToolkit.<Nome>", "JCA", ...)]`; o `Execute` apenas chama `ToolLauncher<TJanela>.Launch()`.
2. `SetAtributesToolkit.addin` — entrada `<Plugin>` com o mesmo `Id`, `ClassName` e `FileName=SetAtributesToolkit`.
3. `SetAtributesToolkit.xaml` — botão do ribbon cujo `Id` é o mesmo do atributo `[Plugin]`.

O ribbon XAML **deve ter o mesmo nome do AssemblyName** — por isso o csproj o remove de `<Page>` e o inclui como `<Content>` copiado ao output. Adicionar uma ferramenta nova = tocar esses 3 arquivos.

### Abertura de janelas

`ToolLauncher<T>` (`Plugin/PluginHelpers.cs`) abre cada janela como modeless singleton dentro do host Win32 do Navisworks: instância única por tipo (reativa se já aberta), `WindowInteropHelper.Owner` no MainWindow do processo e `ElementHost.EnableModelessKeyboardInterop` para teclado funcionar. Janelas novas devem ser abertas por ele, não com `new Window().Show()`.

### Duas gerações de UI

- **MVVM** (padrão para código novo): `SetColoringRulesWindow` usa ViewModel (`INotifyPropertyChanged` + `AsyncRelayCommand`) delegando a um Service.
- **Code-behind**: `SelectionInspectorWindow` e `NativeAttrLabWindow` concentram a lógica no `.xaml.cs`, usando os helpers/services compartilhados.

### Acesso defensivo à API do Navisworks (reflexão intencional)

A superfície da API varia entre versões/SKUs do Navisworks, então vários pontos descobrem tipos e métodos em runtime — não "simplifique" isso para chamadas diretas:

- `ComBridgeHelper` (`Services/AtributoService.cs`) localiza o `ComApiBridge` em namespaces candidatos.
- `SetColoringService` testa listas de nomes candidatos para os métodos de override de aparência.
- `PropertyExtractionHelper.LoadSelectionSetsViaReflection` tem fast path tipado (via `SelectionSetCache`) e fallback de reflexão; search sets são materializados com `Search.FindAll`.

### Gravação de atributos customizados (COM API)

Fluxo em `AtributoService.GravarAtributos`: `ComApiBridge.State` → `BeginEdit` → por item: `ToInwOpSelection` → `GetGUIPropertyNode(path, true)` → remover categorias relacionadas → montar `InwOaPropertyVec` → `SetUserDefined(0, categoria, internalName, propVec)` → `EndEdit`.

Pontos não óbvios:

- `SetUserDefined` **substitui a categoria inteira**; o "merge" (preservar atributos existentes) é responsabilidade da janela, que lê o estado atual e regrava tudo (ver `NativeAttrLabWindow`).
- Antes de gravar, `RemoverCategoriasRelacionadas` apaga a categoria-alvo **e todas as legadas** para evitar duplicatas.
- Nomes internos de propriedade são sanitizados e desduplicados (`<nome>_prop`, `<nome>_2_prop`...).

### VerumSchema — fonte única de nomes

`Helpers/VerumSchema.cs` centraliza a categoria custom (`Verum_Attributes`), a propriedade de sets (`Verum_AWP`) e as listas de nomes **legados** (`Autis_*`, `AWP_*`, `Sets`) que ainda são reconhecidos na leitura e removidos na regravação. Qualquer renomeação de schema passa por este arquivo, mantendo os nomes antigos na lista de legados para migração de modelos existentes.

### Design System

`Themes/DesignSystem.xaml` define os tokens (5 cores fixas — Ink `#292929`, Gray `#585757`, Cream `#F6F6F6`, Orange `#FC6A0A`, Deep Orange `#E74504` — variações **somente por opacidade**) e os estilos globais (`BtnCta`, `BtnSecondary`, `BtnDanger`, `SearchBox`, `FieldBox`, `ChkCell`, `ProgressDark`). Cada janela importa via:

```xml
<ResourceDictionary Source="/SetAtributesToolkit;component/Themes/DesignSystem.xaml"/>
```

Regras ao mexer em XAML de UI: layout em 3 linhas (header chrome / content / footer) e anti-padrões — nunca introduzir 6ª cor; nunca texto claro sobre card claro nem escuro sobre chrome; laranja só como acento/CTA; `RowHover`/`RowSelect` só em superfícies claras; botões sem borda.