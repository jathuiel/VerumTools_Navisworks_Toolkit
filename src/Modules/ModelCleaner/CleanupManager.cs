using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.ModelCleaner
{
    /// <summary>
    /// Enumera e remove Search/Selection Sets e Viewpoints do documento — a base da
    /// ferramenta de Limpeza. Operações verificadas na API 2026:
    ///  - <c>DocumentSelectionSets</c>/<c>DocumentSavedViewpoints</c> expõem <c>Remove(SavedItem)</c>
    ///    (retorna <c>false</c> se o item não está mais na coleção — não lança) e <c>Clear()</c>.
    ///  - A travessia é hierárquica: <c>RootItem.Children</c>, descendo em <c>GroupItem</c> (pastas).
    ///  - <c>SelectionSet.HasSearch</c> distingue Search Set de set explícito (NÃO ler <c>.Search</c>
    ///    quando <c>!HasSearch</c> — lança).
    /// Remoções NÃO exigem Transaction (não são mudanças de modelo); o Navisworks mantém
    /// seu próprio histórico (Ctrl+Z costuma reverter).
    /// </summary>
    public class CleanupManager
    {
        private readonly NavisworksInterop _interop;
        private readonly Document _document;

        public CleanupManager(NavisworksInterop interop)
        {
            _interop = interop ?? throw new ArgumentNullException(nameof(interop));
            _document = _interop.GetActiveDocument();
        }

        /// <summary>Todos os Search/Selection Sets (folhas), com seu caminho e tipo.</summary>
        public List<CleanupItemData> GetSearchSets()
        {
            var list = new List<CleanupItemData>();
            try
            {
                var root = _document.SelectionSets.RootItem as GroupItem;
                if (root != null)
                    SavedItemTree.VisitLeaves(root.Children, string.Empty,
                        (item, path) => list.Add(ToCleanupItem(item, path, CleanupKind.SearchSet)));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Falha ao listar os Search Sets", ex);
            }
            return list;
        }

        /// <summary>Todos os Viewpoints (folhas), com seu caminho.</summary>
        public List<CleanupItemData> GetViewpoints()
        {
            var list = new List<CleanupItemData>();
            try
            {
                var root = _document.SavedViewpoints.RootItem as GroupItem;
                if (root != null)
                    SavedItemTree.VisitLeaves(root.Children, string.Empty,
                        (item, path) => list.Add(ToCleanupItem(item, path, CleanupKind.Viewpoint)));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Falha ao listar os Viewpoints", ex);
            }
            return list;
        }

        // A travessia (pastas → folhas) é compartilhada em SavedItemTree; aqui só a FOLHA
        // vira item da lista.
        private static CleanupItemData ToCleanupItem(SavedItem item, string path, CleanupKind kind)
        {
            return new CleanupItemData
            {
                DisplayName = string.IsNullOrEmpty(item.DisplayName) ? "(sem nome)" : item.DisplayName,
                TypeLabel = TypeLabelFor(item, kind),
                Path = path,
                Kind = kind,
                Item = item
            };
        }

        private static string TypeLabelFor(SavedItem item, CleanupKind kind)
        {
            if (kind == CleanupKind.Viewpoint)
                return "Viewpoint";
            var ss = item as SelectionSet;
            if (ss != null)
                return ss.HasSearch ? "Busca" : "Explícito"; // só HasSearch — NUNCA ler .Search aqui
            return "Conjunto";
        }

        /// <summary>
        /// Remove os itens informados, por referência, da coleção correspondente (por Kind).
        /// Conta sucessos e falhas (Remove devolve false se o item já não existe).
        /// </summary>
        public CleanupResult RemoveItems(IEnumerable<CleanupItemData> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            int removed = 0, failed = 0;
            foreach (var it in items)
            {
                if (it?.Item == null) { failed++; continue; }
                try
                {
                    bool ok = it.Kind == CleanupKind.SearchSet
                        ? _document.SelectionSets.Remove(it.Item)
                        : _document.SavedViewpoints.Remove(it.Item);
                    if (ok) removed++; else failed++;
                }
                catch
                {
                    failed++;
                }
            }
            return new CleanupResult(removed, failed);
        }

        /// <summary>Remove TODOS os Search/Selection Sets do documento.</summary>
        public void ClearAllSearchSets()
        {
            try { _document.SelectionSets.Clear(); }
            catch (Exception ex) { throw new InvalidOperationException("Falha ao limpar os Search Sets", ex); }
        }

        /// <summary>Remove TODOS os Viewpoints do documento.</summary>
        public void ClearAllViewpoints()
        {
            try { _document.SavedViewpoints.Clear(); }
            catch (Exception ex) { throw new InvalidOperationException("Falha ao limpar os Viewpoints", ex); }
        }
    }

    /// <summary>Resultado de uma remoção em lote: quantos saíram e quantos falharam.</summary>
    public struct CleanupResult
    {
        public int Removed { get; }
        public int Failed { get; }
        public CleanupResult(int removed, int failed) { Removed = removed; Failed = failed; }
    }
}
