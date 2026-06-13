using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.ViewBuilder
{
    /// <summary>
    /// Contextualiza os itens de um Selection Set isolando o "nível" pelo SOURCE FILE
    /// (Categoria <c>Item</c> / Atributo <c>Source File</c>) — o delimitador real dos níveis
    /// num modelo consolidado, onde a hierarquia da árvore nem sempre separa as origens:
    ///
    ///  • Os itens do set mantêm aparência original.
    ///  • Elementos do MESMO Source File dos itens do set recebem ghosting cinza
    ///    semi-transparente: contexto local visível sem dominar a vista.
    ///  • Elementos de OUTROS Source Files (outras disciplinas/andares/origens) são
    ///    completamente OCULTADOS, eliminando a sobreposição visual de níveis alheios.
    ///
    /// Fluxo:
    ///  1. <c>BuildContext</c> — numa única passada: keep-path (ancestrais+self, sem enumerar
    ///     Descendants), os próprios itens e o conjunto de Source Files dos itens do set.
    ///  2. <c>BuildOffPathSplit</c> — divide os irmãos off-path em toHide (Source File fora do
    ///     set) e toGhost (mesmo Source File): 1 passagem, sem duplicatas.
    ///  3. Transaction: ResetAllHidden + ResetAllPermanentMaterials +
    ///     SetHidden(toHide) + OverridePermanentColor/Transparency(toGhost).
    ///
    /// O <c>CaptureRuntimeOverrides</c> do <see cref="ViewpointManager"/> captura visibilidade
    /// e overrides de aparência — o viewpoint salvo inclui o ghost e o hide automaticamente.
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

        public void IsolateSelectionSet(SelectionSetData selectionSet,
            IsolationMode mode = IsolationMode.SourceFileLevel)
        {
            if (selectionSet == null)
                throw new ArgumentNullException(nameof(selectionSet));

            if (!selectionSet.HasItems)
                throw new InvalidOperationException(
                    $"Selection Set '{selectionSet.Name}' está vazio ou é inválido");

            try
            {
                PerfLog.Info($"--- Isolate '{selectionSet.Name}' (ItemCount={selectionSet.ItemCount}, mode={mode}) ---");

                // Cache de Source File por nó, compartilhado por BuildContext e BuildOffPathSplit:
                // a leitura de PropertyCategories é a operação mais cara aqui, então cada nó é
                // lido no máximo uma vez por isolamento.
                var srcCache = new Dictionary<ModelItem, string>();

                // Contexto numa ÚNICA passada (1 marshalling de AncestorsAndSelf por item — o
                // custo dominante). NÃO enumeramos Descendants: visibilidade é hierárquica, então
                // a subárvore de cada item permanece visível por si só desde que não a escondamos.
                //   • keep        — ancestrais+self dos itens (caminho a manter visível).
                //   • selfSet     — os próprios itens (nunca escondemos/enumeramos seus filhos).
                //   • keepSources — Source Files dos itens do set: a "fronteira de contexto".
                var ctx = PerfLog.Time("BuildContext",
                    () => BuildContext(selectionSet.ModelItems, srcCache));
                var keep = ctx.Keep;
                var selfSet = ctx.Self;
                var keepSources = ctx.KeepSources;

                // Diagnóstico: quantos itens do set realmente entraram no keep. Se
                // selfInKeep < selfCount, a igualdade de ModelItem está falhando e os itens
                // não seriam reexibidos (set sumiria). Deve ser sempre selfInKeep==selfCount.
                var selfInKeep = 0;
                foreach (var s in selfSet)
                    if (keep.Contains(s)) selfInKeep++;
                PerfLog.Info($"    keepCount={keep.Count}  selfCount={selfSet.Count}  selfInKeep={selfInKeep}");
                PerfLog.Info($"    keepSources=[{string.Join(" | ", keepSources)}]");

                // SetItemsOnly: oculta TODO off-path (sem ghost). SourceFileLevel: divide em
                // hide (outras origens) e ghost (mesma origem = contexto do nível).
                List<ModelItem> toHide, toGhost;
                if (mode == IsolationMode.SetItemsOnly)
                {
                    toHide = PerfLog.Time("BuildOffPathHideAll",
                        () => BuildOffPathHideAll(keep, selfSet));
                    toGhost = new List<ModelItem>();
                }
                else
                {
                    var split = PerfLog.Time("BuildOffPathSplit",
                        () => BuildOffPathSplit(keep, selfSet, keepSources, srcCache));
                    toHide  = split.ToHide;
                    toGhost = split.ToGhost;
                }
                PerfLog.Info($"    toHideCount={toHide.Count}  toGhostCount={toGhost.Count}");

                using (var txn = _document.BeginTransaction($"Contextualizar '{selectionSet.Name}'"))
                {
                    // Baseline: tudo visível + sem overrides de cor/transparência anteriores.
                    PerfLog.TimeVoid("ResetAllHidden",
                        () => _document.Models.ResetAllHidden());
                    PerfLog.TimeVoid("ResetAllPermanentMaterials",
                        () => _document.Models.ResetAllPermanentMaterials());

                    // Oculta completamente elementos de OUTROS Source Files (outras disciplinas/
                    // andares/origens): elimina a sobreposição visual de níveis alheios.
                    if (toHide.Count > 0)
                        PerfLog.TimeVoid("SetHidden(outras-origens,true)",
                            () => _document.Models.SetHidden(toHide, true));

                    // Ghosting nos elementos do MESMO Source File (contexto local do nível):
                    // cinza semi-transparente preserva o entorno sem dominar a vista.
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
        /// Monta, numa ÚNICA passada sobre os itens do set, o contexto usado pelo isolamento:
        /// <list type="bullet">
        ///   <item><term>Keep</term><description>
        ///     Cada item e seus ancestrais até a raiz (visibilidade é hierárquica — um pai
        ///     oculto esconde o filho). NÃO inclui descendentes: a subárvore de cada item fica
        ///     visível por não ser escondida. O próprio item é adicionado EXPLICITAMENTE além de
        ///     <c>AncestorsAndSelf</c>: é o que precisa permanecer visível, e não dependemos da
        ///     semântica/ordem de <c>AncestorsAndSelf</c> para garanti-lo — se ele faltar, o set
        ///     inteiro permanece oculto.
        ///   </description></item>
        ///   <item><term>Self</term><description>
        ///     Os próprios itens do set (sem ancestrais): usados para NÃO esconder os filhos de
        ///     um item selecionado — toda a subárvore dele permanece visível.
        ///   </description></item>
        ///   <item><term>KeepSources</term><description>
        ///     O conjunto de Source Files dos itens do set: a fronteira de contexto. Tudo cujo
        ///     Source File estiver aqui é contexto local (ghost); o restante é outro nível (hide).
        ///     Vazio (itens sem Source File) ⇒ não há como delimitar ⇒ fallback "ghost em tudo".
        ///   </description></item>
        /// </list>
        /// </summary>
        private static (HashSet<ModelItem> Keep, HashSet<ModelItem> Self, HashSet<string> KeepSources) BuildContext(
            ModelItemCollection items, Dictionary<ModelItem, string> srcCache)
        {
            var keep = new HashSet<ModelItem>();
            var self = new HashSet<ModelItem>();
            var keepSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                if (item == null)
                    continue;

                self.Add(item);
                keep.Add(item); // garante o próprio item no caminho (mantido visível)

                foreach (var ancestor in item.AncestorsAndSelf)
                    keep.Add(ancestor);

                var src = SourceFileOf(item, srcCache);
                if (!string.IsNullOrEmpty(src))
                    keepSources.Add(src);
            }

            return (keep, self, keepSources);
        }

        /// <summary>
        /// Divide os filhos off-path da keep-path em dois grupos pelo Source File:
        /// <list type="bullet">
        ///   <item><term>ToHide</term><description>
        ///     Filhos cujo Source File NÃO está em <paramref name="keepSources"/> (outras
        ///     disciplinas/andares/origens): completamente ocultados.
        ///   </description></item>
        ///   <item><term>ToGhost</term><description>
        ///     Filhos do MESMO Source File do set (contexto local): ghosting cinza
        ///     semi-transparente para manter o entorno visível.
        ///   </description></item>
        /// </list>
        /// Otimização: como todos os filhos de um nó herdam o Source File do pai, classificamos o
        /// ramo inteiro pelo Source File do PRÓPRIO nó keep-path (1 leitura). Só quando o nó é um
        /// agregador sem Source File próprio (ex.: a raiz que reúne várias origens) descemos para
        /// classificar cada filho individualmente.
        ///
        /// Itens do set (<paramref name="selfSet"/>) são pulados: nunca tocamos nos filhos da
        /// geometria selecionada. Lista (não HashSet): cada filho tem um único pai, portanto não
        /// há duplicatas. Se <paramref name="keepSources"/> for vazio (itens sem Source File),
        /// tudo vai para ToGhost — o fallback "ghost em tudo", sem ocultar nada.
        /// </summary>
        private static (List<ModelItem> ToHide, List<ModelItem> ToGhost) BuildOffPathSplit(
            HashSet<ModelItem> keep, HashSet<ModelItem> selfSet, HashSet<string> keepSources,
            Dictionary<ModelItem, string> srcCache)
        {
            var toHide  = new List<ModelItem>();
            var toGhost = new List<ModelItem>();

            // Sem Source File identificável nos itens do set: não há como delimitar níveis →
            // degrada para "ghost em tudo" (nada é ocultado), o comportamento de contexto puro.
            var canDelimit = keepSources.Count > 0;

            foreach (var node in keep)
            {
                if (selfSet.Contains(node)) continue;

                if (!canDelimit)
                {
                    foreach (var child in node.Children)
                        if (!keep.Contains(child))
                            toGhost.Add(child);
                    continue;
                }

                // Classificação primária pelo Source File do próprio nó keep-path: todos os
                // filhos herdam o source do pai, então decidimos o ramo inteiro com 1 leitura.
                var nodeSource = SourceFileOf(node, srcCache);
                if (!string.IsNullOrEmpty(nodeSource))
                {
                    var dest = keepSources.Contains(nodeSource) ? toGhost : toHide;
                    foreach (var child in node.Children)
                        if (!keep.Contains(child))
                            dest.Add(child);
                }
                else
                {
                    // Nó agregador sem Source File próprio: classifica cada filho off-path pelo
                    // seu próprio Source File. Source vazio/desconhecido → ghost (conservador:
                    // não ocultamos o que não conseguimos atribuir a outro nível).
                    foreach (var child in node.Children)
                    {
                        if (keep.Contains(child)) continue;
                        var childSource = SourceFileOf(child, srcCache);
                        var outside = !string.IsNullOrEmpty(childSource) && !keepSources.Contains(childSource);
                        (outside ? toHide : toGhost).Add(child);
                    }
                }
            }

            return (toHide, toGhost);
        }

        /// <summary>
        /// Modo "apenas itens do SET": coleta todos os filhos off-path da keep-path para serem
        /// ocultados, sem distinção de Source File e sem ghosting. Resultado: somente a geometria
        /// do set (e sua subárvore) permanece visível; todo o resto do modelo some.
        ///
        /// Itens do set (<paramref name="selfSet"/>) são pulados: a subárvore deles é preservada.
        /// Lista (não HashSet): cada filho tem um único pai, logo não há duplicatas.
        /// </summary>
        private static List<ModelItem> BuildOffPathHideAll(
            HashSet<ModelItem> keep, HashSet<ModelItem> selfSet)
        {
            var toHide = new List<ModelItem>();
            foreach (var node in keep)
            {
                if (selfSet.Contains(node)) continue;
                foreach (var child in node.Children)
                    if (!keep.Contains(child))
                        toHide.Add(child);
            }
            return toHide;
        }

        /// <summary>
        /// Lê o Source File de um nó (Categoria <c>Item</c> / Atributo <c>Source File</c>),
        /// usando nomes INTERNOS invariantes (<c>LcOaNode</c> + property contendo
        /// <c>SourceFile</c>) com os DisplayNames ("Item" / "Source File") como reforço caso a
        /// UI esteja localizada. O resultado é memoizado em <paramref name="cache"/>: a leitura
        /// de <c>PropertyCategories</c> marshala COM e é o custo dominante deste fluxo.
        /// Retorna string vazia quando o nó não expõe a propriedade.
        /// </summary>
        private static string SourceFileOf(ModelItem node, Dictionary<ModelItem, string> cache)
        {
            if (node == null) return string.Empty;
            if (cache.TryGetValue(node, out var cached)) return cached;

            var value = string.Empty;
            try
            {
                foreach (PropertyCategory cat in node.PropertyCategories)
                {
                    var catInternal = cat.Name;
                    var isItemCat =
                        string.Equals(catInternal, "LcOaNode", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(cat.DisplayName, "Item", StringComparison.OrdinalIgnoreCase);
                    if (!isItemCat) continue;

                    foreach (DataProperty prop in cat.Properties)
                    {
                        var propInternal = prop.Name;
                        var isSource =
                            (propInternal != null &&
                             propInternal.IndexOf("SourceFile", StringComparison.OrdinalIgnoreCase) >= 0) ||
                            string.Equals(prop.DisplayName, "Source File", StringComparison.OrdinalIgnoreCase);
                        if (!isSource) continue;

                        value = PropertyExtractionHelper.SafeValue(prop.Value) ?? string.Empty;
                        break;
                    }

                    if (!string.IsNullOrEmpty(value)) break;
                }
            }
            catch
            {
                value = string.Empty;
            }

            cache[node] = value;
            return value;
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
