using System.Collections.Generic;
using Autodesk.Navisworks.Api;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Shared
{
    internal sealed class SetCacheEntry
    {
        public string Name { get; }
        public string SaveName { get; }
        public string Type { get; }
        public SavedItem Item { get; }
        public ModelItemCollection Items { get; }
        public HashSet<ModelItem> ItemsSet { get; }

        public SetCacheEntry(string name, string saveName, string type, SavedItem item, ModelItemCollection items)
        {
            Name = name;
            SaveName = saveName;
            Type = type;
            Item = item;
            Items = items ?? new ModelItemCollection();
            ItemsSet = new HashSet<ModelItem>();
            foreach (ModelItem mi in Items)
                ItemsSet.Add(mi);
        }
    }

    internal static class SelectionSetCache
    {
        public static List<SetCacheEntry> Collect(Document doc)
        {
            var result = new List<SetCacheEntry>();
            if (doc?.SelectionSets?.RootItem is GroupItem group)
                Collect(group, string.Empty, result);
            return result;
        }

        private static void Collect(GroupItem group, string prefix, List<SetCacheEntry> result)
        {
            foreach (var child in group.Children)
            {
                if (child is SelectionSet ss)
                {
                    var items = GetItems(ss);
                    if (items.Count == 0) continue;

                    var type = ss.HasExplicitModelItems ? "Selection" : (ss.HasSearch ? "Search" : "Other");
                    result.Add(new SetCacheEntry(
                        prefix + ss.DisplayName,
                        ss.DisplayName,
                        type,
                        ss,
                        items));
                }
                else if (child is GroupItem grp)
                {
                    Collect(grp, prefix + grp.DisplayName + " > ", result);
                }
            }
        }

        private static ModelItemCollection GetItems(SelectionSet ss)
        {
            var coll = new ModelItemCollection();
            if (ss.HasExplicitModelItems)
                coll.AddRange(ss.ExplicitModelItems);
            else if (ss.HasSearch)
                coll.AddRange(ss.Search.FindAll(false));
            return coll;
        }
    }
}
