using Autodesk.Navisworks.Api;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using NwApp = Autodesk.Navisworks.Api.Application;
using WpfColor = System.Windows.Media.Color;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.SelectionInspector
{
    public partial class SelectionInspectorWindow : Window
    {
        private List<ModelItem> _allItems = new List<ModelItem>();
        private readonly ObservableCollection<CheckableItem> _categories = new ObservableCollection<CheckableItem>();
        private List<ColDef> _currentColumns = new List<ColDef>();
        private bool _rebuilding;

        public SelectionInspectorWindow()
        {
            InitializeComponent();
            UiCommon.ApplyBranding(this, PartnersLogo);
            CategoriesList.ItemsSource = _categories;

            Loaded += async (s, e) =>
            {
                Activate();
                Topmost = true;
                Topmost = false;
                await LoadSelectionAsync();
            };
        }

        private async Task LoadSelectionAsync()
        {
            SetStatus("Carregando selecao...", StatusLevel.Info);

            var doc = NwApp.ActiveDocument;
            if (doc == null || doc.CurrentSelection.IsEmpty)
            {
                SetStatus("Nenhum elemento selecionado no modelo.", StatusLevel.Warning);
                TxtSubtitle.Text = "Nenhum elemento selecionado.";
                return;
            }

            _allItems = doc.CurrentSelection.SelectedItems.Cast<ModelItem>().ToList();

            var catNames = await Task.Run(() =>
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in _allItems)
                {
                    foreach (PropertyCategory cat in item.PropertyCategories)
                    {
                        if (!string.IsNullOrWhiteSpace(cat.DisplayName))
                            names.Add(cat.DisplayName);
                    }
                }

                return names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            });

            _categories.Clear();
            foreach (var name in catNames)
            {
                var entry = new CheckableItem { Name = name };
                entry.PropertyChanged += OnCategoryCheckedChanged;
                _categories.Add(entry);
            }

            TxtSubtitle.Text = $"{_allItems.Count} elemento(s) selecionado(s) | {_categories.Count} categorias";
            SetStatus($"{_allItems.Count} elemento(s) | {_categories.Count} categoria(s) disponiveis.", StatusLevel.Info);
        }

        private async void OnCategoryCheckedChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CheckableItem.IsChecked))
                await RebuildGridAsync();
        }

        private async Task RebuildGridAsync()
        {
            if (_rebuilding) return;
            _rebuilding = true;

            try
            {
                var checkedCats = _categories.Where(c => c.IsChecked).Select(c => c.Name).ToList();

                InspectorGrid.Columns.Clear();
                InspectorGrid.ItemsSource = null;

                if (checkedCats.Count == 0 || _allItems.Count == 0)
                {
                    TxtEmptyHint.Visibility = Visibility.Visible;
                    BtnExportCsv.IsEnabled = false;
                    BtnExportExcel.IsEnabled = false;
                    BtnExportClip.IsEnabled = false;
                    TxtStatus.Text = $"{_allItems.Count} elemento(s) | 0 categorias | 0 colunas";
                    SetStatus("Marque categorias para visualizar.", StatusLevel.Info);
                    return;
                }

                TxtEmptyHint.Visibility = Visibility.Collapsed;
                SetStatus("Construindo tabela...", StatusLevel.Info);

                var result = await Task.Run(() => BuildDataTable(checkedCats));
                _currentColumns = result.Columns;

                var headerTemplate = (DataTemplate)FindResource("ColHeaderTemplate");
                foreach (var col in result.Columns)
                {
                    InspectorGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = new ColumnHeaderData { Category = col.Category, Property = col.Property },
                        HeaderTemplate = headerTemplate,
                        Binding = new Binding($"[{col.Key}]"),
                        Width = new DataGridLength(1, DataGridLengthUnitType.Auto),
                        MinWidth = 110,
                        IsReadOnly = true
                    });
                }

                InspectorGrid.ItemsSource = result.Table.DefaultView;

                BtnExportCsv.IsEnabled = true;
                BtnExportExcel.IsEnabled = true;
                BtnExportClip.IsEnabled = true;
                SetStatus($"{_allItems.Count} elemento(s) | {checkedCats.Count} categoria(s) | {result.Columns.Count} coluna(s)", StatusLevel.Success);
            }
            catch (Exception ex)
            {
                SetStatus($"Erro ao construir grid: {ExceptionHelper.UnwrapMessage(ex)}", StatusLevel.Error);
            }
            finally
            {
                _rebuilding = false;
            }
        }

        private BuildResult BuildDataTable(List<string> checkedCats)
        {
            var catPropMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var catName in checkedCats)
                catPropMap[catName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in _allItems)
            {
                foreach (PropertyCategory cat in item.PropertyCategories)
                {
                    if (!catPropMap.TryGetValue(cat.DisplayName, out var propSet)) continue;
                    foreach (DataProperty prop in cat.Properties)
                    {
                        if (!string.IsNullOrWhiteSpace(prop.DisplayName))
                            propSet.Add(prop.DisplayName);
                    }
                }
            }

            var columns = new List<ColDef>();
            int idx = 0;
            foreach (var catName in checkedCats)
            {
                foreach (var propName in catPropMap[catName].OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                {
                    columns.Add(new ColDef
                    {
                        Category = catName,
                        Property = propName,
                        Key = $"c{idx++}"
                    });
                }
            }

            var dt = new DataTable();
            foreach (var col in columns)
                dt.Columns.Add(new DataColumn(col.Key, typeof(string)));

            foreach (var item in _allItems)
            {
                var catCache = new Dictionary<string, PropertyCategory>(StringComparer.OrdinalIgnoreCase);
                foreach (PropertyCategory cat in item.PropertyCategories)
                    catCache[cat.DisplayName] = cat;

                var row = dt.NewRow();
                foreach (var col in columns)
                {
                    if (!catCache.TryGetValue(col.Category, out var category)) continue;
                    foreach (DataProperty prop in category.Properties)
                    {
                        if (!string.Equals(prop.DisplayName, col.Property, StringComparison.OrdinalIgnoreCase)) continue;
                        row[col.Key] = PropertyExtractionHelper.SafeValue(prop.Value);
                        break;
                    }
                }
                dt.Rows.Add(row);
            }

            return new BuildResult { Table = dt, Columns = columns };
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cat in _categories) cat.IsChecked = true;
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cat in _categories) cat.IsChecked = false;
        }

        private void BtnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.SaveFileDialog
            {
                Title = "Exportar CSV",
                Filter = "CSV (*.csv)|*.csv",
                DefaultExt = "csv",
                FileName = "selection_inspector"
            };

            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                ExportToCsv(dlg.FileName);
                SetStatus($"CSV exportado: {Path.GetFileName(dlg.FileName)}", StatusLevel.Success);
            }
            catch (Exception ex)
            {
                SetStatus($"Erro ao exportar CSV: {ExceptionHelper.UnwrapMessage(ex)}", StatusLevel.Error);
            }
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.SaveFileDialog
            {
                Title = "Exportar Excel XML",
                Filter = "Excel XML (*.xml)|*.xml",
                DefaultExt = "xml",
                FileName = "selection_inspector"
            };

            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                ExportToExcelXml(dlg.FileName);
                SetStatus($"Excel XML exportado: {Path.GetFileName(dlg.FileName)}", StatusLevel.Success);
            }
            catch (Exception ex)
            {
                SetStatus($"Erro ao exportar Excel: {ExceptionHelper.UnwrapMessage(ex)}", StatusLevel.Error);
            }
        }

        private void BtnExportClip_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sb = BuildTsv();
                Clipboard.SetText(sb.ToString());
                SetStatus("Dados copiados para a area de transferencia.", StatusLevel.Success);
            }
            catch (Exception ex)
            {
                SetStatus($"Erro ao copiar: {ExceptionHelper.UnwrapMessage(ex)}", StatusLevel.Error);
            }
        }

        private void ExportToCsv(string path)
        {
            var sb = BuildCsv();
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        private void ExportToExcelXml(string path)
        {
            var sb = BuildExcelXml();
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        private StringBuilder BuildCsv()
        {
            var sb = new StringBuilder();
            if (!(InspectorGrid.ItemsSource is DataView dv)) return sb;

            var headers = _currentColumns.Select(c => EscapeCsv($"{c.Category} / {c.Property}"));
            sb.AppendLine(string.Join(";", headers));

            foreach (DataRowView rv in dv)
            {
                var vals = _currentColumns.Select(c => EscapeCsv(rv.Row[c.Key]?.ToString() ?? string.Empty));
                sb.AppendLine(string.Join(";", vals));
            }

            return sb;
        }

        private StringBuilder BuildTsv()
        {
            var sb = new StringBuilder();
            if (!(InspectorGrid.ItemsSource is DataView dv)) return sb;

            sb.AppendLine(string.Join("\t", _currentColumns.Select(c => $"{c.Category} / {c.Property}")));
            foreach (DataRowView rv in dv)
                sb.AppendLine(string.Join("\t", _currentColumns.Select(c => rv.Row[c.Key]?.ToString() ?? string.Empty)));

            return sb;
        }

        private StringBuilder BuildExcelXml()
        {
            var sb = new StringBuilder();
            if (!(InspectorGrid.ItemsSource is DataView dv)) return sb;

            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
            sb.AppendLine(" <Styles>");
            sb.AppendLine("  <Style ss:ID=\"header\"><Font ss:Bold=\"1\" ss:Size=\"10\"/>");
            sb.AppendLine("   <Interior ss:Color=\"#D9E1F2\" ss:Pattern=\"Solid\"/></Style>");
            sb.AppendLine(" </Styles>");
            sb.AppendLine(" <Worksheet ss:Name=\"Selection Inspector\">");
            sb.AppendLine($"  <Table ss:ExpandedColumnCount=\"{_currentColumns.Count}\" ss:ExpandedRowCount=\"{dv.Count + 1}\">");

            sb.Append("   <Row>");
            foreach (var col in _currentColumns)
                sb.Append($"<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">{EscapeXml($"{col.Category} / {col.Property}")}</Data></Cell>");
            sb.AppendLine("</Row>");

            foreach (DataRowView rv in dv)
            {
                sb.Append("   <Row>");
                foreach (var col in _currentColumns)
                    sb.Append($"<Cell><Data ss:Type=\"String\">{EscapeXml(rv.Row[col.Key]?.ToString() ?? string.Empty)}</Data></Cell>");
                sb.AppendLine("</Row>");
            }

            sb.AppendLine("  </Table>");
            sb.AppendLine(" </Worksheet>");
            sb.AppendLine("</Workbook>");

            return sb;
        }

        private static string EscapeCsv(string s)
        {
            if (s == null) return string.Empty;
            return (s.Contains(';') || s.Contains('"') || s.Contains('\n'))
                ? $"\"{s.Replace("\"", "\"\"")}\""
                : s;
        }

        private static string EscapeXml(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private enum StatusLevel { Info, Success, Warning, Error }

        private void SetStatus(string message, StatusLevel level)
        {
            TxtStatus.Text = message;
            // Tokens semânticos do DesignSystem.xaml (alinhados à paleta da marca).
            var token = level switch
            {
                StatusLevel.Success => "StatusSuccess",
                StatusLevel.Warning => "StatusWarning",
                StatusLevel.Error => "StatusError",
                _ => "StatusInfo",
            };
            StatusIndicator.Fill = (TryFindResource(token) as Brush) ?? StatusIndicator.Fill;
        }
    }
}
