using System.ComponentModel.DataAnnotations;


namespace Util
{
    /// <summary>
    /// Validação customizada para CEP
    /// </summary>
    public class CepAttribute : ValidationAttribute
    {
        /// <summary>
        /// Construtor
        /// </summary>
        public CepAttribute() { }

        /// <summary>
        /// Validação server
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return true;
            // CEPs iniciados em 0 são válidos (ex.: 01310-100, em São Paulo)
            var somenteNumeros = Methods.RemoveNaoNumericos(value.ToString()!);
            return somenteNumeros.Length == 8;
        }

        public string GetErrorMessage() =>
            $"CEP Inválido";
    }
}
