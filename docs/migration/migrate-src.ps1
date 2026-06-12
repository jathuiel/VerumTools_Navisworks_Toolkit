# migrate-src.ps1 — Migração dos fontes para a solução consolidada NavisworksToolkit
# Executado em 2026-06-11 (ETAPAS 5–7 do plano de consolidação).
# Copia (nunca move) os fontes dos dois projetos para /src, renomeando namespaces
# e atualizando XAML (x:Class, clr-namespace, pack URIs). Originais intactos.

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

# ── Estrutura de pastas ───────────────────────────────────────────────────────
$dirs = @(
  'src\Core', 'src\Shared', 'src\UI', 'src\Assets', 'src\Resources\Icons', 'src\en-US',
  'src\Modules\ViewBuilder', 'src\Modules\ModelCleaner', 'src\Modules\ImageExporter',
  'src\Modules\SelectionInspector', 'src\Modules\AttributeLab', 'src\Modules\SetColoring',
  'templates'
)
$dirs | ForEach-Object { New-Item -ItemType Directory -Force $_ | Out-Null }

# ── Transformação de arquivos C# ──────────────────────────────────────────────
function Migrate-Cs([string]$src, [string]$dst, [string]$ns) {
  $text = Get-Content $src -Raw
  $text = $text -replace '(?m)^using AutoViewTool(\.\w+)?;\r?\n', ''
  $text = $text -replace '(?m)^namespace (AutoViewTool(\.\w+)?|SetAtributesToolkit)\b', "namespace $ns"
  $text = $text -replace '(?m)^namespace ', "using NavisworksToolkit.Core;`r`nusing NavisworksToolkit.Shared;`r`n`r`nnamespace "
  Set-Content -Path $dst -Value $text -Encoding utf8BOM -NoNewline
  Write-Host "cs   $src -> $dst [$ns]"
}

# ── Transformação de XAML de janelas ──────────────────────────────────────────
function Migrate-Xaml([string]$src, [string]$dst, [string]$ns) {
  $text = Get-Content $src -Raw
  $text = $text -replace 'x:Class="(?:AutoViewTool\.UI|SetAtributesToolkit)\.(\w+)"', ('x:Class="' + $ns + '.$1"')
  $text = $text -replace 'clr-namespace:SetAtributesToolkit"', ("clr-namespace:$ns`"")
  $text = $text -replace '/AutoViewTool;component/src/UI/Theme\.xaml', '/NavisworksToolkit;component/UI/Theme.xaml'
  $text = $text -replace '/SetAtributesToolkit;component/Themes/DesignSystem\.xaml', '/NavisworksToolkit;component/UI/DesignSystem.xaml'
  Set-Content -Path $dst -Value $text -Encoding utf8BOM -NoNewline
  Write-Host "xaml $src -> $dst [$ns]"
}

# ── Projeto A (Auto_ViewTool) ─────────────────────────────────────────────────
$A = 'Auto_ViewTool'
$mapA = @(
  @("$A\src\Core\NavisworksInterop.cs",   'src\Core\NavisworksInterop.cs',                   'NavisworksToolkit.Core'),
  @("$A\src\Core\SavedItemTree.cs",       'src\Core\SavedItemTree.cs',                       'NavisworksToolkit.Core'),
  @("$A\src\Core\PerfLog.cs",             'src\Core\PerfLog.cs',                             'NavisworksToolkit.Core'),
  @("$A\src\Core\XlsxFile.cs",            'src\Shared\XlsxFile.cs',                          'NavisworksToolkit.Shared'),
  @("$A\src\Models\ISelectableItem.cs",   'src\Shared\ISelectableItem.cs',                   'NavisworksToolkit.Shared'),
  @("$A\src\UI\UiCommon.cs",              'src\Shared\UiCommon.cs',                          'NavisworksToolkit.Shared'),
  @("$A\src\UI\SelectableListUi.cs",      'src\Shared\SelectableListUi.cs',                  'NavisworksToolkit.Shared'),
  @("$A\src\Core\SelectionSetManager.cs", 'src\Modules\ViewBuilder\SelectionSetManager.cs',  'NavisworksToolkit.Modules.ViewBuilder'),
  @("$A\src\Core\IsolationHandler.cs",    'src\Modules\ViewBuilder\IsolationHandler.cs',     'NavisworksToolkit.Modules.ViewBuilder'),
  @("$A\src\Core\ViewpointManager.cs",    'src\Modules\ViewBuilder\ViewpointManager.cs',     'NavisworksToolkit.Modules.ViewBuilder'),
  @("$A\src\Core\TemplateManager.cs",     'src\Modules\ViewBuilder\TemplateManager.cs',      'NavisworksToolkit.Modules.ViewBuilder'),
  @("$A\src\Models\SelectionSetData.cs",  'src\Modules\ViewBuilder\SelectionSetData.cs',     'NavisworksToolkit.Modules.ViewBuilder'),
  @("$A\src\Models\ViewpointData.cs",     'src\Modules\ViewBuilder\ViewpointData.cs',        'NavisworksToolkit.Modules.ViewBuilder'),
  @("$A\src\Models\ViewpointTemplateRow.cs",'src\Modules\ViewBuilder\ViewpointTemplateRow.cs','NavisworksToolkit.Modules.ViewBuilder'),
  @("$A\src\UI\MainWindow.xaml.cs",       'src\Modules\ViewBuilder\MainWindow.xaml.cs',      'NavisworksToolkit.Modules.ViewBuilder'),
  @("$A\src\Core\CleanupManager.cs",      'src\Modules\ModelCleaner\CleanupManager.cs',      'NavisworksToolkit.Modules.ModelCleaner'),
  @("$A\src\Models\CleanupItemData.cs",   'src\Modules\ModelCleaner\CleanupItemData.cs',     'NavisworksToolkit.Modules.ModelCleaner'),
  @("$A\src\UI\CleanupWindow.xaml.cs",    'src\Modules\ModelCleaner\CleanupWindow.xaml.cs',  'NavisworksToolkit.Modules.ModelCleaner'),
  @("$A\src\Core\ExportManager.cs",       'src\Modules\ImageExporter\ExportManager.cs',      'NavisworksToolkit.Modules.ImageExporter'),
  @("$A\src\Models\ExportItemData.cs",    'src\Modules\ImageExporter\ExportItemData.cs',     'NavisworksToolkit.Modules.ImageExporter'),
  @("$A\src\UI\ExportWindow.xaml.cs",     'src\Modules\ImageExporter\ExportWindow.xaml.cs',  'NavisworksToolkit.Modules.ImageExporter')
)
foreach ($m in $mapA) { Migrate-Cs $m[0] $m[1] $m[2] }

$xamlA = @(
  @("$A\src\UI\MainWindow.xaml",    'src\Modules\ViewBuilder\MainWindow.xaml',     'NavisworksToolkit.Modules.ViewBuilder'),
  @("$A\src\UI\CleanupWindow.xaml", 'src\Modules\ModelCleaner\CleanupWindow.xaml', 'NavisworksToolkit.Modules.ModelCleaner'),
  @("$A\src\UI\ExportWindow.xaml",  'src\Modules\ImageExporter\ExportWindow.xaml', 'NavisworksToolkit.Modules.ImageExporter')
)
foreach ($m in $xamlA) { Migrate-Xaml $m[0] $m[1] $m[2] }

# ── Projeto B (SetAtributesToolkit) ───────────────────────────────────────────
$B = 'SetAtributesToolkit'
$mapB = @(
  @("$B\Helpers\AsyncRelayCommand.cs",        'src\Shared\AsyncRelayCommand.cs',         'NavisworksToolkit.Shared'),
  @("$B\Helpers\ExceptionHelper.cs",          'src\Shared\ExceptionHelper.cs',           'NavisworksToolkit.Shared'),
  @("$B\Helpers\PropertyExtractionHelper.cs", 'src\Shared\PropertyExtractionHelper.cs',  'NavisworksToolkit.Shared'),
  @("$B\Helpers\SelectionSetCache.cs",        'src\Shared\SelectionSetCache.cs',         'NavisworksToolkit.Shared'),
  @("$B\Helpers\VerumSchema.cs",              'src\Shared\VerumSchema.cs',               'NavisworksToolkit.Shared'),
  @("$B\Models\CheckableItem.cs",             'src\Shared\CheckableItem.cs',             'NavisworksToolkit.Shared'),
  @("$B\Models\SelectionSetItem.cs",          'src\Shared\SelectionSetItem.cs',          'NavisworksToolkit.Shared'),
  @("$B\Models\AtributoCustom.cs",            'src\Modules\AttributeLab\AtributoCustom.cs',          'NavisworksToolkit.Modules.AttributeLab'),
  @("$B\Models\AttributeEntry.cs",            'src\Modules\AttributeLab\AttributeEntry.cs',          'NavisworksToolkit.Modules.AttributeLab'),
  @("$B\Models\LabCatNode.cs",                'src\Modules\AttributeLab\LabCatNode.cs',              'NavisworksToolkit.Modules.AttributeLab'),
  @("$B\Models\LabPropNode.cs",               'src\Modules\AttributeLab\LabPropNode.cs',             'NavisworksToolkit.Modules.AttributeLab'),
  @("$B\Models\SetAssignment.cs",             'src\Modules\AttributeLab\SetAssignment.cs',           'NavisworksToolkit.Modules.AttributeLab'),
  @("$B\Services\AtributoService.cs",         'src\Modules\AttributeLab\AtributoService.cs',         'NavisworksToolkit.Modules.AttributeLab'),
  @("$B\Services\AttributeTemplateSerializer.cs",'src\Modules\AttributeLab\AttributeTemplateSerializer.cs','NavisworksToolkit.Modules.AttributeLab'),
  @("$B\Views\NativeAttrLabWindow.xaml.cs",   'src\Modules\AttributeLab\NativeAttrLabWindow.xaml.cs','NavisworksToolkit.Modules.AttributeLab'),
  @("$B\Models\InspectorModels.cs",           'src\Modules\SelectionInspector\InspectorModels.cs',   'NavisworksToolkit.Modules.SelectionInspector'),
  @("$B\Views\SelectionInspectorWindow.xaml.cs",'src\Modules\SelectionInspector\SelectionInspectorWindow.xaml.cs','NavisworksToolkit.Modules.SelectionInspector'),
  @("$B\Models\ColoringRule.cs",              'src\Modules\SetColoring\ColoringRule.cs',             'NavisworksToolkit.Modules.SetColoring'),
  @("$B\Services\SetColoringService.cs",      'src\Modules\SetColoring\SetColoringService.cs',       'NavisworksToolkit.Modules.SetColoring'),
  @("$B\ViewModels\SetColoringRulesViewModel.cs",'src\Modules\SetColoring\SetColoringRulesViewModel.cs','NavisworksToolkit.Modules.SetColoring'),
  @("$B\Views\SetColoringRulesWindow.xaml.cs",'src\Modules\SetColoring\SetColoringRulesWindow.xaml.cs','NavisworksToolkit.Modules.SetColoring')
)
foreach ($m in $mapB) { Migrate-Cs $m[0] $m[1] $m[2] }

$xamlB = @(
  @("$B\Views\NativeAttrLabWindow.xaml",     'src\Modules\AttributeLab\NativeAttrLabWindow.xaml',          'NavisworksToolkit.Modules.AttributeLab'),
  @("$B\Views\SelectionInspectorWindow.xaml",'src\Modules\SelectionInspector\SelectionInspectorWindow.xaml','NavisworksToolkit.Modules.SelectionInspector'),
  @("$B\Views\SetColoringRulesWindow.xaml",  'src\Modules\SetColoring\SetColoringRulesWindow.xaml',        'NavisworksToolkit.Modules.SetColoring')
)
foreach ($m in $xamlB) { Migrate-Xaml $m[0] $m[1] $m[2] }

# ── Temas (sem transformação interna — copiados como estão) ───────────────────
Copy-Item "$A\src\UI\Theme.xaml" 'src\UI\Theme.xaml'
Copy-Item "$A\src\UI\THEME_MAP.md" 'src\UI\THEME_MAP.md'
Copy-Item "$B\Themes\DesignSystem.xaml" 'src\UI\DesignSystem.xaml'

# ── Assets embutidos, ícones do ribbon, template ──────────────────────────────
Copy-Item 'assets\logos\Icone.png' 'src\Assets\Icone.png'
Copy-Item 'assets\logos\logo_partners.png' 'src\Assets\logo_partners.png'
Copy-Item 'assets\icons\*.png' 'src\Resources\Icons\'
Copy-Item "$A\templates\AutoViewTool_Template.xlsx" 'templates\AutoViewTool_Template.xlsx'

Write-Host "`nMigração concluída."
