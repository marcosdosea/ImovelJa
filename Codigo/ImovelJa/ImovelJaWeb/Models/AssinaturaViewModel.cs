using System.ComponentModel.DataAnnotations;

namespace ImovelJaWeb.Models
{
    public class AssinaturaViewModel
    {
        [Display(Name = "Código")]
        [Key]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        public uint Id { get; set; }

        [Display(Name = "Data de contratação")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        public DateTime DataContratacao { get; set; }

        [Display(Name = "Próxima cobrança")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        public DateTime DataProximaCobranca { get; set; }

        [Display(Name = "Situação")]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [RegularExpression("^(A|C)$", ErrorMessage = "O campo {0} deve ser A ou C")]
        public string Status { get; set; } = "A";

        [Display(Name = "Pessoa")]
        [Range(1, uint.MaxValue, ErrorMessage = "O campo {0} é obrigatório")]
        public uint IdPessoa { get; set; }

        [Display(Name = "Plano")]
        [Range(1, uint.MaxValue, ErrorMessage = "Selecione um plano")]
        public uint IdPlano { get; set; }
    }
}
