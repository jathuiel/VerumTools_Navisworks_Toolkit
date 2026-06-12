using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.ViewBuilder
{
    /// <summary>
    /// Contextualiza os itens de um Selection Set usando ghosting: o ramo do set fica com
    /// aparência original; todo o resto recebe um override de cor cinza semi-transparente
    /// (<see cref="GhostColor"/> / <see cref="GhostTransparency"/>), permanecendo visível
    /// para dar contexto espacial sem dominar a vista.
    ///
    /// Performance (crítico): o custo real NÃO está em esconder, e sim em ENUMERAR
    /// ModelItemCollection grandes em código gerenciado — cada item vira um wrapper COM
    /// marshalado a ~1-2 ms. Esta versão usa visibilidade HIERÁRQUICA para montar o ramo sem
    /// enumerar Descendants:
    ///  1. <c>ResetAllHidden</c> + <c>ResetAllPermanentColors</c> (estado limpo) — 2 chamadas nativas.
    ///  2. Mantemos só o CAMINHO (ancestrais+self dos itens do set).
    ///  3. Aplicamos o ghost APENAS nos irmãos OFF-PATH, numa única chamada
    ///     <c>OverridePermanentColor(toGhost, GhostColor, GhostTransparency)</c>.
    /// Tudo dentro de uma Transaction (1 redraw, 1 undo).
    /// <para>
    /// O <c>CaptureRuntimeOverrides</c> do <see cref="ViewpointManager"/> captura overrides de
    /// aparência além de visibilidade — o viewpoint salvo inclui o ghost automaticamente.
    /// </para>
    /// </summary>
    public class IsolationHandler
    {
        // Cor e transparência do "fantasma": cinza neutro a 70 % de transparência.
        // Ajuste GhostTransparency entre 0.0 (opaco) e 1.0 (invisível) conforme preferência.
        private static readonly Color GhostColor = new Color(0.88, 0.88, 0.88);
        private const float GhostTransparency = 0.75f;
        private readonly NavisworksInterop _interop;
        private readonly Document _document;

        public IsolationHandler(NavisworksInterop interop)
        {
            _interop = interop ?? throw new ArgumentNullException(nameof(interop));
            _document = _interop.GetActiveDocument();
        }

        public void IsolateSelectionSet(SelectionSetData selectionSet)
        {
            if (selectionSet == null)
                throw new ArgumentNullException(nameof(selectionSet));

            if (!selectionSet.HasItems)
                throw new InvalidOperationException(
                    $"Selection Set '{selectionSet.Name}' está vazio ou é inválido");

            try
            {
                PerfLog.Info($"--- Isolate '{selectionSet.Name}' (ItemCount={selectionSet.ItemCount}) ---");

                // Caminho a manter visível: SOMENTE ancestrais+self dos itens do set.
                // NÃO enumeramos Descendants — visibilidade é hierárquica, então a subárvore
                // de cada item permanece visível por si só desde que não a escondamos.
                var keep = PerfLog.Time("BuildKeepPath",
                    () => BuildKeepPath(selectionSet.ModelItems));
                var selfSet = BuildSelfSet(selectionSet.ModelItems);

                // Diagnóstico: quantos itens do set realmente entraram no keep. Se
                // selfInKeep < selfCount, a igualdade de ModelItem está falhando e os itens
                // não seriam reexibidos (set sumiria). Deve ser sempre selfInKeep==selfCount.
                var selfInKeep = 0;
                foreach (var s in selfSet)
                    if (keep.Contains(s)) selfInKeep++;
                PerfLog.Info($"    keepCount={keep.Count}  selfCount={selfSet.Count}  selfInKeep={selfInKeep}");

                // Coleta os filhos OFF-PATH: filhos de nós-ancestrais que NÃO estão no
                // caminho. Só estes recebem o ghost. O caminho (keep) e as subárvores dos
                // itens do set NUNCA são tocados, então permanecem com aparência original a
                // partir do ResetAllHidden/ResetAllPermanentColors. Pulamos itens do set para
                // jamais enumerar/ghostear os filhos da geometria selecionada.
                var toGhost = PerfLog.Time("BuildOffPath",
                    () => BuildOffPath(keep, selfSet));
                PerfLog.Info($"    toGhostCount={toGhost.Count}");

                using (var txn = _document.BeginTransaction($"Contextualizar '{selectionSet.Name}'"))
                {
                    // Baseline: tudo visível + sem overrides de cor/transparência anteriores.
                    PerfLog.TimeVoid("ResetAllHidden",
                        () => _document.Models.ResetAllHidden());
                    PerfLog.TimeVoid("ResetAllPermanentMaterials",
                        () => _document.Models.ResetAllPermanentMaterials());

                    // Ghost nos irmãos off-path: cinza semi-transparente para dar contexto
                    // sem dominar a vista. O ramo do set permanece com aparência original.
                    // OverridePermanentColor + OverridePermanentTransparency são dois overrides
                    // independentes; ambos são capturados por CaptureRuntimeOverrides().
                    if (toGhost.Count > 0)
                    {
                        PerfLog.TimeVoid("OverridePermanentColor(ghost)",
                            () => _document.Models.OverridePermanentColor(toGhost, GhostColor));
                        PerfLog.TimeVoid("OverridePermanentTransparency(ghost)",
                            () => _document.Models.OverridePermanentTransparency(toGhost, GhostTransparency));
                    }

                    PerfLog.TimeVoid("txn.Commit", () => txn.Commit());
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Falha ao isolar o Selection Set '{selectionSet.Name}'", ex);
            }
        }

        /// <summary>
        /// O CAMINHO a manter visível: cada item do set e seus ancestrais até a raiz
        /// (visibilidade é hierárquica — um pai oculto esconde o filho). NÃO inclui
        /// descendentes: a subárvore de cada item fica visível por não ser escondida.
        ///
        /// O próprio item é adicionado EXPLICITAMENTE (além de <c>AncestorsAndSelf</c>):
        /// é o item que precisa ser reexibido no passo final, e não podemos depender da
        /// semântica/ordem de <c>AncestorsAndSelf</c> para garanti-lo no conjunto — se ele
        /// faltar, o set inteiro permanece oculto. Sem early-break, então independe da ordem.
        /// </summary>
        private static HashSet<ModelItem> BuildKeepPath(ModelItemCollection items)
        {
            var keep = new HashSet<ModelItem>();
            foreach (var item in items)
            {
                if (item == null)
                    continue;

                keep.Add(item); // garante o próprio item no caminho (reexibido no passo final)
                foreach (var ancestor in item.AncestorsAndSelf)
                    keep.Add(ancestor);
            }
            return keep;
        }

        /// <summary>
        /// Os próprios itens do set (sem ancestrais). Usado para NÃO esconder os filhos de um
        /// item selecionado — toda a subárvore dele deve permanecer visível.
        /// </summary>
        private static HashSet<ModelItem> BuildSelfSet(ModelItemCollection items)
        {
            var self = new HashSet<ModelItem>();
            foreach (var item in items)
            {
                if (item != null)
                    self.Add(item);
            }
            return self;
        }

        /// <summary>
        /// Os filhos OFF-PATH a esconder: para cada nó do caminho que NÃO é um item do set,
        /// os filhos diretos que não pertencem ao caminho. Esconder estes oculta toda a
        /// subárvore fora do ramo (visibilidade hierárquica), sem tocar no caminho nem nas
        /// subárvores dos itens selecionados — logo não é preciso reexibir nada.
        ///
        /// Itens do set são pulados (<paramref name="selfSet"/>): nunca enumeramos/escondemos
        /// os filhos da geometria selecionada. Lista (não HashSet): cada filho tem um único
        /// pai, então não há duplicatas a deduplicar — e evitamos o custo de hashing em massa.
        /// </summary>
        private static List<ModelItem> BuildOffPath(HashSet<ModelItem> keep, HashSet<ModelItem> selfSet)
        {
            var toGhost = new List<ModelItem>();
            foreach (var node in keep)
            {
                if (selfSet.Contains(node))
                    continue;

                foreach (var child in node.Children)
                {
                    if (!keep.Contains(child))
                        toGhost.Add(child);
                }
            }
            return toGhost;
        }

        public void ResetIsolation()
        {
            try
            {
                _interop.ResetVisibility();
                // Limpa o ghost: overrides de cor/transparência aplicados pelo ghosting não
                // são desfeitos por ResetAllHidden — precisam de uma chamada explícita.
                _document.Models.ResetAllPermanentMaterials();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Falha ao restaurar o isolamento", ex);
            }
        }
    }
}
