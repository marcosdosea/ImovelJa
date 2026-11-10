using System.ComponentModel.DataAnnotations;

namespace ImovelJaWeb.Models
{
    public class ImovelFiltroViewModel
    {
        [Display(Name = "Termo de busca")]
        [StringLength(100, ErrorMessage = "O campo {0} deve ter no máximo {1} caracteres")]
        [RegularExpression(@"^[\p{L}\p{N}\s,.-]*$", ErrorMessage = "O campo {0} deve conter apenas letras, números, espaços, hífens, vírgulas e pontos")]
        public string? Termo { get; set; }

        [Display(Name = "Localização")]
        [StringLength(100, ErrorMessage = "O campo {0} deve ter no máximo {1} caracteres")]
        [RegularExpression(@"^[\p{L}\p{N}\s,.-]*$", ErrorMessage = "O campo {0} deve conter apenas letras, números, espaços, hífens, vírgulas e pontos")]
        public string? Localizacao { get; set; }

        [Display(Name = "Tipos de imóvel")]
        public List<string>? Tipos { get; set; }

        [Display(Name = "Valor mínimo")]
        [Range(0, 9999999999.99, ErrorMessage = "O campo {0} não pode ser negativo")]
        public decimal? ValorMinimo { get; set; }

        [Display(Name = "Valor máximo")]
        [Range(0, 9999999999.99, ErrorMessage = "O campo {0} não pode ser negativo")]
        public decimal? ValorMaximo { get; set; }

        [Display(Name = "Quantidade de quartos")]
        [Range(0, 100, ErrorMessage = "O campo {0} deve ser um número entre 0 e 100")]
        public uint? QuantidadeQuartos { get; set; }

        [Display(Name = "Área mínima")]
        [Range(0, 9999999999.99, ErrorMessage = "O campo {0} não pode ser negativo")]
        public decimal? AreaMinima { get; set; }

        [Display(Name = "Área máxima")]
        [Range(0, 9999999999.99, ErrorMessage = "O campo {0} não pode ser negativo")]
        public decimal? AreaMaxima { get; set; }
    }
}
