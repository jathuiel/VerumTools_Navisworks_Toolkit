# Setup & Build Instructions

## ✅ SDK Verification (2026-06-10)

### Identificar sua instalação do Navisworks 2026

Escolha o que você tem instalado:

| Produto | Caminho padrão | Observação |
|---------|---|---|
| **Navisworks Simulate 2026** | `C:\Program Files\Autodesk\Navisworks Simulate 2026\` | Ambiente de simulação e visualização |
| **Navisworks Manage 2026** | `C:\Program Files\Autodesk\Navisworks Manage 2026\` | Ambiente de gestão e BIM (inclui coordenação) |

> O plugin funciona em ambas as versões. Use o caminho correto nos passos abaixo.

### Verificação de DLLs necessárias

Confirme que os arquivos abaixo existem no seu diretório de instalação:

```
C:\Program Files\Autodesk\Navisworks [Simulate|Manage] 2026\
├── Autodesk.Navisworks.Api.dll ✓
├── Autodesk.Navisworks.Interop.ComApi.dll ✓
├── Autodesk.Navisworks.Api.xml (IntelliSense)
└── Autodesk.Navisworks.Interop.ComApi.xml (IntelliSense)
```

**Verificar via PowerShell:**
```powershell
# Substitua pelo seu caminho (Simulate ou Manage)
$NavisPath = "C:\Program Files\Autodesk\Navisworks Simulate 2026"
ls "$NavisPath\Autodesk.Navisworks*.dll"
```

### Configurar .csproj com o caminho correto

Abra `Auto_ViewTool.csproj` e localize as referências. Elas devem apontar para **seu diretório específico**:

```xml
<!-- Exemplo para Navisworks Simulate 2026 -->
<Reference Include="Autodesk.Navisworks.Api, Version=26.0.0.0">
  <HintPath>C:\Program Files\Autodesk\Navisworks Simulate 2026\Autodesk.Navisworks.Api.dll</HintPath>
</Reference>

<Reference Include="Autodesk.Navisworks.Interop.ComApi, Version=26.0.0.0">
  <HintPath>C:\Program Files\Autodesk\Navisworks Simulate 2026\Autodesk.Navisworks.Interop.ComApi.dll</HintPath>
</Reference>

<!-- OU para Navisworks Manage 2026 (se usá-lo) -->
<!-- <Reference Include="Autodesk.Navisworks.Api, Version=26.0.0.0">
  <HintPath>C:\Program Files\Autodesk\Navisworks Manage 2026\Autodesk.Navisworks.Api.dll</HintPath>
</Reference> -->
```

Se os caminhos estiverem errados, a compilação falhará com "Autodesk.Navisworks.Api not found".

## 🔨 Build Steps

### Prerequisites
- [x] Visual Studio 2022+ (C# support)
- [x] .NET Framework 4.8 SDK
- [x] Navisworks Simulate 2026 installed
- [x] HintPath references verified

### Compile
```powershell
# From project root
msbuild Auto_ViewTool.csproj /p:Configuration=Release

# Or in Visual Studio
# Build > Build Solution (Ctrl+Shift+B)
```

### Output
```
bin/Release/AutoViewTool.dll     (Main assembly)
bin/Release/AutoViewTool.pdb     (Debug symbols)
```

### Expected Warnings (Safe to Ignore)
- "HintPath does not match ReferencePath" — Normal for relative refs
- Metadata version mismatches — Handled by version binding

## 📦 Installation in Navisworks

> ⚠️ **Regra obrigatória:** O Navisworks só carrega um plugin .NET se a DLL estiver numa **subpasta cujo nome seja igual ao da DLL** (sem extensão).
> Como a DLL é `AutoViewTool.dll`, a subpasta deve se chamar `AutoViewTool`.

### Opção A — Instalar em Navisworks Simulate 2026

```powershell
$dest = "C:\Program Files\Autodesk\Navisworks Simulate 2026\Plugins\AutoViewTool"
New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item "bin\Release\AutoViewTool.dll" -Destination $dest
# (opcional) copie também os PDB para depuração:
# Copy-Item "bin\Release\AutoViewTool.pdb" -Destination $dest
```

### Opção B — Instalar em Navisworks Manage 2026

```powershell
$dest = "C:\Program Files\Autodesk\Navisworks Manage 2026\Plugins\AutoViewTool"
New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item "bin\Release\AutoViewTool.dll" -Destination $dest
```

### Opção C — Instalar em ambas (desenvolvimento em ambas)

```powershell
# Simulate
$sim = "C:\Program Files\Autodesk\Navisworks Simulate 2026\Plugins\AutoViewTool"
New-Item -ItemType Directory -Force $sim | Out-Null
Copy-Item "bin\Release\AutoViewTool.dll" -Destination $sim

# Manage
$mgm = "C:\Program Files\Autodesk\Navisworks Manage 2026\Plugins\AutoViewTool"
New-Item -ItemType Directory -Force $mgm | Out-Null
Copy-Item "bin\Release\AutoViewTool.dll" -Destination $mgm
```

### Após copiar: Reiniciar e Verificar

1. **Feche** o Navisworks completamente (ele trava a DLL enquanto roda).
2. **Reabra** Navisworks (Simulate, Manage ou ambas).
3. **Verifique:**
   - Vá a **Add-Ins > Add-In Plugins**
   - Procure por **"Auto ViewTool Creator"**
   - O status deve mostrar **Loaded** (não "Failed")
4. Na aba **Add-Ins** do ribbon, você deve ver os botões:
   - Auto ViewTool
   - Auto Cleanup
   - Auto Export

## 🧪 Test Build

```powershell
# Quick compile check
dotnet build Auto_ViewTool.csproj --configuration Debug

# If using older msbuild
msbuild Auto_ViewTool.csproj /p:Configuration=Debug /verbosity:minimal
```

## 📋 Checklist

- [x] SDK paths verified in `.csproj`
- [x] Navisworks 2026 Simulate installed
- [x] DLL references point to correct location
- [x] .NET Framework 4.8 available
- [ ] Initial build successful
- [ ] Plugin loads in Navisworks
- [ ] UI window appears without errors
- [ ] Selection sets enumerate correctly

## 🐛 Troubleshooting

### Build Error: "Autodesk.Navisworks.Api not found"

**Causa**: HintPath incorreto no `.csproj`.

**Solução**:
1. Verifique qual versão você instalou (Simulate, Manage ou ambas).
2. Confirme o caminho correto:
   ```powershell
   # Para Simulate
   dir "C:\Program Files\Autodesk\Navisworks Simulate 2026\Autodesk.Navisworks*.dll"
   
   # Para Manage
   dir "C:\Program Files\Autodesk\Navisworks Manage 2026\Autodesk.Navisworks*.dll"
   ```
3. Atualize `.csproj` com o caminho que existe.

### Plugin doesn't load in Navisworks

**Causa**: Estrutura de pasta errada ou plugin não reiniciado.

**Solução**:
- ✓ Confirme a pasta existe: `Plugins\AutoViewTool\AutoViewTool.dll`
- ✓ Verifique que a DLL está compilada para x64 (Navisworks 2026 é 64-bit)
- ✓ **Feche completamente o Navisworks** antes de copiar a DLL
- ✓ Reabra e verifique em **Add-Ins > Add-In Plugins** se o status é **Loaded**
- Check logs: `%temp%\Navisworks\logs\`

### IntelliSense not working

**Solução**:
- Confirme que os arquivos `.xml` estão no mesmo diretório das DLLs:
  - `Autodesk.Navisworks.Api.xml`
  - `Autodesk.Navisworks.Interop.ComApi.xml`
- Feche e reabra Visual Studio

### "Plugins\AutoViewTool não encontrado" ao compilar

Se o `.csproj` contém pós-build que copia a DLL, confirme que o caminho está certo:
```xml
<!-- Verificar este bloco se existir em .csproj -->
<Target Name="AfterBuild">
  <Copy SourceFiles="..." DestinationFolder="..." />
</Target>
```
Ajuste para o seu diretório de instalação (Simulate ou Manage).

## 📚 Resources

- [Navisworks Plugin Development](https://help.autodesk.com/view/NAV/2026/ENU/?guid=GUID-00000000-0000-0000-0000-000000000000)
- [Autodesk Navisworks API Docs](https://www.autodesk.com/developer/navisworks)
- [Project README](./README.md)
- [Development Guide](./CLAUDE.md)
