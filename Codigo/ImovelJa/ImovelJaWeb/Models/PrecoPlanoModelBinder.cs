using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ImovelJaWeb.Models
{
    public class PrecoPlanoModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);

            var valorInformado = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valorInformado == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valorInformado);

            var texto = valorInformado.FirstValue;
            var nome = bindingContext.ModelMetadata.GetDisplayName();

            if (string.IsNullOrWhiteSpace(texto))
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    $"O campo {nome} é obrigatório.");
                return Task.CompletedTask;
            }

            if (!decimal.TryParse(
                    texto,
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var valor))
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    $"O campo {nome} deve ser um número.");
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(valor);
            return Task.CompletedTask;
        }
    }
}
