using System;
using System.Globalization;
using Autodesk.Navisworks.Api;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.ViewBuilder
{
    /// <summary>
    /// Cria e gerencia viewpoints a partir dos Selection Sets.
    ///
    /// A câmera é posicionada num ângulo ISOMÉTRICO enquadrando a bounding box do set,
    /// usando a API gerenciada (<see cref="Viewpoint.ZoomBox"/>) em vez do zoom animado
    /// da COM (<c>ZoomInCurViewOnCurSel</c>). Ganhos:
    ///  - Performance: não há seleção dos itens (evita renderizar o highlight de milhares
    ///    de elementos) nem animação de transição de câmera — os dois gargalos do lote.
    ///  - Correção: o eixo da câmera passa a acompanhar a posição do set (isométrico),
    ///    em vez de ficar preso ao eixo corrente da vista.
    /// O viewpoint é então capturado da vista corrente (já com o isolamento aplicado) e
    /// salvo no documento.
    /// </summary>
    public class ViewpointManager
    {
        private readonly NavisworksInterop _interop;
        private readonly Document _document;

        public ViewpointManager(NavisworksInterop interop)
        {
            _interop = interop ?? throw new ArgumentNullException(nameof(interop));
            _document = _interop.GetActiveDocument();
        }

        public ViewpointData CreateViewpointFromSelection(SelectionSetData selectionSet,
            ViewOrientation orientation = ViewOrientation.Isometric,
            CameraProjectionMode projection = CameraProjectionMode.Orthographic)
        {
            if (selectionSet == null)
                throw new ArgumentNullException(nameof(selectionSet));

            if (!selectionSet.HasItems)
                throw new InvalidOperationException(
                    $"Não é possível criar viewpoint a partir do Selection Set vazio '{selectionSet.Name}'");

            try
            {
                var orientationLabel = ViewGenerationOptions.LabelOf(orientation);
                PerfLog.Info($"--- CreateViewpoint '{selectionSet.Name}' ({orientationLabel}, {projection}) ---");

                // Extent geométrico real do set (ignoreHidden=false: inclui itens mesmo que
                // estejam ocultos neste instante). Uma única chamada nativa — sem selecionar.
                var box = PerfLog.Time("BoundingBox(false)",
                    () => selectionSet.ModelItems.BoundingBox(false));

                // Partimos de uma cópia da vista atual para herdar render style, lighting e
                // world-up, e só sobrescrevemos projeção/orientação/enquadramento.
                var viewpoint = PerfLog.Time("CurrentViewpoint.CreateCopy",
                    () => _document.CurrentViewpoint.CreateCopy());
                PerfLog.TimeVoid("ApplyCamera",
                    () => ApplyCamera(viewpoint, box, orientation, projection));

                // Aplica a câmera à vista corrente (1 redraw) para que o capture a registre.
                PerfLog.TimeVoid("CurrentViewpoint.CopyFrom",
                    () => _document.CurrentViewpoint.CopyFrom(viewpoint));

                // Captura a vista corrente JÁ com os overrides de runtime: visibilidade
                // (Hide/Required do isolamento aplicado antes) e aparência. É isto que
                // habilita as caixas "Saved Attributes" no viewpoint.
                var savedViewpoint = PerfLog.Time("CaptureRuntimeOverrides",
                    () => _document.SavedViewpoints.CaptureRuntimeOverrides());
                if (savedViewpoint == null)
                    throw new InvalidOperationException("CaptureRuntimeOverrides retornou nulo");

                // Nome base vindo do template Excel (se houver); senão, gerado automaticamente.
                // O rótulo da orientação é anexado para diferenciar as múltiplas vistas do
                // mesmo set (ex.: "Fachada - Frontal", "Fachada - Superior").
                var baseName = !string.IsNullOrWhiteSpace(selectionSet.TemplateViewpointName)
                    ? selectionSet.TemplateViewpointName
                    : GenerateViewpointName(selectionSet.Name, orientationLabel);
                savedViewpoint.DisplayName = !string.IsNullOrWhiteSpace(selectionSet.TemplateViewpointName)
                    ? $"{baseName} - {orientationLabel}"
                    : baseName;

                PerfLog.TimeVoid("AddCopy",
                    () => _document.SavedViewpoints.AddCopy(savedViewpoint));

                // ReplaceFromCurrentView: garante que a câmera isométrica fique
                // corretamente gravada no viewpoint salvo. CaptureRuntimeOverrides
                // às vezes armazena coordenadas zeradas na primeira captura; o
                // "Update" equivalente ao botão direito do Navisworks re-lê a vista
                // corrente (que já tem a câmera isométrica aplicada pelo CopyFrom
                // acima) e substitui câmera + visibilidade no item já inserido.
                PerfLog.TimeVoid("ReplaceFromCurrentView", () =>
                {
                    // Localiza o item recém-adicionado: AddCopy insere no FINAL de Value
                    // (nível raiz). Iterar do fim garante que pegamos o último com esse
                    // nome mesmo que já exista um homônimo mais antigo.
                    SavedViewpoint addedVp = null;
                    foreach (var item in _document.SavedViewpoints.Value)
                    {
                        if (item is SavedViewpoint sv &&
                            sv.DisplayName == savedViewpoint.DisplayName)
                            addedVp = sv;
                    }
                    if (addedVp != null)
                        _document.SavedViewpoints.ReplaceFromCurrentView(addedVp);
                    else
                        PerfLog.Info("    ReplaceFromCurrentView: viewpoint não encontrado em Value (pulado)");
                });

                // Descrição: a do template tem prioridade; senão, a auto-gerada.
                var description = !string.IsNullOrWhiteSpace(selectionSet.TemplateDescription)
                    ? selectionSet.TemplateDescription
                    : $"Gerado automaticamente (vista {orientationLabel}, {projection}) a partir do " +
                      $"Selection Set: {selectionSet.Name} " +
                      $"(visibilidade={savedViewpoint.ContainsVisibilityOverrides}, " +
                      $"aparência={savedViewpoint.ContainsAppearanceOverrides})";

                return new ViewpointData
                {
                    Name = savedViewpoint.DisplayName,
                    Description = description,
                    SelectionSetName = selectionSet.Name
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Falha ao criar viewpoint a partir do Selection Set '{selectionSet.Name}'", ex);
            }
        }

        public void SaveViewpoint(ViewpointData viewpointData)
        {
            if (viewpointData == null)
                throw new ArgumentNullException(nameof(viewpointData));

            // Viewpoints já são persistidos no documento ao chamar AddCopy().
            // Método mantido como ponto de extensão para lógica de persistência futura.
        }

        public int GetViewpointCount()
        {
            try
            {
                return _document.SavedViewpoints.Value.Count;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Falha ao contar os viewpoints", ex);
            }
        }

        /// <summary>
        /// Orienta a câmera segundo a <paramref name="orientation"/> escolhida (isométrica ou uma
        /// das vistas ortogonais), define a <paramref name="projection"/> e enquadra a bounding
        /// box do set com <see cref="Viewpoint.ZoomBox"/>.
        ///
        /// Os eixos são derivados do "up" do modelo, então funciona tanto em modelos Z-up
        /// (Revit/Civil, o caso comum) quanto Y-up. A partir do up, derivamos:
        ///   • <c>forward</c> (h1) — eixo horizontal "frente/trás" (Y num modelo Z-up);
        ///   • <c>right</c>        — eixo horizontal "esquerda/direita" (X num modelo Z-up).
        /// A direção de visada (da câmera para a cena) e o "up" da vista são escolhidos por
        /// orientação. Pares opostos (Front/Back, Left/Right) são espelhados.
        /// </summary>
        private static void ApplyCamera(Viewpoint viewpoint, BoundingBox3D box,
            ViewOrientation orientation, CameraProjectionMode projection)
        {
            viewpoint.Projection = projection == CameraProjectionMode.Perspective
                ? ViewpointProjection.Perspective
                : ViewpointProjection.Orthographic;

            // Eixo vertical do mundo (com fallback Z-up se o viewpoint não tiver um).
            var up = (viewpoint.HasWorldUpVector
                        ? viewpoint.WorldUpVector.ToVector3D()
                        : new Vector3D(0, 0, 1)).Normalize();

            // Dois eixos horizontais perpendiculares ao "up".
            var refAxis = Math.Abs(up.Z) < 0.9 ? new Vector3D(0, 0, 1) : new Vector3D(1, 0, 0);
            var forward = up.Cross(refAxis).Normalize();   // frente/trás (Y se Z-up)
            var right = forward.Cross(up).Normalize();      // esquerda/direita (X se Z-up)

            Vector3D direction, viewUp;
            switch (orientation)
            {
                // Topo: olha para baixo; o "up" da vista é a frente (norte aponta p/ cima no plano).
                case ViewOrientation.Top:
                    direction = up.Negate(); viewUp = forward; break;
                // Frontal: câmera à frente (-forward) olhando para +forward.
                case ViewOrientation.Front:
                    direction = forward; viewUp = up; break;
                case ViewOrientation.Back:
                    direction = forward.Negate(); viewUp = up; break;
                // Lateral esquerda: câmera à esquerda olhando para a direita (+right).
                case ViewOrientation.Left:
                    direction = right; viewUp = up; break;
                case ViewOrientation.Right:
                    direction = right.Negate(); viewUp = up; break;
                // Isométrica: câmera no canto (+forward,+right,+up) olhando para dentro da caixa.
                case ViewOrientation.Isometric:
                default:
                    direction = forward.Add(right).Add(up).Negate().Normalize();
                    viewUp = up; break;
            }

            viewpoint.AlignDirection(direction);
            viewpoint.AlignUp(viewUp);

            // Guard contra caixa vazia.
            if (box != null && !box.IsEmpty)
            {
                // Leitura EXPLÍCITA do centro (e extents) do bounding box do set, registrada no
                // PerfLog para diagnóstico — dá para conferir que o centro lido corresponde ao
                // set em %TEMP%\AutoViewTool_perf.log.
                var c = box.Center;
                PerfLog.Info(
                    $"    bbox center=({Fmt(c.X)}, {Fmt(c.Y)}, {Fmt(c.Z)})  " +
                    $"min=({Fmt(box.Min.X)}, {Fmt(box.Min.Y)}, {Fmt(box.Min.Z)})  " +
                    $"max=({Fmt(box.Max.X)}, {Fmt(box.Max.Y)}, {Fmt(box.Max.Z)})  " +
                    $"size=({Fmt(box.Max.X - box.Min.X)}, {Fmt(box.Max.Y - box.Min.Y)}, {Fmt(box.Max.Z - box.Min.Z)})");

                // ZoomBox reposiciona a câmera ao longo da direção isométrica olhando para ESTE
                // centro e ajusta os extents para enquadrar a caixa — instantâneo, sem animação.
                // (Não usamos PointAt(center): ele sobrescreveria a direção isométrica recém-
                //  definida, apontando da posição atual da câmera para o centro.)
                viewpoint.ZoomBox(box);
            }
        }

        // Formata com ponto decimal (InvariantCulture) e 3 casas fixas para o log: evita a
        // ambiguidade do pt-BR, onde decimal e separador de coordenada são ambos vírgula.
        private static string Fmt(double v) => v.ToString("0.000", CultureInfo.InvariantCulture);

        private string GenerateViewpointName(string selectionSetName, string orientationLabel)
        {
            // Inclui orientação + milissegundos: um único set pode gerar várias vistas no mesmo
            // instante, então o rótulo garante nomes distintos e legíveis por vista.
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            return $"VP_{selectionSetName}_{orientationLabel}_{timestamp}";
        }
    }
}
