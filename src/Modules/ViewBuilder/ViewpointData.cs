using System;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.ViewBuilder
{
    /// <summary>
    /// Objeto de transferência de dados (DTO) com os metadados de um viewpoint criado.
    /// </summary>
    public class ViewpointData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string SelectionSetName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return $"{Name} ({SelectionSetName})";
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Name)
                && !string.IsNullOrWhiteSpace(SelectionSetName);
        }
    }
}
