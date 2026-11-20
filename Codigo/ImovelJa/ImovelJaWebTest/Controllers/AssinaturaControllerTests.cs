using Core;
using Core.Service;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace ImovelJaWeb.Controllers.Tests;

[TestClass]
public class AssinaturaControllerTests
{
    private Mock<IAssinaturaService> assinaturaServiceMock = null!;
    private Mock<IPlanoService> planoServiceMock = null!;
    private AssinaturaController controller = null!;

    [TestInitialize]
    public void Inicializar()
    {
        assinaturaServiceMock = new Mock<IAssinaturaService>();
        planoServiceMock = new Mock<IPlanoService>();

        assinaturaServiceMock
            .Setup(service => service.GetByPessoa(1))
            .Returns(ObterAssinaturaAtiva());

        assinaturaServiceMock
            .Setup(service => service.GetByPessoa(3))
            .Returns((Assinatura?)null);

        assinaturaServiceMock
            .Setup(service => service.Get(1))
            .Returns(ObterAssinaturaAtiva());

        assinaturaServiceMock
            .Setup(service => service.Get(999))
            .Returns((Assinatura?)null);

        planoServiceMock
            .Setup(service => service.GetAll())
            .Returns(ObterPlanosDeTeste());

        planoServiceMock
            .Setup(service => service.Get(2))
            .Returns(ObterPlanosDeTeste().First(plano => plano.Id == 2));

        planoServiceMock
            .Setup(service => service.Get(3))
            .Returns(ObterPlanosDeTeste().First(plano => plano.Id == 3));

        controller = new AssinaturaController(assinaturaServiceMock.Object, planoServiceMock.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    [TestMethod]
    public void Index_QuandoPessoaPossuiAssinatura_DeveRetornarViewComAssinaturaEPlanoAtual()
    {
        // Act
        var resultado = controller.Index(1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var assinatura = Assert.IsInstanceOfType<AssinaturaViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(1u, assinatura.Id);
        Assert.AreEqual(2u, assinatura.IdPlano);
        Assert.AreEqual("A", assinatura.Status);
        Assert.AreEqual(new DateTime(2026, 11, 10), assinatura.DataProximaCobranca);

        var plano = Assert.IsInstanceOfType<PlanoViewModel>(viewResult.ViewData["PlanoAtual"]);
        Assert.AreEqual("Profissional", plano.Nome);
    }

    [TestMethod]
    public void Index_QuandoPessoaNaoPossuiAssinatura_DeveRetornarViewSemPlanoAtual()
    {
        // Act
        var resultado = controller.Index(3);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var assinatura = Assert.IsInstanceOfType<AssinaturaViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(3u, assinatura.IdPessoa);
        Assert.AreEqual(0u, assinatura.Id);
        Assert.IsNull(viewResult.ViewData["PlanoAtual"]);
    }

    [TestMethod]
    public void Create_QuandoAcessado_DeveRetornarViewSomenteComPlanosAtivos()
    {
        // Act
        var resultado = controller.Create(3);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var assinatura = Assert.IsInstanceOfType<AssinaturaViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(3u, assinatura.IdPessoa);
        VerificarPlanosDisponiveis(viewResult);
    }

    [TestMethod]
    public void Create_QuandoDadosSaoValidos_DeveRegistrarAssinaturaERedirecionarParaIndex()
    {
        // Arrange
        var modelo = new AssinaturaViewModel { IdPessoa = 3, IdPlano = 3 };

        // Act
        var resultado = controller.Create(modelo);

        // Assert
        VerificarRedirecionamentoParaIndex(resultado, 3);
        Assert.AreEqual("Imobiliária", controller.TempData["PlanoAtivado"]);

        assinaturaServiceMock.Verify(
            service => service.Create(It.Is<Assinatura>(assinatura =>
                assinatura.IdPessoa == 3 && assinatura.IdPlano == 3)),
            Times.Once);
    }

    [TestMethod]
    public void Create_QuandoModelStateInvalido_DeveRetornarViewSemChamarService()
    {
        // Arrange
        var modelo = new AssinaturaViewModel { IdPessoa = 3 };
        controller.ModelState.AddModelError(nameof(AssinaturaViewModel.IdPlano), "Selecione um plano");

        // Act
        var resultado = controller.Create(modelo);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);

        Assert.AreSame(modelo, viewResult.ViewData.Model);
        VerificarPlanosDisponiveis(viewResult);

        assinaturaServiceMock.Verify(
            service => service.Create(It.IsAny<Assinatura>()),
            Times.Never);
    }

    [TestMethod]
    public void Create_QuandoPagamentoNaoEAutorizado_DeveRetornarViewComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "Pagamento não autorizado. Verifique os dados de pagamento e tente novamente.";

        assinaturaServiceMock
            .Setup(service => service.Create(It.IsAny<Assinatura>()))
            .Throws(new ServiceException(mensagem));

        var modelo = new AssinaturaViewModel { IdPessoa = 3, IdPlano = 3 };

        // Act
        var resultado = controller.Create(modelo);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);

        Assert.AreSame(modelo, viewResult.ViewData.Model);
        Assert.AreEqual(mensagem, ObterMensagemDeErro());
        VerificarPlanosDisponiveis(viewResult);
    }

    [TestMethod]
    public void Edit_QuandoAssinaturaExiste_DeveRetornarViewComAssinaturaEPlanosAtivos()
    {
        // Act
        var resultado = controller.Edit(1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var assinatura = Assert.IsInstanceOfType<AssinaturaViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(1u, assinatura.Id);
        Assert.AreEqual(2u, assinatura.IdPlano);
        VerificarPlanosDisponiveis(viewResult);
    }

    [TestMethod]
    public void Edit_QuandoAssinaturaNaoExiste_DeveRetornarViewComModeloVazio()
    {
        // Act
        var resultado = controller.Edit(999);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var assinatura = Assert.IsInstanceOfType<AssinaturaViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(0u, assinatura.Id);
    }

    [TestMethod]
    public void Edit_QuandoDadosSaoValidos_DeveTrocarPlanoERedirecionarParaIndex()
    {
        // Arrange
        var modelo = new AssinaturaViewModel { Id = 1, IdPessoa = 1, IdPlano = 3 };

        // Act
        var resultado = controller.Edit(modelo);

        // Assert
        VerificarRedirecionamentoParaIndex(resultado, 1);
        Assert.AreEqual("Imobiliária", controller.TempData["PlanoAtivado"]);

        assinaturaServiceMock.Verify(
            service => service.Edit(It.Is<Assinatura>(assinatura =>
                assinatura.Id == 1 && assinatura.IdPlano == 3)),
            Times.Once);
    }

    [TestMethod]
    public void Edit_QuandoPlanoEstaInativo_DeveRetornarViewComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "Este plano não está disponível para contratação.";

        assinaturaServiceMock
            .Setup(service => service.Edit(It.IsAny<Assinatura>()))
            .Throws(new ServiceException(mensagem));

        var modelo = new AssinaturaViewModel { Id = 1, IdPessoa = 1, IdPlano = 4 };

        // Act
        var resultado = controller.Edit(modelo);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);

        Assert.AreSame(modelo, viewResult.ViewData.Model);
        Assert.AreEqual(mensagem, ObterMensagemDeErro());
        VerificarPlanosDisponiveis(viewResult);
    }

    [TestMethod]
    public void Cancel_QuandoAssinaturaExiste_DeveRetornarViewDeConfirmacaoComPlanoAtual()
    {
        // Act
        var resultado = controller.Cancel(1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var assinatura = Assert.IsInstanceOfType<AssinaturaViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(1u, assinatura.Id);
        Assert.AreEqual(new DateTime(2026, 11, 10), assinatura.DataProximaCobranca);

        var plano = Assert.IsInstanceOfType<PlanoViewModel>(viewResult.ViewData["PlanoAtual"]);
        Assert.AreEqual("Profissional", plano.Nome);
    }

    [TestMethod]
    public void Cancel_QuandoConfirmado_DeveCancelarERedirecionarParaIndex()
    {
        // Arrange
        var modelo = new AssinaturaViewModel { Id = 1, IdPessoa = 1 };

        // Act
        var resultado = controller.Cancel(modelo);

        // Assert
        VerificarRedirecionamentoParaIndex(resultado, 1);

        assinaturaServiceMock.Verify(
            service => service.Cancel(1),
            Times.Once);
    }

    [TestMethod]
    public void Cancel_QuandoAssinaturaJaEstaCancelada_DeveRetornarViewComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "A assinatura já está cancelada.";

        assinaturaServiceMock
            .Setup(service => service.Cancel(1))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.Cancel(new AssinaturaViewModel { Id = 1, IdPessoa = 1 });

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var assinatura = Assert.IsInstanceOfType<AssinaturaViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(1u, assinatura.Id);
        Assert.AreEqual(2u, assinatura.IdPlano);
        Assert.AreEqual(mensagem, ObterMensagemDeErro());
    }

    private string ObterMensagemDeErro()
    {
        var erros = controller.ModelState.Values
            .SelectMany(entrada => entrada.Errors)
            .ToList();

        Assert.HasCount(1, erros);
        return erros[0].ErrorMessage;
    }

    private static void VerificarRedirecionamentoParaIndex(ActionResult resultado, uint idPessoa)
    {
        var redirectResult = Assert.IsInstanceOfType<RedirectToActionResult>(resultado);

        Assert.IsNull(redirectResult.ControllerName);
        Assert.AreEqual(nameof(AssinaturaController.Index), redirectResult.ActionName);
        Assert.IsNotNull(redirectResult.RouteValues);
        Assert.AreEqual(idPessoa, redirectResult.RouteValues["idPessoa"]);
    }

    private static void VerificarPlanosDisponiveis(ViewResult viewResult)
    {
        var planos = Assert.IsInstanceOfType<IEnumerable<PlanoViewModel>>(
            viewResult.ViewData["PlanosDisponiveis"]).ToList();

        Assert.HasCount(3, planos);
        Assert.IsTrue(planos.All(plano => plano.Status == "A"));
        Assert.AreEqual("Básico", planos[0].Nome);
    }

    private static Assinatura ObterAssinaturaAtiva()
    {
        return new Assinatura
        {
            Id = 1,
            IdPessoa = 1,
            IdPlano = 2,
            Status = "A",
            DataContratacao = new DateTime(2026, 10, 10),
            DataProximaCobranca = new DateTime(2026, 11, 10)
        };
    }

    private static IEnumerable<Plano> ObterPlanosDeTeste()
    {
        return
        [
            new Plano { Id = 1, Nome = "Básico", Valor = 0m, LimiteImoveisAnunciados = 1, Status = "A" },
            new Plano { Id = 2, Nome = "Profissional", Valor = 49.90m, LimiteImoveisAnunciados = 10, Status = "A" },
            new Plano { Id = 3, Nome = "Imobiliária", Valor = 199.90m, LimiteImoveisAnunciados = 1_000_000, Status = "A" },
            new Plano { Id = 4, Nome = "Premium", Valor = 99.90m, LimiteImoveisAnunciados = 30, Status = "I" }
        ];
    }
}
