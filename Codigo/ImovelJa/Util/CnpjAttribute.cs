using System.ComponentModel.DataAnnotations;


namespace Util
{
    /// <summary>
    /// Validação customizada para CNPJ
    /// </summary>
    public class CnpjAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return true;
            var somenteNumeros = Methods.RemoveNaoNumericos(value.ToString()!);
            return Methods.ValidarCnpj(somenteNumeros);
        }

        public string GetErrorMessage() =>
            $"CNPJ Inválido";
    }
}
