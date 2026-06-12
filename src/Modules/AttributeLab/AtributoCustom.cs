using NavisworksToolkit.Core;
using NavisworksToolkit.Shared;

namespace NavisworksToolkit.Modules.AttributeLab
{
    public class AtributoCustom
    {
        public string Categoria { get; set; }
        public string Nome { get; set; }
        public string Valor { get; set; }
        public string Tipo { get; set; }

        public AtributoCustom(string categoria, string nome, string valor, string tipo = "string")
        {
            Categoria = categoria;
            Nome = nome;
            Valor = valor;
            Tipo = tipo;
        }
    }
}
