using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ImovelJaWeb.Models
{
    public class PlanoViewModel
    {
        [Display(Name = "Código")]
        [Key]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        public uint Id { get; set; }

        [Display(Name = "Nome")]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [StringLength(100, ErrorMessage = "O campo {0} deve ter no máximo {1} caracteres")]
        [RegularExpression("^(Básico|Profissional|Imobiliária)$", ErrorMessage = "O campo {0} deve ser Básico, Profissional ou Imobiliária")]
        public string Nome { get; set; } = null!;

        [Display(Name = "Preço (R$)")]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [Range(0, 99999999.99, ErrorMessage = "O campo {0} deve estar entre {1} e {2}")]
        [DisplayFormat(DataFormatString = "{0:0.00}", ApplyFormatInEditMode = true)]
        [ModelBinder(BinderType = typeof(PrecoPlanoModelBinder))]
        public decimal Valor { get; set; }

        [Display(Name = "Limite de imóveis anunciados")]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [Range(0, uint.MaxValue, ErrorMessage = "O campo {0} não pode ser negativo")]
        public uint LimiteImoveisAnunciados { get; set; }

        [Display(Name = "Benefícios")]
        public string? Beneficios { get; set; }

        [Display(Name = "Situação")]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [RegularExpression("^(A|I)$", ErrorMessage = "O campo {0} deve ser A ou I")]
        public string Status { get; set; } = "A";
    }
}
