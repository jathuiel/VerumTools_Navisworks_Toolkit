using Autodesk.Navisworks.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Shared
{
    internal static class PropertyExtractionHelper
    {
        public static Dictionary<string, Dictionary<string, string>> ExtractPropertiesFromItems(IEnumerable<ModelItem> items)
        {
            var map = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                foreach (PropertyCategory cat in item.PropertyCategories)
                {
                    if (!map.ContainsKey(cat.DisplayName))
                        map[cat.DisplayName] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    foreach (DataProperty prop in cat.Properties)
                    {
                        if (!string.IsNullOrWhiteSpace(prop.DisplayName))
                            if (!map[cat.DisplayName].ContainsKey(prop.DisplayName))
                                map[cat.DisplayName][prop.DisplayName] = SafeValue(prop.Value);
                    }
                }
            }
            return map;
        }

        public static ObservableCollection<SelectionSetItem> LoadSelectionSetsViaReflection(object document)
        {
            var sets = new ObservableCollection<SelectionSetItem>();
            if (document == null) return sets;

            // Fast path: typed API when document is a Document instance.
            if (document is Document typedDoc)
            {
                try
                {
                    foreach (var entry in SelectionSetCache.Collect(typedDoc))
                    {
                        sets.Add(new SelectionSetItem
                        {
                            Name = entry.Name,
                            ElementCount = entry.Items.Count,
                            Items = new List<ModelItem>(entry.Items.Cast<ModelItem>())
                        });
                    }
                    return sets;
                }
                catch
                {
                    sets.Clear();
                    // Fall through to reflection path.
                }
            }

            // Reflection fallback for unknown document wrappers.
            var selectionSets = GetPropertyValue(document, "SelectionSets");
            if (selectionSets == null) return sets;

            var visited = new HashSet<int>();
            try
            {
                CollectSelectionSets(selectionSets, document, sets, visited);
            }
            catch
            {
                // Avoid bubbling reflection/COM exceptions to the UI command.
            }

            return sets;
        }

        private static void CollectSelectionSets(object node, object document, ObservableCollection<SelectionSetItem> sets, HashSet<int> visited)
        {
            if (node == null) return;

            var key = RuntimeHelpers.GetHashCode(node);
            if (!visited.Add(key)) return;

            try
            {
                if (TryCreateSelectionSetItem(node, document, out var setItem))
                    sets.Add(setItem);
            }
            catch
            {
                // Skip problematic nodes and keep traversing.
            }

            IEnumerable<object> children;
            try
            {
                children = EnumerateChildNodes(node);
            }
            catch
            {
                return;
            }

            foreach (var child in children)
            {
                try
                {
                    CollectSelectionSets(child, document, sets, visited);
                }
                catch
                {
                    // Keep processing siblings.
                }
            }
        }

        private static bool TryCreateSelectionSetItem(object node, object document, out SelectionSetItem setItem)
        {
            setItem = null;

            try
            {
                var nodeType = node.GetType();
                var typeName = nodeType.Name;
                var typeNameLower = typeName.ToLowerInvariant();
                var looksLikeSelectionSet =
                    typeNameLower.EndsWith("selectionset") ||
                    typeNameLower.EndsWith("searchset");

                var items = TryGetModelItemsFromProperty(node, "SelectedItems")
                            ?? TryGetModelItemsFromProperty(node, "ExplicitModelItems")
                            ?? TryGetModelItemsFromProperty(node, "ModelItems")
                            ?? TryGetModelItemsFromProperty(node, "Items")
                            ?? TryResolveSearchSetItems(node, document);

                if (items == null)
                {
                    if (!looksLikeSelectionSet) return false;
                    items = new List<ModelItem>();
                }

                var name = GetStringProperty(node, "DisplayName", "Name");
                if (string.IsNullOrWhiteSpace(name))
                    name = "SelectionSet";

                setItem = new SelectionSetItem
                {
                    Name = name,
                    ElementCount = items.Count,
                    Items = items
                };

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static List<ModelItem> TryGetModelItemsFromProperty(object node, string propertyName)
        {
            if (node == null) return null;

            var prop = node.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (prop == null) return null;

            object value;
            try
            {
                value = prop.GetValue(node, null);
            }
            catch
            {
                return null;
            }

            if (value == null)
            {
                if (IsModelItemCollectionType(prop.PropertyType))
                    return new List<ModelItem>();
                return null;
            }

            if (!(value is IEnumerable rawEnumerable)) return null;

            var items = new List<ModelItem>();
            var hasAnyValue = false;

            try
            {
                foreach (var entry in rawEnumerable)
                {
                    if (entry == null) continue;
                    hasAnyValue = true;

                    if (entry is ModelItem modelItem)
                    {
                        items.Add(modelItem);
                        continue;
                    }

                    return null;
                }
            }
            catch
            {
                return null;
            }

            if (!hasAnyValue && !IsModelItemCollectionType(prop.PropertyType))
                return null;

            return items;
        }

        private static List<ModelItem> TryResolveSearchSetItems(object node, object document)
        {
            if (node == null || document == null) return null;

            var searchObj = GetPropertyValue(node, "Search");
            if (searchObj == null) return null;

            List<MethodInfo> methods;
            try
            {
                methods = searchObj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(m => m.Name == "FindAll")
                    .ToList();
            }
            catch
            {
                return null;
            }

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                object[] args = null;

                if (parameters.Length == 1 &&
                    parameters[0].ParameterType.IsAssignableFrom(document.GetType()))
                {
                    args = new[] { document };
                }
                else if (parameters.Length == 2 &&
                         parameters[0].ParameterType.IsAssignableFrom(document.GetType()) &&
                         parameters[1].ParameterType == typeof(bool))
                {
                    args = new object[] { document, false };
                }

                if (args == null) continue;

                try
                {
                    var result = method.Invoke(searchObj, args);
                    var parsed = ParseModelItemEnumerable(result);
                    if (parsed != null) return parsed;
                }
                catch
                {
                    // Ignore and try other signatures when available.
                }
            }

            return null;
        }

        private static IEnumerable<object> EnumerateChildNodes(object node)
        {
            var children = new List<object>();
            var seen = new HashSet<int>();

            void AddChild(object candidate)
            {
                if (candidate == null) return;
                if (candidate is ModelItem) return;

                var key = RuntimeHelpers.GetHashCode(candidate);
                if (!seen.Add(key)) return;

                children.Add(candidate);
            }

            AddChild(GetPropertyValue(node, "RootItem", "Root"));

            foreach (var propName in new[] { "Children", "SavedItems", "Items" })
            {
                var value = GetPropertyValue(node, propName);
                foreach (var child in EnumerateObjects(value))
                    AddChild(child);
            }

            foreach (var child in EnumerateObjects(node))
                AddChild(child);

            return children;
        }

        private static List<object> EnumerateObjects(object value)
        {
            var objects = new List<object>();

            if (value == null || value is string || value is ModelItem) return objects;
            if (!(value is IEnumerable enumerable)) return objects;

            try
            {
                foreach (var item in enumerable)
                {
                    if (item == null || item is ModelItem) continue;
                    objects.Add(item);
                }
            }
            catch
            {
                return objects;
            }

            return objects;
        }

        private static List<ModelItem> ParseModelItemEnumerable(object value)
        {
            if (!(value is IEnumerable enumerable)) return null;

            var items = new List<ModelItem>();
            try
            {
                foreach (var item in enumerable)
                {
                    if (item == null) continue;
                    if (item is ModelItem modelItem)
                    {
                        items.Add(modelItem);
                        continue;
                    }

                    return null;
                }
            }
            catch
            {
                return null;
            }

            return items;
        }

        private static bool IsModelItemCollectionType(Type type)
        {
            if (type == null) return false;
            if (typeof(ModelItemCollection).IsAssignableFrom(type)) return true;
            if (type.Name.IndexOf("ModelItemCollection", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static string GetStringProperty(object source, params string[] propertyNames)
        {
            if (source == null || propertyNames == null) return null;

            var sourceType = source.GetType();
            foreach (var name in propertyNames)
            {
                var prop = sourceType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                if (prop == null) continue;

                try
                {
                    var value = prop.GetValue(source, null);
                    if (value is string text) return text;
                }
                catch
                {
                    // Ignore and keep searching.
                }
            }

            return null;
        }

        private static object GetPropertyValue(object source, params string[] propertyNames)
        {
            if (source == null || propertyNames == null) return null;

            var sourceType = source.GetType();
            foreach (var name in propertyNames)
            {
                var prop = sourceType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                if (prop == null) continue;

                try
                {
                    return prop.GetValue(source, null);
                }
                catch
                {
                    // Ignore and keep searching.
                }
            }

            return null;
        }

        internal static string SafeValue(VariantData v)
        {
            if (v == null || v.DataType == VariantDataType.None) return string.Empty;
            try { return v.ToDisplayString() ?? string.Empty; }
            catch { return string.Empty; }
        }
    }
}
