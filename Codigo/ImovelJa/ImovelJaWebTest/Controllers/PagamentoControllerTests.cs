using Core;
using Core.Service;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ImovelJaWeb.Controllers.Tests;

[TestClass]
public class PagamentoControllerTests
{
    private const uint IdInquilino = 2;
    private const uint IdProprietario = 3;

    private Mock<IPagamentoService> pagamentoServiceMock = null!;
    private Mock<IAluguelService> aluguelServiceMock = null!;
    private Mock<IImovelService> imovelServiceMock = null!;
    private PagamentoController controller = null!;

    [TestInitialize]
    public void Inicializar()
    {
        pagamentoServiceMock = new Mock<IPagamentoService>();
        aluguelServiceMock = new Mock<IAluguelService>();
        imovelServiceMock = new Mock<IImovelService>();

        pagamentoServiceMock
            .Setup(service => service.Get(1))
            .Returns(ObterPagamento());

        pagamentoServiceMock
            .Setup(service => service.Get(999))
            .Returns((Pagamento?)null);

        pagamentoServiceMock
            .Setup(service => service.Create(It.IsAny<Pagamento>()))
            .Returns(10u);

        aluguelServiceMock
            .Setup(service => service.GetDoProprietario(4, IdProprietario))
            .Returns(ObterAluguel());

        controller = new PagamentoController(pagamentoServiceMock.Object, aluguelServiceMock.Object, imovelServiceMock.Object);
    }

    [TestMethod]
    public void Edit_QuandoPagamentoPertenceAoInquilino_DeveAbrirOFormulario()
    {
        // Act
        var resultado = controller.Edit(1, 2);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var pagamento = Assert.IsInstanceOfType<PagamentoViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(1u, pagamento.Id);
        Assert.AreEqual(4u, pagamento.IdAluguel);
        Assert.AreEqual(700m, pagamento.ValorAluguel);
        Assert.AreEqual("Casa — Itabaiana", pagamento.NomeImovel);
        Assert.AreEqual("PE", pagamento.Status);
        Assert.IsTrue(pagamento.PossuiComprovante);
        Assert.IsNull(pagamento.Comprovante);
        Assert.AreEqual(2u, viewResult.ViewData[AluguelController.ChaveIdPessoa]);
    }

    [TestMethod]
    public void Edit_QuandoONomeJaTrazACidade_DeveManterONome()
    {
        // Arrange
        var pagamento = ObterPagamento();
        pagamento.IdAluguelNavigation.IdImovelNavigation!.Nome = "Casa - Itabaiana";
        pagamentoServiceMock
            .Setup(service => service.Get(1))
            .Returns(pagamento);

        // Act
        var resultado = controller.Edit(1, 2);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var modelo = Assert.IsInstanceOfType<PagamentoViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual("Casa - Itabaiana", modelo.NomeImovel);
    }

    [TestMethod]
    public void Edit_QuandoPagamentoNaoExiste_DeveRetornarNaoEncontrado()
    {
        // Act
        var resultado = controller.Edit(999, 2);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
    }

    [TestMethod]
    public void Edit_QuandoPagamentoNaoPertenceAoInquilino_DeveRetornarNaoEncontrado()
    {
        // Act
        var resultado = controller.Edit(1, 9);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
    }

    [TestMethod]
    public void Edit_QuandoPessoaNaoInformada_DeveRetornarNaoEncontrado()
    {
        // Act
        var resultado = controller.Edit(1, 0);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
    }

    [TestMethod]
    public void Edit_QuandoServicoLancaExcecao_DeveExibirAMensagem()
    {
        // Arrange
        const string texto = "Pagamento não encontrado.";
        pagamentoServiceMock
            .Setup(service => service.Get(1))
            .Throws(new ServiceException(texto));

        // Act
        var resultado = controller.Edit(1, 2);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var pagamento = Assert.IsInstanceOfType<PagamentoViewModel>(viewResult.ViewData.Model);
        Assert.AreEqual(1u, pagamento.Id);

        var erros = controller.ModelState.Values.SelectMany(entrada => entrada.Errors).ToList();
        Assert.HasCount(1, erros);
        Assert.AreEqual(texto, erros[0].ErrorMessage);
    }

    [TestMethod]
    public void Edit_QuandoComprovanteEnviado_DeveGravarOArquivoEVoltarParaOAluguel()
    {
        // Arrange
        var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };
        var modelo = ObterModelo();

        // Act
        var resultado = controller.Edit(modelo, Arquivo(bytes), 2, true);

        // Assert
        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(resultado);
        Assert.AreEqual(nameof(AluguelController.Details), redirect.ActionName);
        Assert.AreEqual("Aluguel", redirect.ControllerName);
        Assert.AreEqual(4u, Convert.ToUInt32(redirect.RouteValues!["id"]));
        Assert.AreEqual(2u, Convert.ToUInt32(redirect.RouteValues!["idPessoa"]));

        pagamentoServiceMock.Verify(
            service => service.Edit(It.Is<Pagamento>(pagamento =>
                pagamento.Id == 1
                && pagamento.IdAluguel == 4
                && pagamento.Status == "PA"
                && pagamento.ValorPago == 200m
                && pagamento.Comprovante != null
                && pagamento.Comprovante.SequenceEqual(bytes))),
            Times.Once);
    }

    [TestMethod]
    public void Edit_QuandoNenhumArquivoNovoEEnviado_DeveManterOComprovanteNuloParaOServico()
    {
        // Act
        var resultado = controller.Edit(ObterModelo(), null, 2, false);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(resultado);
        pagamentoServiceMock.Verify(
            service => service.Edit(It.Is<Pagamento>(pagamento =>
                pagamento.Comprovante == null && pagamento.Status == "PE")),
            Times.Once);
    }

    [TestMethod]
    public void Edit_QuandoModelStateInvalido_DeveRetornarOFormularioSemGravar()
    {
        // Arrange
        controller.ModelState.AddModelError(nameof(PagamentoViewModel.ValorPago), "O campo Valor pago (R$) deve ser maior que zero");

        // Act
        var resultado = controller.Edit(ObterModelo(), null, 2, false);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var pagamento = Assert.IsInstanceOfType<PagamentoViewModel>(viewResult.ViewData.Model);
        Assert.AreEqual(200m, pagamento.ValorPago);

        pagamentoServiceMock.Verify(service => service.Edit(It.IsAny<Pagamento>()), Times.Never);
        pagamentoServiceMock.Verify(service => service.Get(It.IsAny<uint>()), Times.Never);
    }

    [TestMethod]
    public void Edit_QuandoServicoRecusaOEnvio_DeveManterOFormularioComAMensagem()
    {
        // Arrange
        const string texto = "Já existe um pagamento registrado para este mês.";
        pagamentoServiceMock
            .Setup(service => service.Edit(It.IsAny<Pagamento>()))
            .Throws(new ServiceException(texto));

        // Act
        var resultado = controller.Edit(ObterModelo(), null, 2, false);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var pagamento = Assert.IsInstanceOfType<PagamentoViewModel>(viewResult.ViewData.Model);
        Assert.AreEqual(1u, pagamento.Id);
        Assert.AreEqual("Casa — Itabaiana", pagamento.NomeImovel);

        var erros = controller.ModelState.Values.SelectMany(entrada => entrada.Errors).ToList();
        Assert.HasCount(1, erros);
        Assert.AreEqual(texto, erros[0].ErrorMessage);
    }

    [TestMethod]
    public void Edit_QuandoFormularioTrocaOAluguelEOMes_DeveManterOsDadosGravados()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.IdAluguel = 8;
        modelo.MesReferencia = new DateTime(2027, 3, 1);

        // Act
        var resultado = controller.Edit(modelo, null, 2, false);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(resultado);
        pagamentoServiceMock.Verify(
            service => service.Edit(It.Is<Pagamento>(pagamento =>
                pagamento.IdAluguel == 4
                && pagamento.MesReferencia == new DateTime(2026, 12, 1))),
            Times.Once);
    }

    [TestMethod]
    public void Edit_QuandoEnvioSemPessoaInformada_DeveRetornarNaoEncontradoSemGravar()
    {
        // Act
        var resultado = controller.Edit(ObterModelo(), null, 0, false);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
        pagamentoServiceMock.Verify(service => service.Edit(It.IsAny<Pagamento>()), Times.Never);
    }

    [TestMethod]
    public void Edit_QuandoEnvioNaoPertenceAoInquilino_DeveRetornarNaoEncontradoSemGravar()
    {
        // Act
        var resultado = controller.Edit(ObterModelo(), null, 9, false);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
        pagamentoServiceMock.Verify(service => service.Edit(It.IsAny<Pagamento>()), Times.Never);
    }

    [TestMethod]
    public void Comprovante_QuandoArquivoExiste_DeveDevolverParaVisualizacao()
    {
        // Act
        var resultado = controller.Comprovante(1, 2);

        // Assert
        var arquivo = Assert.IsInstanceOfType<FileContentResult>(resultado);
        CollectionAssert.AreEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 }, arquivo.FileContents);
        Assert.AreEqual("application/pdf", arquivo.ContentType);
        Assert.AreEqual(string.Empty, arquivo.FileDownloadName);
    }

    [TestMethod]
    public void Comprovante_QuandoNaoHaArquivo_DeveRetornarNaoEncontrado()
    {
        // Arrange
        var pagamento = ObterPagamento();
        pagamento.Comprovante = null;
        pagamentoServiceMock.Setup(service => service.Get(1)).Returns(pagamento);

        // Act
        var resultado = controller.Comprovante(1, 2);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
    }

    [TestMethod]
    public void Comprovante_QuandoPagamentoNaoPertenceAoInquilino_DeveRetornarNaoEncontrado()
    {
        // Act
        var resultado = controller.Comprovante(1, 9);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
    }

    [TestMethod]
    public void Comprovante_QuandoPessoaNaoInformada_DeveRetornarNaoEncontrado()
    {
        // Act
        var resultado = controller.Comprovante(1, 0);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
    }

    [TestMethod]
    public void Comprovante_QuandoServicoLancaExcecao_DeveRetornarNaoEncontrado()
    {
        // Arrange
        pagamentoServiceMock
            .Setup(service => service.Get(1))
            .Throws(new ServiceException("Pagamento não encontrado."));

        // Act
        var resultado = controller.Comprovante(1, 2);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
    }

    [TestMethod]
    public void Details_QuandoInquilinoAbre_DeveExibirOPagamentoSemMarcarComoDono()
    {
        // Act
        var resultado = controller.Details(1, IdInquilino);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var pagamento = Assert.IsInstanceOfType<PagamentoViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(1u, pagamento.Id);
        Assert.AreEqual(200m, pagamento.ValorPago);
        Assert.AreEqual(700m, pagamento.ValorAluguel);
        Assert.AreEqual("Casa — Itabaiana", pagamento.NomeImovel);
        Assert.IsTrue(pagamento.PossuiComprovante);
        Assert.IsFalse((bool)viewResult.ViewData["Dono"]!);
    }

    [TestMethod]
    public void Details_QuandoDonoDoImovelAbre_DeveExibirOPagamentoComoDono()
    {
        // Act
        var resultado = controller.Details(1, IdProprietario);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var pagamento = Assert.IsInstanceOfType<PagamentoViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(1u, pagamento.Id);
        Assert.IsTrue((bool)viewResult.ViewData["Dono"]!);
        Assert.AreEqual(IdProprietario, viewResult.ViewData[AluguelController.ChaveIdPessoa]);
    }

    [TestMethod]
    public void Details_QuandoPagamentoNaoExiste_DeveRetornarNaoEncontrado()
    {
        // Act
        var resultado = controller.Details(999, IdProprietario);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
    }

    [TestMethod]
    public void Details_QuandoPessoaNaoEInquilinaNemDona_DeveRetornarNaoEncontrado()
    {
        // Act
        var resultado = controller.Details(1, 9);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
        aluguelServiceMock.Verify(service => service.GetDoProprietario(4, 9), Times.Once);
    }

    [TestMethod]
    public void Details_QuandoServicoLancaExcecao_DeveExibirAMensagem()
    {
        // Arrange
        const string mensagem = "Pagamento não encontrado.";
        pagamentoServiceMock
            .Setup(service => service.Get(1))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.Details(1, IdProprietario);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var pagamento = Assert.IsInstanceOfType<PagamentoViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(1u, pagamento.Id);
        Assert.AreEqual(mensagem, ObterMensagemDeErro());
    }

    [TestMethod]
    public void Create_QuandoDonoAbreUmMes_DeveRetornarFormularioComOMesEOValorDoAluguel()
    {
        // Act
        var resultado = controller.Create(4, new DateTime(2027, 1, 15), IdProprietario);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var pagamento = Assert.IsInstanceOfType<PagamentoViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(0u, pagamento.Id);
        Assert.AreEqual(4u, pagamento.IdAluguel);
        Assert.AreEqual(new DateTime(2027, 1, 1), pagamento.MesReferencia);
        Assert.AreEqual(700m, pagamento.ValorAluguel);
        Assert.AreEqual("Casa — Itabaiana", pagamento.NomeImovel);
        Assert.AreEqual("PE", pagamento.Status);
        Assert.AreEqual(IdProprietario, viewResult.ViewData[AluguelController.ChaveIdPessoa]);
    }

    [TestMethod]
    public void Create_QuandoMesNaoInformado_DeveUsarOMesAtual()
    {
        // Act
        var resultado = controller.Create(4, default, IdProprietario);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var pagamento = Assert.IsInstanceOfType<PagamentoViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), pagamento.MesReferencia);
    }

    [TestMethod]
    public void Create_QuandoMesJaTemPagamento_DeveAbrirAEdicaoDoPagamento()
    {
        // Arrange
        pagamentoServiceMock
            .Setup(service => service.GetByAluguel(4))
            .Returns(new List<Pagamento> { ObterPagamento() });

        // Act
        var resultado = controller.Create(4, new DateTime(2026, 12, 20), IdProprietario);

        // Assert
        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(resultado);
        Assert.AreEqual(nameof(PagamentoController.Edit), redirect.ActionName);
        Assert.AreEqual(1u, Convert.ToUInt32(redirect.RouteValues!["id"]));
        Assert.AreEqual(IdProprietario, Convert.ToUInt32(redirect.RouteValues!["idPessoa"]));
    }

    [TestMethod]
    public void Create_QuandoPessoaNaoEDonaDoImovel_DeveRetornarNaoEncontrado()
    {
        // Act
        var resultado = controller.Create(4, new DateTime(2027, 1, 1), IdInquilino);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
    }

    [TestMethod]
    public void CreatePost_QuandoDadosValidos_DeveRegistrarEVoltarParaOAluguel()
    {
        // Arrange
        var modelo = ObterModeloNovo();

        // Act
        var resultado = controller.Create(modelo, null, IdProprietario, false);

        // Assert
        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(resultado);
        Assert.AreEqual(nameof(AluguelController.Details), redirect.ActionName);
        Assert.AreEqual("Aluguel", redirect.ControllerName);
        Assert.AreEqual(4u, Convert.ToUInt32(redirect.RouteValues!["id"]));
        Assert.AreEqual(IdProprietario, Convert.ToUInt32(redirect.RouteValues!["idPessoa"]));

        pagamentoServiceMock.Verify(
            service => service.Create(It.Is<Pagamento>(pagamento =>
                pagamento.Id == 0
                && pagamento.IdAluguel == 4
                && pagamento.MesReferencia == new DateTime(2027, 1, 1)
                && pagamento.ValorPago == 300m
                && pagamento.Status == "PE"
                && pagamento.Comprovante == null)),
            Times.Once);
    }

    [TestMethod]
    public void CreatePost_QuandoMarcadoComoPago_DeveEnviarASituacaoPaga()
    {
        // Arrange
        var modelo = ObterModeloNovo();
        modelo.ValorPago = 700m;

        // Act
        controller.Create(modelo, null, IdProprietario, true);

        // Assert
        pagamentoServiceMock.Verify(
            service => service.Create(It.Is<Pagamento>(pagamento => pagamento.Status == "PA")),
            Times.Once);
    }

    [TestMethod]
    public void CreatePost_QuandoEnviaComprovante_DeveGravarOArquivo()
    {
        // Arrange
        var arquivo = Arquivo([0x25, 0x50, 0x44, 0x46, 0x2D]);

        // Act
        controller.Create(ObterModeloNovo(), arquivo, IdProprietario, false);

        // Assert
        pagamentoServiceMock.Verify(
            service => service.Create(It.Is<Pagamento>(pagamento =>
                pagamento.Comprovante != null && pagamento.Comprovante.Length == 5)),
            Times.Once);
    }

    [TestMethod]
    public void CreatePost_QuandoModelStateInvalido_DeveRetornarOFormularioSemGravar()
    {
        // Arrange
        controller.ModelState.AddModelError(nameof(PagamentoViewModel.ValorPago), "Valor inválido");

        // Act
        var resultado = controller.Create(ObterModeloNovo(), null, IdProprietario, false);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        Assert.IsInstanceOfType<PagamentoViewModel>(viewResult.ViewData.Model);
        pagamentoServiceMock.Verify(service => service.Create(It.IsAny<Pagamento>()), Times.Never);
    }

    [TestMethod]
    public void CreatePost_QuandoServicoRecusa_DeveManterOFormularioComAMensagem()
    {
        // Arrange
        const string mensagem = "Já existe um pagamento registrado para este mês.";
        pagamentoServiceMock
            .Setup(service => service.Create(It.IsAny<Pagamento>()))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.Create(ObterModeloNovo(), null, IdProprietario, false);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var pagamento = Assert.IsInstanceOfType<PagamentoViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(new DateTime(2027, 1, 1), pagamento.MesReferencia);
        Assert.AreEqual(mensagem, ObterMensagemDeErro());
    }

    [TestMethod]
    public void CreatePost_QuandoPessoaNaoEDonaDoImovel_DeveRetornarNaoEncontradoSemGravar()
    {
        // Act
        var resultado = controller.Create(ObterModeloNovo(), null, IdInquilino, false);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
        pagamentoServiceMock.Verify(service => service.Create(It.IsAny<Pagamento>()), Times.Never);
    }

    [TestMethod]
    public void Edit_QuandoDonoDoImovelAbre_DeveAbrirOFormulario()
    {
        // Act
        var resultado = controller.Edit(1, IdProprietario);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var pagamento = Assert.IsInstanceOfType<PagamentoViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(1u, pagamento.Id);
        Assert.AreEqual(4u, pagamento.IdAluguel);
    }

    [TestMethod]
    public void EditPost_QuandoDonoDoImovelAltera_DeveGravarEVoltarParaOAluguel()
    {
        // Arrange
        var modelo = ObterModelo();
        modelo.ValorPago = 700m;

        // Act
        var resultado = controller.Edit(modelo, null, IdProprietario, true);

        // Assert
        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(resultado);
        Assert.AreEqual(nameof(AluguelController.Details), redirect.ActionName);
        Assert.AreEqual(IdProprietario, Convert.ToUInt32(redirect.RouteValues!["idPessoa"]));

        pagamentoServiceMock.Verify(
            service => service.Edit(It.Is<Pagamento>(pagamento =>
                pagamento.Id == 1 && pagamento.ValorPago == 700m && pagamento.Status == "PA")),
            Times.Once);
    }

    [TestMethod]
    public void Comprovante_QuandoDonoDoImovelPede_DeveDevolverOArquivo()
    {
        // Act
        var resultado = controller.Comprovante(1, IdProprietario);

        // Assert
        var arquivo = Assert.IsInstanceOfType<FileContentResult>(resultado);
        Assert.AreEqual("application/pdf", arquivo.ContentType);
    }

    private string ObterMensagemDeErro()
    {
        var erros = controller.ModelState.Values.SelectMany(entrada => entrada.Errors).ToList();
        Assert.HasCount(1, erros);
        return erros[0].ErrorMessage;
    }

    private static PagamentoViewModel ObterModeloNovo()
    {
        return new PagamentoViewModel
        {
            MesReferencia = new DateTime(2027, 1, 1),
            ValorPago = 300m,
            DataPagamento = new DateTime(2027, 1, 4),
            Status = "PE",
            IdAluguel = 4,
            ValorAluguel = 700m,
            NomeImovel = "Casa — Itabaiana"
        };
    }

    private static Aluguel ObterAluguel()
    {
        return ObterPagamento().IdAluguelNavigation;
    }

    private static PagamentoViewModel ObterModelo()
    {
        return new PagamentoViewModel
        {
            Id = 1,
            MesReferencia = new DateTime(2026, 12, 1),
            ValorPago = 200m,
            DataPagamento = new DateTime(2026, 12, 4),
            Status = "PE",
            IdAluguel = 4,
            ValorAluguel = 700m,
            NomeImovel = "Casa — Itabaiana"
        };
    }

    private static Pagamento ObterPagamento()
    {
        return new Pagamento
        {
            Id = 1,
            MesReferencia = new DateTime(2026, 12, 1),
            ValorPago = 200m,
            DataPagamento = new DateTime(2026, 12, 3),
            Comprovante = [0x25, 0x50, 0x44, 0x46],
            Status = "PE",
            IdAluguel = 4,
            IdAluguelNavigation = new Aluguel
            {
                Id = 4,
                Valor = 700m,
                DiaPagamento = 5,
                Status = "A",
                IdImovel = 1,
                IdPessoaInquilino = 2,
                IdImovelNavigation = new Imovel
                {
                    Id = 1,
                    Nome = "Casa",
                    Tipo = "C",
                    Rua = "Rua do Rotary, 120",
                    Bairro = "Rotary Club",
                    Cep = "49500000",
                    Cidade = "Itabaiana",
                    Estado = "SE",
                    Valor = 700m,
                    Finalidade = "A",
                    Status = "A",
                    StatusValidacao = "A",
                    IdPessoaProprietario = 3
                }
            }
        };
    }

    private static IFormFile Arquivo(byte[] bytes)
    {
        var arquivo = new Mock<IFormFile>();
        arquivo.Setup(item => item.Length).Returns(bytes.Length);
        arquivo.Setup(item => item.CopyTo(It.IsAny<Stream>()))
            .Callback<Stream>(destino => destino.Write(bytes, 0, bytes.Length));
        return arquivo.Object;
    }
}
