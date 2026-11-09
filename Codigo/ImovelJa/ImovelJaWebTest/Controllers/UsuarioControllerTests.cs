using Core;
using Core.Service;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace ImovelJaWeb.Controllers.Tests;

[TestClass]
public class UsuarioControllerTests
{
    private Mock<IPessoaService> pessoaServiceMock = null!;
    private Mock<IImovelService> imovelServiceMock = null!;
    private UsuarioController controller = null!;

    [TestInitialize]
    public void Inicializar()
    {
        pessoaServiceMock = new Mock<IPessoaService>();
        imovelServiceMock = new Mock<IImovelService>();

        pessoaServiceMock
            .Setup(service => service.GetAll())
            .Returns(ObterPessoasDeTeste());

        pessoaServiceMock
            .Setup(service => service.Get(1))
            .Returns(ObterPessoaAlvo());

        pessoaServiceMock
            .Setup(service => service.Get(999))
            .Returns((Pessoa?)null);

        imovelServiceMock
            .Setup(service => service.GetByProprietario(1))
            .Returns(ObterImoveisDoProprietarioDeTeste());

        controller = new UsuarioController(
            pessoaServiceMock.Object,
            imovelServiceMock.Object)
        {
            TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>())
        };
    }

    [TestMethod]
    public void Get_QuandoExistemPessoas_DeveRetornarViewComTodasAsPessoas()
    {
        // Act
        var resultado = controller.Index();

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var pessoas = Assert.IsInstanceOfType<IEnumerable<PessoaViewModel>>(
            viewResult.ViewData.Model);

        Assert.HasCount(2, pessoas);

        var primeiraPessoa = pessoas.First();

        Assert.AreEqual(1u, primeiraPessoa.Id);
        Assert.AreEqual("Maria José Santos", primeiraPessoa.Nome);
        Assert.AreEqual("F", primeiraPessoa.Tipo);
        Assert.AreEqual("A", primeiraPessoa.Status);

        pessoaServiceMock.Verify(
            service => service.GetAll(),
            Times.Once);
    }

    [TestMethod]
    public void Get_QuandoNaoExistemPessoas_DeveRetornarViewComColecaoVazia()
    {
        // Arrange
        pessoaServiceMock
            .Setup(service => service.GetAll())
            .Returns(Array.Empty<Pessoa>());

        // Act
        var resultado = controller.Index();

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var pessoas = Assert.IsInstanceOfType<IEnumerable<PessoaViewModel>>(
            viewResult.ViewData.Model);

        Assert.IsEmpty(pessoas);

        pessoaServiceMock.Verify(
            service => service.GetAll(),
            Times.Once);
    }

    [TestMethod]
    public void Details_QuandoPessoaExiste_DeveRetornarViewComPessoaEImoveis()
    {
        // Act
        var resultado = controller.Details(1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var pessoa = Assert.IsInstanceOfType<PessoaViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(1u, pessoa.Id);
        Assert.AreEqual("Maria José Santos", pessoa.Nome);
        Assert.AreEqual("52998224725", pessoa.Cpf);

        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(
            viewResult.ViewData["imoveis"]);

        Assert.HasCount(2, imoveis);
        Assert.AreEqual("Casa com 3 quartos no Centro", imoveis.First().Nome);

        pessoaServiceMock.Verify(
            service => service.Get(1),
            Times.Once);
        imovelServiceMock.Verify(
            service => service.GetByProprietario(1),
            Times.Once);
    }

    [TestMethod]
    public void Details_QuandoPessoaNaoPossuiImoveis_DeveRetornarViewComListaVazia()
    {
        // Arrange
        imovelServiceMock
            .Setup(service => service.GetByProprietario(1))
            .Returns(Array.Empty<Imovel>());

        // Act
        var resultado = controller.Details(1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var imoveis = Assert.IsInstanceOfType<IEnumerable<ImovelViewModel>>(
            viewResult.ViewData["imoveis"]);

        Assert.IsEmpty(imoveis);
    }

    [TestMethod]
    public void Details_QuandoPessoaNaoExiste_DeveRetornarNotFound()
    {
        // Act
        var resultado = controller.Details(999);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(resultado);

        pessoaServiceMock.Verify(
            service => service.Get(999),
            Times.Once);
        imovelServiceMock.Verify(
            service => service.GetByProprietario(It.IsAny<uint>()),
            Times.Never);
    }

    [TestMethod]
    public void Block_QuandoPessoaExiste_DeveBloquearContaERedirecionarParaLista()
    {
        // Act
        var resultado = controller.Activate(1,false);

        // Assert
        VerificarRedirecionamentoParaLista(resultado);

        pessoaServiceMock.Verify(
            service => service.Activate(1, false),
            Times.Once);

        Assert.AreEqual("Usuário bloqueado com sucesso.", controller.TempData["Mensagem"]);
        Assert.IsNull(controller.TempData["Erro"]);
    }

    [TestMethod]
    public void Block_QuandoPessoaNaoExiste_DeveRedirecionarComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "Pessoa não encontrada.";

        pessoaServiceMock
            .Setup(service => service.Activate(999, false))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.Activate(999, false);

        // Assert
        VerificarRedirecionamentoParaLista(resultado);

        Assert.AreEqual(mensagem, controller.TempData["Erro"]);
        Assert.IsNull(controller.TempData["Mensagem"]);
    }

    [TestMethod]
    public void Activate_QuandoPessoaExiste_DeveAtivarContaERedirecionarParaLista()
    {
        // Act
        var resultado = controller.Activate(2,true);

        // Assert
        VerificarRedirecionamentoParaLista(resultado);

        pessoaServiceMock.Verify(
            service => service.Activate(2, true),
            Times.Once);

        Assert.AreEqual("Usuário ativado com sucesso.", controller.TempData["Mensagem"]);
        Assert.IsNull(controller.TempData["Erro"]);
    }

    [TestMethod]
    public void Activate_QuandoPessoaNaoExiste_DeveRedirecionarComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "Pessoa não encontrada.";

        pessoaServiceMock
            .Setup(service => service.Activate(999, true))
            .Throws(new ServiceException(mensagem));

        // Act
        var resultado = controller.Activate(999, true);

        // Assert
        VerificarRedirecionamentoParaLista(resultado);

        Assert.AreEqual(mensagem, controller.TempData["Erro"]);
        Assert.IsNull(controller.TempData["Mensagem"]);
    }

    private static void VerificarRedirecionamentoParaLista(ActionResult resultado)
    {
        var redirectResult =
            Assert.IsInstanceOfType<RedirectToActionResult>(resultado);

        Assert.IsNull(redirectResult.ControllerName);
        Assert.AreEqual(
            nameof(UsuarioController.Index),
            redirectResult.ActionName);
    }

    private static Pessoa ObterPessoaAlvo()
    {
        return new Pessoa
        {
            Id = 1,
            Tipo = "F",
            Nome = "Maria José Santos",
            Email = "maria.santos@gmail.com",
            Telefone = "79998123456",
            Cpf = "52998224725",
            DataNascimento = new DateTime(1985, 3, 14),
            Status = "A"
        };
    }

    private static IEnumerable<Pessoa> ObterPessoasDeTeste()
    {
        return
        [
            ObterPessoaAlvo(),
            new Pessoa
            {
                Id = 2,
                Tipo = "J",
                Nome = "Padaria Boa Massa Ltda",
                Email = "padaria@gmail.com",
                Telefone = "7932221111",
                Cnpj = "45723174000110",
                Status = "B"
            }
        ];
    }

    private static IEnumerable<Imovel> ObterImoveisDoProprietarioDeTeste()
    {
        return
        [
            new Imovel
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
                StatusValidacao = "A",
                IdPessoaProprietario = 1
            },
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
                StatusValidacao = "A",
                IdPessoaProprietario = 1
            }
        ];
    }
}