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
    private Mock<IPagamentoService> pagamentoServiceMock = null!;
    private Mock<IImovelService> imovelServiceMock = null!;
    private PagamentoController controller = null!;

    [TestInitialize]
    public void Inicializar()
    {
        pagamentoServiceMock = new Mock<IPagamentoService>();
        imovelServiceMock = new Mock<IImovelService>();

        pagamentoServiceMock
            .Setup(service => service.Get(1))
            .Returns(ObterPagamento());

        pagamentoServiceMock
            .Setup(service => service.Get(999))
            .Returns((Pagamento?)null);

        controller = new PagamentoController(pagamentoServiceMock.Object, imovelServiceMock.Object);
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
    public void Edit_QuandoComprovanteEnviado_DeveGravarOArquivoEVoltarParaOsAlugueis()
    {
        // Arrange
        var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };
        var modelo = ObterModelo();

        // Act
        var resultado = controller.Edit(modelo, Arquivo(bytes), 2, true);

        // Assert
        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(resultado);
        Assert.AreEqual(nameof(AluguelController.Index), redirect.ActionName);
        Assert.AreEqual("Aluguel", redirect.ControllerName);
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
        var resultado = controller.Comprovante(1);

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
        var resultado = controller.Comprovante(1);

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
        var resultado = controller.Comprovante(1);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
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
