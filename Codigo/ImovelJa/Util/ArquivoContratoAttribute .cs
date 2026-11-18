using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Util
{
    /// <summary>
    /// Contrato opcional: até 5 MB e conteúdo real PDF, JPG ou PNG (confere os primeiros bytes, não a extensão)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ArquivoContratoAttribute : ValidationAttribute
    {
        public const long TamanhoMaximo = 5 * 1024 * 1024;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not IFormFile arquivo || arquivo.Length == 0)
                return ValidationResult.Success;

            if (arquivo.Length > TamanhoMaximo)
                return new ValidationResult("O contrato deve ter no máximo 5 MB.");

            var inicio = new byte[4];
            int lidos;
            using (var stream = arquivo.OpenReadStream())
                lidos = stream.Read(inicio, 0, 4);

            bool pdf = lidos >= 4 && inicio[0] == 0x25 && inicio[1] == 0x50 && inicio[2] == 0x44 && inicio[3] == 0x46;
            bool jpg = lidos >= 3 && inicio[0] == 0xFF && inicio[1] == 0xD8 && inicio[2] == 0xFF;
            bool png = lidos >= 4 && inicio[0] == 0x89 && inicio[1] == 0x50 && inicio[2] == 0x4E && inicio[3] == 0x47;

            return pdf || jpg || png
                ? ValidationResult.Success
                : new ValidationResult("O contrato deve ser um arquivo PDF, JPG ou PNG.");
        }
    }
}