using System;
using System.Collections.Generic;
using System.Text;

namespace Core.DTO
{
    /// <summary>
    /// Critérios opcionais da busca pública de imóveis
    /// </summary>
    public class ImovelFiltroDTO
    {
        public string? Termo { get; set; }
        public string? Localizacao { get; set; }
        public List<string>? Tipos { get; set; }
        public decimal? ValorMinimo { get; set; }
        public decimal? ValorMaximo { get; set; }
        public uint? Quartos { get; set; }
        public decimal? AreaMinima { get; set; }
        public decimal? AreaMaxima { get; set; }
    }
}
