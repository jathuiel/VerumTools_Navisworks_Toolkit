# Guia do Usuário — Verum Toolkit

Suíte de três ferramentas que automatiza o trabalho com Viewpoints e Selection Sets no
Navisworks. Após a instalação, a ribbon ganha a aba **Verum Toolkit** com o painel
*Automation Tools* e três botões: **ViewBuilder**, **ModelCleaner** e **ImageExporter**.

> Versão **v1.1.2** · Navisworks Simulate / Manage **2025–2027**

---

## Índice

1. [Instalação](#instalação)
2. [Pré-requisitos](#pré-requisitos)
3. [ViewBuilder — Criar Viewpoints](#1-viewbuilder--criar-viewpoints)
4. [ModelCleaner — Limpeza](#2-modelcleaner--limpeza)
5. [ImageExporter — Exportar Imagens](#3-imageexporter--exportar-imagens)
6. [Fluxos de trabalho comuns](#fluxos-de-trabalho-comuns)
7. [Perguntas frequentes](#perguntas-frequentes)

---

## Instalação

> **Feche o Navisworks antes de instalar ou atualizar** — ele trava os arquivos do
> plugin enquanto está aberto.

**Opção 1 — Instalador (recomendado):** execute `VerumToolkit_v<versão>_Setup.exe` e
siga o assistente. Não requer privilégios de administrador; a instalação vale para o
seu usuário do Windows. O desinstalador fica disponível em
*Configurações → Aplicativos instalados*.

**Opção 2 — Manual (zip):** extraia o `AutoViewTool_v<versão>_bundle.zip` e copie a
pasta `AutoViewTool.bundle` para:

```
%APPDATA%\Autodesk\ApplicationPlugins\
```

Em ambos os casos, abra o Navisworks em seguida e confira a aba **Verum Toolkit**
na ribbon.

---

## Pré-requisitos

- Navisworks **Simulate** ou **Manage**, versão **2025, 2026 ou 2027**.
- Um arquivo de modelo aberto (`.nwd`, `.nwc` ou `.nwf`).
- Para criar viewpoints: o modelo deve conter **Selection Sets** (painel *Sets* do Navisworks).
- Para exportar imagens: o modelo deve conter **Viewpoints** salvos.

---

## 1. ViewBuilder — Criar Viewpoints

**Abre:** aba **Verum Toolkit** → botão **ViewBuilder** (janela *View Builder — Criador de Viewpoints*)

Cria viewpoints isométricos automaticamente a partir dos Selection Sets do modelo.
Os elementos do set ficam com aparência original; todo o resto recebe um override cinza
semi-transparente (*ghosting*), mantendo o contexto espacial do modelo. A câmera é
posicionada em ângulo isométrico e enquadra exatamente a bounding box do conjunto.

### Fluxo básico

1. **Abra a ferramenta** clicando em **ViewBuilder** no ribbon.
   A janela lista todos os Selection Sets encontrados no modelo (colunas NOME, ITENS, DESCRIÇÃO).

2. **Selecione os sets** que deseja transformar em viewpoints:
   - Clique no checkbox da linha para marcar individualmente.
   - Clique no checkbox do **cabeçalho da coluna** para marcar / desmarcar todos os visíveis.

3. **Filtre a lista** (opcional): digite no campo de busca para filtrar por nome ou descrição.
   O botão **✕** à direita do campo limpa o filtro. A seleção de itens não é perdida ao filtrar.

4. Observe o **painel de estatísticas** (coluna direita):
   - `Selection Sets` — total de sets no modelo.
   - `Viewpoints` — viewpoints já salvos no documento.
   - `Tempo est.` — estimativa de duração para criar os viewpoints marcados.

5. Clique em **Criar Viewpoints**.
   A barra de progresso no rodapé avança por set (o botão **Cancelar** interrompe a operação).
   Ao final, o status mostra quantos foram criados.

6. Os viewpoints aparecem no painel **Saved Viewpoints** do Navisworks com o nome
   `VP_<NomeDoSet>_<timestamp>` (ou com o nome definido no template — ver abaixo).

> **Restaurar Visibilidade**: Use quando o modelo ficar com elementos ghostados ou ocultos (ex.: após
> cancelar uma operação ou fechar a janela sem concluir). Clique em **Restaurar Visibilidade** para reverter
> visibilidade e overrides de cor/transparência ao estado original (equivale a *Reset All* + *Reset Colors*
> no próprio Navisworks).

---

### Usar template Excel (nomes e descrições customizados)

O template permite definir nomes e descrições personalizados para cada viewpoint antes de criá-los.

**Passo 1 — Gerar o modelo:**

1. Clique em **Gerar modelo (.xlsx)** e escolha onde salvar o arquivo
   (nome sugerido: `ViewBuilder_Template.xlsx`).
2. O Excel gerado tem duas abas:
   - **Viewpoints** — a aba de dados, com o cabeçalho e algumas linhas de **exemplo**
     usando nomes reais de sets do modelo (quando existirem).
   - **Instruções** — passo a passo de preenchimento.

**Passo 2 — Preencher o template:**

Na aba **Viewpoints**, preencha **uma linha por viewpoint** que deseja criar:

| Coluna | Obrigatória? | Descrição |
|---|---|---|
| `SelectionSet` | **Sim** | Nome de um Selection Set existente no modelo. |
| `NomeDoViewpoint` | Não | Nome do viewpoint a criar. Em branco = nome automático (`VP_<set>_<timestamp>`). |
| `Descricao` | Não | Texto livre associado ao viewpoint. |

Pode editar em qualquer editor compatível (Excel, LibreOffice, Google Sheets).
**Não renomeie nem reordene as colunas.** Observações úteis:
- Maiúsculas/minúsculas e espaços nas pontas são ignorados ao casar os nomes.
- Se o mesmo set aparecer em mais de uma linha, é criado apenas **1** viewpoint para ele.
- Linhas em branco são ignoradas.

**Passo 3 — Importar:**

1. Clique em **Importar template (.xlsx)** e selecione o arquivo preenchido.
2. A ferramenta cruza a coluna `SelectionSet` com os sets do modelo: os que casarem
   são **marcados automaticamente na lista**, já com nome/descrição do template.
3. Revise a seleção e clique em **Criar Viewpoints** normalmente.

---

## 2. ModelCleaner — Limpeza

**Abre:** aba **Verum Toolkit** → botão **ModelCleaner** (janela *Model Cleaner — Limpeza de Search Sets e Viewpoints*)

Remove Search Sets e Viewpoints do modelo, individualmente ou em lote.

> **Atenção:** a remoção em geral pode ser desfeita com **Ctrl+Z** no Navisworks, mas
> "Limpar tudo" remove todos os itens de uma vez — confirme antes de clicar.

### Interface

A janela tem **duas colunas**:

| Coluna esquerda | Coluna direita |
|---|---|
| **SEARCH SETS / CONJUNTOS** | **VIEWPOINTS** |
| Lista todos os sets do modelo | Lista todos os viewpoints salvos |
| Coluna TIPO: *Busca* (dinâmico) ou *Explícito* (seleção fixa) | Nome + caminho hierárquico |

Cada coluna tem campo de busca, checkbox de selecionar-todos e contador no rodapé.

### Remover itens selecionados

1. Marque os sets e/ou viewpoints que deseja excluir (use a busca para localizar mais rápido).
2. Clique em **Remover selecionados**.
3. Confirme a caixa de diálogo. Os itens marcados são removidos; o restante permanece intacto.

### Limpar tudo

1. Clique em **Limpar tudo**.
2. Uma caixa de confirmação pede separadamente para Search Sets e Viewpoints — confirme apenas
   o que deseja apagar. Você pode limpar só os sets, só os viewpoints ou ambos.

### Atualizar a lista

Se você adicionou ou removeu itens no Navisworks enquanto a janela estava aberta,
clique em **Atualizar** para recarregar as listas.

---

## 3. ImageExporter — Exportar Imagens

**Abre:** aba **Verum Toolkit** → botão **ImageExporter** (janela *Image Exporter — Exportar imagens de Viewpoints*)

Exporta imagens JPG dos viewpoints selecionados. Cada imagem é gerada a partir da câmera e
visibilidade exata do viewpoint (incluindo markups, se a opção estiver ativa).

### Selecionar viewpoints

- A lista exibe todos os viewpoints salvos no modelo.
- Viewpoints com nomes repetidos são sinalizados com o badge laranja **duplicado** — o arquivo
  gerado recebe um sufixo numérico automático (`_1`, `_2`, …) para evitar sobrescritas.
- Use o campo de busca para filtrar e o checkbox do cabeçalho para marcar todos os visíveis.
- O contador **"X selecionado(s)"** no painel de opções confirma quantos serão exportados.

### Configurar as opções

| Campo | Padrão | Descrição |
|---|---|---|
| **Pasta de destino** | *(vazio)* | Clique em **...** para escolher a pasta de saída. Campo obrigatório. |
| **Resolução (px)** | 1920 × 1080 | Largura e altura da imagem em pixels. |
| **Qualidade JPEG** | 90 | Valor de 0 (menor arquivo) a 100 (máxima qualidade). |
| **Prefixo do nome** | *(vazio)* | Texto adicionado antes do nome do viewpoint no nome do arquivo. |
| **Sufixo do nome** | *(vazio)* | Texto adicionado após o nome do viewpoint, antes da extensão. |
| **Incluir markups (overlays)** | ✔ marcado | Quando marcado, overlays e redlines do Navisworks aparecem na imagem. |

**Exemplo de nome gerado:**
- Prefixo `PROJ01_` + viewpoint `TANQUES` + sufixo `_rev2` → `PROJ01_TANQUES_rev2.jpg`

### Exportar

1. Certifique-se de que a **Pasta de destino** está preenchida.
2. Clique em **Exportar selecionados**.
3. A barra de progresso avança por imagem. O status no rodapé mostra o viewpoint atual.
4. Ao final, a pasta de destino é aberta automaticamente no Windows Explorer.

> Se quiser re-executar com os mesmos viewpoints após alterar opções, clique em **Atualizar**
> para recarregar a lista sem fechar a janela.

---

## Fluxos de trabalho comuns

### Criar viewpoints de um modelo novo do zero

```
1. Abra o modelo no Navisworks.
2. Certifique-se de que os Selection Sets estão criados (painel Sets).
3. Verum Toolkit → ViewBuilder → marque todos → Criar Viewpoints.
4. Verifique os viewpoints criados no painel Saved Viewpoints.
```

### Ciclo de revisão com nomes padronizados

```
1. ViewBuilder → Gerar modelo (.xlsx).
2. Preencha SelectionSet / NomeDoViewpoint / Descricao conforme a nomenclatura do projeto.
3. ViewBuilder → Importar template (.xlsx) → Criar Viewpoints.
```

### Exportar imagens para relatório

```
1. Verum Toolkit → ImageExporter.
2. Marque os viewpoints de interesse (use a busca para filtrar por disciplina).
3. Configure: pasta de destino, resolução 1920×1080, qualidade 90, prefixo = código do projeto.
4. Exportar selecionados → a pasta abre ao final.
```

### Limpar viewpoints de rascunho antes de entregar o modelo

```
1. Verum Toolkit → ModelCleaner.
2. Na coluna Viewpoints, busque o padrão dos rascunhos (ex.: "VP_TESTE").
3. Marque-os → Remover selecionados → confirmar.
```

### Recriar todos os viewpoints após alteração de sets

```
1. ModelCleaner → Viewpoints → selecionar todos → Remover selecionados.
2. ViewBuilder → selecionar todos os sets → Criar Viewpoints.
```

---

## Perguntas frequentes

**A aba Verum Toolkit não aparece na ribbon.**
Verifique, nesta ordem:
1. O bundle está no local certo? Deve existir
   `%APPDATA%\Autodesk\ApplicationPlugins\AutoViewTool.bundle\PackageContents.xml`.
2. O Navisworks foi **reiniciado** após a instalação? O plugin só carrega na abertura.
3. A sua versão do Navisworks é 2025, 2026 ou 2027? Versões fora desse intervalo não
   carregam o bundle.
4. Existe uma cópia antiga em
   `C:\Program Files\Autodesk\Navisworks <Edição> <Ano>\Plugins\AutoViewTool\`?
   Remova-a (requer admin) para evitar conflito de cópias duplicadas.

**A lista de Selection Sets está vazia.**
O modelo não tem sets criados, ou o arquivo foi aberto sem os sets vinculados (verifique o
painel *Sets* do próprio Navisworks).

**Importei o template, mas nenhum set foi marcado.**
O valor da coluna `SelectionSet` precisa corresponder ao nome de um set existente no
modelo (maiúsculas/minúsculas e espaços nas pontas são tolerados, mas o nome em si
deve ser o mesmo). Confira também se os dados estão na aba **Viewpoints**.

**O viewpoint foi criado, mas a câmera parece estar no ponto de origem.**
Isso não deve ocorrer nas versões atuais (a câmera é aplicada e confirmada automaticamente
após a criação). Se ainda acontecer, verifique se o bounding box do set não é vazio
(set com itens sem geometria).

**As imagens exportadas ficaram pretas.**
O Navisworks pode precisar de alguns instantes para renderizar a cena antes de capturar.
Se o problema persistir, tente reduzir a resolução ou fechar outras janelas pesadas.

**A imagem não tem os markups/redlines.**
Verifique se a opção **Incluir markups (overlays)** está marcada no ImageExporter.
Também confirme que os redlines estão associados ao viewpoint (visíveis ao clicar nele no
painel Saved Viewpoints).

**"Nenhum documento ativo" ao abrir qualquer ferramenta.**
Abra um arquivo de modelo (`.nwd`, `.nwc` ou `.nwf`) antes de acionar qualquer botão da suíte.

**Falha ao instalar/atualizar ("arquivo em uso").**
O Navisworks trava os arquivos do plugin enquanto está aberto. Feche o Navisworks,
clique em **Repetir** no instalador (ou execute-o novamente) e reabra o Navisworks ao final.
A instalação padrão não precisa de administrador.

**O campo de busca não aceita digitação.**
Clique uma vez dentro do campo para garantir o foco do teclado. Se o problema persistir,
a versão instalada é antiga — atualize para a versão atual com o instalador.

---

*Para dúvidas técnicas ou sugestão de melhorias, consulte o desenvolvedor responsável.*
