namespace ImovelJaWeb.Models
{
    public class ConversaViewModel
    {
        public uint IdPessoa { get; set; }

        public string NomePessoa { get; set; } = string.Empty;

        public uint? IdImovel { get; set; }

        public string? NomeImovel { get; set; }

        public uint IdDestinatario { get; set; }

        public string NomeDestinatario { get; set; } = string.Empty;

        public bool ConversaAberta { get; set; }

        public IList<ConversaItemViewModel> Conversas { get; set; } = [];

        public IList<MensagemViewModel> Mensagens { get; set; } = [];

        public MensagemViewModel NovaMensagem { get; set; } = new();
    }

    public class ConversaItemViewModel
    {
        public uint IdPessoa { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string? NomeImovel { get; set; }

        public string Texto { get; set; } = string.Empty;

        public DateTime? DataEnvio { get; set; }

        public bool Ativa { get; set; }
    }
}
