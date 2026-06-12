using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.AttributeLab
{
    public sealed class SetAssignment
    {
        public string Name { get; }
        public string Nome => Name;

        public SetAssignment(string name)
        {
            Name = name ?? string.Empty;
        }
    }
}
