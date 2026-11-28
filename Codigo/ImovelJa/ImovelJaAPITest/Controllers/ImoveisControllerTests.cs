using Core;
using Core.DTO;
using Core.Service;
using ImovelJaAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ImovelJaAPI.Controllers.Tests;

[TestClass]
public class ImoveisControllerTests
{
    private Mock<IImovelService> imovelServiceMock = null!;
    private ImoveisController controller = null!;

    [TestInitialize]
    public void Inicializar()
    {
        imovelServiceMock = new Mock<IImovelService>();

        imovelServiceMock
            .Setup(service => service.GetByFiltro(It.IsAny<ImovelFiltroDTO>()))
            .Returns(new List<Imovel> { ObterImovelPublico(3), ObterImovelPublico(4) });

        imovelServiceMock
            .Setup(service => service.GetPublico(3))
            .Returns(ObterImovelPublico(3));

        imovelServiceMock
            .Setup(service => service.GetPublico(999))
            .Returns((Imovel?)null);

        controller = new ImoveisController(imovelServiceMock.Object);
    }

    [TestMethod]
    public void Get_QuandoFiltroInformado_DeveRetornarOkComOsImoveisEncontrados()
    {
        // Arrange
        var filtro = new ImovelFiltroViewModel
        {
            Termo = "Casa",
            Localizacao = "Itabaiana",
            Tipos = new List<string> { "C" },
            ValorMaximo = 2000m
        };

        // Act
        var resultado = controller.Get(filtro);

        // Assert
        var okResult = Assert.IsInstanceOfType<OkObjectResult>(resultado.Result);
        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(okResult.Value);

        Assert.HasCount(2, imoveis);
        Assert.AreEqual(3u, imoveis.First().Id);
        Assert.AreEqual("Casa com quintal no Centro", imoveis.First().Nome);

        imovelServiceMock.Verify(service => service.GetByFiltro(It.Is<ImovelFiltroDTO>(dto =>
            dto.Termo == "Casa" &&
            dto.Localizacao == "Itabaiana" &&
            dto.Tipos!.Contains("C") &&
            dto.ValorMaximo == 2000m)), Times.Once);
    }

    [TestMethod]
    public void Get_QuandoNenhumImovelAtendeAoFiltro_DeveRetornarOkComListaVazia()
    {
        // Arrange
        imovelServiceMock
            .Setup(service => service.GetByFiltro(It.IsAny<ImovelFiltroDTO>()))
            .Returns(new List<Imovel>());

        // Act
        var resultado = controller.Get(new ImovelFiltroViewModel { Termo = "Mansão" });

        // Assert
        var okResult = Assert.IsInstanceOfType<OkObjectResult>(resultado.Result);
        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(okResult.Value);

        Assert.IsEmpty(imoveis);
    }

    [TestMethod]
    public void Get_QuandoValorMinimoMaiorQueValorMaximo_DeveRetornarBadRequest()
    {
        // Arrange
        var filtro = new ImovelFiltroViewModel { ValorMinimo = 3000m, ValorMaximo = 1000m };

        // Act
        var resultado = controller.Get(filtro);

        // Assert
        Assert.IsInstanceOfType<BadRequestObjectResult>(resultado.Result);
        Assert.IsTrue(controller.ModelState.ContainsKey(nameof(ImovelFiltroViewModel.ValorMaximo)));
        imovelServiceMock.Verify(service => service.GetByFiltro(It.IsAny<ImovelFiltroDTO>()), Times.Never);
    }

    [TestMethod]
    public void Get_QuandoAreaMinimaMaiorQueAreaMaxima_DeveRetornarBadRequest()
    {
        // Arrange
        var filtro = new ImovelFiltroViewModel { AreaMinima = 200m, AreaMaxima = 80m };

        // Act
        var resultado = controller.Get(filtro);

        // Assert
        Assert.IsInstanceOfType<BadRequestObjectResult>(resultado.Result);
        Assert.IsTrue(controller.ModelState.ContainsKey(nameof(ImovelFiltroViewModel.AreaMaxima)));
        imovelServiceMock.Verify(service => service.GetByFiltro(It.IsAny<ImovelFiltroDTO>()), Times.Never);
    }

    [TestMethod]
    public void GetPorId_QuandoAnuncioEhPublico_DeveRetornarOkComOImovel()
    {
        // Act
        var resultado = controller.Get(3u);

        // Assert
        var okResult = Assert.IsInstanceOfType<OkObjectResult>(resultado.Result);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(okResult.Value);

        Assert.AreEqual(3u, imovel.Id);
        Assert.AreEqual("Casa com quintal no Centro", imovel.Nome);
        Assert.AreEqual("Itabaiana", imovel.Cidade);
        Assert.AreEqual(1500m, imovel.Valor);
    }

    [TestMethod]
    public void GetPorId_QuandoAnuncioNaoExisteOuNaoEhPublico_DeveRetornarNotFound()
    {
        // Act
        var resultado = controller.Get(999u);

        // Assert
        Assert.IsInstanceOfType<NotFoundObjectResult>(resultado.Result);
    }

    private static Imovel ObterImovelPublico(uint id)
    {
        return new Imovel
        {
            Id = id,
            Nome = "Casa com quintal no Centro",
            Descricao = "Casa térrea com quintal amplo.",
            Tipo = "C",
            AreaTotal = 150m,
            QuantidadeBanheiros = 2,
            QuantidadeQuartos = 3,
            QuantidadeVagasGaragem = 1,
            Rua = "Rua Sete de Setembro, 245",
            Bairro = "Centro",
            Cep = "49500000",
            Cidade = "Itabaiana",
            Estado = "Sergipe",
            Valor = 1500m,
            Finalidade = "A",
            Status = "D",
            StatusValidacao = "A",
            IdPessoaProprietario = 1
        };
    }
}
