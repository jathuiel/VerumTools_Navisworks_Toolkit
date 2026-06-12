# Spec-Driven Development

Desenvolvimento em 3 fases isoladas, com limpeza de contexto (`/clear`) entre elas. O objetivo é separar a estruturação de requisitos da engenharia de código: a implementação trabalha guiada por um plano fechado, sem re-explorar o repositório e sem refatorar lógica já validada.

## Estrutura

```
specs/
├── README.md                      # este arquivo
├── _templates/
│   ├── prd.template.md            # esqueleto da fase de pesquisa
│   └── spec.template.md           # esqueleto da fase de especificação
└── NNN-slug-da-feature/           # uma pasta por feature (ex.: 001-export-pdf)
    ├── prd.md                     # Fase 1 — repositório bruto de informações
    └── spec.md                    # Fase 2 — plano de ação técnico
```

## Fluxo

| Etapa | Comando | Consome | Produz |
|---|---|---|---|
| 1. Pesquisa | `/prd <descrição da feature>` | repositório + docs externas | `specs/NNN-slug/prd.md` |
| limpeza | `/clear` | — | contexto zerado |
| 2. Especificação | `/spec NNN` | **apenas** o `prd.md` | `specs/NNN-slug/spec.md` |
| limpeza | `/clear` | — | contexto zerado |
| 3. Implementação | `/implement NNN` | **apenas** o `spec.md` | código + build |

## Papel de cada arquivo

**`prd.md` — repositório bruto de informações.** Documenta tudo o que a especificação vai precisar: arquivos do repositório que serão impactados (com `caminho:linha`), padrões de arquitetura e funções existentes a reaproveitar, recortes úteis de documentação externa (API Navisworks, WPF etc.) e as restrições do projeto que se aplicam. **Não contém solução nem código novo** — é contexto consolidado, não plano.

**`spec.md` — plano de ação técnico.** Consome o `prd.md` e lista exatamente quais arquivos serão criados ou alterados, com a lógica e os trechos de código de cada arquivo de forma isolada. É o prompt definitivo da implementação: quem codifica não deveria precisar de nenhuma outra fonte.

## Regras do fluxo

1. **Uma fase por sessão.** Limpe o contexto entre fases; a carga gasta processando documentação bruta não deve competir com a geração de código.
2. **A implementação não desvia do spec.** Se o `spec.md` se mostrar inviável ou incompleto durante a codificação, a divergência volta para o spec (atualize-o e registre o motivo) antes de virar código.
3. **Código validado não se refatora de carona.** Tudo o que não está listado na tabela de arquivos do `spec.md` está fora do escopo — em especial as lógicas marcadas como "Não tocar" no `prd.md`.
4. **Reaproveitar antes de reinventar.** Os padrões e helpers catalogados no `prd.md` (seção 4) são de uso obrigatório quando aplicáveis.
5. **Validação deste projeto é manual.** Não há testes automatizados: o critério de aceite mínimo é `dotnet build` limpo + verificação funcional dentro do Navisworks (descrita no spec).
