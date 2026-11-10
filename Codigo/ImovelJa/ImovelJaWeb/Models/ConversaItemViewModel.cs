namespace ImovelJaWeb.Models
{
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
