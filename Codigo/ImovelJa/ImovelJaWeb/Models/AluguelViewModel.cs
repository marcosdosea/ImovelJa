using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Util;

namespace ImovelJaWeb.Models
{
    public class AluguelViewModel
    {
        [Display(Name = "Código")]
        [Key]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        public uint Id { get; set; }

        [Display(Name = "Dia do pagamento")]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [Range(1, 31, ErrorMessage = "O campo {0} deve estar entre {1} e {2}")]
        public int DiaPagamento { get; set; }

        [Display(Name = "Próximo vencimento")]
        [DataType(DataType.Date)]
        public DateTime? DataVencimentoProximo { get; set; }

        [DataType(DataType.Currency)]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [Range(0.01, 9999999999.99, ErrorMessage = "O campo {0} deve ser maior que zero")]
        public decimal Valor { get; set; }

        [Display(Name = "Contrato (PDF, JPG ou PNG)")]
        [ArquivoContrato]
        public IFormFile? Contrato { get; set; }

        [Display(Name = "Contrato anexado")]
        public bool TemContrato { get; set; }

        [Display(Name = "Situação")]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [RegularExpression("^(A|C)$", ErrorMessage = "O campo {0} deve ser A ou C")]
        public string Status { get; set; } = "A";

        [Display(Name = "Imóvel")]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        public uint IdImovel { get; set; }

        /// <summary>
        /// Diferente de zero: inquilino já cadastrado. Zero no Create: usa os dados de Inquilino para cadastrar (RN04)
        /// </summary>
        [Display(Name = "Inquilino")]
        public uint IdPessoaInquilino { get; set; }

        public PessoaViewModel? Inquilino { get; set; }
    }
}