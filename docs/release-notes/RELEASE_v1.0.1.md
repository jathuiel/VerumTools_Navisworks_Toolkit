# Navisworks Toolkit v1.0.1 — Identidade visual: tema claro e ícones azuis

Repaginação visual da suíte para o **tema claro corporativo**, aplicada de forma consistente nas 6 janelas. Plataforma unificada para **Autodesk Navisworks 2026** (Simulate/Manage): uma única DLL com seis ferramentas na tab própria **"Navisworks Toolkit"** do ribbon.

## Alterado
- **Design system (tema claro)** em `DesignSystem.xaml` e nas 6 janelas: superfícies claras (`#E5E7EB` · painel `#E0E2E6` · cards `#F3F4F6`), texto escuro (`#1F2937` / `#4B5563` / desabilitado `#9CA3AF`) e **acento azul institucional `#2563EB`** (hover `#1D4ED8`). Padronizados estados de botões, campos, cabeçalhos de tabela (`#D6D9DE`), hover/seleção (`#DCEAFE` / `#BFDBFE`), zebra de linhas (`#ECEEF1`) e tokens de status (sucesso `#15803D`, aviso `#D97706`, erro `#DC2626`). Contraste WCAG AA verificado nas combinações principais.
- **Ícones do ribbon**: acento recolorido de laranja para **azul `#2563EB`** nos 6 ícones (16/32 px) por mapeamento de matiz, preservando traço escuro e anti-aliasing.
- **Logo** das janelas atualizada para a identidade "verum partners — Part of Accenture".

## Layout
- Toolbar do **Visual Sets** migrada para `WrapPanel` (evita corte de botões em janelas estreitas).
- Painel de opções do **Image Capture** em `ScrollViewer` (evita corte em alturas baixas / DPI alto).

## Removido
- Pasta **`assets/` (raiz)**: `icons/` e `logos/` eram cópias byte-a-byte de `src/`, e `assets/images/` (originais 512 px) não eram usados em runtime. Fonte de arte única passa a ser `src/Resources/Icons/` (ribbon) e `src/Assets/` (logo/ícone embutidos na DLL).

## Instalação
1. Compile e instale com `./deploy.ps1` (como Administrador, com o Navisworks fechado).
2. Reinicie o Navisworks — a tab **"Navisworks Toolkit"** aparece no ribbon.

## Artefato
- `NavisworksToolkit_v1.0.1_src.zip` — código-fonte (135 arquivos · 0,38 MB)
- SHA-256: `0E418BDC030E4DB0614C0ECB07FE6CB4C3F7A08DAAD81746E503A3DDFB5184D4`
