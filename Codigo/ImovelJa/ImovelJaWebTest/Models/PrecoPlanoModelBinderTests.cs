using System.Globalization;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ImovelJaWeb.Models.Tests;

[TestClass]
public class PrecoPlanoModelBinderTests
{
    [TestMethod]
    public async Task BindModelAsync_QuandoPrecoUsaPontoEmCulturaPtBr_DeveManterOsCentavos()
    {
        // Arrange
        var culturaAnterior = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
        try
        {
            var contexto = CriarContexto("59.90");

            // Act
            await new PrecoPlanoModelBinder().BindModelAsync(contexto);

            // Assert
            Assert.AreEqual(59.90m, contexto.Result.Model);
            Assert.IsEmpty(contexto.ModelState["Valor"]!.Errors);
        }
        finally
        {
            CultureInfo.CurrentCulture = culturaAnterior;
        }
    }

    [TestMethod]
    public async Task BindModelAsync_QuandoPrecoNaoENumero_DeveInvalidarOCampo()
    {
        // Arrange
        var contexto = CriarContexto("abc");

        // Act
        await new PrecoPlanoModelBinder().BindModelAsync(contexto);

        // Assert
        Assert.IsFalse(contexto.Result.IsModelSet);
        Assert.IsFalse(contexto.ModelState.IsValid);
    }

    private static DefaultModelBindingContext CriarContexto(string valor)
    {
        var provedor = new EmptyModelMetadataProvider();

        return new DefaultModelBindingContext
        {
            ModelName = "Valor",
            ModelMetadata = provedor.GetMetadataForType(typeof(decimal)),
            ModelState = new ModelStateDictionary(),
            ValueProvider = new ValorInformado(valor)
        };
    }

    private sealed class ValorInformado : IValueProvider
    {
        private readonly string valor;

        public ValorInformado(string valor)
        {
            this.valor = valor;
        }

        public bool ContainsPrefix(string prefix)
        {
            return prefix == "Valor";
        }

        public ValueProviderResult GetValue(string key)
        {
            if (key != "Valor")
            {
                return ValueProviderResult.None;
            }

            return new ValueProviderResult(valor);
        }
    }
}
