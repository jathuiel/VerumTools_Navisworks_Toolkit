# Auto ViewTool Creator - Development Guide

## Project Context

**Objective**: Create a Navisworks plugin that automatically creates viewpoints by isolating objects from selection sets.

**Stack**: C# .NET Framework 4.8, WPF (XAML), Navisworks COM Interop

**Architecture**: 
- Core Layer (NavisworksInterop, SelectionSetManager, IsolationHandler, ViewpointManager)
- UI Layer (MainWindow XAML, ViewModel pattern)
- Model Layer (SelectionSetData, ViewpointData DTOs)
- AddIn Layer (IAddInPlugin implementation)

## Development Priorities

1. **Stability** - Robust error handling, null checks, exception wrapping
2. **Performance** - Batch COM operations, HashSet lookups for large models
3. **UX** - Clear feedback, status messages, validation before operations
4. **Extensibility** - Clean separation of concerns, reusable managers

## Code Patterns

### Error Handling
All public methods should:
- Validate inputs with guard clauses
- Wrap exceptions with context
- Throw InvalidOperationException for business logic errors
- Use try/catch at UI boundaries

Example:
```csharp
public void DoSomething(string input)
{
    if (string.IsNullOrWhiteSpace(input))
        throw new ArgumentException("Input cannot be empty", nameof(input));
    
    try
    {
        // operation
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Failed to do something", ex);
    }
}
```

### Null Checks
Use guard clauses at entry points:
```csharp
var doc = _interop.GetActiveDocument(); // Already validates internally
if (collection == null)
    return new List<T>();
```

### Resource Management
Implement IDisposable for components managing COM references:
```csharp
public class ComponentName : IDisposable
{
    private bool _disposed;
    
    public void Dispose()
    {
        if (_disposed) return;
        try { /* cleanup */ } finally { _disposed = true; }
    }
    
    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(GetType().Name);
    }
}
```

## File Structure Rules

- `/src/Core/` - Business logic, no UI dependencies, pure Navisworks interactions
- `/src/UI/` - WPF windows, event handlers, user interaction
- `/src/Models/` - DTOs and data classes, serializable
- `AddIn.cs` - Entry point, minimal logic

## Common Navisworks Patterns

### Getting Active Document
```csharp
var doc = Application.ActiveDocument;
if (doc == null) throw new InvalidOperationException("No active document");
```

### Iterating ModelItems
```csharp
foreach (var item in collection)
{
    if (item == null) continue;
    // Safe to use item here
}
```

### Creating Viewpoints
```csharp
var vp = new Viewpoint { Name = "...", Description = "..." };
vp.Position = doc.CurrentViewpoint.Position;
doc.Viewpoints.Add(vp);
```

### Hiding/Showing Items
```csharp
item.IsHidden = true;  // Hide
item.IsHidden = false; // Show
```

## Testing Strategy

Since this is a plugin:
1. Manual testing in Navisworks is required
2. Unit tests for Core logic (managers) are preferred
3. Integration tests should verify COM interactions
4. UI testing requires Navisworks instance running

## Build Instructions

```powershell
# Ensure paths in .csproj point to Navisworks 2026 SDK
# Compile
msbuild Auto_ViewTool.csproj /p:Configuration=Release

# Output: bin/Release/AutoViewTool.dll
# Copy to: C:\Program Files\Autodesk\Navisworks Manager 2026\Plugins\
```

## Versioning & Snapshots

O projeto mantém histórico completo de versões via pacotes `.zip` (um por marco),
permitindo rollback: para recuperar um estado anterior, basta extrair o `.zip`.

- **Fonte da versão**: arquivo `VERSION` na raiz (SemVer `MAJOR.MINOR.PATCH`).
- **Script**: `Snapshot-Version.ps1` empacota fonte + projeto + docs (exclui
  `bin/obj/.vs/.git/.claude`) e grava em `..\Auto_ViewTool_releases\`.
- **Padrão de nome**: `Auto_ViewTool_v[Versão]_AAAA-MM-DD_HH-MM.zip`.
- **Histórico**: cada release é registrada em `..\Auto_ViewTool_releases\RELEASES.md`
  com data, versão, arquivo, contagem, tamanho e SHA-256 (integridade).

**Gerar um snapshot ao final de cada marco** (escolha o incremento conforme o tipo):

```powershell
# Correção relevante  -> Patch (x.y.Z+1)
./Snapshot-Version.ps1 -Bump Patch -Message "Descrição da correção"

# Nova funcionalidade -> Minor (x.Y+1.0)
./Snapshot-Version.ps1 -Bump Minor -Message "Descrição da feature"

# Mudança incompatível -> Major (X+1.0.0)
./Snapshot-Version.ps1 -Bump Major -Message "Descrição da mudança"

# Empacotar incluindo a DLL já compilada (bin\Release)
./Snapshot-Version.ps1 -Bump Patch -Message "..." -IncludeBuild
```

## Next Tasks (by priority)

1. ✅ Scaffold project structure
2. ✅ Create Core managers (NavisworksInterop, SelectionSetManager, IsolationHandler, ViewpointManager)
3. ✅ Create UI shell (MainWindow.xaml)
4. ⬜ Test compilation and Navisworks integration
5. ⬜ Add error handling in MainWindow event handlers
6. ⬜ Implement batch isolation for performance
7. ⬜ Add custom properties to viewpoints
8. ⬜ Unit tests for managers

## Known Issues & Limitations

- Plugin requires Navisworks 2026 installed
- COM interop requires target architecture matching (x64)
- Selection sets are immutable once created
- Viewpoints are saved at document level

## Performance Considerations

- `GetAllModelItems()` is O(n) - consider caching for large models
- Batch hide operations are more efficient than individual item changes
- Use HashSet for O(1) lookups when checking visibility state
