using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Navisworks.Api;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.ViewBuilder
{
    /// <summary>
    /// Lê e gerencia os Selection Sets do documento Navisworks.
    /// Percorre recursivamente as pastas (FolderItem) e resolve cada SelectionSet,
    /// incluindo os baseados em busca (search-based), via GetSelectedItems().
    /// </summary>
    public class SelectionSetManager
    {
        private readonly NavisworksInterop _interop;

        public SelectionSetManager(NavisworksInterop interop)
        {
            _interop = interop ?? throw new ArgumentNullException(nameof(interop));
        }

        public List<SelectionSetData> GetAllSelectionSets()
        {
            try
            {
                var doc = _interop.GetActiveDocument();
                var result = new List<SelectionSetData>();

                // RootItem é um FolderItem (GroupItem) cujos filhos são pastas ou SelectionSets.
                CollectSets(doc.SelectionSets.RootItem, result);

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Falha ao obter os Selection Sets", ex);
            }
        }

        private void CollectSets(SavedItem item, List<SelectionSetData> result)
        {
            if (item == null)
                return;

            // Um SelectionSet deriva de SavedItem (não de GroupItem); uma pasta é um GroupItem.
            if (item is SelectionSet set)
            {
                result.Add(ResolveSet(set));
            }
            else if (item is GroupItem folder)
            {
                foreach (var child in folder.Children)
                    CollectSets(child, result);
            }
        }

        /// <summary>
        /// Resolve um único SelectionSet de forma defensiva. Sets search-based criados
        /// por outros plugins (ex.: ConstructSyncToolkit) podem falhar ao resolver
        /// (GetSelectedItems lança exceção). Nesse caso, não derrubamos a lista inteira:
        /// retornamos um item marcado com o erro para que os demais sets continuem visíveis.
        /// </summary>
        private SelectionSetData ResolveSet(SelectionSet set)
        {
            var name = string.IsNullOrWhiteSpace(set.DisplayName) ? "Sem nome" : set.DisplayName;

            try
            {
                var items = set.GetSelectedItems() ?? new ModelItemCollection();

                return new SelectionSetData
                {
                    Name = name,
                    // IMPORTANTE: acessar set.Search quando HasSearch == false lança
                    // InvalidOperationException ("Invalid operation when !HasSearch").
                    // Sempre cheque HasSearch primeiro.
                    Description = set.HasSearch ? "Set baseado em busca" : "Set explícito",
                    ItemCount = items.Count,
                    ModelItems = items
                };
            }
            catch (Exception ex)
            {
                // Não propaga: registra o set como não-resolvível e segue com os demais.
                return new SelectionSetData
                {
                    Name = name,
                    Description = $"⚠ Falha ao resolver: {ex.Message}",
                    ItemCount = 0,
                    ModelItems = new ModelItemCollection()
                };
            }
        }

        public SelectionSetData GetSelectionSetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("O nome do Selection Set não pode ser nulo ou vazio", nameof(name));

            var match = GetAllSelectionSets()
                .FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (match == null)
                throw new InvalidOperationException($"Selection Set '{name}' não encontrado");

            return match;
        }

        public int GetSelectionSetCount()
        {
            return GetAllSelectionSets().Count;
        }

        public bool ValidateSelectionSet(SelectionSetData data)
        {
            return data != null && data.IsValid();
        }
    }
}
