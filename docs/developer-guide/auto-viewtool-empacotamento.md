# Empacotamento — Auto ViewTool

Passo a passo para empacotar o plugin como um **Application Plugin (bundle)** do
Navisworks, pronto para instalar e distribuir.

O bundle é a forma recomendada (vs. soltar a DLL em `Program Files\...\Plugins\`):
- **Não exige admin** quando instalado no perfil do usuário.
- Mira **Manage e Simulate 2026** com a mesma DLL (`Platform="NAVMAN|NAVSIM"`).
- Fica **fora de `Program Files`** — sem UAC e sem o problema da subpasta obrigatória.

> **Alvo (verificado):** Navisworks **2026 = série `Nw23`** (2024=Nw21, 2025=Nw22,
> 2026=Nw23), Win64, plugin `.NET` (`AppType="ManagedPlugin"`).

---

## Pré-requisitos

- DLL compilada em `bin\Release\AutoViewTool.dll` (gere com `./build.ps1 -Configuration Release`).
- Arquivos na raiz: `PackageContents.xml` (manifesto) e `Package-Plugin.ps1` (script).

## Passo 1 — Empacotar

```powershell
# Empacota a partir da DLL já compilada
./Package-Plugin.ps1

# (ou) recompila a Release e empacota num passo só
./Package-Plugin.ps1 -Build
```

Saída em `dist\`:

```
dist\
├── AutoViewTool.bundle\            <- pasta a instalar
│   ├── PackageContents.xml         (AppVersion sincronizada com o arquivo VERSION)
│   └── Contents\
│       └── AutoViewTool.dll
└── AutoViewTool_v<versao>_bundle.zip   <- artefato para distribuir
```

## Passo 2 — Instalar

**Opção A — automática, por usuário (sem admin):**

```powershell
# FECHE o Navisworks antes (ele trava a DLL carregada)
./Package-Plugin.ps1 -Install
```

Instala em `%APPDATA%\Autodesk\ApplicationPlugins\AutoViewTool.bundle\`.

**Opção B — manual:** copie a pasta `dist\AutoViewTool.bundle` para **uma** destas:

| Local | Escopo | Admin? |
|---|---|---|
| `%APPDATA%\Autodesk\ApplicationPlugins\` | Só o usuário atual | Não |
| `%PROGRAMDATA%\Autodesk\ApplicationPlugins\` | Todos os usuários | Sim |

**Opção C — instalador `.exe` (Inno Setup):**

```powershell
# 1) Gere o bundle (se ainda não gerou)
./Package-Plugin.ps1 -Build

# 2) Compile o instalador (requer Inno Setup 6)
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" VerumToolkit-Setup.iss
```

Gera `dist\VerumToolkit_v<versão>_Setup.exe` — instalador **por usuário (sem admin)**
que copia o bundle para `%APPDATA%\Autodesk\ApplicationPlugins\AutoViewTool.bundle\`.
O instalador:
- Exige o Navisworks fechado (detecta `Roamer.exe` e oferece Repetir/Cancelar).
- Limpa a versão anterior do bundle antes de copiar a nova.
- Registra em "Aplicativos instalados" do Windows e cria desinstalador
  (a desinstalação remove a pasta do bundle por completo).
- Avisa se existir cópia antiga na pasta clássica `Plugins\` (duplicidade).
- A versão é lida do arquivo `VERSION` na compilação (mesma fonte do
  `Package-Plugin.ps1`).

## Passo 3 — Verificar

1. Abra o Navisworks **Manage** (ou **Simulate**) **2026**.
2. Aba **Add-Ins** → o botão **Auto ViewTool** deve aparecer.
3. Clique e confirme que a janela abre.

## Passo 4 — Distribuir

Envie `dist\AutoViewTool_v<versao>_bundle.zip`. Quem recebe:
1. Extrai o zip (resulta na pasta `AutoViewTool.bundle`).
2. Copia essa pasta para `%APPDATA%\Autodesk\ApplicationPlugins\` (Passo 2, Opção B).
3. Reinicia o Navisworks.

**Ou** envie `dist\VerumToolkit_v<versão>_Setup.exe` (Passo 2, Opção C): quem
recebe executa com dois cliques — sem admin, sem PowerShell e sem extrair zip.

---

## Atualizar uma versão já instalada

O Navisworks **trava** a DLL enquanto roda (`Roamer.exe`). Para atualizar:
1. **Feche** o Navisworks.
2. Rode `./Package-Plugin.ps1 -Install` (ou substitua a pasta do bundle manualmente).
3. Reabra o Navisworks.

## Alternativa — pasta `Plugins\` clássica

Sem bundle, a DLL precisa ficar numa **subpasta com o mesmo nome do arquivo**:

```
C:\Program Files\Autodesk\Navisworks Manage 2026\Plugins\AutoViewTool\AutoViewTool.dll
```

Exige admin (UAC) e fechar o Navisworks antes de copiar. O `deploy.ps1` cobre esse caminho.

## Troubleshooting — Bundle não aparece

| Problema | Causa | Solução |
|----------|-------|---------|
| Bundle não carrega no Navisworks | Pasta está no local errado | Verifique: `%APPDATA%\Autodesk\ApplicationPlugins\AutoViewTool.bundle\` ou `%PROGRAMDATA%\Autodesk\ApplicationPlugins\AutoViewTool.bundle\` |
| Botões não aparecem na ribbon | Navisworks não foi reiniciado | Feche e reabra o Navisworks. A DLL é bloqueada enquanto o app roda. |
| Erro "versão incompatível" | `SeriesMin/SeriesMax` fora do range | Confirme no `PackageContents.xml` que `SeriesMin/SeriesMax` cobrem sua versão: 2026 = `Nw23` |
| Bundle instalado mas plugin continua carregando de `Plugins\` | Duas cópias instaladas | Remova `C:\Program Files\...\Plugins\AutoViewTool\` (via admin) |
| `Package-Plugin.ps1 -Install` falha com "acesso negado" | Terminal sem privilégios | Rode como Administrador (`Run as administrator`) |

**Para verificar manualmente:**
```powershell
# Listar bundles instalados do usuário
ls "$env:APPDATA\Autodesk\ApplicationPlugins\"

# Listar bundles instalados globalmente (requer admin)
ls "$env:PROGRAMDATA\Autodesk\ApplicationPlugins\"

# Confirmar que PackageContents.xml está presente
ls "$env:APPDATA\Autodesk\ApplicationPlugins\AutoViewTool.bundle\PackageContents.xml"
```

---

## Observações

- **Não** inclua as DLLs `Autodesk.Navisworks.*` no bundle — o host as fornece em runtime.
- `dist\` é saída gerada (como `bin\`/`obj\`); não precisa versionar.
- Para mirar outro ano, ajuste `SeriesMin/SeriesMax` no `PackageContents.xml`
  (2025=`Nw22`, 2027=`Nw24`) e recompile contra o SDK daquele ano.
