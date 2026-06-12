# PRD — [Nome da Feature]

> **Fase:** Pesquisa  
> **Data:** AAAA-MM-DD  
> **Feature:** [descrição de uma linha]

---

## 1. Objetivo

O que esta feature precisa resolver?
Qual é o comportamento esperado do usuário final?

---

## 2. Arquivos Impactados

Lista de todos os arquivos que provavelmente serão criados ou alterados.
Preencha após o mapeamento da base de código.

| Arquivo | Tipo de mudança | Motivo |
|---------|----------------|--------|
| `src/Core/ExemploManager.cs` | Alterar | Adicionar método X |
| `src/UI/MainWindow.xaml` | Alterar | Novo botão Y |
| `src/Models/ExemploData.cs` | Criar | DTO para Z |

---

## 3. Funções e Padrões Reutilizáveis

Liste funções existentes no projeto que **devem** ser reaproveitadas.
Evite que a IA reinvente lógicas já validadas.

```csharp
// Exemplo: padrão de isolamento já validado em IsolationHandler.cs
public void IsolateItems(IEnumerable<ModelItem> items)
{
    // lógica atual — NÃO reescrever
}
```

---

## 4. Documentação Externa Relevante

Cole aqui trechos de documentação da API Navisworks, MSDN ou outras fontes
que a IA precisará consultar durante a especificação.

### Navisworks API
```
// Cole trechos relevantes da API
```

### Outros
```
// Cole trechos de outras fontes
```

---

## 5. Restrições e Regras de Negócio

- [ ] Regra 1: ...
- [ ] Regra 2: ...
- [ ] Regra 3: ...

---

## 6. Pontos de Integração

Onde esta feature se conecta com outras partes do sistema?

- **Entrada:** como os dados chegam até esta feature
- **Saída:** o que esta feature produz e quem consome
- **Eventos/Callbacks:** quais eventos são disparados ou escutados

---

## 7. Critérios de Aceitação

O que precisa ser verdade para considerar a feature completa?

- [ ] Critério 1: ...
- [ ] Critério 2: ...
- [ ] Critério 3: ...

---

## 8. Fora de Escopo

O que explicitamente **não** será implementado nesta feature:

- Não inclui: ...
- Não inclui: ...
