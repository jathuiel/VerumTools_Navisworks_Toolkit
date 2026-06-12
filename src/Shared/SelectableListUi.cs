using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Shared
{
    /// <summary>
    /// Helpers genéricos para as listas com checkbox das três janelas: itens visíveis
    /// (pós-filtro de busca), sincronização do checkbox de cabeçalho ("selecionar todos":
    /// marcado/desmarcado/indeterminado) e o toggle em massa sobre os itens visíveis.
    /// O filtro é só visual (<see cref="ICollectionView"/>): itens marcados continuam
    /// marcados mesmo quando ocultos pelo filtro.
    /// </summary>
    internal static class SelectableListUi
    {
        /// <summary>Itens atualmente visíveis na view (após o filtro de busca).</summary>
        public static List<T> Visible<T>(ICollectionView view) where T : class
            => view != null ? view.Cast<T>().ToList() : new List<T>();

        /// <summary>
        /// Mantém o checkbox do cabeçalho coerente com as linhas VISÍVEIS: marcado (todas),
        /// desmarcado (nenhuma) ou indeterminado (algumas). O handler do cabeçalho é Click
        /// (só dispara por interação do usuário), então setar IsChecked aqui não reentra
        /// na lógica de seleção.
        /// </summary>
        public static void SyncHeader(CheckBox header, ICollectionView view)
        {
            if (header == null) return;
            var visible = Visible<ISelectableItem>(view);
            var selected = visible.Count(i => i.IsSelected);
            if (visible.Count > 0 && selected == visible.Count) header.IsChecked = true;
            else if (selected == 0) header.IsChecked = false;
            else header.IsChecked = null;
        }

        /// <summary>
        /// Toggle do "selecionar todos" sobre os itens VISÍVEIS: se todos já estão marcados,
        /// desmarca-os; senão, marca todos. <paramref name="suppress"/> é o flag da janela
        /// que silencia o sync por-item durante o lote (o cabeçalho é sincronizado uma única
        /// vez no fim).
        /// </summary>
        public static void ToggleAll(CheckBox header, ICollectionView view, ref bool suppress)
        {
            var visible = Visible<ISelectableItem>(view);
            var target = !(visible.Count > 0 && visible.All(i => i.IsSelected));
            suppress = true;
            foreach (var item in visible)
                item.IsSelected = target;
            suppress = false;
            SyncHeader(header, view);
        }

        /// <summary>Busca case-insensitive de substring, tolerante a nulos.</summary>
        public static bool Matches(string value, string term)
            => value != null && value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
