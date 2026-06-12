# SPEC — [Nome da Feature]

> **Fase:** Especificação Técnica  
> **Gerado a partir de:** `prd.md`  
> **Data:** AAAA-MM-DD  
> **Ordem de implementação:** siga os itens numerados em sequência

---

## Resumo do Plano

Descrição técnica de uma a três frases do que será implementado.
Nada fora deste documento será codificado.

---

## Ordem de Implementação

```
1. src/Models/ExemploData.cs          (criar — sem dependências)
2. src/Core/ExemploManager.cs         (alterar — depende de #1)
3. src/UI/MainWindow.xaml             (alterar — depende de #2)
4. src/UI/MainWindowViewModel.cs      (alterar — depende de #2 e #3)
```

---

## Arquivos

### 1. `src/Models/ExemploData.cs` — CRIAR

**Motivo:** DTO que trafega dados entre o Core e a UI.

```csharp
namespace AutoViewTool.Models
{
    public class ExemploData
    {
        public string Id { get; set; }
        public string Nome { get; set; }
        public bool Ativo { get; set; }
    }
}
```

**Contratos:**
- `Id`: string não nula, gerada pelo Core
- `Nome`: string fornecida pelo usuário via UI
- `Ativo`: estado padrão = `true`

---

### 2. `src/Core/ExemploManager.cs` — ALTERAR

**Motivo:** Adicionar método `ProcessarExemplo` que consome `ExemploData`.

**Adicionar após a linha X (método `MetodoExistente`):**

```csharp
/// <summary>Processa um ExemploData validando estado antes de operar.</summary>
public ExemploData ProcessarExemplo(ExemploData dados)
{
    if (dados == null)
        throw new ArgumentNullException(nameof(dados));
    if (!dados.Ativo)
        throw new InvalidOperationException("ExemploData inativo não pode ser processado.");

    try
    {
        // lógica central aqui
        return dados;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Falha ao processar exemplo.", ex);
    }
}
```

**Não alterar:** nenhum outro método existente no arquivo.

---

### 3. `src/UI/MainWindow.xaml` — ALTERAR

**Motivo:** Adicionar botão que dispara `ProcessarExemplo`.

**Adicionar dentro do `<StackPanel x:Name="ButtonPanel">`:**

```xml
<Button x:Name="BtnProcessar"
        Content="Processar"
        Command="{Binding ProcessarCommand}"
        Margin="0,4,0,0"
        Height="30" />
```

**Não alterar:** layout existente, outros botões, estilos globais.

---

### 4. `src/UI/MainWindowViewModel.cs` — ALTERAR

**Motivo:** Expor `ProcessarCommand` que invoca `ExemploManager.ProcessarExemplo`.

**Adicionar na região `Commands`:**

```csharp
public ICommand ProcessarCommand { get; }

// No construtor, após os commands existentes:
ProcessarCommand = new RelayCommand(ExecutarProcessar, PodeProcessar);
```

**Adicionar na região `Command Handlers`:**

```csharp
private void ExecutarProcessar()
{
    try
    {
        var resultado = _exemploManager.ProcessarExemplo(DadosSelecionados);
        StatusMessage = $"Processado: {resultado.Nome}";
    }
    catch (Exception ex)
    {
        StatusMessage = $"Erro: {ex.Message}";
    }
}

private bool PodeProcessar() => DadosSelecionados != null && DadosSelecionados.Ativo;
```

**Não alterar:** outros commands, construtor existente além da linha indicada.

---

## Checklist de Implementação

- [ ] 1. `ExemploData.cs` criado e compilando
- [ ] 2. `ExemploManager.ProcessarExemplo` adicionado e testado em isolamento
- [ ] 3. Botão adicionado ao XAML sem quebrar layout existente
- [ ] 4. `ProcessarCommand` exposto e vinculado ao botão
- [ ] 5. Build sem erros (`msbuild /p:Configuration=Release`)
- [ ] 6. Teste manual no Navisworks confirmando o fluxo ponta a ponta

---

## O que está fora deste spec

Qualquer coisa não listada acima está **fora de escopo**.
Se durante a implementação surgir necessidade de algo não previsto aqui,
**pare e atualize o spec antes de codificar**.
