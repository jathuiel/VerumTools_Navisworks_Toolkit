using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.ComApi;
using ComApi = Autodesk.Navisworks.Api.Interop.ComApi;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.AttributeLab
{
    public static class AtributoService
    {
        private const string CATEGORIA_PADRAO = VerumSchema.CategoriaPrincipal;

        public static List<AtributoCustom> LerPropriedades(ModelItem item)
        {
            var resultado = new List<AtributoCustom>();
            if (item == null) return resultado;

            foreach (PropertyCategory categoria in item.PropertyCategories)
            {
                foreach (DataProperty prop in categoria.Properties)
                {
                    resultado.Add(new AtributoCustom(
                        categoria.DisplayName ?? categoria.Name,
                        prop.DisplayName ?? prop.Name,
                        FormatarValor(prop.Value),
                        ObterTipo(prop.Value)));
                }
            }

            return resultado;
        }

        public static List<string> LerSetsSalvos(
            ModelItem item,
            IEnumerable<string> nomesSetsValidos = null,
            string nomeCategoria = null)
        {
            var resultado = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (item == null) return resultado.ToList();

            var nomesValidos = new HashSet<string>(
                (nomesSetsValidos ?? Enumerable.Empty<string>())
                    .Where(nome => !string.IsNullOrWhiteSpace(nome))
                    .Select(nome => nome.Trim()),
                StringComparer.OrdinalIgnoreCase);

            foreach (PropertyCategory categoria in item.PropertyCategories)
            {
                var nomeCat = categoria.DisplayName ?? categoria.Name ?? string.Empty;
                if (!ObterCategoriasRelacionadas(nomeCategoria ?? CATEGORIA_PADRAO)
                    .Contains(nomeCat, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (DataProperty prop in categoria.Properties)
                {
                    var nomeProp = (prop.DisplayName ?? prop.Name ?? string.Empty).Trim();
                    var valorProp = FormatarValor(prop.Value)?.Trim() ?? string.Empty;

                    if (EhPropriedadeDeSets(nomeProp))
                    {
                        foreach (var nomeSet in SepararListaSets(valorProp))
                        {
                            if (nomesValidos.Count == 0 || nomesValidos.Contains(nomeSet))
                                resultado.Add(nomeSet);
                        }
                        continue;
                    }

                    if (nomesValidos.Count > 0 &&
                        nomesValidos.Contains(nomeProp) &&
                        string.Equals(valorProp, nomeProp, StringComparison.OrdinalIgnoreCase))
                    {
                        resultado.Add(nomeProp);
                    }
                }
            }

            return resultado.OrderBy(nome => nome, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static (int sucesso, int erros, string mensagem) GravarAtributos(
            ModelItemCollection itens,
            List<AtributoCustom> atributos,
            string nomeCategoria = null,
            Dictionary<ModelItem, List<SetAssignment>> setsPorItem = null)
        {
            if (itens == null || itens.Count == 0) return (0, 0, "No elements selected.");

            bool temAtributos = atributos != null && atributos.Count > 0;
            bool temSets = setsPorItem != null && setsPorItem.Values.Any(sets => sets != null && sets.Count > 0);
            if (!temAtributos && !temSets) return (0, 0, "No attributes or sets to write.");
            atributos = atributos ?? new List<AtributoCustom>();

            var categoria = string.IsNullOrWhiteSpace(nomeCategoria)
                ? CATEGORIA_PADRAO
                : nomeCategoria.Trim();
            var internalName = categoria.Replace(" ", "_") + "_Internal";

            ComApi.InwOpState10 oState = null;
            bool editStarted = false;

            try
            {
                oState = (ComApi.InwOpState10)ComBridgeHelper.ObterEstado();
                oState.BeginEdit("setattributes_gravar");
                editStarted = true;

                int sucesso = 0;
                int erros = 0;
                string ultimoErro = null;

                foreach (ModelItem item in itens)
                {
                    try
                    {
                        var itemColl = new ModelItemCollection();
                        itemColl.Add(item);
                        var sel = ComApiBridge.ToInwOpSelection(itemColl);
                        var paths = sel.Paths();
                        var path = (ComApi.InwOaPath3)paths.Last();
                        var guiNode = (ComApi.InwGUIPropertyNode2)oState.GetGUIPropertyNode(path, true);

                        RemoverCategoriasRelacionadas(guiNode, categoria);

                        var propVec = (ComApi.InwOaPropertyVec)oState.ObjectFactory(
                            ComApi.nwEObjectType.eObjectType_nwOaPropertyVec, null, null);

                        var nomesInternosUsados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        int propriedadesAdicionadas = 0;

                        foreach (var atr in atributos)
                        {
                            if (string.IsNullOrWhiteSpace(atr?.Nome)) continue;

                            AdicionarPropriedade(
                                oState,
                                propVec,
                                atr.Nome,
                                ConverterValor(atr),
                                nomesInternosUsados);
                            propriedadesAdicionadas++;
                        }

                        if (setsPorItem != null &&
                            setsPorItem.TryGetValue(item, out var setsDoItem) &&
                            setsDoItem != null)
                        {
                            var nomesSets = setsDoItem
                                .Where(setInfo => !string.IsNullOrWhiteSpace(setInfo?.Nome))
                                .Select(setInfo => setInfo.Nome.Trim())
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(nome => nome, StringComparer.OrdinalIgnoreCase)
                                .ToList();

                            if (nomesSets.Count > 0)
                            {
                                AdicionarPropriedade(
                                    oState,
                                    propVec,
                                    VerumSchema.PropriedadeSets,
                                    string.Join(" | ", nomesSets),
                                    nomesInternosUsados);
                                propriedadesAdicionadas++;
                            }
                        }

                        if (propriedadesAdicionadas == 0)
                            continue;

                        guiNode.SetUserDefined(0, categoria, internalName, propVec);
                        sucesso++;
                    }
                    catch (Exception ex)
                    {
                        erros++;
                        ultimoErro = ex.Message;
                    }
                }

                var msg = $"Saved: {sucesso} element(s). Errors: {erros}.";
                if (erros > 0 && !string.IsNullOrWhiteSpace(ultimoErro))
                    msg += $"\n\nLast error: {ultimoErro}";

                return (sucesso, erros, msg);
            }
            catch (Exception ex)
            {
                return (0, itens.Count, $"Error accessing COM API: {ex.Message}");
            }
            finally
            {
                if (editStarted)
                {
                    try { oState.EndEdit(); }
                    catch { }
                }
            }
        }

        public static (int sucesso, int erros, string mensagem) ExcluirAtributos(
            ModelItemCollection itens,
            string nomeCategoria = null)
        {
            if (itens == null || itens.Count == 0) return (0, 0, "No elements selected.");

            var categoria = string.IsNullOrWhiteSpace(nomeCategoria)
                ? CATEGORIA_PADRAO
                : nomeCategoria.Trim();

            ComApi.InwOpState10 oState = null;
            bool editStarted = false;

            try
            {
                oState = (ComApi.InwOpState10)ComBridgeHelper.ObterEstado();
                oState.BeginEdit("setattributes_excluir");
                editStarted = true;

                int removidos = 0;
                int naoEncontrados = 0;
                int erros = 0;
                string ultimoErro = null;

                foreach (ModelItem item in itens)
                {
                    try
                    {
                        var itemColl = new ModelItemCollection();
                        itemColl.Add(item);
                        var sel = ComApiBridge.ToInwOpSelection(itemColl);
                        var paths = sel.Paths();
                        var path = (ComApi.InwOaPath3)paths.Last();
                        var guiNode = (ComApi.InwGUIPropertyNode2)oState.GetGUIPropertyNode(path, true);

                        if (RemoverCategoriasRelacionadas(guiNode, categoria))
                            removidos++;
                        else
                            naoEncontrados++;
                    }
                    catch (Exception ex)
                    {
                        erros++;
                        ultimoErro = ex.Message;
                    }
                }

                var msg = $"Removed: {removidos} element(s). Not found: {naoEncontrados}. Errors: {erros}.";
                if (erros > 0 && !string.IsNullOrWhiteSpace(ultimoErro))
                    msg += $"\n\nLast error: {ultimoErro}";

                return (removidos, erros, msg);
            }
            catch (Exception ex)
            {
                return (0, itens.Count, $"Error accessing COM API: {ex.Message}");
            }
            finally
            {
                if (editStarted)
                {
                    try { oState.EndEdit(); }
                    catch { }
                }
            }
        }

        private static void AdicionarPropriedade(
            ComApi.InwOpState10 oState,
            ComApi.InwOaPropertyVec propVec,
            string nome,
            object valor,
            HashSet<string> nomesInternosUsados)
        {
            var prop = (ComApi.InwOaProperty)oState.ObjectFactory(
                ComApi.nwEObjectType.eObjectType_nwOaProperty, null, null);

            prop.name = CriarNomeInternoPropriedade(nome, nomesInternosUsados);
            prop.UserName = nome;
            prop.value = valor ?? string.Empty;
            propVec.Properties().Add(prop);
        }

        private static string CriarNomeInternoPropriedade(string nome, HashSet<string> nomesInternosUsados)
        {
            if (nomesInternosUsados == null)
                nomesInternosUsados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(nome))
                nome = "prop";

            var baseName = new string(nome.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "prop";

            string candidate = baseName + "_prop";
            int suffix = 2;
            while (!nomesInternosUsados.Add(candidate))
                candidate = $"{baseName}_{suffix++}_prop";

            return candidate;
        }

        private static bool RemoverCategoriasRelacionadas(ComApi.InwGUIPropertyNode2 guiNode, string nomeCategoriaPrincipal)
        {
            bool removeuAlguma = false;
            foreach (var nomeCategoria in ObterCategoriasRelacionadas(nomeCategoriaPrincipal))
            {
                if (RemoverCategoriaExistente(guiNode, nomeCategoria))
                    removeuAlguma = true;
            }
            return removeuAlguma;
        }

        private static IEnumerable<string> ObterCategoriasRelacionadas(string nomeCategoriaPrincipal)
        {
            var nomes = new List<string>();
            if (!string.IsNullOrWhiteSpace(nomeCategoriaPrincipal))
                nomes.Add(nomeCategoriaPrincipal.Trim());
            nomes.AddRange(VerumSchema.CategoriasLegadas);

            return nomes
                .Where(nome => !string.IsNullOrWhiteSpace(nome))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static bool RemoverCategoriaExistente(ComApi.InwGUIPropertyNode2 guiNode, string nomeCategoria)
        {
            int idx = 1;
            foreach (ComApi.InwGUIAttribute2 attr in guiNode.GUIAttributes())
            {
                try
                {
                    if (attr.UserDefined && string.Equals(attr.ClassUserName, nomeCategoria, StringComparison.OrdinalIgnoreCase))
                    {
                        guiNode.RemoveUserDefined(idx);
                        return true;
                    }

                    if (attr.UserDefined) idx++;
                }
                catch
                {
                    // Keep iterating remaining attributes.
                }
            }

            return false;
        }

        private static object ConverterValor(AtributoCustom atr)
        {
            var tipo = atr?.Tipo?.ToLowerInvariant();
            var valor = atr?.Valor ?? string.Empty;

            switch (tipo)
            {
                case "double":
                case "float":
                    if (double.TryParse(valor.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double dv))
                        return dv;
                    return valor;

                case "int":
                    if (int.TryParse(valor, out int iv))
                        return iv;
                    return valor;

                case "bool":
                case "boolean":
                    if (!bool.TryParse(valor, out bool bv))
                        bv = valor.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                             valor.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                             valor.Equals("sim", StringComparison.OrdinalIgnoreCase) ||
                             valor == "1";
                    return bv;

                default:
                    return valor;
            }
        }

        private static string FormatarValor(VariantData valor)
        {
            if (valor == null) return string.Empty;
            switch (valor.DataType)
            {
                case VariantDataType.Double: return valor.ToDouble().ToString("G");
                case VariantDataType.Int32: return valor.ToInt32().ToString();
                case VariantDataType.Boolean: return valor.ToBoolean() ? "True" : "False";
                case VariantDataType.DisplayString: return valor.ToDisplayString();
                case VariantDataType.IdentifierString: return valor.ToIdentifierString();
                default: return valor.ToString();
            }
        }

        private static string ObterTipo(VariantData valor)
        {
            if (valor == null) return "string";
            switch (valor.DataType)
            {
                case VariantDataType.Double: return "double";
                case VariantDataType.Int32: return "int";
                case VariantDataType.Boolean: return "bool";
                default: return "string";
            }
        }

        private static IEnumerable<string> SepararListaSets(string texto)
        {
            return (texto ?? string.Empty)
                .Split(new[] { '|', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(parte => parte.Trim())
                .Where(parte => !string.IsNullOrWhiteSpace(parte));
        }

        private static bool EhPropriedadeDeSets(string nomePropriedade)
        {
            if (string.IsNullOrWhiteSpace(nomePropriedade))
                return false;

            if (string.Equals(nomePropriedade, VerumSchema.PropriedadeSets, StringComparison.OrdinalIgnoreCase))
                return true;

            return VerumSchema.PropriedadesSetsLegadas.Any(
                nome => string.Equals(nome, nomePropriedade, StringComparison.OrdinalIgnoreCase));
        }
    }

    internal static class ComBridgeHelper
    {
        private static readonly string[] BRIDGE_CANDIDATES =
        {
            "Autodesk.Navisworks.Api.ComApi.ComApiBridge",
            "Autodesk.Navisworks.ComApi.ComApiBridge",
        };

        private static Type _bridgeType;
        private static readonly object LockObj = new object();

        private static Type ObterTipoBridge()
        {
            if (_bridgeType != null) return _bridgeType;

            lock (LockObj)
            {
                if (_bridgeType != null) return _bridgeType;

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var candidato in BRIDGE_CANDIDATES)
                    {
                        try
                        {
                            var tipo = asm.GetType(candidato);
                            if (tipo == null) continue;
                            _bridgeType = tipo;
                            return _bridgeType;
                        }
                        catch
                        {
                            // Ignore probing exceptions.
                        }
                    }
                }

                _bridgeType = TentarCarregarDllComApi();
                if (_bridgeType == null)
                {
                    throw new InvalidOperationException(
                        "ComApiBridge not found in any loaded assembly.\n" +
                        $"Candidates tried:\n - {string.Join("\n - ", BRIDGE_CANDIDATES)}");
                }

                return _bridgeType;
            }
        }

        private static Type TentarCarregarDllComApi()
        {
            var dllsNome = new[] { "Autodesk.Navisworks.ComApi", "Autodesk.Navisworks.Api.ComApi" };

            foreach (var nome in dllsNome)
            {
                try
                {
                    var asm = Assembly.Load(nome);
                    foreach (var candidato in BRIDGE_CANDIDATES)
                    {
                        var tipo = asm?.GetType(candidato);
                        if (tipo != null) return tipo;
                    }
                }
                catch
                {
                    // Ignore and continue.
                }
            }

            return null;
        }

        public static ComApi.InwOpState ObterEstado()
        {
            var bridge = ObterTipoBridge();
            var prop = bridge.GetProperty("State", BindingFlags.Public | BindingFlags.Static);

            if (prop == null)
                throw new InvalidOperationException("Property 'State' not found in ComApiBridge.");

            var state = prop.GetValue(null) as ComApi.InwOpState;
            if (state == null)
                throw new InvalidOperationException("ComApiBridge.State returned null.");

            return state;
        }
    }
}
