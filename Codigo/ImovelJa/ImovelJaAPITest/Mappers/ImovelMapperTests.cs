using Core;
using ImovelJaAPI.Models;

namespace ImovelJaAPI.Mappers.Tests;

[TestClass]
public class ImovelMapperTests
{
    [TestMethod]
    public void ToDTO_QuandoFiltroPreenchido_DeveCopiarTodosOsCampos()
    {
        // Arrange
        var filtro = new ImovelFiltroViewModel
        {
            Termo = "Casa",
            Localizacao = "Itabaiana",
            Tipos = new List<string> { "C", "A" },
            ValorMinimo = 500m,
            ValorMaximo = 1500m,
            QuantidadeQuartos = 2,
            AreaMinima = 50m,
            AreaMaxima = 120m
        };

        // Act
        var dto = ImovelMapper.ToDTO(filtro);

        // Assert
        Assert.AreEqual("Casa", dto.Termo);
        Assert.AreEqual("Itabaiana", dto.Localizacao);
        CollectionAssert.AreEqual(new List<string> { "C", "A" }, dto.Tipos);
        Assert.AreEqual(500m, dto.ValorMinimo);
        Assert.AreEqual(1500m, dto.ValorMaximo);
        Assert.AreEqual(2u, dto.QuantidadeQuartos);
        Assert.AreEqual(50m, dto.AreaMinima);
        Assert.AreEqual(120m, dto.AreaMaxima);
    }

    [TestMethod]
    public void ToViewModel_QuandoImovelInformado_DeveManterOsDadosDoAnuncio()
    {
        // Arrange
        var imovel = new Imovel
        {
            Id = 1,
            Nome = "Casa no Centro",
            Tipo = "C",
            Rua = "Rua A",
            Bairro = "Centro",
            Cep = "49500000",
            Estado = "SE",
            Cidade = "Itabaiana",
            Valor = 900m,
            Finalidade = "A",
            IdPessoaProprietario = 2
        };

        // Act
        var viewModel = ImovelMapper.ToViewModel(imovel);

        // Assert
        Assert.AreEqual(1u, viewModel.Id);
        Assert.AreEqual("Casa no Centro", viewModel.Nome);
        Assert.AreEqual("Itabaiana", viewModel.Cidade);
        Assert.AreEqual(900m, viewModel.Valor);
        Assert.AreEqual(2u, viewModel.IdPessoaProprietario);
    }
}
