using System.Data;
using System.Collections.Generic;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.SelectionInspector
{
    public class ColumnHeaderData
    {
        public string Category { get; set; }
        public string Property { get; set; }
    }

    internal class ColDef
    {
        public string Category { get; set; }
        public string Property { get; set; }
        public string Key { get; set; }
    }

    internal class BuildResult
    {
        public DataTable Table { get; set; }
        public List<ColDef> Columns { get; set; }
    }
}
