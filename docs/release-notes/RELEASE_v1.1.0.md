# Navisworks Toolkit v1.1.0 — Novas vistas isométricas e enquadramento de contexto

Evolução funcional do **Smart Views**: oito novas orientações isométricas por Selection Set e um enquadramento mais útil da vista Isométrica, além de um refinamento de acabamento na UI. Plataforma unificada para **Autodesk Navisworks 2026** (Simulate/Manage): uma única DLL com seis ferramentas na tab própria **"Navisworks Toolkit"** do ribbon.

## Adicionado
- **Smart Views — 8 novas orientações isométricas**, somadas às existentes (Isométrica, Topo, Frente, Trás, Esquerda, Direita). Cada orientação marcada gera um viewpoint independente para o mesmo set:
  - **Isométricas superiores** (canto de cima, 3 eixos): `Top Front Right`, `Top Front Left`, `Top Back Right`, `Top Back Left`.
  - **Isométricas intermediárias** (de cima ao longo de uma direção, 2 eixos): `Top Front`, `Top Back`, `Top Right`, `Top Left`.
  - O painel ganhou dois grupos rotulados (**ISOMÉTRICAS SUPERIORES** e **ISOMÉTRICAS INTERMEDIÁRIAS**). Os eixos derivam do *world-up* do modelo (funciona em modelos Z-up e Y-up); o nome descreve a **posição da câmera**, que olha para o centro da bounding box.
  - Novos valores em `ViewOrientation` e rótulos em `ViewGenerationOptions.LabelOf`; câmera implementada em `ViewpointManager.ApplyCamera`; checkboxes em `MainWindow.xaml`/`.xaml.cs`.

## Alterado
- **Vista Isométrica — enquadramento do contexto do nível**: a Isométrica passa a enquadrar **toda a geometria visível** após o isolamento (o set + o *ghosting* do mesmo Source File), via `_document.GetBoundingBox(true)` em `ViewpointManager.ApplyCamera`, em vez de cortar justo na bounding box do set. No modo **Apenas itens do set** (sem contexto) recai naturalmente na bbox do set, pois só ele está visível. As demais vistas (ortogonais e as 8 novas isométricas) continuam justas ao set.
- **Bordas afinadas para 0.8px** (*hairline*) em todo o tema `DesignSystem.xaml` e nas 6 janelas dos módulos: botões secundários, campos, *combo boxes*, caixas de busca e divisores de `DataGrid`/`GridView`/`ListView`. Acabamento mais leve, sem alterar cores nem layout.

## Instalação
1. Compile e instale com `./deploy.ps1` (como Administrador, com o Navisworks fechado).
2. Reinicie o Navisworks — a tab **"Navisworks Toolkit"** aparece no ribbon; o rodapé das janelas exibe **v1.1.0**.

## Artefato
- `NavisworksToolkit_v1.1.0_src.zip` — código-fonte, docs, config e templates (87 arquivos · 0,18 MB). Exclui: `bin/`, `obj/`, `.git/`, `.vs/`, `.vscode/`.
- SHA-256: `49C28748FE1593CCF43A60830B55630AC23B838A12040B5AFE7AD974AE16EA42`
