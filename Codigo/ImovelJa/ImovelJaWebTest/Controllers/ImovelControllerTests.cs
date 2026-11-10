using Core;
using Core.Service;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ImovelJaWeb.Controllers.Tests;

[TestClass]
public class ImovelControllerTests
{
    private Mock<IImovelService> imovelServiceMock = null!;
    private ImovelController controller = null!;

    [TestInitialize]
    public void Inicializar()
    {
        imovelServiceMock = new Mock<IImovelService>();

        imovelServiceMock
            .Setup(service => service.GetByValidacao("P"))
            .Returns(ObterImoveisPendentesDeTeste());

        imovelServiceMock
            .Setup(service => service.Get(1))
            .Returns(ObterImovelAlvo());

        imovelServiceMock
            .Setup(service => service.Get(999))
            .Returns((Imovel?)null);

        controller = new ImovelController(imovelServiceMock.Object);
    }

    [TestMethod]
    public void Pendentes_QuandoExistemAnunciosPendentes_DeveRetornarViewComOsPendentes()
    {
        // Act
        var resultado = controller.Pendentes();

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(
            viewResult.ViewData.Model);

        Assert.HasCount(2, imoveis);

        var primeiroImovel = imoveis.First();

        Assert.AreEqual(1u, primeiroImovel.Id);
        Assert.AreEqual("Casa com 3 quartos no Centro", primeiroImovel.Nome);
        Assert.AreEqual("P", primeiroImovel.StatusValidacao);

        imovelServiceMock.Verify(
            service => service.GetByValidacao("P"),
            Times.Once);
    }

    [TestMethod]
    public void Pendentes_QuandoNaoExistemAnunciosPendentes_DeveRetornarViewComColecaoVazia()
    {
        // Arrange
        imovelServiceMock
            .Setup(service => service.GetByValidacao("P"))
            .Returns(Array.Empty<Imovel>());

        // Act
        var resultado = controller.Pendentes();

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(
            viewResult.ViewData.Model);

        Assert.IsEmpty(imoveis);

        imovelServiceMock.Verify(
            service => service.GetByValidacao("P"),
            Times.Once);
    }

    [TestMethod]
    public void Validar_QuandoImovelExiste_DeveRetornarViewComImovel()
    {
        // Act
        var resultado = controller.Validar(1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(1u, imovel.Id);
        Assert.AreEqual("Casa com 3 quartos no Centro", imovel.Nome);
        Assert.AreEqual("C", imovel.Tipo);
        Assert.AreEqual("Rua Sete de Setembro, 245", imovel.Rua);
        Assert.AreEqual("Itabaiana", imovel.Cidade);
        Assert.AreEqual(1200m, imovel.Valor);
        Assert.AreEqual(3u, imovel.QuantidadeQuartos);
        Assert.AreEqual("P", imovel.StatusValidacao);

        imovelServiceMock.Verify(
            service => service.Get(1),
            Times.Once);
    }

    [TestMethod]
    public void Detalhes_QuandoAnuncioAprovadoEDisponivel_DeveRetornarViewComImovel()
    {
        // Arrange
        var publicado = ObterImovelAlvo();
        publicado.Id = 3;
        publicado.Status = "D";
        publicado.StatusValidacao = "A";

        imovelServiceMock
            .Setup(service => service.Get(3))
            .Returns(publicado);

        // Act
        var resultado = controller.Detalhes(3);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(3u, imovel.Id);
        Assert.AreEqual("Casa com 3 quartos no Centro", imovel.Nome);
        Assert.AreEqual("A", imovel.StatusValidacao);
        Assert.AreEqual("D", imovel.Status);

        imovelServiceMock.Verify(
            service => service.Get(3),
            Times.Once);
    }

    [TestMethod]
    public void Detalhes_QuandoAnuncioNaoEstaPublicado_DeveRetornarViewComModeloVazio()
    {
        // Act
        var resultado = controller.Detalhes(1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(0u, imovel.Id);
    }

    [TestMethod]
    public void Detalhes_QuandoImovelNaoExiste_DeveRetornarViewComModeloVazio()
    {
        // Act
        var resultado = controller.Detalhes(999);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(0u, imovel.Id);

        imovelServiceMock.Verify(
            service => service.Get(999),
            Times.Once);
    }

    [TestMethod]
    public void Validar_QuandoImovelNaoExiste_DeveRetornarViewComModeloVazio()
    {
        // Act
        var resultado = controller.Validar(999);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(0u, imovel.Id);
        Assert.IsTrue(string.IsNullOrEmpty(imovel.Nome));

        imovelServiceMock.Verify(
            service => service.Get(999),
            Times.Once);
    }

    [TestMethod]
    public void Approve_QuandoAprovado_DeveAprovarAnuncioERedirecionarParaPendentes()
    {
        // Arrange
        var modelo = new ImovelViewModel { Id = 1 };

        // Act
        var resultado = controller.Approve(modelo, true);

        // Assert
        VerificarRedirecionamentoParaPendentes(resultado);

        imovelServiceMock.Verify(
            service => service.Approve(1, true),
            Times.Once);
    }

    [TestMethod]
    public void Approve_QuandoReprovado_DeveReprovarAnuncioERedirecionarParaPendentes()
    {
        // Arrange
        var modelo = new ImovelViewModel { Id = 2 };

        // Act
        var resultado = controller.Approve(modelo, false);

        // Assert
        VerificarRedirecionamentoParaPendentes(resultado);

        imovelServiceMock.Verify(
            service => service.Approve(2, false),
            Times.Once);
    }

    [TestMethod]
    public void Approve_QuandoAnuncioNaoEstaPendente_DeveRetornarViewValidarComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "Somente anúncios pendentes podem ser validados.";

        imovelServiceMock
            .Setup(service => service.Approve(1, true))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.Approve(new ImovelViewModel { Id = 1 }, true);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(nameof(ImovelController.Validar), viewResult.ViewName);
        Assert.AreEqual(1u, imovel.Id);
        Assert.AreEqual("Casa com 3 quartos no Centro", imovel.Nome);

        var erros = controller.ModelState.Values
            .SelectMany(entrada => entrada.Errors)
            .ToList();

        Assert.HasCount(1, erros);
        Assert.AreEqual(mensagem, erros[0].ErrorMessage);
    }

    [TestMethod]
    public void Approve_QuandoImovelNaoExiste_DeveRetornarViewValidarComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "Imóvel não encontrado.";

        imovelServiceMock
            .Setup(service => service.Approve(999, true))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.Approve(new ImovelViewModel { Id = 999 }, true);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(nameof(ImovelController.Validar), viewResult.ViewName);
        Assert.AreEqual(999u, imovel.Id);

        var erros = controller.ModelState.Values
            .SelectMany(entrada => entrada.Errors)
            .ToList();

        Assert.HasCount(1, erros);
        Assert.AreEqual(mensagem, erros[0].ErrorMessage);
    }

    private static void VerificarRedirecionamentoParaPendentes(ActionResult resultado)
    {
        var redirectResult =
            Assert.IsInstanceOfType<RedirectToActionResult>(resultado);

        Assert.IsNull(redirectResult.ControllerName);
        Assert.AreEqual(
            nameof(ImovelController.Pendentes),
            redirectResult.ActionName);
    }

    private static Imovel ObterImovelAlvo()
    {
        return new Imovel
        {
            Id = 1,
            Nome = "Casa com 3 quartos no Centro",
            Descricao = "Casa térrea com quintal amplo, próxima à Praça Fausto Cardoso.",
            Tipo = "C",
            AreaTotal = 180m,
            QuantidadeBanheiros = 2,
            QuantidadeQuartos = 3,
            QuantidadeVagasGaragem = 2,
            Rua = "Rua Sete de Setembro, 245",
            Bairro = "Centro",
            Cep = "49500000",
            Cidade = "Itabaiana",
            Estado = "Sergipe",
            Valor = 1200m,
            Finalidade = "A",
            Status = "D",
            StatusValidacao = "P",
            IdPessoaProprietario = 1
        };
    }

    private static IEnumerable<Imovel> ObterImoveisPendentesDeTeste()
    {
        return
        [
            ObterImovelAlvo(),
            new Imovel
            {
                Id = 2,
                Nome = "Apartamento mobiliado perto da UFS",
                Descricao = "Apartamento de 2 quartos, a cinco minutos do campus.",
                Tipo = "A",
                AreaTotal = 65m,
                QuantidadeBanheiros = 1,
                QuantidadeQuartos = 2,
                QuantidadeVagasGaragem = 1,
                Rua = "Avenida Vereador Olímpio Grande, 1010",
                Bairro = "Porto",
                Cep = "49506036",
                Cidade = "Itabaiana",
                Estado = "Sergipe",
                Valor = 900m,
                Finalidade = "A",
                Status = "D",
                StatusValidacao = "P",
                IdPessoaProprietario = 1
            }
        ];
    }
}
