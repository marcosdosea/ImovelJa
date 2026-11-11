using System.ComponentModel.DataAnnotations;
using Core;
using Core.Service;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ImovelJaWeb.Controllers.Tests;

[TestClass]
public class PlanoControllerTests
{
    private Mock<IPlanoService> planoServiceMock = null!;
    private PlanoController controller = null!;

    [TestInitialize]
    public void Inicializar()
    {
        planoServiceMock = new Mock<IPlanoService>();

        planoServiceMock
            .Setup(service => service.GetAll())
            .Returns(ObterPlanosDeTeste());

        planoServiceMock
            .Setup(service => service.Get(1))
            .Returns(ObterPlanoBasico());

        planoServiceMock
            .Setup(service => service.Get(2))
            .Returns(ObterPlanoProfissional());

        planoServiceMock
            .Setup(service => service.Get(3))
            .Returns(ObterPlanoImobiliaria());

        planoServiceMock
            .Setup(service => service.Get(999))
            .Returns((Plano?)null);

        controller = new PlanoController(planoServiceMock.Object);
    }

    [TestMethod]
    public void Index_QuandoExistemPlanos_DeveRetornarViewComOsPlanos()
    {
        // Act
        var resultado = controller.Index();

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var planos = Assert.IsInstanceOfType<IEnumerable<PlanoViewModel>>(
            viewResult.ViewData.Model);

        Assert.HasCount(3, planos);

        var primeiro = planos.First();

        Assert.AreEqual(1u, primeiro.Id);
        Assert.AreEqual("Básico", primeiro.Nome);
        Assert.AreEqual(0m, primeiro.Valor);
        Assert.AreEqual("A", primeiro.Status);

        planoServiceMock.Verify(
            service => service.GetAll(),
            Times.Once);
    }

    [TestMethod]
    public void Index_QuandoNaoExistemPlanos_DeveRetornarViewComColecaoVazia()
    {
        // Arrange
        planoServiceMock
            .Setup(service => service.GetAll())
            .Returns(Array.Empty<Plano>());

        // Act
        var resultado = controller.Index();

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var planos = Assert.IsInstanceOfType<IEnumerable<PlanoViewModel>>(
            viewResult.ViewData.Model);

        Assert.IsEmpty(planos);

        planoServiceMock.Verify(
            service => service.GetAll(),
            Times.Once);
    }

    [TestMethod]
    public void Details_QuandoPlanoExiste_DeveRetornarViewComPlano()
    {
        // Act
        var resultado = controller.Details(2);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var plano = Assert.IsInstanceOfType<PlanoViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(2u, plano.Id);
        Assert.AreEqual("Profissional", plano.Nome);
        Assert.AreEqual(49.90m, plano.Valor);
        Assert.AreEqual(10u, plano.LimiteImoveisAnunciados);
        Assert.AreEqual("A", plano.Status);

        planoServiceMock.Verify(
            service => service.Get(2),
            Times.Once);
    }

    [TestMethod]
    public void Details_QuandoPlanoNaoExiste_DeveRetornarViewComModeloVazio()
    {
        // Act
        var resultado = controller.Details(999);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var plano = Assert.IsInstanceOfType<PlanoViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(0u, plano.Id);
        Assert.IsTrue(string.IsNullOrEmpty(plano.Nome));

        planoServiceMock.Verify(
            service => service.Get(999),
            Times.Once);
    }

    [TestMethod]
    public void Edit_QuandoPlanoExiste_DeveRetornarViewComPlano()
    {
        // Act
        var resultado = controller.Edit(3);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var plano = Assert.IsInstanceOfType<PlanoViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(3u, plano.Id);
        Assert.AreEqual("Imobiliária", plano.Nome);
        Assert.AreEqual(199.90m, plano.Valor);
        Assert.AreEqual(PlanoTexto.LimiteIlimitado, plano.LimiteImoveisAnunciados);

        planoServiceMock.Verify(
            service => service.Get(3),
            Times.Once);
    }

    [TestMethod]
    public void Edit_QuandoPlanoNaoExiste_DeveRetornarViewComModeloVazio()
    {
        // Act
        var resultado = controller.Edit(999);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var plano = Assert.IsInstanceOfType<PlanoViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(0u, plano.Id);
        Assert.IsTrue(string.IsNullOrEmpty(plano.Nome));

        planoServiceMock.Verify(
            service => service.Get(999),
            Times.Once);
    }

    [TestMethod]
    public void Edit_QuandoDadosValidos_DeveEditarERedirecionarParaIndex()
    {
        // Arrange
        var modelo = ObterModeloProfissional();
        modelo.Valor = 59.90m;
        modelo.Beneficios = "Até 10 imóveis anunciados\nRelatórios mensais";

        // Act
        var resultado = controller.Edit(modelo);

        // Assert
        VerificarRedirecionamentoParaIndex(resultado);

        planoServiceMock.Verify(
            service => service.Edit(It.Is<Plano>(plano =>
                plano.Id == 2
                && plano.Valor == 59.90m
                && plano.Beneficios == "Até 10 imóveis anunciados\nRelatórios mensais"
                && plano.Nome == "Profissional")),
            Times.Once);
    }

    [TestMethod]
    public void Edit_QuandoDadosInvalidos_DeveRetornarViewSemChamarServico()
    {
        // Arrange
        controller.ModelState.AddModelError(
            nameof(PlanoViewModel.Valor),
            "O campo Preço (R$) deve estar entre 0 e 99999999,99");

        var modelo = ObterModeloProfissional();
        modelo.Valor = -1m;

        // Act
        var resultado = controller.Edit(modelo);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var plano = Assert.IsInstanceOfType<PlanoViewModel>(viewResult.ViewData.Model);

        Assert.IsNull(viewResult.ViewName);
        Assert.AreEqual(2u, plano.Id);
        Assert.AreEqual(-1m, plano.Valor);
        Assert.IsFalse(controller.ModelState.IsValid);

        planoServiceMock.Verify(
            service => service.Edit(It.IsAny<Plano>()),
            Times.Never);
    }

    [TestMethod]
    public void Edit_QuandoValorDoBasicoNaoEZero_DeveRetornarViewComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "O plano Básico deve ter valor zero.";

        planoServiceMock
            .Setup(service => service.Edit(It.IsAny<Plano>()))
            .Throws(new ServiceException(mensagem));

        var modelo = ObterModeloBasico();
        modelo.Valor = 10m;

        // Act
        var resultado = controller.Edit(modelo);

        // Assert
        VerificarViewDeEdicaoComErro(resultado, 1, mensagem);
    }

    [TestMethod]
    public void Edit_QuandoValorENegativo_DeveRetornarViewComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "O valor do plano não pode ser negativo.";

        planoServiceMock
            .Setup(service => service.Edit(It.IsAny<Plano>()))
            .Throws(new ServiceException(mensagem));

        var modelo = ObterModeloProfissional();
        modelo.Valor = -5m;

        // Act
        var resultado = controller.Edit(modelo);

        // Assert
        VerificarViewDeEdicaoComErro(resultado, 2, mensagem);
    }

    [TestMethod]
    public void Edit_QuandoPlanoNaoExisteNoPost_DeveRetornarViewComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "Plano não encontrado.";

        planoServiceMock
            .Setup(service => service.Edit(It.IsAny<Plano>()))
            .Throws(new ServiceException(mensagem));

        var modelo = ObterModeloProfissional();
        modelo.Id = 999;

        // Act
        var resultado = controller.Edit(modelo);

        // Assert
        VerificarViewDeEdicaoComErro(resultado, 999, mensagem);
    }

    [TestMethod]
    public void Activate_QuandoDesativaPlano_DeveDesativarERedirecionarParaIndex()
    {
        // Act
        var resultado = controller.Activate(2, false, null);

        // Assert
        VerificarRedirecionamentoParaIndex(resultado);

        planoServiceMock.Verify(
            service => service.Activate(2, false),
            Times.Once);
    }

    [TestMethod]
    public void Activate_QuandoAtivaPlano_DeveAtivarERedirecionarParaIndex()
    {
        // Act
        var resultado = controller.Activate(3, true, null);

        // Assert
        VerificarRedirecionamentoParaIndex(resultado);

        planoServiceMock.Verify(
            service => service.Activate(3, true),
            Times.Once);
    }

    [TestMethod]
    public void Activate_QuandoDadosInvalidos_DeveRetornarIndexSemChamarServico()
    {
        // Arrange
        controller.ModelState.AddModelError(string.Empty, "Dados inválidos");

        // Act
        var resultado = controller.Activate(2, false, null);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var planos = Assert.IsInstanceOfType<IEnumerable<PlanoViewModel>>(
            viewResult.ViewData.Model);

        Assert.AreEqual(nameof(PlanoController.Index), viewResult.ViewName);
        Assert.HasCount(3, planos);

        planoServiceMock.Verify(
            service => service.Activate(It.IsAny<uint>(), It.IsAny<bool>()),
            Times.Never);
    }

    [TestMethod]
    public void Activate_QuandoDesativaBasico_DeveRetornarIndexComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "O plano Básico não pode ser desativado.";

        planoServiceMock
            .Setup(service => service.Activate(1, false))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.Activate(1, false, null);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var planos = Assert.IsInstanceOfType<IEnumerable<PlanoViewModel>>(
            viewResult.ViewData.Model);

        Assert.AreEqual(nameof(PlanoController.Index), viewResult.ViewName);
        Assert.HasCount(3, planos);
        Assert.AreEqual(mensagem, ObterMensagemDeErro());
    }

    [TestMethod]
    public void Activate_QuandoFalhaNaEdicao_DeveRetornarViewEditComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "O plano Básico não pode ser desativado.";

        planoServiceMock
            .Setup(service => service.Activate(1, false))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.Activate(1, false, nameof(PlanoController.Edit));

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var plano = Assert.IsInstanceOfType<PlanoViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(nameof(PlanoController.Edit), viewResult.ViewName);
        Assert.AreEqual(1u, plano.Id);
        Assert.AreEqual("Básico", plano.Nome);
        Assert.AreEqual(mensagem, ObterMensagemDeErro());
    }

    [TestMethod]
    public void Activate_QuandoPlanoNaoExiste_DeveRetornarIndexComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "Plano não encontrado.";

        planoServiceMock
            .Setup(service => service.Activate(999, true))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.Activate(999, true, null);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);

        Assert.AreEqual(nameof(PlanoController.Index), viewResult.ViewName);
        Assert.AreEqual(mensagem, ObterMensagemDeErro());
    }

    [TestMethod]
    public void PlanoViewModel_QuandoValorENegativo_DeveSerInvalido()
    {
        // Arrange
        var modelo = ObterModeloProfissional();
        modelo.Valor = -1m;

        // Act
        var resultados = Validar(modelo);

        // Assert
        Assert.IsTrue(resultados.Any(resultado =>
            resultado.MemberNames.Contains(nameof(PlanoViewModel.Valor))));
    }

    [TestMethod]
    public void PlanoViewModel_QuandoNomeExcedeTamanho_DeveSerInvalido()
    {
        // Arrange
        var modelo = ObterModeloProfissional();
        modelo.Nome = new string('A', 101);

        // Act
        var resultados = Validar(modelo);

        // Assert
        Assert.IsTrue(resultados.Any(resultado =>
            resultado.MemberNames.Contains(nameof(PlanoViewModel.Nome))));
    }

    [TestMethod]
    public void PlanoViewModel_QuandoNomeNaoEstaNoDicionario_DeveSerInvalido()
    {
        // Arrange
        var modelo = ObterModeloProfissional();
        modelo.Nome = "Premium";

        // Act
        var resultados = Validar(modelo);

        // Assert
        Assert.IsTrue(resultados.Any(resultado =>
            resultado.MemberNames.Contains(nameof(PlanoViewModel.Nome))));
    }

    [TestMethod]
    public void PlanoViewModel_QuandoStatusEInvalido_DeveSerInvalido()
    {
        // Arrange
        var modelo = ObterModeloProfissional();
        modelo.Status = "X";

        // Act
        var resultados = Validar(modelo);

        // Assert
        Assert.IsTrue(resultados.Any(resultado =>
            resultado.MemberNames.Contains(nameof(PlanoViewModel.Status))));
    }

    [TestMethod]
    public void PlanoViewModel_QuandoDadosCorrespondemAoDicionario_DeveSerValido()
    {
        // Act
        var resultados = Validar(ObterModeloImobiliaria());

        // Assert
        Assert.IsEmpty(resultados);
    }

    private string ObterMensagemDeErro()
    {
        var erros = controller.ModelState.Values
            .SelectMany(entrada => entrada.Errors)
            .ToList();

        Assert.HasCount(1, erros);
        return erros[0].ErrorMessage;
    }

    private static void VerificarRedirecionamentoParaIndex(ActionResult resultado)
    {
        var redirectResult = Assert.IsInstanceOfType<RedirectToActionResult>(resultado);

        Assert.IsNull(redirectResult.ControllerName);
        Assert.AreEqual(nameof(PlanoController.Index), redirectResult.ActionName);
    }

    private void VerificarViewDeEdicaoComErro(ActionResult resultado, uint id, string mensagem)
    {
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var plano = Assert.IsInstanceOfType<PlanoViewModel>(viewResult.ViewData.Model);

        Assert.IsNull(viewResult.ViewName);
        Assert.AreEqual(id, plano.Id);
        Assert.AreEqual(mensagem, ObterMensagemDeErro());
    }

    private static List<ValidationResult> Validar(PlanoViewModel modelo)
    {
        var contexto = new ValidationContext(modelo);
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, contexto, resultados, validateAllProperties: true);
        return resultados;
    }

    private static PlanoViewModel ObterModeloBasico()
    {
        return new PlanoViewModel
        {
            Id = 1,
            Nome = "Básico",
            Valor = 0m,
            LimiteImoveisAnunciados = 1,
            Beneficios = "1 imóvel anunciado",
            Status = "A"
        };
    }

    private static PlanoViewModel ObterModeloProfissional()
    {
        return new PlanoViewModel
        {
            Id = 2,
            Nome = "Profissional",
            Valor = 49.90m,
            LimiteImoveisAnunciados = 10,
            Beneficios = "Até 10 imóveis anunciados",
            Status = "A"
        };
    }

    private static PlanoViewModel ObterModeloImobiliaria()
    {
        return new PlanoViewModel
        {
            Id = 3,
            Nome = "Imobiliária",
            Valor = 199.90m,
            LimiteImoveisAnunciados = PlanoTexto.LimiteIlimitado,
            Beneficios = "Imóveis ilimitados",
            Status = "A"
        };
    }

    private static Plano ObterPlanoBasico()
    {
        return new Plano
        {
            Id = 1,
            Nome = "Básico",
            Valor = 0m,
            LimiteImoveisAnunciados = 1,
            Beneficios = "1 imóvel anunciado\nAnúncio de venda ou aluguel\nChat com interessados",
            Status = "A"
        };
    }

    private static Plano ObterPlanoProfissional()
    {
        return new Plano
        {
            Id = 2,
            Nome = "Profissional",
            Valor = 49.90m,
            LimiteImoveisAnunciados = 10,
            Beneficios = "Até 10 imóveis anunciados\nDestaque na busca\nGestão de aluguéis\nSuporte por e-mail",
            Status = "A"
        };
    }

    private static Plano ObterPlanoImobiliaria()
    {
        return new Plano
        {
            Id = 3,
            Nome = "Imobiliária",
            Valor = 199.90m,
            LimiteImoveisAnunciados = PlanoTexto.LimiteIlimitado,
            Beneficios = "Imóveis ilimitados\nSuporte dedicado\nAPI de integração\nRelatórios avançados\nMúltiplos usuários",
            Status = "A"
        };
    }

    private static IEnumerable<Plano> ObterPlanosDeTeste()
    {
        return
        [
            ObterPlanoBasico(),
            ObterPlanoProfissional(),
            ObterPlanoImobiliaria()
        ];
    }
}
