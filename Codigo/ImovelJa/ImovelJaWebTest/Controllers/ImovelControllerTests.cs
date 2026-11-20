using Core;
using Core.DTO;
using Core.Service;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ImovelJaWeb.Controllers.Tests;

[TestClass]
public class ImovelControllerTests
{
    private Mock<IImovelService> imovelServiceMock = null!;
    private Mock<IAluguelService> aluguelServiceMock = null!;
    private ImovelController controller = null!;

    [TestInitialize]
    public void Inicializar()
    {
        imovelServiceMock = new Mock<IImovelService>();
        aluguelServiceMock = new Mock<IAluguelService>();

        aluguelServiceMock
            .Setup(service => service.GetByInquilino(It.IsAny<uint>()))
            .Returns(Array.Empty<Aluguel>());

        imovelServiceMock
            .Setup(service => service.GetByValidacao("P"))
            .Returns(ObterImoveisPendentesDeTeste());

        imovelServiceMock
            .Setup(service => service.Get(1))
            .Returns(ObterImovelAlvo());

        imovelServiceMock
            .Setup(service => service.GetDoProprietario(1, 1))
            .Returns(ObterImovelAlvo());

        imovelServiceMock
            .Setup(service => service.Get(999))
            .Returns((Imovel?)null);

        imovelServiceMock
            .Setup(service => service.GetPublico(3))
            .Returns(ObterImovelPublico(3));

        imovelServiceMock
            .Setup(service => service.GetPublico(999))
            .Returns((Imovel?)null);

        imovelServiceMock
            .Setup(service => service.GetByProprietario(It.IsAny<uint>()))
            .Returns(Array.Empty<Imovel>());

        controller = new ImovelController(imovelServiceMock.Object, aluguelServiceMock.Object);
    }

    [TestMethod]
    public void Review_QuandoExistemAnunciosPendentes_DeveRetornarViewComOsPendentes()
    {
        // Act
        var resultado = controller.Review();

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
    public void Review_QuandoNaoExistemAnunciosPendentes_DeveRetornarViewComColecaoVazia()
    {
        // Arrange
        imovelServiceMock
            .Setup(service => service.GetByValidacao("P"))
            .Returns(Array.Empty<Imovel>());

        // Act
        var resultado = controller.Review();

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
    public void Validate_QuandoImovelExiste_DeveRetornarViewComImovel()
    {
        // Act
        var resultado = controller.Validate(1);

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
    public void Validate_QuandoImovelNaoExiste_DeveRetornarViewComModeloVazio()
    {
        // Act
        var resultado = controller.Validate(999);

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
    public void Validate_QuandoAprovado_DeveAprovarAnuncioERedirecionarParaReview()
    {
        // Arrange
        var modelo = new ImovelViewModel { Id = 1 };

        // Act
        var resultado = controller.Validate(modelo, true);

        // Assert
        VerificarRedirecionamentoParaReview(resultado);

        imovelServiceMock.Verify(
            service => service.Approve(1, true),
            Times.Once);
    }

    [TestMethod]
    public void Validate_QuandoReprovado_DeveReprovarAnuncioERedirecionarParaReview()
    {
        // Arrange
        var modelo = new ImovelViewModel { Id = 2 };

        // Act
        var resultado = controller.Validate(modelo, false);

        // Assert
        VerificarRedirecionamentoParaReview(resultado);

        imovelServiceMock.Verify(
            service => service.Approve(2, false),
            Times.Once);
    }

    [TestMethod]
    public void Validate_QuandoAnuncioNaoEstaPendente_DeveRetornarViewValidateComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "Somente anúncios pendentes podem ser validados.";

        imovelServiceMock
            .Setup(service => service.Approve(1, true))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.Validate(new ImovelViewModel { Id = 1 }, true);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(nameof(ImovelController.Validate), viewResult.ViewName);
        Assert.AreEqual(1u, imovel.Id);
        Assert.AreEqual("Casa com 3 quartos no Centro", imovel.Nome);

        var erros = controller.ModelState.Values
            .SelectMany(entrada => entrada.Errors)
            .ToList();

        Assert.HasCount(1, erros);
        Assert.AreEqual(mensagem, erros[0].ErrorMessage);
    }

    [TestMethod]
    public void Validate_QuandoImovelNaoExiste_DeveRetornarViewValidateComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "Imóvel não encontrado.";

        imovelServiceMock
            .Setup(service => service.Approve(999, true))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.Validate(new ImovelViewModel { Id = 999 }, true);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(nameof(ImovelController.Validate), viewResult.ViewName);
        Assert.AreEqual(999u, imovel.Id);

        var erros = controller.ModelState.Values
            .SelectMany(entrada => entrada.Errors)
            .ToList();

        Assert.HasCount(1, erros);
        Assert.AreEqual(mensagem, erros[0].ErrorMessage);
    }

    [TestMethod]
    public void Search_QuandoExistemResultados_DeveRetornarViewComOsImoveisEncontrados()
    {
        // Arrange
        imovelServiceMock
            .Setup(service => service.GetByFiltro(
                It.Is<ImovelFiltroDTO>(f => f.Termo == "casa" && f.ValorMaximo == 2000m)))
            .Returns([ObterImovelPublico(3), ObterImovelPublico(6)]);

        var filtro = new ImovelFiltroViewModel { Termo = "casa", ValorMaximo = 2000m };

        // Act
        var resultado = controller.Search(filtro);

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
    public void Search_QuandoNaoHaResultados_DeveRetornarViewComColecaoVazia()
    {
        // Arrange
        imovelServiceMock
            .Setup(service => service.GetByFiltro(It.IsAny<ImovelFiltroDTO>()))
            .Returns(Array.Empty<Imovel>());

        // Act
        var resultado = controller.Search(new ImovelFiltroViewModel { Termo = "castelo" });

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(
            viewResult.ViewData.Model);

        Assert.IsEmpty(imoveis);
        Assert.IsTrue(controller.ModelState.IsValid);
    }

    [TestMethod]
    public void Search_QuandoValorMinimoMaiorQueMaximo_DeveRetornarViewComErroSemConsultarService()
    {
        // Arrange
        var filtro = new ImovelFiltroViewModel { ValorMinimo = 2000m, ValorMaximo = 1000m };

        // Act
        var resultado = controller.Search(filtro);

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
    public void Search_QuandoAreaMinimaMaiorQueMaxima_DeveRetornarViewComErroSemConsultarService()
    {
        // Arrange
        var filtro = new ImovelFiltroViewModel { AreaMinima = 200m, AreaMaxima = 100m };

        // Act
        var resultado = controller.Search(filtro);

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
    public void Search_QuandoModelStateJaPossuiErro_DeveRetornarViewVaziaSemConsultarService()
    {
        // Arrange
        controller.ModelState.AddModelError(
            nameof(ImovelFiltroViewModel.ValorMinimo),
            "O campo Valor mínimo não pode ser negativo");

        // Act
        var resultado = controller.Search(new ImovelFiltroViewModel { ValorMinimo = -1m });

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
    public void Details_QuandoImovelAprovadoEDisponivel_DeveRetornarViewComImovel()
    {
        // Act
        var resultado = controller.Details(3);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(3u, imovel.Id);
        Assert.AreEqual("Casa com quintal no Centro", imovel.Nome);
        Assert.AreEqual("A", imovel.StatusValidacao);
        Assert.AreEqual("D", imovel.Status);

        imovelServiceMock.Verify(
            service => service.GetPublico(3),
            Times.Once);
    }

    [TestMethod]
    public void Details_QuandoImovelNaoExiste_DeveRetornarNotFound()
    {
        // Act
        var resultado = controller.Details(999);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);

        imovelServiceMock.Verify(
            service => service.GetPublico(999),
            Times.Once);
    }

    [TestMethod]
    [DataRow(1u)] // pendente
    [DataRow(4u)] // reprovado
    [DataRow(5u)] // aprovado, mas alugado
    public void Details_QuandoServiceNaoRetornaAnuncioPublico_DeveRetornarNotFound(uint id)
    {
        // Act: o service retorna nulo (sem configuração no mock) para anúncios que não são públicos
        var resultado = controller.Details(id);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);

        imovelServiceMock.Verify(
            service => service.GetPublico(id),
            Times.Once);
    }

    [TestMethod]
    public void ImovelViewModel_QuandoCepValido_DeveSerValido()
    {
        // Arrange
        var modelo = new ImovelViewModel { Cep = "49500000" };

        // Act
        var resultados = Validar(modelo);

        // Assert
        Assert.IsFalse(resultados.Any(resultado =>
            resultado.MemberNames.Contains(nameof(ImovelViewModel.Cep))));
    }

    [TestMethod]
    public void ImovelViewModel_QuandoCepValidoIniciadoEmZero_DeveSerValido()
    {
        // Arrange
        var modelo = new ImovelViewModel { Cep = "01310100" };

        // Act
        var resultados = Validar(modelo);

        // Assert
        Assert.IsFalse(resultados.Any(resultado =>
            resultado.MemberNames.Contains(nameof(ImovelViewModel.Cep))));
    }

    [TestMethod]
    public void ImovelViewModel_QuandoCepComQuantidadeDeDigitosErrada_DeveSerInvalido()
    {
        // Arrange
        var modelo = new ImovelViewModel { Cep = "4950000" };

        // Act
        var resultados = Validar(modelo);

        // Assert
        Assert.IsTrue(resultados.Any(resultado =>
            resultado.MemberNames.Contains(nameof(ImovelViewModel.Cep))));
    }

    private static List<ValidationResult> Validar(ImovelViewModel modelo)
    {
        var contexto = new ValidationContext(modelo);
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, contexto, resultados, validateAllProperties: true);
        return resultados;
    }

    [TestMethod]
    public void Index_QuandoProprietarioTemAnuncios_DeveRetornarViewComOsAnuncios()
    {
        // Arrange
        imovelServiceMock
            .Setup(service => service.GetByProprietario(1))
            .Returns(ObterImoveisPendentesDeTeste());

        // Act
        var resultado = controller.Index(1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(viewResult.ViewData.Model);

        Assert.HasCount(2, imoveis);
        Assert.AreEqual(1u, imoveis.First().Id);
        Assert.AreEqual(1u, controller.ViewData["IdPessoa"]);

        imovelServiceMock.Verify(service => service.GetByProprietario(1), Times.Once);
    }

    [TestMethod]
    public void Index_QuandoProprietarioNaoTemAnuncios_DeveRetornarViewComColecaoVazia()
    {
        // Act
        var resultado = controller.Index(2);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(viewResult.ViewData.Model);

        Assert.IsEmpty(imoveis);
        Assert.AreEqual(2u, controller.ViewData["IdPessoa"]);
        Assert.IsEmpty(Alugueis(controller));
    }

    [TestMethod]
    public void Index_QuandoPessoaSoEProprietaria_DeveListarOsAnunciosSemAluguel()
    {
        // Arrange
        imovelServiceMock
            .Setup(service => service.GetByProprietario(1))
            .Returns(new List<Imovel> { ObterImovelAlvo() });

        // Act
        var resultado = controller.Index(1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(viewResult.ViewData.Model);

        Assert.HasCount(1, imoveis);
        Assert.IsEmpty(Alugueis(controller));
        aluguelServiceMock.Verify(service => service.GetByInquilino(1), Times.Once);
    }

    [TestMethod]
    public void Index_QuandoPessoaSoEInquilina_DeveListarSomenteOAluguelAtivo()
    {
        // Arrange
        aluguelServiceMock
            .Setup(service => service.GetByInquilino(2))
            .Returns(new List<Aluguel>
            {
                ObterAluguel(4, "A", ObterImovelAlvo()),
                ObterAluguel(5, "C", ObterImovelAlvo())
            });

        // Act
        var resultado = controller.Index(2);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(viewResult.ViewData.Model);
        var alugueis = Alugueis(controller);

        Assert.IsEmpty(imoveis);
        Assert.HasCount(1, alugueis);
        Assert.AreEqual(4u, alugueis[0].Id);
        Assert.AreEqual("A", alugueis[0].Status);
        Assert.AreEqual("Casa com 3 quartos no Centro", alugueis[0].Imovel?.Nome);
    }

    [TestMethod]
    public void Index_QuandoPessoaEProprietariaEInquilina_DeveListarAnunciosEAluguel()
    {
        // Arrange
        imovelServiceMock
            .Setup(service => service.GetByProprietario(1))
            .Returns(new List<Imovel> { ObterImovelAlvo() });
        aluguelServiceMock
            .Setup(service => service.GetByInquilino(1))
            .Returns(new List<Aluguel> { ObterAluguel(8, "A", ObterImovelAlvo()) });

        // Act
        var resultado = controller.Index(1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(viewResult.ViewData.Model);
        var alugueis = Alugueis(controller);

        Assert.HasCount(1, imoveis);
        Assert.HasCount(1, alugueis);
        Assert.AreEqual(1u, imoveis.First().Id);
        Assert.AreEqual(8u, alugueis[0].Id);
    }

    [TestMethod]
    public void Create_DeveRetornarViewComOProprietarioInformado()
    {
        // Act
        var resultado = controller.Create(2);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(0u, imovel.Id);
        Assert.AreEqual(2u, imovel.IdPessoaProprietario);
        Assert.AreEqual(2u, controller.ViewData["IdPessoa"]);
    }

    [TestMethod]
    public void Create_QuandoDadosValidos_DeveCadastrarERedirecionarParaIndex()
    {
        // Arrange
        var modelo = ObterModelo(0, 2);
        modelo.Nome = "Kitnet mobiliada no Centro";
        modelo.Valor = 550m;

        // Act
        var resultado = controller.Create(modelo);

        // Assert
        VerificarRedirecionamentoParaIndex(resultado, 2);

        imovelServiceMock.Verify(
            service => service.Create(It.Is<Imovel>(imovel =>
                imovel.Nome == "Kitnet mobiliada no Centro"
                && imovel.Valor == 550m
                && imovel.IdPessoaProprietario == 2)),
            Times.Once);
    }

    [TestMethod]
    public void Create_QuandoDadosInvalidos_DeveRetornarViewSemChamarServico()
    {
        // Arrange
        controller.ModelState.AddModelError(nameof(ImovelViewModel.Nome), "O campo Nome é obrigatório");
        var modelo = ObterModelo(0, 1);
        modelo.Nome = "";

        // Act
        var resultado = controller.Create(modelo);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(viewResult.ViewData.Model);

        Assert.IsNull(viewResult.ViewName);
        Assert.AreEqual(1u, imovel.IdPessoaProprietario);
        Assert.IsFalse(controller.ModelState.IsValid);

        imovelServiceMock.Verify(service => service.Create(It.IsAny<Imovel>()), Times.Never);
    }

    [TestMethod]
    public void Create_QuandoLimiteDoPlanoFoiAtingido_DeveRetornarViewComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "O plano Básico permite no máximo 1 imóvel(is) anunciado(s).";

        imovelServiceMock
            .Setup(service => service.Create(It.IsAny<Imovel>()))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.Create(ObterModelo(0, 1));

        // Assert
        VerificarViewComErro<ImovelViewModel>(resultado, mensagem);
    }

    [TestMethod]
    public void Create_QuandoProprietarioNaoExiste_DeveRetornarViewComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "Proprietário não encontrado.";

        imovelServiceMock
            .Setup(service => service.Create(It.IsAny<Imovel>()))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.Create(ObterModelo(0, 999));

        // Assert
        var viewResult = VerificarViewComErro<ImovelViewModel>(resultado, mensagem);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(viewResult.ViewData.Model);
        Assert.AreEqual(999u, imovel.IdPessoaProprietario);
    }

    [TestMethod]
    public void Edit_QuandoImovelDoProprietarioExiste_DeveRetornarViewComImovel()
    {
        // Act
        var resultado = controller.Edit(1, 1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(1u, imovel.Id);
        Assert.AreEqual("Casa com 3 quartos no Centro", imovel.Nome);
        Assert.AreEqual(1u, imovel.IdPessoaProprietario);

        imovelServiceMock.Verify(service => service.GetDoProprietario(1, 1), Times.Once);
    }

    [TestMethod]
    public void Edit_QuandoImovelNaoExiste_DeveRetornarViewComModeloVazio()
    {
        // Act
        var resultado = controller.Edit(999, 1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(0u, imovel.Id);
        Assert.AreEqual(1u, imovel.IdPessoaProprietario);
    }

    [TestMethod]
    public void Edit_QuandoImovelNaoEhDoProprietario_DeveRetornarViewComModeloVazio()
    {
        // Act
        var resultado = controller.Edit(1, 2);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(0u, imovel.Id);
        Assert.AreEqual(2u, imovel.IdPessoaProprietario);
    }

    [TestMethod]
    public void Edit_QuandoDadosValidos_DeveEditarERedirecionarParaIndex()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.Nome = "Casa reformada no Centro";
        modelo.Valor = 1500m;

        // Act
        var resultado = controller.Edit(modelo);

        // Assert
        VerificarRedirecionamentoParaIndex(resultado, 1);

        imovelServiceMock.Verify(
            service => service.Edit(It.Is<Imovel>(imovel =>
                imovel.Id == 1
                && imovel.Nome == "Casa reformada no Centro"
                && imovel.Valor == 1500m
                && imovel.IdPessoaProprietario == 1)),
            Times.Once);
    }

    [TestMethod]
    public void Edit_QuandoDadosInvalidos_DeveRetornarViewSemChamarServico()
    {
        // Arrange
        controller.ModelState.AddModelError(nameof(ImovelViewModel.Valor), "O campo Valor deve ser maior que zero");
        var modelo = ObterModelo();
        modelo.Valor = 0m;

        // Act
        var resultado = controller.Edit(modelo);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(1u, imovel.Id);
        Assert.IsFalse(controller.ModelState.IsValid);

        imovelServiceMock.Verify(service => service.Edit(It.IsAny<Imovel>()), Times.Never);
    }

    [TestMethod]
    public void Edit_QuandoServicoLancaExcecao_DeveRetornarViewComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "Imóvel não encontrado.";

        imovelServiceMock
            .Setup(service => service.Edit(It.IsAny<Imovel>()))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.Edit(ObterModelo());

        // Assert
        VerificarViewComErro<ImovelViewModel>(resultado, mensagem);
    }

    [TestMethod]
    public void Edit_QuandoAnuncioNaoPertenceAoProprietario_DeveRetornarViewComMensagemDeErro()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.IdPessoaProprietario = 2;

        // Act
        var resultado = controller.Edit(modelo);

        // Assert
        VerificarViewComErro<ImovelViewModel>(resultado, "Imóvel não encontrado.");

        imovelServiceMock.Verify(service => service.Edit(It.IsAny<Imovel>()), Times.Never);
    }

    [TestMethod]
    public void Delete_QuandoImovelDoProprietarioExiste_DeveRetornarViewDeConfirmacao()
    {
        // Act
        var resultado = controller.Delete(1, 1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(1u, imovel.Id);
        Assert.AreEqual("Casa com 3 quartos no Centro", imovel.Nome);
    }

    [TestMethod]
    public void Delete_QuandoImovelNaoExiste_DeveRetornarViewComModeloVazio()
    {
        // Act
        var resultado = controller.Delete(999, 1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(0u, imovel.Id);
    }

    [TestMethod]
    public void Delete_QuandoConfirmado_DeveExcluirERedirecionarParaIndex()
    {
        // Act
        var resultado = controller.DeleteConfirmed(1, 1);

        // Assert
        VerificarRedirecionamentoParaIndex(resultado, 1);

        imovelServiceMock.Verify(service => service.Delete(1), Times.Once);
    }

    [TestMethod]
    public void Delete_QuandoHaHistoricoDeAluguel_DeveRetornarViewComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "Não é possível excluir um imóvel com histórico de aluguel.";

        imovelServiceMock
            .Setup(service => service.Delete(1))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.DeleteConfirmed(1, 1);

        // Assert
        var viewResult = VerificarViewComErro<ImovelViewModel>(resultado, mensagem);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(viewResult.ViewData.Model);
        Assert.AreEqual(1u, imovel.Id);
        Assert.AreEqual("Casa com 3 quartos no Centro", imovel.Nome);
    }

    [TestMethod]
    public void Delete_QuandoImovelNaoEhDoProprietario_DeveRetornarViewComMensagemDeErro()
    {
        // Act
        var resultado = controller.DeleteConfirmed(1, 2);

        // Assert
        var viewResult = VerificarViewComErro<ImovelViewModel>(resultado, "Imóvel não encontrado.");
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(viewResult.ViewData.Model);
        Assert.AreEqual(0u, imovel.Id);

        imovelServiceMock.Verify(service => service.Delete(It.IsAny<uint>()), Times.Never);
    }

    [TestMethod]
    public void Delete_QuandoDadosInvalidos_DeveRetornarViewSemChamarServico()
    {
        // Arrange
        controller.ModelState.AddModelError(string.Empty, "Dados inválidos");

        // Act
        var resultado = controller.DeleteConfirmed(1, 1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(1u, imovel.Id);
        Assert.IsFalse(controller.ModelState.IsValid);

        imovelServiceMock.Verify(service => service.Delete(It.IsAny<uint>()), Times.Never);
    }

    [TestMethod]
    public void Pause_QuandoPausa_DevePausarERedirecionarParaIndex()
    {
        // Act
        var resultado = controller.Pause(1, true, 1);

        // Assert
        VerificarRedirecionamentoParaIndex(resultado, 1);

        imovelServiceMock.Verify(service => service.Pause(1, true), Times.Once);
    }

    [TestMethod]
    public void Pause_QuandoRetoma_DeveRetomarERedirecionarParaIndex()
    {
        // Act
        var resultado = controller.Pause(1, false, 1);

        // Assert
        VerificarRedirecionamentoParaIndex(resultado, 1);

        imovelServiceMock.Verify(service => service.Pause(1, false), Times.Once);
    }

    [TestMethod]
    public void Pause_QuandoSituacaoNaoPermite_DeveRetornarIndexComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "Não é possível pausar um imóvel nesta situação.";

        imovelServiceMock
            .Setup(service => service.Pause(1, true))
            .Throws(new ServiceException(mensagem));

        imovelServiceMock
            .Setup(service => service.GetByProprietario(1))
            .Returns(ObterImoveisPendentesDeTeste());

        // Act
        var resultado = controller.Pause(1, true, 1);

        // Assert
        var viewResult = VerificarViewComErro<IEnumerable<ImovelViewModel>>(resultado, mensagem);
        Assert.AreEqual(nameof(ImovelController.Index), viewResult.ViewName);
        Assert.HasCount(2, (IEnumerable<ImovelViewModel>)viewResult.ViewData.Model!);
    }

    [TestMethod]
    public void Pause_QuandoImovelNaoExiste_DeveRetornarIndexComMensagemDeErro()
    {
        // Act
        var resultado = controller.Pause(999, true, 1);

        // Assert
        var viewResult = VerificarViewComErro<IEnumerable<ImovelViewModel>>(resultado, "Imóvel não encontrado.");
        Assert.AreEqual(nameof(ImovelController.Index), viewResult.ViewName);

        imovelServiceMock.Verify(
            service => service.Pause(It.IsAny<uint>(), It.IsAny<bool>()),
            Times.Never);
    }

    [TestMethod]
    public void Pause_QuandoDadosInvalidos_DeveRetornarIndexSemChamarServico()
    {
        // Arrange
        controller.ModelState.AddModelError(string.Empty, "Dados inválidos");

        // Act
        var resultado = controller.Pause(1, true, 1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        Assert.AreEqual(nameof(ImovelController.Index), viewResult.ViewName);

        imovelServiceMock.Verify(
            service => service.Pause(It.IsAny<uint>(), It.IsAny<bool>()),
            Times.Never);
    }

    [TestMethod]
    public void Details_QuandoIdPessoaEhOProprietario_DeveExibirAnuncioEmQualquerSituacao()
    {
        // Act: o anúncio 1 está pendente e não entra na busca pública
        var resultado = controller.Details(1, 1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(1u, imovel.Id);
        Assert.AreEqual("P", imovel.StatusValidacao);
        Assert.AreEqual(true, viewResult.ViewData["Dono"]);

        imovelServiceMock.Verify(service => service.GetPublico(It.IsAny<uint>()), Times.Never);
    }

    [TestMethod]
    public void Details_QuandoIdPessoaEhOProprietarioEImovelEstaAlugado_DeveExibirOAnuncio()
    {
        // Arrange
        imovelServiceMock
            .Setup(service => service.GetDoProprietario(5, 1))
            .Returns(ObterImovelPublico(5, "A"));

        aluguelServiceMock
            .Setup(service => service.GetByImovel(5))
            .Returns(new List<Aluguel>
            {
                ObterAluguel(6, "C", ObterImovelPublico(5, "A")),
                ObterAluguel(7, "A", ObterImovelPublico(5, "A"))
            });

        // Act
        var resultado = controller.Details(5, 1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(5u, imovel.Id);
        Assert.AreEqual("A", imovel.Status);
        Assert.AreEqual(true, viewResult.ViewData["Dono"]);
        Assert.AreEqual(7u, viewResult.ViewData["IdAluguel"]);

        imovelServiceMock.Verify(service => service.GetPublico(It.IsAny<uint>()), Times.Never);
    }

    [TestMethod]
    public void Details_QuandoIdPessoaNaoEhOProprietario_DeveManterARegraPublica()
    {
        // Act
        var resultado = controller.Details(1, 2);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);

        imovelServiceMock.Verify(service => service.GetPublico(1), Times.Once);
    }

    [TestMethod]
    public void Details_QuandoIdPessoaNaoEhOProprietarioMasAnuncioEhPublico_DeveExibirOAnuncio()
    {
        // Act
        var resultado = controller.Details(3, 2);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imovel = Assert.IsInstanceOfType<ImovelViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(3u, imovel.Id);
        Assert.IsNull(viewResult.ViewData["Dono"]);

        imovelServiceMock.Verify(service => service.GetPublico(3), Times.Once);
    }

    private static ImovelViewModel ObterModelo(uint id = 1, uint idPessoa = 1)
    {
        return new ImovelViewModel
        {
            Id = id,
            Nome = "Casa com 3 quartos no Centro",
            Descricao = "Casa térrea com quintal amplo.",
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
            IdPessoaProprietario = idPessoa
        };
    }

    private static void VerificarRedirecionamentoParaIndex(ActionResult resultado, uint idPessoa)
    {
        var redirectResult = Assert.IsInstanceOfType<RedirectToActionResult>(resultado);

        Assert.IsNull(redirectResult.ControllerName);
        Assert.AreEqual(nameof(ImovelController.Index), redirectResult.ActionName);
        Assert.IsNotNull(redirectResult.RouteValues);
        Assert.AreEqual(idPessoa.ToString(), redirectResult.RouteValues["idPessoa"]?.ToString());
    }

    private ViewResult VerificarViewComErro<TModelo>(ActionResult resultado, string mensagem)
    {
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        Assert.IsInstanceOfType<TModelo>(viewResult.ViewData.Model);

        var erros = controller.ModelState.Values
            .SelectMany(entrada => entrada.Errors)
            .ToList();

        Assert.HasCount(1, erros);
        Assert.AreEqual(mensagem, erros[0].ErrorMessage);
        return viewResult;
    }

    private static void VerificarRedirecionamentoParaReview(ActionResult resultado)
    {
        var redirectResult =
            Assert.IsInstanceOfType<RedirectToActionResult>(resultado);

        Assert.IsNull(redirectResult.ControllerName);
        Assert.AreEqual(
            nameof(ImovelController.Review),
            redirectResult.ActionName);
    }

    private static List<AluguelViewModel> Alugueis(ImovelController controller)
    {
        return Assert.IsInstanceOfType<List<AluguelViewModel>>(controller.ViewBag.Alugueis);
    }

    private static Aluguel ObterAluguel(uint id, string status, Imovel imovel)
    {
        return new Aluguel
        {
            Id = id,
            DiaPagamento = 10,
            Valor = 1200m,
            Status = status,
            IdImovel = imovel.Id,
            IdPessoaInquilino = 2,
            IdImovelNavigation = imovel
        };
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