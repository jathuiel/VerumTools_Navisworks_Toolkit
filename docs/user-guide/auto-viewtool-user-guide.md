# Guia do Usuário — Navisworks Toolkit

Suíte de seis ferramentas que automatiza o trabalho com Viewpoints, Selection Sets e
atributos BIM no Navisworks. Após a instalação, a ribbon ganha a aba **Navisworks Toolkit**
com seis botões: **Smart Views**, **Model Cleanup**, **Image Capture**,
**Selection Inspector**, **Property Explorer** e **Visual Sets**.

> Versão **v1.0.1** · Navisworks Simulate / Manage **2026**

---

## Índice

1. [Instalação](#instalação)
2. [Pré-requisitos](#pré-requisitos)
3. [Smart Views — Criar Viewpoints](#1-smart-views--criar-viewpoints)
4. [Model Cleanup — Limpeza](#2-model-cleanup--limpeza)
5. [Image Capture — Exportar Imagens](#3-image-capture--exportar-imagens)
6. [Selection Inspector — Inspecionar Propriedades](#4-selection-inspector--inspecionar-propriedades)
7. [Property Explorer — Atributos Customizados](#5-property-explorer--atributos-customizados)
8. [Visual Sets — Coloração por Set](#6-visual-sets--coloração-por-set)
9. [Fluxos de trabalho comuns](#fluxos-de-trabalho-comuns)
10. [Perguntas frequentes](#perguntas-frequentes)

---

## Instalação

> **Feche o Navisworks antes de instalar ou atualizar** — ele trava os arquivos do
> plugin enquanto está aberto.

Execute o script `deploy.ps1` **como Administrador** (clique com o botão direito →
*Executar como administrador*). O script compila o projeto em Release e copia os
arquivos para:

```
C:\Program Files\Autodesk\Navisworks Simulate 2026\Plugins\NavisworksToolkit\
```

Abra o Navisworks em seguida e confira a aba **Navisworks Toolkit** na ribbon.

---

## Pré-requisitos

- Navisworks **Simulate** ou **Manage**, versão **2026**.
- Um arquivo de modelo aberto (`.nwd`, `.nwc` ou `.nwf`).
- Para criar viewpoints: o modelo deve conter **Selection Sets** (painel *Sets* do Navisworks).
- Para exportar imagens: o modelo deve conter **Viewpoints** salvos.

---

## 1. Smart Views — Criar Viewpoints

**Abre:** aba **Navisworks Toolkit** → botão **Smart Views** (janela *Smart Views*)

Cria viewpoints automaticamente a partir dos Selection Sets do modelo, com controle
de isolamento, projeção de câmera e orientações de vista.

### Fluxo básico

1. **Abra a ferramenta** clicando em **Smart Views** no ribbon.
   A janela lista todos os Selection Sets encontrados no modelo (colunas NOME, ITENS, DESCRIÇÃO).

2. **Selecione os sets** que deseja transformar em viewpoints:
   - Clique no checkbox da linha para marcar individualmente.
   - Clique no checkbox do **cabeçalho da coluna** para marcar / desmarcar todos os visíveis.

3. **Filtre a lista** (opcional): digite no campo de busca para filtrar por nome ou descrição.
   O botão **✕** à direita do campo limpa o filtro. A seleção de itens não é perdida ao filtrar.

4. **Configure as opções** no painel direito:

   **ISOLAMENTO**
   | Opção | Comportamento |
   |---|---|
   | Até o Source File (com contexto) | Ghosting cinza nos elementos do mesmo Source File; oculta os demais. |
   | Apenas itens do SET | Oculta tudo que não pertence ao set — mostra somente a geometria selecionada. |

   **CÂMERA**
   | Opção | Comportamento |
   |---|---|
   | Ortográfica | Projeção paralela — sem perspectiva. |
   | Perspectiva | Projeção cônica — visão mais natural. |

   **VISTAS** — marque as orientações desejadas; cada orientação marcada gera **um viewpoint independente** por set:
   - Isométrica · Vista superior · Vista frontal · Vista traseira · Vista lateral esquerda · Vista lateral direita

5. Observe o **painel de estatísticas** (coluna direita):
   - `Selection Sets` — total de sets no modelo.
   - `Viewpoints` — viewpoints já salvos no documento.
   - `Tempo est.` — estimativa de duração para criar os viewpoints marcados.

6. Clique em **Criar Viewpoints**.
   A barra de progresso no rodapé avança por set (o botão **Cancelar** interrompe a operação).
   Ao final, o status mostra quantos foram criados.

7. Os viewpoints aparecem no painel **Saved Viewpoints** do Navisworks com o nome
   `VP_<NomeDoSet>_<Orientação>` (ou com o nome definido no template — ver abaixo).

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
| `NomeDoViewpoint` | Não | Nome do viewpoint a criar. Em branco = nome automático (`VP_<set>_<Orientação>`). |
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

## 2. Model Cleanup — Limpeza

**Abre:** aba **Navisworks Toolkit** → botão **Model Cleanup** (janela *Model Cleanup*)

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

## 3. Image Capture — Exportar Imagens

**Abre:** aba **Navisworks Toolkit** → botão **Image Capture** (janela *Image Capture*)

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

## 4. Selection Inspector — Inspecionar Propriedades

**Abre:** aba **Navisworks Toolkit** → botão **Selection Inspector** (janela *Selection Inspector*)

Inspeciona e exporta as propriedades BIM dos elementos selecionados no modelo.

### Como usar

1. Selecione um ou mais elementos no Navisworks.
2. Abra o **Selection Inspector** — a grade é preenchida automaticamente com todas as
   categorias e propriedades dos elementos selecionados.
3. Filtre as categorias desejadas pelos **checkboxes** na coluna esquerda.
4. Exporte os dados em um dos formatos disponíveis:

| Botão | Formato | Uso indicado |
|---|---|---|
| **Exportar CSV** | `.csv` (separador `;`, BOM UTF-8) | Excel, Power BI, scripts |
| **Exportar XML** | `.xml` (Excel XML) | Abre no Excel com formatação de células |
| **Copiar** | TSV na área de transferência | Colar diretamente em planilha aberta |

> O processamento ocorre em background — a interface não trava durante a leitura de grandes seleções.

---

## 5. Property Explorer — Atributos Customizados

**Abre:** aba **Navisworks Toolkit** → botão **Property Explorer** (janela *Property Explorer*)

Grava e remove atributos customizados nos elementos selecionados, com suporte a
importação de categorias nativas e registro de Selection Sets.

### Seções da interface

**1 — Atributos Nativos**
- Árvore de categorias e propriedades lidas diretamente da seleção atual.
- Clique em **Importar** ao lado de uma categoria para copiar seus atributos para o laboratório.

**2 — Nome da categoria custom**
- Campo livre para nomear a categoria que receberá os atributos gravados.
- Preenchido automaticamente ao importar uma categoria nativa.

**3 — Selection Sets detectados**
- Lista os sets aos quais os elementos selecionados pertencem.
- Marque os sets a registrar como atributo `_sets` na categoria customizada.
- A seleção persiste entre sessões: na próxima abertura, os sets previamente gravados são marcados.

**4 — Atributos extras a gravar**
- Grade editável com colunas **Nome / Valor / Tipo** (`string`, `int`, `double`, `boolean`).
- Linhas adicionadas manualmente ou importadas de template (`.csv` / `.xml`).

### Gravar e remover

- **Gravar**: aplica os atributos configurados nos elementos selecionados via COM API,
  com merge — preserva atributos existentes e atualiza apenas os editados.
  Um log de verificação exibe o estado antes e depois da gravação.
- **Remover**: exclui a categoria customizada (e categorias legadas) de todos os elementos selecionados.

---

## 6. Visual Sets — Coloração por Set

**Abre:** aba **Navisworks Toolkit** → botão **Visual Sets** (janela *Visual Sets*)

Aplica substituições de cor e transparência aos elementos do modelo baseadas em
Selection Sets, sem alterar o arquivo original.

### Como usar

1. Abra o **Visual Sets** — a lista carrega todos os Selection Sets do documento.
2. Se houver elementos selecionados na cena, os sets que os contêm são **marcados
   automaticamente** como habilitados (auto-seleção inteligente).
3. Para cada set, defina:
   - **Cor** via color picker.
   - **Transparência** de 0 % (opaco) a 100 % (invisível).
   - **Ativo / Inativo** — checkbox para incluir ou excluir o set da aplicação.
4. Clique em **Aplicar** para sobrepor os overrides no modelo.
5. Clique em **Remover overrides** para limpar todas as substituições de cor e transparência.

### Importar e exportar regras

As regras podem ser salvas e carregadas em formato XML:

```xml
<?xml version="1.0" encoding="utf-8"?>
<ColoringRules version="1.0">
  <Rule SelectionSet="Estrutura"  Color="#FF0000" Transparency="0"  Enabled="true"/>
  <Rule SelectionSet="Hidraulica" Color="#0000FF" Transparency="30" Enabled="true"/>
</ColoringRules>
```

Use **Exportar regras** para salvar o arquivo e **Importar regras** para reaplicar em outra sessão ou modelo.

---

## Fluxos de trabalho comuns

### Criar viewpoints de um modelo novo do zero

```
1. Abra o modelo no Navisworks.
2. Certifique-se de que os Selection Sets estão criados (painel Sets).
3. Navisworks Toolkit → Smart Views → marque todos → Criar Viewpoints.
4. Verifique os viewpoints criados no painel Saved Viewpoints.
```

### Ciclo de revisão com nomes padronizados

```
1. Smart Views → Gerar modelo (.xlsx).
2. Preencha SelectionSet / NomeDoViewpoint / Descricao conforme a nomenclatura do projeto.
3. Smart Views → Importar template (.xlsx) → Criar Viewpoints.
```

### Exportar imagens para relatório

```
1. Navisworks Toolkit → Image Capture.
2. Marque os viewpoints de interesse (use a busca para filtrar por disciplina).
3. Configure: pasta de destino, resolução 1920×1080, qualidade 90, prefixo = código do projeto.
4. Exportar selecionados → a pasta abre ao final.
```

### Limpar viewpoints de rascunho antes de entregar o modelo

```
1. Navisworks Toolkit → Model Cleanup.
2. Na coluna Viewpoints, busque o padrão dos rascunhos (ex.: "VP_TESTE").
3. Marque-os → Remover selecionados → confirmar.
```

### Recriar todos os viewpoints após alteração de sets

```
1. Model Cleanup → Viewpoints → selecionar todos → Remover selecionados.
2. Smart Views → selecionar todos os sets → Criar Viewpoints.
```

### Documentar atributos de elementos para relatório BIM

```
1. Selecione os elementos no Navisworks.
2. Navisworks Toolkit → Selection Inspector.
3. Filtre as categorias relevantes.
4. Exportar CSV → abrir no Excel.
```

### Aplicar paleta de cores por disciplina para apresentação

```
1. Navisworks Toolkit → Visual Sets.
2. Atribua cores por set (ex.: vermelho = estrutura, azul = hidráulica).
3. Aplicar → faça a captura de tela ou use com Image Capture.
4. Remover overrides ao finalizar.
```

---

## Perguntas frequentes

**A aba Navisworks Toolkit não aparece na ribbon.**
Verifique, nesta ordem:
1. O plugin está no local certo? Deve existir a pasta
   `C:\Program Files\Autodesk\Navisworks Simulate 2026\Plugins\NavisworksToolkit\`.
2. O Navisworks foi **reiniciado** após a instalação? O plugin só carrega na abertura.
3. A sua versão do Navisworks é a **2026**? Outras versões não são garantidas.
4. O `deploy.ps1` foi executado **como Administrador**? Sem privilégio de admin, a cópia
   para `Program Files` falha silenciosamente.

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
Verifique se a opção **Incluir markups (overlays)** está marcada no Image Capture.
Também confirme que os redlines estão associados ao viewpoint (visíveis ao clicar nele no
painel Saved Viewpoints).

**"Nenhum documento ativo" ao abrir qualquer ferramenta.**
Abra um arquivo de modelo (`.nwd`, `.nwc` ou `.nwf`) antes de acionar qualquer botão da suíte.

**Falha ao instalar/atualizar ("arquivo em uso").**
O Navisworks trava os arquivos do plugin enquanto está aberto. Feche o Navisworks,
execute o `deploy.ps1` novamente como Administrador e reabra o Navisworks ao final.

**O campo de busca não aceita digitação.**
Clique uma vez dentro do campo para garantir o foco do teclado. Se o problema persistir,
a versão instalada é antiga — execute o `deploy.ps1` para atualizar.

**Os overrides de cor não somem após fechar o Visual Sets.**
Os overrides de cor são persistidos no documento. Use o botão **Remover overrides** antes
de fechar, ou execute *Reset Colors* pelo menu do próprio Navisworks.

---

*Para dúvidas técnicas ou sugestão de melhorias, consulte o desenvolvedor responsável.*
