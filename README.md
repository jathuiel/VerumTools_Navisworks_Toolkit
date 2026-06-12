# Navisworks Toolkit

Plataforma unificada de plugins para **Autodesk Navisworks 2026** (Simulate/Manage), resultado da consolidação dos projetos **Auto_ViewTool (Verum Toolkit)** e **SetAtributesToolkit (Construct Sync Toolkit)**. Uma única DLL expõe **seis ferramentas** na tab própria **"Navisworks Toolkit"** do ribbon.

> Versão: **2.1.2** · .NET Framework 4.8 · WPF · x64

## Ferramentas

| Painel | Botão | O que faz | Origem |
|---|---|---|---|
| **Visualization** | Smart Views | Cria viewpoints isométricos em lote a partir de Selection Sets; elementos fora do set ficam transparentes (*ghosting*) para contexto espacial (template Excel opcional) | Auto_ViewTool |
| **Visualization** | Visual Sets | Aplica overrides de cor/transparência por Selection Set (import/export XML) | SetAtributesToolkit |
| **Visualization** | Image Capture | Exporta JPG dos viewpoints (resolução, qualidade, markups) | Auto_ViewTool |
| **Selection** | Selection Inspector | Inspeção tabular de propriedades BIM com exportação CSV/Excel/TSV | SetAtributesToolkit |
| **Data** | Property Explorer | Grava/remove atributos customizados via COM API (merge, sets, templates) | SetAtributesToolkit |
| **Model** | Model Cleanup | Remove Search Sets e Viewpoints indesejados | Auto_ViewTool |

> Nomes comerciais definidos nos [UX Standards](./docs/architecture/ux-standards.md); os IDs técnicos dos comandos permanecem os da consolidação.

## Estrutura do repositório

```
NavisworksToolkit.sln        # Solução única
build.ps1 / deploy.ps1       # Compilar / instalar no Navisworks
src/                         # Código consolidado
├── NavisworksToolkit.csproj # SDK-style, net48, x64, WPF
├── Core/                    # Entry point (CommandHandlerPlugin), ToolLauncher, interop, log
├── Shared/                  # Utilitários entre módulos (XlsxFile, helpers, VerumSchema...)
├── Modules/                 # Um diretório por ferramenta (6 módulos)
├── UI/                      # Theme.xaml + DesignSystem.xaml (mesma paleta de 5 cores)
├── Assets/                  # Ícone e logo embutidos na DLL
├── Resources/Icons/         # Ícones do ribbon (16/32 px)
└── en-US/                   # Layout do ribbon (NavisworksToolkit.xaml)
config/                      # Configuração central (config.json) e documentação
assets/                      # Identidade visual consolidada (logos, icons, images)
docs/                        # Documentação consolidada (user/dev guides, arquitetura, specs)
└── migration/               # Inventário, MigrationReport, checklist e scripts da consolidação
templates/                   # Template Excel do ViewBuilder
Archive/                     # Projetos originais (movidos da raiz) + backups verificados
├── Auto_ViewTool/           # Projeto original A (intacto, funcional)
├── SetAtributesToolkit/     # Projeto original B (intacto, com histórico git)
├── ProjectA/ ProjectB/      # Cópias de backup da ETAPA 2 (verificadas byte a byte)
└── README.md                # Explica cada item do Archive
```

## Compilação e instalação

**Pré-requisitos:** Visual Studio 2022 ou .NET SDK (msbuild/dotnet), .NET Framework 4.8, Navisworks 2026 instalado (HintPaths apontam para `C:\Program Files\Autodesk\Navisworks Simulate 2026\`).

```powershell
./build.ps1                 # compila (Release) -> src\bin\Release\NavisworksToolkit.dll
./deploy.ps1                # compila e instala (rode como Administrador, Navisworks fechado)
./deploy.ps1 -Sku Manage    # instala no Navisworks Manage
```

Depois **reinicie o Navisworks** — a tab **"Navisworks Toolkit"** aparece no ribbon com os painéis Visualization, Selection, Data e Model.

## Documentação

- [Documentação consolidada](./docs/README.md) — guias de usuário e desenvolvedor das seis ferramentas
- [Arquitetura consolidada](./docs/architecture/consolidated-architecture.md)
- [Relatório de migração](./docs/migration/MigrationReport.md) — o que foi migrado, conflitos e recomendações
- [Checklist de validação](./docs/migration/VALIDATION_CHECKLIST.md)
- [Inventário da auditoria](./docs/migration/INVENTORY.md)
- [Changelog](./docs/release-notes/CHANGELOG.md)

## Preservação

Nenhum arquivo dos projetos originais foi excluído ou alterado: as pastas `Auto_ViewTool/` e `SetAtributesToolkit/` foram movidas intactas para `Archive/` (incluindo o histórico git do segundo), e há ainda backup integral verificado (contagem e bytes) em `Archive/ProjectA` e `Archive/ProjectB`. Ver [`Archive/README.md`](./Archive/README.md).
