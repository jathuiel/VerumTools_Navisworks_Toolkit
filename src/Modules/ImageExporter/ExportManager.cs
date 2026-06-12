using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Autodesk.Navisworks.Api;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.ImageExporter
{
    /// <summary>Parâmetros de uma exportação de imagens (resolução, qualidade, nomeação).</summary>
    public class ExportOptions
    {
        public string OutputFolder { get; set; }
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
        public long JpegQuality { get; set; } = 90;     // 0–100
        public string Prefix { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
        public bool IncludeMarkups { get; set; } = true; // ScenePlusOverlay vs Scene
    }

    /// <summary>
    /// Núcleo da exportação de imagens dos Viewpoints. Verificado na API 2026 por reflexão:
    ///  - <c>View.GenerateImage(ImageGenerationStyle, w, h, bool)</c> retorna <c>System.Drawing.Bitmap</c>.
    ///  - <c>DocumentSavedViewpoints.CurrentSavedViewpoint</c> é settable: aplica o viewpoint
    ///    (câmera + visibilidade + overrides) à vista corrente antes de renderizar.
    ///  - <c>ImageGenerationStyle.ScenePlusOverlay</c> inclui overlays/redlines (markups).
    /// Renderizar é pesado e roda na thread STA da UI: a janela faz o loop com Dispatcher.Yield
    /// e chama <see cref="ExportOne"/> por item (este método é síncrono e renderiza 1 imagem).
    /// </summary>
    public class ExportManager
    {
        private readonly NavisworksInterop _interop;
        private readonly Document _document;

        public ExportManager(NavisworksInterop interop)
        {
            _interop = interop ?? throw new ArgumentNullException(nameof(interop));
            _document = _interop.GetActiveDocument();
        }

        /// <summary>Todos os Viewpoints (folhas) com caminho; duplicatas por nome são sinalizadas.</summary>
        public List<ExportItemData> GetViewpoints()
        {
            var list = new List<ExportItemData>();
            try
            {
                var root = _document.SavedViewpoints.RootItem as GroupItem;
                if (root != null)
                    SavedItemTree.VisitLeaves(root.Children, string.Empty,
                        (item, path) => AddIfViewpoint(item, path, list));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Falha ao listar os Viewpoints", ex);
            }
            MarkDuplicates(list);
            return list;
        }

        // A travessia (pastas → folhas) é compartilhada em SavedItemTree; só viewpoints
        // REAIS entram na lista (cortes/animações são ignorados).
        private static void AddIfViewpoint(SavedItem item, string path, List<ExportItemData> list)
        {
            var svp = item as SavedViewpoint;
            if (svp == null) return;

            list.Add(new ExportItemData
            {
                DisplayName = string.IsNullOrEmpty(item.DisplayName) ? "(sem nome)" : item.DisplayName,
                Path = path,
                Viewpoint = svp
            });
        }

        // Sinaliza viewpoints que compartilham o mesmo nome (case-insensitive).
        private static void MarkDuplicates(List<ExportItemData> list)
        {
            foreach (var g in list.GroupBy(i => i.DisplayName ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                if (g.Count() > 1)
                    foreach (var i in g) i.IsDuplicate = true;
            }
        }

        /// <summary>
        /// Renderiza UM viewpoint e salva como JPG na pasta de saída. <paramref name="usedNames"/>
        /// acumula os nomes-base já usados nesta sessão de exportação para desambiguar colisões
        /// (dois viewpoints de mesmo nome → "nome", "nome_2", ...). Retorna o caminho gravado.
        /// </summary>
        public string ExportOne(ExportItemData item, ExportOptions options, ISet<string> usedNames)
        {
            if (item?.Viewpoint == null) throw new ArgumentNullException(nameof(item));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(options.OutputFolder))
                throw new InvalidOperationException("Pasta de saída não definida");

            var view = _document.ActiveView;
            if (view == null)
                throw new InvalidOperationException("Não há vista ativa para renderizar");

            // Aplica o viewpoint (câmera + visibilidade + aparência salvas) à vista corrente.
            _document.SavedViewpoints.CurrentSavedViewpoint = item.Viewpoint;

            var style = options.IncludeMarkups ? ImageGenerationStyle.ScenePlusOverlay : ImageGenerationStyle.Scene;
            var w = Math.Max(16, options.Width);
            var h = Math.Max(16, options.Height);

            using (var bmp = view.GenerateImage(style, w, h, true))
            {
                Directory.CreateDirectory(options.OutputFolder);
                var name = MakeUnique(BuildBaseName(item, options), usedNames);
                var path = System.IO.Path.Combine(options.OutputFolder, name + ".jpg");
                SaveJpeg(bmp, path, options.JpegQuality);
                return path;
            }
        }

        private static string BuildBaseName(ExportItemData item, ExportOptions options)
        {
            var name = $"{options.Prefix}{Sanitize(item.DisplayName)}{options.Suffix}".Trim();
            return string.IsNullOrWhiteSpace(name) ? "viewpoint" : name;
        }

        private static string MakeUnique(string baseName, ISet<string> used)
        {
            var name = baseName;
            var n = 2;
            while (used.Contains(name)) { name = $"{baseName}_{n}"; n++; }
            used.Add(name);
            return name;
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "viewpoint";
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s.Trim();
        }

        // Salva JPEG controlando a qualidade (0–100) via o encoder JPEG do GDI+.
        private static void SaveJpeg(Bitmap bmp, string path, long quality)
        {
            var q = Math.Max(0L, Math.Min(100L, quality));
            var codec = ImageCodecInfo.GetImageEncoders()
                .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);

            if (codec == null)
            {
                bmp.Save(path, ImageFormat.Jpeg);
                return;
            }
            using (var ep = new EncoderParameters(1))
            {
                ep.Param[0] = new EncoderParameter(Encoder.Quality, q);
                bmp.Save(path, codec, ep);
            }
        }
    }
}
