using Core;
using Core.Service;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ImovelJaWeb.Controllers.Tests;

[TestClass]
public class AluguelControllerTests
{
    private Mock<IAluguelService> aluguelServiceMock = null!;
    private Mock<IPagamentoService> pagamentoServiceMock = null!;
    private Mock<IImovelService> imovelServiceMock = null!;
    private Mock<IPessoaService> pessoaServiceMock = null!;
    private AluguelController controller = null!;

    [TestInitialize]
    public void Inicializar()
    {
        aluguelServiceMock = new Mock<IAluguelService>();
        pagamentoServiceMock = new Mock<IPagamentoService>();
        imovelServiceMock = new Mock<IImovelService>();
        pessoaServiceMock = new Mock<IPessoaService>();

        aluguelServiceMock
            .Setup(service => service.GetByInquilino(2))
            .Returns(new List<Aluguel> { ObterAluguel() });

        aluguelServiceMock
            .Setup(service => service.GetDoInquilino(1, 2))
            .Returns(ObterAluguel());

        aluguelServiceMock
            .Setup(service => service.GetDoInquilino(999, 2))
            .Returns((Aluguel?)null);

        pagamentoServiceMock
            .Setup(service => service.GetByAluguel(1))
            .Returns(ObterPagamentos());

        pessoaServiceMock
            .Setup(service => service.Get(2))
            .Returns(new Pessoa { Id = 2, Tipo = "F", Nome = "João da Conceição", Status = "A" });

        controller = new AluguelController(
            aluguelServiceMock.Object,
            pagamentoServiceMock.Object,
            imovelServiceMock.Object,
            pessoaServiceMock.Object);
    }

    [TestMethod]
    public void Index_QuandoInquilinoPossuiAluguel_DeveListarComOsDadosDoImovel()
    {
        // Act
        var resultado = controller.Index(2);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var alugueis = Assert.IsInstanceOfType<List<AluguelViewModel>>(viewResult.ViewData.Model);

        Assert.HasCount(1, alugueis);
        Assert.AreEqual(1u, alugueis[0].Id);
        Assert.AreEqual(2u, alugueis[0].IdPessoaInquilino);
        Assert.AreEqual(700m, alugueis[0].Valor);
        var imovel = alugueis[0].Imovel;
        Assert.IsNotNull(imovel);
        Assert.AreEqual("Casa - Itabaiana", imovel.Nome);
        Assert.AreEqual("Rotary Club", imovel.Bairro);
        Assert.AreEqual("Itabaiana", imovel.Cidade);
        Assert.AreEqual(2u, viewResult.ViewData[AluguelController.ChaveIdPessoa]);
        Assert.AreEqual("João da Conceição", viewResult.ViewData["NomeUsuario"]);

        aluguelServiceMock.Verify(service => service.GetByInquilino(2), Times.Once);
        imovelServiceMock.Verify(service => service.Get(It.IsAny<uint>()), Times.Never);
    }

    [TestMethod]
    public void Index_QuandoOAluguelNaoTrazOImovel_DeveBuscarOImovelPeloServico()
    {
        // Arrange
        var aluguel = ObterAluguel();
        aluguel.IdImovelNavigation = null!;
        aluguelServiceMock
            .Setup(service => service.GetByInquilino(2))
            .Returns(new List<Aluguel> { aluguel });
        imovelServiceMock
            .Setup(service => service.Get(1))
            .Returns(ObterImovel());

        // Act
        var resultado = controller.Index(2);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var alugueis = Assert.IsInstanceOfType<List<AluguelViewModel>>(viewResult.ViewData.Model);

        var imovel = alugueis[0].Imovel;
        Assert.IsNotNull(imovel);
        Assert.AreEqual("Casa - Itabaiana", imovel.Nome);
        imovelServiceMock.Verify(service => service.Get(1), Times.Once);
    }

    [TestMethod]
    public void Details_QuandoAluguelPertenceAoInquilino_DeveExibirImovelInquilinoEHistorico()
    {
        // Act
        var resultado = controller.Details(1, 2);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var aluguel = Assert.IsInstanceOfType<AluguelViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(1u, aluguel.Id);
        Assert.AreEqual(700m, aluguel.Valor);
        Assert.IsTrue(aluguel.PossuiContrato);
        var imovel = aluguel.Imovel;
        var inquilino = aluguel.Inquilino;
        Assert.IsNotNull(imovel);
        Assert.AreEqual("Casa - Itabaiana", imovel.Nome);
        Assert.IsNotNull(inquilino);
        Assert.AreEqual("João da Conceição", inquilino.Nome);
        Assert.HasCount(2, aluguel.Pagamentos);
        Assert.AreEqual(new DateTime(2026, 11, 1), aluguel.Pagamentos[0].MesReferencia);
        Assert.AreEqual("PE", aluguel.Pagamentos[0].Status);
        Assert.IsTrue(aluguel.Pagamentos[1].PossuiComprovante);
        Assert.IsNull(aluguel.Pagamentos[1].Comprovante);

        pagamentoServiceMock.Verify(service => service.GetByAluguel(1), Times.Once);
    }

    [TestMethod]
    public void Details_QuandoAluguelNaoPertenceAoInquilino_DeveRetornarNaoEncontrado()
    {
        // Act
        var resultado = controller.Details(1, 9);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
        aluguelServiceMock.Verify(service => service.GetDoInquilino(1, 9), Times.Once);
        pagamentoServiceMock.Verify(service => service.GetByAluguel(It.IsAny<uint>()), Times.Never);
    }

    [TestMethod]
    public void Details_QuandoAluguelNaoExiste_DeveRetornarNaoEncontrado()
    {
        // Act
        var resultado = controller.Details(999, 2);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
    }

    [TestMethod]
    public void Details_QuandoServicoLancaExcecao_DeveExibirAMensagem()
    {
        // Arrange
        const string texto = "Aluguel não encontrado.";
        aluguelServiceMock
            .Setup(service => service.GetDoInquilino(1, 2))
            .Throws(new ServiceException(texto));

        // Act
        var resultado = controller.Details(1, 2);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var aluguel = Assert.IsInstanceOfType<AluguelViewModel>(viewResult.ViewData.Model);

        Assert.AreEqual(1u, aluguel.Id);
        Assert.AreEqual(2u, aluguel.IdPessoaInquilino);
        var erros = controller.ModelState.Values.SelectMany(entrada => entrada.Errors).ToList();
        Assert.HasCount(1, erros);
        Assert.AreEqual(texto, erros[0].ErrorMessage);
    }

    [TestMethod]
    public void Contrato_QuandoArquivoPertenceAoInquilino_DeveDevolverOPdf()
    {
        // Act
        var resultado = controller.Contrato(1, 2);

        // Assert
        var arquivo = Assert.IsInstanceOfType<FileContentResult>(resultado);
        CollectionAssert.AreEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 }, arquivo.FileContents);
        Assert.AreEqual("application/pdf", arquivo.ContentType);
        Assert.AreEqual("contrato.pdf", arquivo.FileDownloadName);
    }

    [TestMethod]
    public void Contrato_QuandoAluguelNaoPertenceAoInquilino_DeveRetornarNaoEncontrado()
    {
        // Act
        var resultado = controller.Contrato(1, 9);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
        aluguelServiceMock.Verify(service => service.GetDoInquilino(1, 9), Times.Once);
    }

    [TestMethod]
    public void Contrato_QuandoNaoHaArquivo_DeveRetornarNaoEncontrado()
    {
        // Arrange
        var aluguel = ObterAluguel();
        aluguel.ArquivoContrato = null;
        aluguelServiceMock.Setup(service => service.GetDoInquilino(1, 2)).Returns(aluguel);

        // Act
        var resultado = controller.Contrato(1, 2);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
    }

    [TestMethod]
    public void Contrato_QuandoServicoLancaExcecao_DeveRetornarNaoEncontrado()
    {
        // Arrange
        aluguelServiceMock
            .Setup(service => service.GetDoInquilino(1, 2))
            .Throws(new ServiceException("Aluguel não encontrado."));

        // Act
        var resultado = controller.Contrato(1, 2);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);
    }

    private static Aluguel ObterAluguel()
    {
        return new Aluguel
        {
            Id = 1,
            DiaPagamento = 5,
            DataVencimentoProximo = new DateTime(2026, 12, 5),
            Valor = 700m,
            Status = "A",
            IdImovel = 1,
            IdPessoaInquilino = 2,
            ArquivoContrato = [0x25, 0x50, 0x44, 0x46],
            IdImovelNavigation = ObterImovel(),
            IdPessoaInquilinoNavigation = new Pessoa
            {
                Id = 2,
                Tipo = "F",
                Nome = "João da Conceição",
                Cpf = "39053344705",
                Telefone = "79998887766",
                Status = "A"
            }
        };
    }

    private static Imovel ObterImovel()
    {
        return new Imovel
        {
            Id = 1,
            Nome = "Casa - Itabaiana",
            Descricao = "Casa térrea próxima ao Rotary Club.",
            Tipo = "C",
            AreaTotal = 38m,
            QuantidadeBanheiros = 1,
            QuantidadeQuartos = 1,
            Rua = "Rua do Rotary, 120",
            Bairro = "Rotary Club",
            Cep = "49500000",
            Cidade = "Itabaiana",
            Estado = "SE",
            Valor = 700m,
            Finalidade = "A",
            Status = "A",
            StatusValidacao = "A",
            IdPessoaProprietario = 1
        };
    }

    private static List<Pagamento> ObterPagamentos()
    {
        return
        [
            new Pagamento
            {
                Id = 10,
                MesReferencia = new DateTime(2026, 11, 1),
                Status = "PE",
                IdAluguel = 1
            },
            new Pagamento
            {
                Id = 11,
                MesReferencia = new DateTime(2026, 10, 1),
                Status = "PA",
                ValorPago = 700m,
                DataPagamento = new DateTime(2026, 10, 5),
                Comprovante = [1, 2, 3],
                IdAluguel = 1
            }
        ];
    }
}
