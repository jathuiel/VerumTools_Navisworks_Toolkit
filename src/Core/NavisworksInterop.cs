using System;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.DocumentParts;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Core
{
    /// <summary>
    /// Wrapper robusto para interação com a Navisworks API.
    /// Trata verificações de nulo, encapsulamento de exceções e validação do estado do documento.
    /// </summary>
    public class NavisworksInterop : IDisposable
    {
        private bool _disposed;

        public Document GetActiveDocument()
        {
            return Get("Falha ao acessar o documento ativo", () =>
            {
                var doc = Application.ActiveDocument;
                if (doc == null)
                    throw new InvalidOperationException("Nenhum documento Navisworks ativo");
                return doc;
            });
        }

        /// <summary>Itens atualmente selecionados no documento.</summary>
        public ModelItemCollection GetCurrentSelection()
            => Get("Falha ao obter a seleção atual",
                   () => GetActiveDocument().CurrentSelection.SelectedItems);

        /// <summary>Conjuntos de seleção (Selection Sets) armazenados no documento.</summary>
        public DocumentSelectionSets GetSelectionSets()
            => Get("Falha ao obter os Selection Sets",
                   () => GetActiveDocument().SelectionSets);

        /// <summary>Viewpoints salvos no documento.</summary>
        public DocumentSavedViewpoints GetSavedViewpoints()
            => Get("Falha ao obter os viewpoints salvos",
                   () => GetActiveDocument().SavedViewpoints);

        /// <summary>Reexibe todos os itens do modelo (desfaz qualquer isolamento).</summary>
        public void ResetVisibility()
        {
            ThrowIfDisposed();

            try
            {
                var doc = GetActiveDocument();
                // ResetAllHidden é uma única operação nativa: reexibe tudo sem
                // enumerar/marshalar o modelo inteiro (RootItemDescendantsAndSelf).
                doc.Models.ResetAllHidden();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Falha ao restaurar a visibilidade", ex);
            }
        }

        /// <summary>
        /// Boilerplate comum dos getters: valida o estado do wrapper, deixa passar
        /// InvalidOperationException (erro de negócio já contextualizado, ex.: "Nenhum
        /// documento ativo") e embrulha qualquer outra exceção com a mensagem de contexto.
        /// </summary>
        private T Get<T>(string errorMessage, Func<T> getter)
        {
            ThrowIfDisposed();
            try
            {
                return getter();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(errorMessage, ex);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
