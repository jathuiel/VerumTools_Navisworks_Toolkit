using System;

using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Shared
{
    internal static class ExceptionHelper
    {
        internal static string UnwrapMessage(Exception ex)
        {
            if (ex == null) return "Erro desconhecido.";
            while (ex.InnerException != null)
                ex = ex.InnerException;
            return ex.Message;
        }
    }
}
