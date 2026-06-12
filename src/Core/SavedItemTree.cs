using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Core
{
    /// <summary>
    /// Travessia compartilhada da árvore de <see cref="SavedItem"/> (Selection Sets ou
    /// Viewpoints): desce pelas pastas (<see cref="GroupItem"/>) montando o caminho
    /// "Pasta A / Sub" e entrega cada FOLHA ao callback. Usada por CleanupManager e
    /// ExportManager — antes cada um tinha sua cópia do mesmo laço recursivo.
    /// </summary>
    internal static class SavedItemTree
    {
        public static void VisitLeaves(IEnumerable<SavedItem> children, string path,
                                       Action<SavedItem, string> onLeaf)
        {
            if (children == null) return;
            foreach (var item in children)
            {
                if (item == null) continue;

                var group = item as GroupItem;
                if (item.IsGroup && group != null)
                {
                    var sub = string.IsNullOrEmpty(path)
                        ? item.DisplayName
                        : $"{path} / {item.DisplayName}";
                    VisitLeaves(group.Children, sub, onLeaf);
                }
                else
                {
                    onLeaf(item, path);
                }
            }
        }
    }
}
