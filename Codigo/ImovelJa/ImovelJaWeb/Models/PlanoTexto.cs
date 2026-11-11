namespace ImovelJaWeb.Models
{
    public static class PlanoTexto
    {
        /// <summary>
        /// A partir deste limite a tela mostra "Ilimitado", como no plano Imobiliária.
        /// </summary>
        public const uint LimiteIlimitado = 1_000_000;

        public static bool Gratuito(decimal valor)
        {
            return valor == 0m;
        }

        public static bool Inativo(string? status)
        {
            return status == "I";
        }

        public static string Situacao(string? status)
        {
            return Inativo(status) ? "Inativo" : "Ativo";
        }

        public static string Limite(uint limite)
        {
            if (limite >= LimiteIlimitado)
            {
                return "Ilimitado imóveis";
            }

            if (limite == 1)
            {
                return "Até 1 imóvel";
            }

            return $"Até {limite} imóveis";
        }

        public static IReadOnlyList<string> Beneficios(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return [];
            }

            return texto.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}
