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
| [`architecture/`](./architecture/) | Arquitetura da solução consolidada, design system, registro de plugins |
| [`release-notes/`](./release-notes/) | Changelog consolidado e histórico de versões |
| [`migration/`](./migration/) | Artefatos da consolidação: inventário, MigrationReport, checklist de validação e log de deploy |

## Documentos principais

### Arquitetura
- [Arquitetura consolidada](./architecture/consolidated-architecture.md) — estrutura da nova solução, ribbon única, namespaces

### Release notes
- [CHANGELOG consolidado](./release-notes/CHANGELOG.md)

### Migração (consolidação 2026-06-11)
- [MigrationReport](./migration/MigrationReport.md) — relatório completo da consolidação
- [Checklist de validação](./migration/VALIDATION_CHECKLIST.md)
- [Inventário da auditoria](./migration/INVENTORY.md)

> **Nota de preservação:** os documentos nesta pasta são cópias organizadas dos originais; nenhum conteúdo foi removido. Referências internas a caminhos antigos (ex.: `./SETUP.md`) referem-se à estrutura dos projetos originais.
