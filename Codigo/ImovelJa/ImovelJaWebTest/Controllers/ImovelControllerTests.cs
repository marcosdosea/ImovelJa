using Core;
using Core.Service;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Core.DTO;

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

        imovelServiceMock
            .Setup(service => service.Get(3))
            .Returns(ObterImovelPublico(3));

        imovelServiceMock
            .Setup(service => service.Get(4))
            .Returns(ObterImovelPublico(4, statusValidacao: "R"));

        imovelServiceMock
            .Setup(service => service.Get(5))
            .Returns(ObterImovelPublico(5, status: "A"));

        controller = new ImovelController(imovelServiceMock.Object);
    }

    [TestMethod]
    public void Analise_QuandoExistemAnunciosPendentes_DeveRetornarViewComOsPendentes()
    {
        // Act
        var resultado = controller.Analise();

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(
            viewResult.ViewData.Model);

        Assert.HasCount(2, imoveis);

        var primeiroImovel = imoveis.First();

        Assert.AreEqual(1u, primeiroImovel.Id);
        Assert.AreEqual("Casa com 3 quartos no Centro", primeiroImovel.Nome);
        Assert.AreEqual("P", primeiroImovel.StatusValidacao);
        Assert.AreEqual(new DateTime(2026, 7, 21, 14, 30, 0), primeiroImovel.DataCadastro);

        imovelServiceMock.Verify(
            service => service.GetByValidacao("P"),
            Times.Once);
    }

    [TestMethod]
    public void Analise_QuandoNaoExistemAnunciosPendentes_DeveRetornarViewComColecaoVazia()
    {
        // Arrange
        imovelServiceMock
            .Setup(service => service.GetByValidacao("P"))
            .Returns(Array.Empty<Imovel>());

        // Act
        var resultado = controller.Analise();

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
    public void Approve_QuandoAprovado_DeveAprovarAnuncioERedirecionarParaAnalise()
    {
        // Arrange
        var modelo = new ImovelViewModel { Id = 1 };

        // Act
        var resultado = controller.Approve(modelo, true);

        // Assert
        VerificarRedirecionamentoParaAnalise(resultado);

        imovelServiceMock.Verify(
            service => service.Approve(1, true),
            Times.Once);
    }

    [TestMethod]
    public void Approve_QuandoReprovado_DeveReprovarAnuncioERedirecionarParaAnalise()
    {
        // Arrange
        var modelo = new ImovelViewModel { Id = 2 };

        // Act
        var resultado = controller.Approve(modelo, false);

        // Assert
        VerificarRedirecionamentoParaAnalise(resultado);

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

    [TestMethod]
    public void Buscar_QuandoExistemResultados_DeveRetornarViewComOsImoveisEncontrados()
    {
        // Arrange
        imovelServiceMock
            .Setup(service => service.GetByFiltro(
                It.Is<ImovelFiltroDTO>(f => f.Termo == "casa" && f.ValorMaximo == 2000m)))
            .Returns([ObterImovelPublico(3), ObterImovelPublico(6)]);

        var filtro = new ImovelFiltroViewModel { Termo = "casa", ValorMaximo = 2000m };

        // Act
        var resultado = controller.Buscar(filtro);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(
            viewResult.ViewData.Model);

        Assert.HasCount(2, imoveis);
        Assert.AreEqual(3u, imoveis.First().Id);
        Assert.AreEqual("Casa com quintal no Centro", imoveis.First().Nome);
        Assert.IsTrue(controller.ModelState.IsValid);

        imovelServiceMock.Verify(
            service => service.GetByFiltro(It.IsAny<ImovelFiltroDTO>()),
            Times.Once);
    }

    [TestMethod]
    public void Buscar_QuandoNaoHaResultados_DeveRetornarViewComColecaoVazia()
    {
        // Arrange
        imovelServiceMock
            .Setup(service => service.GetByFiltro(It.IsAny<ImovelFiltroDTO>()))
            .Returns(Array.Empty<Imovel>());

        // Act
        var resultado = controller.Buscar(new ImovelFiltroViewModel { Termo = "castelo" });

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(
            viewResult.ViewData.Model);

        Assert.IsEmpty(imoveis);
        Assert.IsTrue(controller.ModelState.IsValid);
    }

    [TestMethod]
    public void Buscar_QuandoValorMinimoMaiorQueMaximo_DeveRetornarViewComErroSemConsultarService()
    {
        // Arrange
        var filtro = new ImovelFiltroViewModel { ValorMinimo = 2000m, ValorMaximo = 1000m };

        // Act
        var resultado = controller.Buscar(filtro);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(
            viewResult.ViewData.Model);

        Assert.IsEmpty(imoveis);
        Assert.IsFalse(controller.ModelState.IsValid);
        Assert.IsTrue(controller.ModelState.ContainsKey(nameof(ImovelFiltroViewModel.ValorMaximo)));

        imovelServiceMock.Verify(
            service => service.GetByFiltro(It.IsAny<ImovelFiltroDTO>()),
            Times.Never);
    }

    [TestMethod]
    public void Buscar_QuandoAreaMinimaMaiorQueMaxima_DeveRetornarViewComErroSemConsultarService()
    {
        // Arrange
        var filtro = new ImovelFiltroViewModel { AreaMinima = 200m, AreaMaxima = 100m };

        // Act
        var resultado = controller.Buscar(filtro);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(
            viewResult.ViewData.Model);

        Assert.IsEmpty(imoveis);
        Assert.IsFalse(controller.ModelState.IsValid);
        Assert.IsTrue(controller.ModelState.ContainsKey(nameof(ImovelFiltroViewModel.AreaMaxima)));

        imovelServiceMock.Verify(
            service => service.GetByFiltro(It.IsAny<ImovelFiltroDTO>()),
            Times.Never);
    }

    [TestMethod]
    public void Buscar_QuandoModelStateJaPossuiErro_DeveRetornarViewVaziaSemConsultarService()
    {
        // Arrange
        controller.ModelState.AddModelError(
            nameof(ImovelFiltroViewModel.ValorMinimo),
            "O campo Valor mínimo não pode ser negativo");

        // Act
        var resultado = controller.Buscar(new ImovelFiltroViewModel { ValorMinimo = -1m });

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(
            viewResult.ViewData.Model);

        Assert.IsEmpty(imoveis);

        imovelServiceMock.Verify(
            service => service.GetByFiltro(It.IsAny<ImovelFiltroDTO>()),
            Times.Never);
    }

    [TestMethod]
    public void Detalhes_QuandoImovelAprovadoEDisponivel_DeveRetornarViewComImovel()
    {
        // Act
        var resultado = controller.Detalhes(3);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(3u, imovel.Id);
        Assert.AreEqual("Casa com quintal no Centro", imovel.Nome);
        Assert.AreEqual("A", imovel.StatusValidacao);
        Assert.AreEqual("D", imovel.Status);

        imovelServiceMock.Verify(
            service => service.Get(3),
            Times.Once);
    }

    [TestMethod]
    public void Detalhes_QuandoImovelNaoExiste_DeveRetornarNotFound()
    {
        // Act
        var resultado = controller.Detalhes(999);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);

        imovelServiceMock.Verify(
            service => service.Get(999),
            Times.Once);
    }

    [TestMethod]
    [DataRow(1u)] // pendente
    [DataRow(4u)] // reprovado
    [DataRow(5u)] // aprovado, mas alugado
    public void Detalhes_QuandoImovelNaoAprovadoOuIndisponivel_DeveRetornarNotFound(uint id)
    {
        // Act
        var resultado = controller.Detalhes(id);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
    }

    private static void VerificarRedirecionamentoParaAnalise(ActionResult resultado)
    {
        var redirectResult =
            Assert.IsInstanceOfType<RedirectToActionResult>(resultado);

        Assert.IsNull(redirectResult.ControllerName);
        Assert.AreEqual(
            nameof(ImovelController.Analise),
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
            DataCadastro = new DateTime(2026, 7, 21, 14, 30, 0),
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

    private static Imovel ObterImovelPublico(uint id, string status = "D", string statusValidacao = "A")
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
            Status = status,
            StatusValidacao = statusValidacao,
            IdPessoaProprietario = 1
        };
    }

}
