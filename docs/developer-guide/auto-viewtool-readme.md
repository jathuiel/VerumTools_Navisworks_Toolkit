# Auto ViewTool Creator

Suíte de plugins para Navisworks 2026 que automatiza o trabalho com **Viewpoints** e
**Search Sets**: criação isométrica em lote, limpeza de elementos indesejados e exportação
de imagens. Tudo numa única DLL que expõe **três botões** na faixa *Add-Ins*.

> Versão atual: **v1.0.3** · .NET Framework 4.8 · WPF · Navisworks 2026 (Manage/Simulate)

## Ferramentas (3 plugins)

| Botão (ribbon) | O que faz |
|---|---|
| **Auto ViewTool** | Cria viewpoints isométricos a partir de Selection Sets (lote + template Excel) |
| **Auto Cleanup** | Remove Search Sets e Viewpoints indesejados do modelo |
| **Auto Export** | Exporta imagens JPG dos Viewpoints (resolução/qualidade/markups) |

## Arquitetura

Três plugins independentes numa única DLL (`AutoViewTool.dll`), descobertos pelos
atributos `[Plugin]` + `[AddInPlugin]` (classe base **`AddInPlugin`**):

```
src/
├── AddIn.cs                    # Plugin 1: Auto ViewTool  (criação de viewpoints)
├── CleanupAddIn.cs             # Plugin 2: Auto Cleanup   (limpeza)
├── ExportAddIn.cs              # Plugin 3: Auto Export     (exportação de imagens)
│
├── Core/
│   ├── NavisworksInterop.cs    # Wrapper robusto da API Navisworks
│   ├── SelectionSetManager.cs  # Leitura e gerenciamento de selection sets
│   ├── IsolationHandler.cs     # Isolamento hierárquico otimizado
│   ├── ViewpointManager.cs     # Criação com câmera isométrica (lê o bbox do set)
│   ├── CleanupManager.cs       # Enumeração e remoção de sets/viewpoints
│   ├── ExportManager.cs        # Render de viewpoints → JPG (GenerateImage)
│   ├── TemplateManager.cs      # Geração/importação de templates Excel
│   ├── XlsxFile.cs             # Leitor/escritor XLSX (sem dependências externas)
│   └── PerfLog.cs              # Logger de diagnóstico (%TEMP%\AutoViewTool_perf.log)
│
├── UI/
│   ├── MainWindow.xaml(.cs)    # Interface: criação de viewpoints
│   ├── CleanupWindow.xaml(.cs) # Interface: limpeza
│   ├── ExportWindow.xaml(.cs)  # Interface: exportação de imagens
│   └── Theme.xaml              # Estilos WPF compartilhados (paleta de 5 cores)
│
└── Models/
    ├── SelectionSetData.cs     # DTO de selection set (ItemCount, IsSelected)
    ├── ViewpointData.cs        # DTO de viewpoint (metadados)
    ├── ViewpointTemplateRow.cs # DTO de linha do template Excel
    ├── CleanupItemData.cs      # DTO de item de limpeza (caminho + tipo)
    └── ExportItemData.cs       # DTO de viewpoint exportável (duplicata)

assets/        Icone.png, logo_partners.png   (embutidos na DLL como Resource)
templates/     AutoViewTool_Template.xlsx
```

## Funcionalidades

### Plugin 1 — Auto ViewTool (criação)

- **Leitura de Selection Sets**: enumera recursivamente (hierarquia de pastas), rejeita sets vazios, mostra contagem de itens.
- **Isolamento otimizado**: esconde apenas os "irmãos" fora do caminho do set (visibilidade hierárquica), numa única chamada `SetHidden()` dentro de uma transação (1 redraw, 1 undo). Sem reexibição → rápido mesmo em modelos grandes/achatados.
- **Câmera isométrica**: projeção ortográfica orientada pelo eixo "up" do modelo; lê o **centro do bounding box** do set e enquadra com `ZoomBox` (sem seleção nem animação).
- **Criação em lote**: seleção múltipla via checkbox + estimativa de tempo (calibrada em runtime). Nome automático `VP_<NomeDoSet>_<timestamp>`.
- **Template Excel**: gera um `.xlsx` (com os sets atuais como exemplo) e importa nomes/descrições customizados, casando por nome do set.
- **Busca em tempo real** e **branding** (ícone + logo embutidos).

### Plugin 2 — Auto Cleanup (limpeza)

- **Duas listas** lado a lado: Search/Selection Sets (com tipo *Busca*/*Explícito*) e Viewpoints, com caminho hierárquico.
- **Remoção seletiva** (checkbox + busca) ou **"Limpar tudo"**, sempre com confirmação (operação destrutiva, em geral reversível com `Ctrl+Z`).
- A seleção persiste mesmo com o filtro de busca ativo.

### Plugin 3 — Auto Export (exportação de imagens)

- Lista os Viewpoints (busca + selecionar-todos), **sinalizando duplicatas** (nomes repetidos).
- Exporta **JPG** dos selecionados via `View.GenerateImage`, aplicando cada viewpoint (`CurrentSavedViewpoint`) antes de renderizar.
- Opções: **pasta de destino**, **resolução** (px), **qualidade JPEG** (0–100), **prefixo/sufixo** no nome e **incluir markups** (overlays via `ScenePlusOverlay`).
- Barra de progresso por imagem; ao final, oferece abrir a pasta.

## Compilação

**Pré-requisitos:** Visual Studio 2022+, .NET Framework 4.8, Navisworks 2026 (Manage ou Simulate) instalado. Os `HintPath` no `.csproj` apontam para `C:\Program Files\Autodesk\Navisworks Simulate 2026\`.

```powershell
# Script (acha o MSBuild do VS2022 automaticamente)
./build.ps1 -Configuration Release

# ou MSBuild direto
msbuild Auto_ViewTool.csproj /p:Configuration=Release
```
Saída: `bin\Release\AutoViewTool.dll`.

## Instalação no Navisworks

> ⚠️ O Navisworks só carrega um plugin .NET se a DLL estiver numa **subpasta de `Plugins\` com o mesmo nome do arquivo** (`Plugins\AutoViewTool\AutoViewTool.dll`).

**Opção A — Deploy automático (recomendado para desenvolvimento):**
```powershell
# Compila em Release e copia para a subpasta correta (rode como Administrador)
./deploy.ps1                       # Navisworks Simulate 2026
./deploy.ps1 -NavisVersion 2026 -IncludePdb
```

**Opção B — Bundle de distribuição (sem admin, Manage + Simulate):**
```powershell
./Package-Plugin.ps1               # gera dist\AutoViewTool.bundle (+ zip)
./Package-Plugin.ps1 -Install      # instala em %APPDATA%\Autodesk\ApplicationPlugins\
```
Detalhes em [EMPACOTAMENTO.md](./EMPACOTAMENTO.md).

**Opção C — Manual:** copie `bin\Release\AutoViewTool.dll` para
`...\Navisworks Simulate 2026\Plugins\AutoViewTool\`.

Depois **reinicie o Navisworks**. Os três botões aparecem em **Add-Ins** (feche o Navisworks antes de redeployar — ele trava a DLL enquanto roda).

## Como usar

**Criar viewpoints:** abra um modelo com Selection Sets → **Auto ViewTool** → marque os sets (a busca filtra) → *Criar Viewpoints*. Opcional: *Gerar modelo (.xlsx)*, preencher e *Importar template* para nomes/descrições customizados.

**Limpar:** **Auto Cleanup** → marque sets/viewpoints (ou nada e use *Limpar tudo*) → confirme.

**Exportar imagens:** **Auto Export** → marque os viewpoints → escolha a pasta, ajuste resolução/qualidade/prefixo-sufixo/markups → *Exportar selecionados*.

## Status de funcionalidades

### Implementadas ✅
- Leitura recursiva de Selection Sets (hierarquia de pastas)
- Isolamento hierárquico otimizado (1 chamada nativa, 1 undo)
- Câmera isométrica com leitura do centro do bounding box
- Criação em lote com estimativa de tempo calibrada
- Templates Excel (geração + importação, sem dependências externas)
- Limpeza de Search Sets e Viewpoints (seletiva + "limpar tudo")
- Exportação de imagens JPG (resolução, qualidade, prefixo/sufixo, markups, duplicatas)
- Busca em tempo real nas três janelas
- Teclado funcional em janela modeless (interop Win32 ↔ WPF)
- Branding embutido (ícone + logo)

### Planejadas 🔄
- **Relatórios** de viewpoints: HTML + XLSX (com imagem embutida) + CSV — *próxima etapa*
- Exportação CSV de metadados
- Custom properties por viewpoint (disciplina, revisor, etc.)

## Troubleshooting

| Problema | Causa | Solução |
|----------|-------|---------|
| Plugin não aparece na ribbon | DLL fora da subpasta correta, ou Navisworks não reiniciado | Garanta `Plugins\AutoViewTool\AutoViewTool.dll` e reinicie |
| "Nenhum documento ativo" | Nenhum modelo aberto | Abra um arquivo NWD/NWC/NWF antes de usar |
| Falha ao copiar a DLL no deploy | Navisworks aberto (trava a DLL) ou sem permissão | Feche o Navisworks; rode o terminal como Administrador |
| Imagens exportadas sem markups | Opção desmarcada | Marque "Incluir markups (overlays)" no Auto Export |
| Criação lenta em modelo enorme | Isolamento percorre o ramo (O(n)) | Esperado em modelos de centenas de mil itens; use lotes menores |
| Template não importa | XLSX inválido / colunas erradas | Regenere com "Gerar modelo (.xlsx)" e use como base |

## Performance

- **Isolamento**: mantém só o caminho (ancestrais) dos itens do set e esconde os irmãos off-path numa única chamada `SetHidden()` — O(n) na travessia, 1 redraw, 1 undo.
- **Diagnóstico**: `PerfLog` grava cada fase em `%TEMP%\AutoViewTool_perf.log` (bounding box, isolamento, câmera, captura). O centro do bbox de cada set é registrado (ponto decimal, InvariantCulture).
- **Estimativa de tempo**: modelo linear (`PerSetMs` + `PerItemMs`) refinado em runtime por `_timeCalibration` conforme a máquina/modelo.

## Versionamento & Histórico de Releases

O histórico fica em pacotes `.zip` versionados (SemVer no arquivo `VERSION`), um por marco, permitindo rollback completo.

**Gerar um snapshot** ao final de cada ciclo de desenvolvimento:

```powershell
# Correção/patch (x.y.Z+1)
./Snapshot-Version.ps1 -Bump Patch -Message "Descrição da correção"

# Nova funcionalidade (x.Y+1.0)
./Snapshot-Version.ps1 -Bump Minor -Message "Descrição da feature"

# Mudança incompatível (X+1.0.0)
./Snapshot-Version.ps1 -Bump Major -Message "Descrição da mudança"

# Incluir a DLL compilada (bin\Release\AutoViewTool.dll)
./Snapshot-Version.ps1 -Bump Patch -Message "..." -IncludeBuild
```

Cada release é registrada em `..\Auto_ViewTool_releases\RELEASES.md` com data, versão, arquivo, contagem de arquivos, tamanho e SHA-256 para auditoria de integridade.

**Recuperar um estado anterior:** extraia o `.zip` versionado desejado em `Auto_ViewTool_releases\`.

## Referências

- [Documentação da API Navisworks (Autodesk)](https://www.autodesk.com/developer)
- [CLAUDE.md](./CLAUDE.md) — guia interno de padrões e arquitetura
- [SETUP.md](./SETUP.md) — verificação do SDK e build
- [EMPACOTAMENTO.md](./EMPACOTAMENTO.md) — bundle de distribuição
