using System.ComponentModel.DataAnnotations;

namespace ImovelJaWeb.Models
{
    public class AluguelViewModel
    {
        [Display(Name = "Código")]
        [Key]
        public uint Id { get; set; }

        [Display(Name = "Dia de pagamento")]
        [Range(1, 31, ErrorMessage = "O campo {0} deve estar entre {1} e {2}")]
        public int DiaPagamento { get; set; }

        [Display(Name = "Próximo vencimento")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? DataVencimentoProximo { get; set; }

        [Display(Name = "Valor (R$)")]
        [DataType(DataType.Currency)]
        [Range(0.01, 9999999999.99, ErrorMessage = "O campo {0} deve ser maior que zero")]
        public decimal Valor { get; set; }

        /// <summary>
        /// A = ATIVO, C = CANCELADO
        /// </summary>
        [Display(Name = "Situação")]
        [RegularExpression("^(A|C)$", ErrorMessage = "O campo {0} deve ser A ou C")]
        public string Status { get; set; } = "A";

        [Display(Name = "Imóvel")]
        public uint IdImovel { get; set; }

        [Display(Name = "Inquilino")]
        public uint IdPessoaInquilino { get; set; }

        public bool PossuiContrato { get; set; }

        public ImovelViewModel? Imovel { get; set; }

        public PessoaViewModel? Inquilino { get; set; }

        public List<PagamentoViewModel> Pagamentos { get; set; } = new();
    }
}
