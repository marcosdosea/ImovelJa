using Core;
using Core.Service;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ImovelJaWeb.Controllers.Tests;

[TestClass]
public class MensagemControllerTests
{
    private Mock<IMensagemService> mensagemServiceMock = null!;
    private Mock<IImovelService> imovelServiceMock = null!;
    private Mock<IPessoaService> pessoaServiceMock = null!;
    private MensagemController controller = null!;

    [TestInitialize]
    public void Inicializar()
    {
        mensagemServiceMock = new Mock<IMensagemService>();
        imovelServiceMock = new Mock<IImovelService>();
        pessoaServiceMock = new Mock<IPessoaService>();

        imovelServiceMock
            .Setup(service => service.Get(1))
            .Returns(ObterImovel());

        pessoaServiceMock
            .Setup(service => service.GetAll())
            .Returns(ObterPessoas());

        mensagemServiceMock
            .Setup(service => service.GetByPessoa(It.IsAny<uint>()))
            .Returns(ObterMensagens());

        controller = new MensagemController(
            mensagemServiceMock.Object,
            imovelServiceMock.Object,
            pessoaServiceMock.Object);
    }

    [TestMethod]
    public void Conversa_QuandoAbrePeloAnuncio_DeveDiferenciarMensagensEnviadasERecebidas()
    {
        // Act
        var resultado = controller.Conversa(1, null, 2);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var conversa = Assert.IsInstanceOfType<ConversaViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(1u, conversa.IdImovel);
        Assert.AreEqual("Apartamento 2 Quartos – Centro", conversa.NomeImovel);
        Assert.AreEqual(1u, conversa.IdDestinatario);
        Assert.AreEqual("Maria José Santos", conversa.NomeDestinatario);
        Assert.IsTrue(conversa.ConversaAberta);
        Assert.HasCount(2, conversa.Mensagens);
        Assert.IsTrue(conversa.Mensagens.First().Enviada);
        Assert.AreEqual(
            "Olá! Vi seu anúncio do apartamento. Ainda está disponível?",
            conversa.Mensagens.First().Texto);
        Assert.IsFalse(conversa.Mensagens.Last().Enviada);
        Assert.IsTrue(conversa.Mensagens.First().DataEnvio < conversa.Mensagens.Last().DataEnvio);

        var item = conversa.Conversas.Single();
        Assert.AreEqual("Maria José Santos", item.Nome);
        Assert.AreEqual("Apartamento 2 Quartos – Centro", item.NomeImovel);
        Assert.IsTrue(item.Ativa);
    }

    [TestMethod]
    public void Conversa_QuandoImovelNaoExiste_DeveAbrirAListaSemConversaAtiva()
    {
        // Act
        var resultado = controller.Conversa(999, null, 2);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var conversa = Assert.IsInstanceOfType<ConversaViewModel>(
            viewResult.ViewData.Model);

        Assert.IsNull(conversa.IdImovel);
        Assert.IsFalse(conversa.ConversaAberta);
        Assert.AreEqual(0u, conversa.IdDestinatario);
        Assert.HasCount(1, conversa.Conversas);
        Assert.IsFalse(conversa.Conversas.Single().Ativa);
    }

    [TestMethod]
    public void Conversa_QuandoProprietarioAbreOProprioAnuncio_NaoDeveConversarConsigo()
    {
        // Act
        var resultado = controller.Conversa(1, null, 1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var conversa = Assert.IsInstanceOfType<ConversaViewModel>(
            viewResult.ViewData.Model);

        Assert.IsFalse(conversa.ConversaAberta);
        Assert.AreEqual(0u, conversa.IdDestinatario);
        Assert.AreEqual(2u, conversa.Conversas.Single().IdPessoa);
    }

    [TestMethod]
    public void Create_QuandoMensagemValida_DeveGravarERedirecionarParaConversa()
    {
        // Arrange
        mensagemServiceMock
            .Setup(service => service.Create(It.IsAny<Mensagem>()))
            .Returns(10u);

        var modelo = new MensagemViewModel
        {
            Texto = "O imóvel ainda está disponível?",
            IdPessoaEnviou = 2,
            IdPessoaRecebeu = 1
        };

        // Act
        var resultado = controller.Create(modelo, 1);

        // Assert
        var redirectResult = Assert.IsInstanceOfType<RedirectToActionResult>(resultado);

        Assert.AreEqual(nameof(MensagemController.Conversa), redirectResult.ActionName);
        Assert.AreEqual(1u, Convert.ToUInt32(redirectResult.RouteValues!["idImovel"]));
        Assert.AreEqual(1u, Convert.ToUInt32(redirectResult.RouteValues["idDestinatario"]));
        Assert.AreEqual(2u, Convert.ToUInt32(redirectResult.RouteValues["idPessoa"]));

        mensagemServiceMock.Verify(
            service => service.Create(It.Is<Mensagem>(mensagem =>
                mensagem.Texto == "O imóvel ainda está disponível?"
                && mensagem.IdPessoaEnviou == 2
                && mensagem.IdPessoaRecebeu == 1)),
            Times.Once);
    }

    [TestMethod]
    public void Create_QuandoMensagemEmBranco_DeveManterNaConversaSemGravar()
    {
        // Arrange
        var modelo = new MensagemViewModel
        {
            Texto = "   ",
            IdPessoaEnviou = 2,
            IdPessoaRecebeu = 1
        };

        // Act
        var resultado = controller.Create(modelo, 1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var conversa = Assert.IsInstanceOfType<ConversaViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(nameof(MensagemController.Conversa), viewResult.ViewName);
        Assert.AreEqual("   ", conversa.NovaMensagem.Texto);

        var erros = controller.ModelState.Values
            .SelectMany(entrada => entrada.Errors)
            .ToList();

        Assert.HasCount(1, erros);
        Assert.AreEqual("A mensagem não pode estar em branco.", erros[0].ErrorMessage);

        mensagemServiceMock.Verify(
            service => service.Create(It.IsAny<Mensagem>()),
            Times.Never);
    }

    [TestMethod]
    public void Create_QuandoServicoRecusa_DeveManterNaConversaComMensagemDeErro()
    {
        // Arrange
        const string mensagem = "O destinatário deve ser diferente do remetente.";

        mensagemServiceMock
            .Setup(service => service.Create(It.IsAny<Mensagem>()))
            .Throws(new ServiceException(mensagem));

        var modelo = new MensagemViewModel
        {
            Texto = "Posso visitar amanhã?",
            IdPessoaEnviou = 2,
            IdPessoaRecebeu = 1
        };

        // Act
        var resultado = controller.Create(modelo, 1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var conversa = Assert.IsInstanceOfType<ConversaViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(nameof(MensagemController.Conversa), viewResult.ViewName);
        Assert.AreEqual("Posso visitar amanhã?", conversa.NovaMensagem.Texto);
        Assert.AreEqual("Maria José Santos", conversa.NomeDestinatario);

        var erros = controller.ModelState.Values
            .SelectMany(entrada => entrada.Errors)
            .ToList();

        Assert.HasCount(1, erros);
        Assert.AreEqual(mensagem, erros[0].ErrorMessage);
    }

    private static Imovel ObterImovel()
    {
        return new Imovel
        {
            Id = 1,
            Nome = "Apartamento 2 Quartos – Centro",
            Descricao = "Apartamento arejado, próximo ao metrô.",
            Tipo = "A",
            AreaTotal = 65m,
            QuantidadeBanheiros = 1,
            QuantidadeQuartos = 2,
            QuantidadeVagasGaragem = 1,
            Rua = "Rua Direita, 100",
            Bairro = "Centro",
            Cep = "01000000",
            Cidade = "São Paulo",
            Estado = "SP",
            Valor = 320000m,
            Finalidade = "V",
            Status = "D",
            StatusValidacao = "A",
            IdPessoaProprietario = 1
        };
    }

    private static IEnumerable<Pessoa> ObterPessoas()
    {
        return
        [
            new Pessoa
            {
                Id = 1,
                Tipo = "F",
                Nome = "Maria José Santos",
                Email = "maria.santos@gmail.com",
                Status = "A"
            },
            new Pessoa
            {
                Id = 2,
                Tipo = "F",
                Nome = "João Pedro Almeida",
                Email = "joao.almeida@gmail.com",
                Status = "A"
            }
        ];
    }

    private static IEnumerable<Mensagem> ObterMensagens()
    {
        return
        [
            new Mensagem
            {
                Id = 2,
                Texto = "Oi, João, obrigado pelo interesse! Sim, ainda está disponível.",
                DataEnvio = new DateTime(2026, 11, 9, 14, 15, 0),
                IdPessoaEnviou = 1,
                IdPessoaRecebeu = 2
            },
            new Mensagem
            {
                Id = 1,
                Texto = "Olá! Vi seu anúncio do apartamento. Ainda está disponível?",
                DataEnvio = new DateTime(2026, 11, 9, 14, 10, 0),
                IdPessoaEnviou = 2,
                IdPessoaRecebeu = 1
            }
        ];
    }
}
