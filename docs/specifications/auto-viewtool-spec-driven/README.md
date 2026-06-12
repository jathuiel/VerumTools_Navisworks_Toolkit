# Spec Driven Development — Guia do Fluxo

## Por que esse fluxo existe

Execução por tentativa e erro degrada a base de software rapidamente.
Isolar a fase de estruturação de requisitos da fase de geração de código garante:

- Menor probabilidade de alucinações técnicas
- Sem reinvenção de funções já validadas
- Contexto da IA reservado exclusivamente para a tarefa em execução

---

## As duas fases do fluxo

```
[PESQUISA]          [SPEC]              [IMPLEMENTAÇÃO]
prd.md    ──────>   spec.md   ──────>   código final
```

---

## Passo a Passo

### Fase 1 — Pesquisa → `prd.md`

**O que fazer:**
1. Peça à IA que mapeie a base de código e identifique os arquivos impactados pela feature.
2. Insira trechos de documentação externa relevante (Navisworks API, MSDN, etc.).
3. Liste padrões de código e funções já validadas que devem ser reaproveitadas.
4. Consolide tudo em `features/<nome-da-feature>/prd.md`.

**Prompt base:**
```
Mapeie a base de código e identifique todos os arquivos impactados por [feature].
Liste funções existentes reutilizáveis, padrões de arquitetura do projeto
e pontos de integração. Consolidate tudo em um prd.md.
```

**Entregável:** `features/<nome>/prd.md`

---

### Primeira Limpeza de Contexto

> Limpe o contexto da IA (`/clear` ou nova conversa).
> Isso elimina a carga gasta no processamento da documentação bruta.

---

### Fase 2 — Especificação Técnica → `spec.md`

**O que fazer:**
1. Alimente a IA **exclusivamente** com o `prd.md` gerado.
2. Exija uma especificação listando exatamente:
   - Arquivos que serão criados ou alterados
   - Lógica e trechos de código para cada estrutura
   - Contratos de métodos (assinaturas, parâmetros, retornos)
   - Ordem de implementação (dependências primeiro)

**Prompt base:**
```
Com base exclusivamente no prd.md abaixo, gere um spec.md que liste:
- Cada arquivo a criar ou alterar (caminho completo)
- A lógica exata de cada método ou bloco
- Trechos de código para cada estrutura
- Ordem de implementação respeitando dependências
Não implemente ainda. Apenas especifique.
```

**Entregável:** `features/<nome>/spec.md`

---

### Segunda Limpeza de Contexto

> Limpe o histórico novamente.
> Isso reserva a capacidade operacional do modelo para geração de código.

---

### Fase 3 — Implementação Guiada

**O que fazer:**
1. Forneça o `spec.md` à IA.
2. Instrua a IA a seguir o plano estritamente, arquivo por arquivo.
3. Não permita desvios — se a IA sugerir algo fora do spec, questione.

**Prompt base:**
```
Implemente o plano descrito no spec.md abaixo, seguindo a ordem indicada.
Não crie arquivos ou funções que não estejam no spec.
Para cada arquivo, confirme o que foi feito antes de avançar.
```

---

## Estrutura de Pastas

```
spec-driven/
├── README.md                    ← este arquivo
├── _templates/
│   ├── prd.template.md          ← template para novos PRDs
│   └── spec.template.md         ← template para novos SPECs
└── features/
    └── <nome-da-feature>/
        ├── prd.md               ← fase de pesquisa
        └── spec.md              ← fase de especificação
```

---

## Regras

- `prd.md` nunca é input direto para código. Sempre passa pelo `spec.md` primeiro.
- `spec.md` é o único prompt de implementação. Tudo fora dele é escopo não autorizado.
- Cada feature tem sua própria subpasta em `features/`.
- Ao terminar a implementação, archive a feature (não delete — serve como histórico).
