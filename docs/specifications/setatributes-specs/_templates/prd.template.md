# PRD — <Nome da feature>

> Fase 1 (Pesquisa). Repositório bruto de informações: consolida tudo o que a fase de
> especificação vai precisar, sem propor solução nem escrever código novo.

- **Pasta:** `specs/NNN-slug/`
- **Data:** AAAA-MM-DD
- **Status:** `pesquisa` | `pronto para spec`

## 1. Problema e objetivo

<Que dor ou necessidade esta feature resolve. Em 2–4 frases, o estado atual e o estado desejado.>

## 2. Comportamento esperado

<Requisitos funcionais do ponto de vista do usuário do plugin. Fluxo de uso dentro do
Navisworks: o que o usuário seleciona, clica, vê. Incluir casos de erro relevantes.>

- RF1 — <requisito>
- RF2 — <requisito>

## 3. Mapeamento da base de código — arquivos impactados

<Todos os arquivos que a mudança toca ou dos quais depende. O "como" fica para o spec;
aqui registra-se apenas o papel de cada um.>

| Arquivo | Impacto provável | Papel no contexto da feature |
|---|---|---|
| `Caminho/Arquivo.cs` | alterar / criar / só leitura | <por que entra no escopo> |

## 4. Padrões e funções existentes a reaproveitar

<Catálogo do que já existe e NÃO deve ser reinventado. Citar com `arquivo:linha` e colar
a assinatura ou trecho relevante quando ajudar a fase de spec a não alucinar contratos.>

- `Helpers/Arquivo.cs:NN` — `AssinaturaDoMetodo(...)` — <quando usar>

```csharp
// recorte do código existente, se útil
```

## 5. Documentação externa

<Recortes úteis de docs (API Navisworks, WPF, COM API...), cada um com a fonte. Colar o
trecho, não só o link — o spec será escrito sem acesso garantido à internet.>

### <Tópico> — <fonte/URL>

> <recorte relevante>

## 6. Restrições do projeto aplicáveis

<Regras do CLAUDE.md / THEME_MAP.md que esta feature precisa respeitar. Exemplos do que
verificar: cadeia de registro de plugin (3 arquivos), abertura de janela via ToolLauncher,
MVVM para UI nova, tokens do DesignSystem.xaml, nomes via VerumSchema, idioma pt-BR.>

- <restrição>

## 7. Fora de escopo / Não tocar

<Lógicas validadas que a implementação está proibida de refatorar, mesmo que pareçam
melhoráveis. Ser explícito: arquivo + o que não fazer.>

- <arquivo/lógica> — <motivo>

## 8. Perguntas em aberto

<Dúvidas que precisam de resposta do autor antes ou durante o spec. Se vazio, escrever "Nenhuma".>

- [ ] <pergunta>
