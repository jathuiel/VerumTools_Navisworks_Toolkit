using System;
using System.ComponentModel;
using Autodesk.Navisworks.Api;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.ViewBuilder
{
    /// <summary>
    /// Objeto de transferência de dados (DTO) com as informações de um Selection Set.
    /// Os dados do set são imutáveis; apenas <see cref="IsSelected"/> muda em runtime
    /// (estado do checkbox na UI), por isso implementa INotifyPropertyChanged.
    /// </summary>
    public class SelectionSetData : INotifyPropertyChanged, ISelectableItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int ItemCount { get; set; }
        public ModelItemCollection ModelItems { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Nome do viewpoint vindo do template Excel importado (opcional). Se nulo/vazio,
        /// o <see cref="Core.ViewpointManager"/> gera um nome automático.
        /// </summary>
        public string TemplateViewpointName { get; set; }

        /// <summary>
        /// Descrição do viewpoint vinda do template Excel importado (opcional).
        /// </summary>
        public string TemplateDescription { get; set; }

        private bool _isSelected;
        /// <summary>
        /// Marcado via checkbox na lista para inclusão na geração em lote de viewpoints.
        /// Notifica a UI para que o "selecionar todos" reflita nas linhas e vice-versa.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return $"{Name} ({ItemCount} itens)";
        }

        /// <summary>
        /// O set resolveu para ao menos um item de modelo — pré-condição compartilhada
        /// de isolar (IsolationHandler) e criar viewpoint (ViewpointManager).
        /// </summary>
        public bool HasItems => ModelItems != null && ModelItems.Count > 0;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Name) && HasItems;
        }
    }
}
