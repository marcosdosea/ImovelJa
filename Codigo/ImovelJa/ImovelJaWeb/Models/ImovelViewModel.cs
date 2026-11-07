using System.ComponentModel.DataAnnotations;

namespace ImovelJaWeb.Models
{
    public class ImovelViewModel
    {
        [Display(Name = "Código")]
        [Key]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        public uint Id { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [StringLength(150, ErrorMessage = "O campo {0} deve ter no máximo {1} caracteres")]
        public string Nome { get; set; } = null!;

        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [RegularExpression("^(C|A|T|S|CO|O)$", ErrorMessage = "O campo {0} deve ser C, A, T, S, CO ou O")]
        public string Tipo { get; set; } = null!;

        [Display(Name = "Área total")]
        [Range(0.01, 99999999.99, ErrorMessage = "O campo {0} deve ser maior que zero")]
        public decimal? AreaTotal { get; set; }

        [Display(Name = "Banheiros")]
        public uint? QuantidadeBanheiros { get; set; }

        [Display(Name = "Quartos")]
        public uint? QuantidadeQuartos { get; set; }

        [Display(Name = "Vagas de garagem")]
        public uint? QuantidadeVagasGaragem { get; set; }

        [Display(Name = "Área da cozinha")]
        [Range(0.01, 99999999.99, ErrorMessage = "O campo {0} deve ser maior que zero")]
        public decimal? AreaCozinha { get; set; }

        [Display(Name = "Área da sala")]
        [Range(0.01, 99999999.99, ErrorMessage = "O campo {0} deve ser maior que zero")]
        public decimal? AreaSala { get; set; }

        [Display(Name = "Área do quintal")]
        [Range(0.01, 99999999.99, ErrorMessage = "O campo {0} deve ser maior que zero")]
        public decimal? AreaQuintal { get; set; }

        [Display(Name = "Área da lavanderia")]
        [Range(0.01, 99999999.99, ErrorMessage = "O campo {0} deve ser maior que zero")]
        public decimal? AreaLavanderia { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [StringLength(150, ErrorMessage = "O campo {0} deve ter no máximo {1} caracteres")]
        public string Rua { get; set; } = null!;

        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [StringLength(100, ErrorMessage = "O campo {0} deve ter no máximo {1} caracteres")]
        public string Bairro { get; set; } = null!;

        [Display(Name = "CEP")]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [RegularExpression("^[0-9]{8}$", ErrorMessage = "O campo {0} deve ter 8 dígitos, somente números")]
        public string Cep { get; set; } = null!;

        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [StringLength(50, ErrorMessage = "O campo {0} deve ter no máximo {1} caracteres")]
        public string Estado { get; set; } = null!;

        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [StringLength(100, ErrorMessage = "O campo {0} deve ter no máximo {1} caracteres")]
        public string Cidade { get; set; } = null!;

        [DataType(DataType.Currency)]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [Range(0.01, 9999999999.99, ErrorMessage = "O campo {0} deve ser maior que zero")]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [RegularExpression("^(A|V)$", ErrorMessage = "O campo {0} deve ser A ou V")]
        public string Finalidade { get; set; } = null!;

        [Display(Name = "Situação")]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [RegularExpression("^(D|A|V|P)$", ErrorMessage = "O campo {0} deve ser D, A, V ou P")]
        public string Status { get; set; } = "D";

        [Display(Name = "Situação da validação")]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [RegularExpression("^(P|A|R)$", ErrorMessage = "O campo {0} deve ser P, A ou R")]
        public string StatusValidacao { get; set; } = "P";

        [Display(Name = "Proprietário")]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        public uint IdPessoaProprietario { get; set; }
    }
}
