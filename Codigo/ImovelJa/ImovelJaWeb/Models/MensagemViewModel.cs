using System.ComponentModel.DataAnnotations;

namespace ImovelJaWeb.Models
{
    public class MensagemViewModel
    {
        [Display(Name = "Código")]
        public uint Id { get; set; }

        [Display(Name = "Mensagem")]
        [Required(ErrorMessage = "A mensagem não pode estar em branco.")]
        public string Texto { get; set; } = null!;

        [Display(Name = "Data de envio")]
        public DateTime DataEnvio { get; set; }

        [Display(Name = "Remetente")]
        [Range(1, uint.MaxValue, ErrorMessage = "O campo {0} é obrigatório")]
        public uint IdPessoaEnviou { get; set; }

        [Display(Name = "Destinatário")]
        [Range(1, uint.MaxValue, ErrorMessage = "O campo {0} é obrigatório")]
        public uint IdPessoaRecebeu { get; set; }

        public bool Enviada { get; set; }
    }
}
