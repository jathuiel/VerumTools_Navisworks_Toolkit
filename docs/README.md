# Navisworks Toolkit — Documentação

Documentação consolidada da plataforma **Navisworks Toolkit**, resultado da unificação de dois projetos:

| Projeto de origem | Nome comercial | Versão na consolidação | Ferramentas |
|---|---|---|---|
| `Auto_ViewTool` | Verum Toolkit | 1.1.2 | ViewBuilder, ModelCleaner, ImageExporter |
| `SetAtributesToolkit` | Construct Sync Toolkit | 0.1.8 | Inspecionar Seleção, Laboratório de Atributos, Coloração de Sets |

Os projetos originais estão preservados integralmente em `/Archive`: as pastas originais (`Archive/Auto_ViewTool`, `Archive/SetAtributesToolkit`, esta última com o histórico git) e as cópias de backup verificadas (`Archive/ProjectA`, `Archive/ProjectB`).

## Estrutura desta documentação

| Pasta | Conteúdo |
|---|---|
| [`user-guide/`](./user-guide/) | Guias do usuário final das seis ferramentas |
| [`developer-guide/`](./developer-guide/) | Build, deploy, empacotamento, padrões de código |
| [`architecture/`](./architecture/) | Arquitetura da solução consolidada, design system, registro de plugins |
| [`specifications/`](./specifications/) | Fluxo spec-driven (PRD → Spec → Implement) e specs históricas |
| [`skills/`](./skills/) | Skills e comandos de desenvolvimento assistido (Claude Code) |
| [`release-notes/`](./release-notes/) | Changelog consolidado e histórico de versões |
| [`migration/`](./migration/) | Artefatos da consolidação: inventário, MigrationReport, checklist de validação, scripts e log de deploy |

## Documentos principais

### Guias do usuário
- [Auto ViewTool — Guia do usuário](./user-guide/auto-viewtool-user-guide.md) — ViewBuilder, ModelCleaner, ImageExporter
- [SetAtributes Toolkit — Guia e funcionalidades](./user-guide/setatributes-toolkit-guide.md) — Inspeção, Atributos, Coloração

### Guias do desenvolvedor
- [Auto ViewTool — README original](./developer-guide/auto-viewtool-readme.md)
- [Auto ViewTool — Guia de desenvolvimento](./developer-guide/auto-viewtool-dev-guide.md)
- [Auto ViewTool — Setup do SDK](./developer-guide/auto-viewtool-setup.md)
- [Auto ViewTool — Empacotamento (bundle Autodesk)](./developer-guide/auto-viewtool-empacotamento.md)
- [SetAtributes — Guia de desenvolvimento](./developer-guide/setatributes-dev-guide.md)

### Arquitetura
- [Arquitetura consolidada](./architecture/consolidated-architecture.md) — estrutura da nova solução, ribbon única, namespaces
- [Mapa de tema WPF (Auto ViewTool)](./architecture/theme-map-auto-viewtool.md)

### Release notes
- [CHANGELOG consolidado](./release-notes/CHANGELOG.md)

### Migração (consolidação 2026-06-11)
- [MigrationReport](./migration/MigrationReport.md) — relatório completo da consolidação
- [Checklist de validação](./migration/VALIDATION_CHECKLIST.md)
- [Inventário da auditoria](./migration/INVENTORY.md)

> **Nota de preservação:** os documentos nesta pasta são cópias organizadas dos originais; nenhum conteúdo foi removido. Referências internas a caminhos antigos (ex.: `./SETUP.md`) referem-se à estrutura dos projetos originais.
