using System.ComponentModel.DataAnnotations;

namespace ImovelJaWeb.Models
{
    public class PagamentoViewModel
    {
        [Display(Name = "Código")]
        [Key]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        public uint Id { get; set; }

        [Display(Name = "Mês de referência")]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime MesReferencia { get; set; }

        [Display(Name = "Valor pago (R$)")]
        [Range(0.01, 9999999999.99, ErrorMessage = "O campo {0} deve ser maior que zero")]
        [DataType(DataType.Currency)]
        public decimal? ValorPago { get; set; }

        [Display(Name = "Data do pagamento")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? DataPagamento { get; set; }

        [Display(Name = "Comprovante")]
        public byte[]? Comprovante { get; set; }

        public bool PossuiComprovante { get; set; }

        /// <summary>
        /// PE = PENDENTE, EA = EM ATRASO, PA = PAGO
        /// </summary>
        [Display(Name = "Situação")]
        [Required(ErrorMessage = "O campo {0} é obrigatório")]
        [RegularExpression("^(PE|EA|PA)$", ErrorMessage = "O campo {0} deve ser PE, EA ou PA")]
        public string Status { get; set; } = "PE";

        [Display(Name = "Aluguel")]
        [Range(1, uint.MaxValue, ErrorMessage = "O campo {0} é obrigatório")]
        public uint IdAluguel { get; set; }

        [Display(Name = "Valor do aluguel (R$)")]
        [DataType(DataType.Currency)]
        public decimal ValorAluguel { get; set; }

        [Display(Name = "Imóvel")]
        public string? NomeImovel { get; set; }
    }
}
