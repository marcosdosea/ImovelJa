using System.ComponentModel.DataAnnotations;
using System.Text;
using Core;
using Core.Service;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace ImovelJaWeb.Controllers.Tests;

[TestClass]
public class AluguelControllerTests
{
    private const uint IdInquilino = 20;

    private Mock<IAluguelService> aluguelServiceMock = null!;
    private Mock<IImovelService> imovelServiceMock = null!;
    private Mock<IPessoaService> pessoaServiceMock = null!;
    private AluguelController controller = null!;

    [TestInitialize]
    public void Inicializar()
    {
        aluguelServiceMock = new Mock<IAluguelService>();
        imovelServiceMock = new Mock<IImovelService>();
        pessoaServiceMock = new Mock<IPessoaService>();

        imovelServiceMock
            .Setup(service => service.Get(10))
            .Returns(ObterImovel(10, "D", "A", "A"));

        imovelServiceMock
            .Setup(service => service.Get(11))
            .Returns(ObterImovel(11, "A", "A", "A"));

        imovelServiceMock
            .Setup(service => service.Get(12))
            .Returns(ObterImovel(12, "D", "P", "A"));

        imovelServiceMock
            .Setup(service => service.Get(13))
            .Returns(ObterImovel(13, "D", "A", "V"));

        imovelServiceMock
            .Setup(service => service.Get(999))
            .Returns((Imovel?)null);

        aluguelServiceMock
            .Setup(service => service.Get(1))
            .Returns(ObterAluguel(1, "A"));

        aluguelServiceMock
            .Setup(service => service.Get(2))
            .Returns(ObterAluguel(2, "C"));

        aluguelServiceMock
            .Setup(service => service.Get(999))
            .Returns((Aluguel?)null);

        aluguelServiceMock
            .Setup(service => service.Create(It.IsAny<Aluguel>()))
            .Returns(99u);

        pessoaServiceMock
            .Setup(service => service.Get(IdInquilino))
            .Returns(new Pessoa { Id = IdInquilino, Nome = "Maria José Santos" });

        var httpContext = new DefaultHttpContext();

        controller = new AluguelController(
            aluguelServiceMock.Object,
            imovelServiceMock.Object,
            pessoaServiceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(
                httpContext,
                Mock.Of<ITempDataProvider>())
        };
    }

    [TestMethod]
    public void CreateGet_QuandoImovelDisponivel_DeveRetornarViewComValorDoImovel()
    {
        // Act
        var resultado = controller.Create(10);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var aluguel = Assert.IsInstanceOfType<AluguelViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(10u, aluguel.IdImovel);
        Assert.AreEqual(1500m, aluguel.Valor);
        Assert.AreEqual("F", aluguel.Inquilino!.Tipo);

        imovelServiceMock.Verify(
            service => service.Get(10),
            Times.Once);
    }

    [TestMethod]
    public void CreateGet_QuandoImovelNaoExiste_DeveRetornarNotFound()
    {
        // Act
        var resultado = controller.Create(999);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);

        imovelServiceMock.Verify(
            service => service.Get(999),
            Times.Once);
    }

    [TestMethod]
    public void CreateGet_QuandoImovelJaAlugado_DeveRedirecionarComMensagemDeErro()
    {
        // Act
        var resultado = controller.Create(11);

        // Assert
        VerificarRedirecionamentoParaImovel(resultado, 11);

        Assert.AreEqual("O imóvel não está disponível para aluguel.", controller.TempData["Erro"]);
    }

    [TestMethod]
    public void CreateGet_QuandoAnuncioNaoAprovado_DeveRedirecionarComMensagemDeErro()
    {
        // Act
        var resultado = controller.Create(12);

        // Assert
        VerificarRedirecionamentoParaImovel(resultado, 12);

        Assert.AreEqual("O anúncio do imóvel ainda não foi aprovado.", controller.TempData["Erro"]);
    }

    [TestMethod]
    public void CreateGet_QuandoImovelEDeVenda_DeveRedirecionarComMensagemDeErro()
    {
        // Act
        var resultado = controller.Create(13);

        // Assert
        VerificarRedirecionamentoParaImovel(resultado, 13);

        Assert.AreEqual("O imóvel não é anunciado para aluguel.", controller.TempData["Erro"]);
    }

    [TestMethod]
    public void CreatePost_QuandoDadosValidos_DeveCriarERedirecionarParaDetails()
    {
        // Arrange
        var modelo = ObterModelo();

        // Act
        var resultado = controller.Create(modelo);

        // Assert
        VerificarRedirecionamentoParaDetails(resultado, 99);

        aluguelServiceMock.Verify(
            service => service.Create(It.Is<Aluguel>(aluguel =>
                aluguel.IdImovel == 10
                && aluguel.IdPessoaInquilino == IdInquilino
                && aluguel.DiaPagamento == 5
                && aluguel.Valor == 1500m)),
            Times.Once);
    }

    [TestMethod]
    public void CreatePost_QuandoEnviaContrato_DeveGravarOArquivoNoAluguel()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.Contrato = ObterPdf();

        // Act
        var resultado = controller.Create(modelo);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(resultado);

        aluguelServiceMock.Verify(
            service => service.Create(It.Is<Aluguel>(aluguel =>
                aluguel.ArquivoContrato != null
                && aluguel.ArquivoContrato.Length > 0)),
            Times.Once);
    }

    [TestMethod]
    public void CreatePost_QuandoInquilinoNovo_DeveEnviarDadosDoInquilinoSemEmail()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.IdPessoaInquilino = 0;
        modelo.Inquilino = new PessoaViewModel
        {
            Tipo = "F",
            Nome = "Ana Souza",
            Cpf = "52998224725",
            Email = "ana@teste.com"
        };

        // Act
        var resultado = controller.Create(modelo);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(resultado);

        aluguelServiceMock.Verify(
            service => service.Create(It.Is<Aluguel>(aluguel =>
                aluguel.IdPessoaInquilinoNavigation.Nome == "Ana Souza"
                && aluguel.IdPessoaInquilinoNavigation.Cpf == "52998224725"
                && aluguel.IdPessoaInquilinoNavigation.Email == null)),
            Times.Once);
    }

    [TestMethod]
    public void CreatePost_QuandoInquilinoExisteComErroNosDadosDele_DeveIgnorarOErro()
    {
        // Arrange
        controller.ModelState.AddModelError("Inquilino.Nome", "O campo Nome é obrigatório");

        // Act
        var resultado = controller.Create(ObterModelo());

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(resultado);

        aluguelServiceMock.Verify(
            service => service.Create(It.IsAny<Aluguel>()),
            Times.Once);
    }

    [TestMethod]
    public void CreatePost_QuandoDadosInvalidos_DeveRetornarViewSemChamarServico()
    {
        // Arrange
        controller.ModelState.AddModelError(
            nameof(AluguelViewModel.Valor),
            "O campo Valor é obrigatório");

        var modelo = ObterModelo();

        // Act
        var resultado = controller.Create(modelo);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var aluguel = Assert.IsInstanceOfType<AluguelViewModel>(
            viewResult.ViewData.Model);

        Assert.IsNull(viewResult.ViewName);
        Assert.AreEqual(10u, aluguel.IdImovel);
        Assert.IsFalse(controller.ModelState.IsValid);

        aluguelServiceMock.Verify(
            service => service.Create(It.IsAny<Aluguel>()),
            Times.Never);
    }

    [TestMethod]
    public void CreatePost_QuandoImovelNaoExiste_DeveRetornarNotFound()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.IdImovel = 999;

        // Act
        var resultado = controller.Create(modelo);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);

        aluguelServiceMock.Verify(
            service => service.Create(It.IsAny<Aluguel>()),
            Times.Never);
    }

    [TestMethod]
    public void CreatePost_QuandoImovelJaAlugado_DeveRetornarViewComMensagemDeErro()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.IdImovel = 11;

        // Act
        var resultado = controller.Create(modelo);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);

        Assert.IsNull(viewResult.ViewName);
        Assert.AreEqual("O imóvel não está disponível para aluguel.", ObterMensagemDeErro());

        aluguelServiceMock.Verify(
            service => service.Create(It.IsAny<Aluguel>()),
            Times.Never);
    }

    [TestMethod]
    public void CreatePost_QuandoImovelJaPossuiAluguelAtivo_DeveRetornarViewComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "O imóvel já possui um aluguel ativo.";

        aluguelServiceMock
            .Setup(service => service.Create(It.IsAny<Aluguel>()))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.Create(ObterModelo());

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var aluguel = Assert.IsInstanceOfType<AluguelViewModel>(
            viewResult.ViewData.Model);

        Assert.IsNull(viewResult.ViewName);
        Assert.AreEqual(10u, aluguel.IdImovel);
        Assert.AreEqual(mensagem, ObterMensagemDeErro());
    }

    [TestMethod]
    public void CreatePost_QuandoInquilinoNaoEncontrado_DeveRetornarViewComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "Inquilino não encontrado.";

        aluguelServiceMock
            .Setup(service => service.Create(It.IsAny<Aluguel>()))
            .Throws(new ServiceException(mensagem));

        var modelo = ObterModelo();
        modelo.IdPessoaInquilino = 888;

        // Act
        var resultado = controller.Create(modelo);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(resultado);
        Assert.AreEqual(mensagem, ObterMensagemDeErro());
    }

    [TestMethod]
    public void Details_QuandoAluguelExiste_DeveRetornarViewComAluguelEInquilino()
    {
        // Act
        var resultado = controller.Details(1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var aluguel = Assert.IsInstanceOfType<AluguelViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(1u, aluguel.Id);
        Assert.AreEqual(1500m, aluguel.Valor);
        Assert.AreEqual("A", aluguel.Status);
        Assert.IsTrue(aluguel.TemContrato);
        Assert.AreEqual("Maria José Santos", aluguel.Inquilino!.Nome);

        aluguelServiceMock.Verify(
            service => service.Get(1),
            Times.Once);
    }

    [TestMethod]
    public void Details_QuandoInquilinoNaoEncontrado_DeveRetornarViewSemInquilino()
    {
        // Arrange
        pessoaServiceMock
            .Setup(service => service.Get(IdInquilino))
            .Returns((Pessoa?)null);

        // Act
        var resultado = controller.Details(1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var aluguel = Assert.IsInstanceOfType<AluguelViewModel>(
            viewResult.ViewData.Model);

        Assert.IsNull(aluguel.Inquilino);
    }

    [TestMethod]
    public void Details_QuandoAluguelNaoExiste_DeveRetornarNotFound()
    {
        // Act
        var resultado = controller.Details(999);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);

        aluguelServiceMock.Verify(
            service => service.Get(999),
            Times.Once);
        pessoaServiceMock.Verify(
            service => service.Get(It.IsAny<uint>()),
            Times.Never);
    }

    [TestMethod]
    public void Edit_QuandoDadosValidos_DeveEditarERedirecionarParaDetails()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.Id = 1;
        modelo.DiaPagamento = 10;
        modelo.Valor = 2000m;

        // Act
        var resultado = controller.Edit(modelo);

        // Assert
        VerificarRedirecionamentoParaDetails(resultado, 1);

        aluguelServiceMock.Verify(
            service => service.Edit(It.Is<Aluguel>(aluguel =>
                aluguel.Id == 1
                && aluguel.DiaPagamento == 10
                && aluguel.Valor == 2000m
                && aluguel.Status == "A")),
            Times.Once);
    }

    [TestMethod]
    public void Edit_QuandoImovelEInquilinoVemAlteradosNoFormulario_DeveManterOsOriginais()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.Id = 1;
        modelo.IdImovel = 999;
        modelo.IdPessoaInquilino = 999;

        // Act
        var resultado = controller.Edit(modelo);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(resultado);

        aluguelServiceMock.Verify(
            service => service.Edit(It.Is<Aluguel>(aluguel =>
                aluguel.IdImovel == 10
                && aluguel.IdPessoaInquilino == IdInquilino)),
            Times.Once);
    }

    [TestMethod]
    public void Edit_QuandoNaoEnviaArquivo_DeveManterContratoAtual()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.Id = 1;

        // Act
        controller.Edit(modelo);

        // Assert
        aluguelServiceMock.Verify(
            service => service.Edit(It.Is<Aluguel>(aluguel =>
                aluguel.ArquivoContrato != null
                && aluguel.ArquivoContrato.Length == 3)),
            Times.Once);
    }

    [TestMethod]
    public void Edit_QuandoEnviaArquivoNovo_DeveSubstituirOContrato()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.Id = 1;
        modelo.Contrato = ObterPdf();

        // Act
        controller.Edit(modelo);

        // Assert
        aluguelServiceMock.Verify(
            service => service.Edit(It.Is<Aluguel>(aluguel =>
                aluguel.ArquivoContrato != null
                && aluguel.ArquivoContrato.Length > 3)),
            Times.Once);
    }

    [TestMethod]
    public void Edit_QuandoAluguelNaoExiste_DeveRetornarNotFound()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.Id = 999;

        // Act
        var resultado = controller.Edit(modelo);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);

        aluguelServiceMock.Verify(
            service => service.Edit(It.IsAny<Aluguel>()),
            Times.Never);
    }

    [TestMethod]
    public void Edit_QuandoDadosInvalidos_DeveRetornarDetailsSemChamarServico()
    {
        // Arrange
        controller.ModelState.AddModelError(
            nameof(AluguelViewModel.Valor),
            "O campo Valor é obrigatório");

        var modelo = ObterModelo();
        modelo.Id = 1;

        // Act
        var resultado = controller.Edit(modelo);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var aluguel = Assert.IsInstanceOfType<AluguelViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual("Details", viewResult.ViewName);
        Assert.AreEqual(1u, aluguel.Id);
        Assert.AreEqual("Maria José Santos", aluguel.Inquilino!.Nome);

        aluguelServiceMock.Verify(
            service => service.Edit(It.IsAny<Aluguel>()),
            Times.Never);
    }

    [TestMethod]
    public void Edit_QuandoAluguelCancelado_DeveRetornarDetailsComMensagemDeErro()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.Id = 2;

        // Act
        var resultado = controller.Edit(modelo);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);

        Assert.AreEqual("Details", viewResult.ViewName);
        Assert.AreEqual("Não é possível editar um aluguel cancelado.", ObterMensagemDeErro());

        aluguelServiceMock.Verify(
            service => service.Edit(It.IsAny<Aluguel>()),
            Times.Never);
    }

    [TestMethod]
    public void Edit_QuandoServicoRecusa_DeveRetornarDetailsComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "Aluguel não encontrado.";

        aluguelServiceMock
            .Setup(service => service.Edit(It.IsAny<Aluguel>()))
            .Throws(new ServiceException(mensagem));

        var modelo = ObterModelo();
        modelo.Id = 1;

        // Act
        var resultado = controller.Edit(modelo);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var aluguel = Assert.IsInstanceOfType<AluguelViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual("Details", viewResult.ViewName);
        Assert.AreEqual(1u, aluguel.Id);
        Assert.AreEqual(mensagem, ObterMensagemDeErro());
    }

    [TestMethod]
    public void DeleteGet_QuandoAluguelAtivo_DeveRetornarViewDeConfirmacao()
    {
        // Act
        var resultado = controller.Delete(1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var aluguel = Assert.IsInstanceOfType<AluguelViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(1u, aluguel.Id);
        Assert.AreEqual("Maria José Santos", aluguel.Inquilino!.Nome);

        aluguelServiceMock.Verify(
            service => service.Get(1),
            Times.Once);
    }

    [TestMethod]
    public void DeleteGet_QuandoAluguelNaoExiste_DeveRetornarNotFound()
    {
        // Act
        var resultado = controller.Delete(999);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);

        aluguelServiceMock.Verify(
            service => service.Get(999),
            Times.Once);
    }

    [TestMethod]
    public void DeleteGet_QuandoAluguelJaCancelado_DeveRedirecionarParaDetailsComMensagemDeErro()
    {
        // Act
        var resultado = controller.Delete(2);

        // Assert
        VerificarRedirecionamentoParaDetails(resultado, 2);

        Assert.AreEqual("O aluguel já está cancelado.", controller.TempData["Erro"]);
    }

    [TestMethod]
    public void DeleteConfirmed_QuandoAluguelAtivo_DeveCancelarERedirecionarParaDetails()
    {
        // Act
        var resultado = controller.DeleteConfirmed(1);

        // Assert
        VerificarRedirecionamentoParaDetails(resultado, 1);

        aluguelServiceMock.Verify(
            service => service.Delete(1),
            Times.Once);
    }

    [TestMethod]
    public void DeleteConfirmed_QuandoAluguelNaoExiste_DeveRetornarNotFound()
    {
        // Act
        var resultado = controller.DeleteConfirmed(999);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);

        aluguelServiceMock.Verify(
            service => service.Delete(It.IsAny<uint>()),
            Times.Never);
    }

    [TestMethod]
    public void DeleteConfirmed_QuandoAluguelJaCancelado_DeveRedirecionarSemChamarServico()
    {
        // Act
        var resultado = controller.DeleteConfirmed(2);

        // Assert
        VerificarRedirecionamentoParaDetails(resultado, 2);

        Assert.AreEqual("O aluguel já está cancelado.", controller.TempData["Erro"]);

        aluguelServiceMock.Verify(
            service => service.Delete(It.IsAny<uint>()),
            Times.Never);
    }

    [TestMethod]
    public void DeleteConfirmed_QuandoServicoRecusa_DeveRetornarViewComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "Falha ao cancelar o aluguel.";

        aluguelServiceMock
            .Setup(service => service.Delete(1))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.DeleteConfirmed(1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var aluguel = Assert.IsInstanceOfType<AluguelViewModel>(
            viewResult.ViewData.Model);

        Assert.IsNull(viewResult.ViewName);
        Assert.AreEqual(1u, aluguel.Id);
        Assert.AreEqual(mensagem, ObterMensagemDeErro());
    }

    [TestMethod]
    public void AluguelViewModel_QuandoDadosValidos_DeveSerValido()
    {
        // Act
        var resultados = Validar(ObterModelo());

        // Assert
        Assert.IsEmpty(resultados);
    }

    [TestMethod]
    public void AluguelViewModel_QuandoValorEZero_DeveSerInvalido()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.Valor = 0m;

        // Act
        var resultados = Validar(modelo);

        // Assert
        Assert.IsTrue(resultados.Any(resultado =>
            resultado.MemberNames.Contains(nameof(AluguelViewModel.Valor))));
    }

    [TestMethod]
    public void AluguelViewModel_QuandoDiaDePagamentoEZero_DeveSerInvalido()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.DiaPagamento = 0;

        // Act
        var resultados = Validar(modelo);

        // Assert
        Assert.IsTrue(resultados.Any(resultado =>
            resultado.MemberNames.Contains(nameof(AluguelViewModel.DiaPagamento))));
    }

    [TestMethod]
    public void AluguelViewModel_QuandoDiaDePagamentoPassaDe31_DeveSerInvalido()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.DiaPagamento = 32;

        // Act
        var resultados = Validar(modelo);

        // Assert
        Assert.IsTrue(resultados.Any(resultado =>
            resultado.MemberNames.Contains(nameof(AluguelViewModel.DiaPagamento))));
    }

    [TestMethod]
    public void AluguelViewModel_QuandoStatusEInvalido_DeveSerInvalido()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.Status = "X";

        // Act
        var resultados = Validar(modelo);

        // Assert
        Assert.IsTrue(resultados.Any(resultado =>
            resultado.MemberNames.Contains(nameof(AluguelViewModel.Status))));
    }

    [TestMethod]
    public void AluguelViewModel_QuandoContratoEPdf_DeveSerValido()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.Contrato = ObterPdf();

        // Act
        var resultados = Validar(modelo);

        // Assert
        Assert.IsEmpty(resultados);
    }

    [TestMethod]
    public void AluguelViewModel_QuandoContratoNaoEPdfNemImagem_DeveSerInvalido()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.Contrato = ObterArquivo("texto simples", "contrato.pdf");

        // Act
        var resultados = Validar(modelo);

        // Assert
        Assert.HasCount(1, resultados);
        Assert.AreEqual(
            "O contrato deve ser um arquivo PDF, JPG ou PNG.",
            resultados[0].ErrorMessage);
    }
    private static List<ValidationResult> Validar(AluguelViewModel modelo)
    {
        var contexto = new ValidationContext(modelo);
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, contexto, resultados, validateAllProperties: true);
        return resultados;
    }

    private string ObterMensagemDeErro()
    {
        var erros = controller.ModelState.Values
            .SelectMany(entrada => entrada.Errors)
            .ToList();

        Assert.HasCount(1, erros);
        return erros[0].ErrorMessage;
    }

    private static void VerificarRedirecionamentoParaDetails(IActionResult resultado, uint id)
    {
        var redirectResult =
            Assert.IsInstanceOfType<RedirectToActionResult>(resultado);

        Assert.AreEqual(
            nameof(AluguelController.Details),
            redirectResult.ActionName);
        Assert.AreEqual(id, Convert.ToUInt32(redirectResult.RouteValues!["id"]));
    }

    private static void VerificarRedirecionamentoParaImovel(IActionResult resultado, uint idImovel)
    {
        var redirectResult =
            Assert.IsInstanceOfType<RedirectToActionResult>(resultado);

        Assert.AreEqual("Imovel", redirectResult.ControllerName);
        Assert.AreEqual("Details", redirectResult.ActionName);
        Assert.AreEqual(idImovel, Convert.ToUInt32(redirectResult.RouteValues!["id"]));
    }

    private static IFormFile ObterArquivo(string conteudo, string nome)
    {
        var bytes = Encoding.ASCII.GetBytes(conteudo);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "Contrato", nome);
    }

    private static IFormFile ObterPdf()
    {
        return ObterArquivo("%PDF-1.4 contrato de aluguel", "contrato.pdf");
    }

    private static AluguelViewModel ObterModelo()
    {
        return new AluguelViewModel
        {
            IdImovel = 10,
            IdPessoaInquilino = IdInquilino,
            DiaPagamento = 5,
            Valor = 1500m,
            Status = "A"
        };
    }

    private static Aluguel ObterAluguel(uint id, string status)
    {
        return new Aluguel
        {
            Id = id,
            IdImovel = 10,
            IdPessoaInquilino = IdInquilino,
            DiaPagamento = 5,
            Valor = 1500m,
            ArquivoContrato = [1, 2, 3],
            Status = status
        };
    }

    private static Imovel ObterImovel(uint id, string status, string statusValidacao, string finalidade)
    {
        return new Imovel
        {
            Id = id,
            Nome = "Casa Azul",
            Tipo = "C",
            Rua = "Rua A",
            Bairro = "Centro",
            Cep = "49400000",
            Cidade = "Lagarto",
            Estado = "SE",
            Valor = 1500m,
            Finalidade = finalidade,
            Status = status,
            StatusValidacao = statusValidacao,
            IdPessoaProprietario = 1
        };
    }
}