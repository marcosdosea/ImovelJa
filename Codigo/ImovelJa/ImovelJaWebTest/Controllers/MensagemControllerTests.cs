using System.ComponentModel.DataAnnotations;
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

        mensagemServiceMock
            .Setup(service => service.GetByPessoa(It.IsAny<uint>()))
            .Returns(ObterMensagens());

        mensagemServiceMock
            .Setup(service => service.GetConversa(It.IsAny<uint>(), It.IsAny<uint>()))
            .Returns(ObterConversa());

        pessoaServiceMock
            .Setup(service => service.Get(1))
            .Returns(new Pessoa { Id = 1, Nome = "Maria José Santos" });
        pessoaServiceMock
            .Setup(service => service.Get(2))
            .Returns(new Pessoa { Id = 2, Nome = "João da Conceição" });
        pessoaServiceMock
            .Setup(service => service.Get(3))
            .Returns(new Pessoa { Id = 3, Nome = "Ana Souza" });
        pessoaServiceMock
            .Setup(service => service.Get(4))
            .Returns(new Pessoa { Id = 4, Nome = "Carla Lima" });

        controller = new MensagemController(
            mensagemServiceMock.Object,
            imovelServiceMock.Object,
            pessoaServiceMock.Object);
    }

    [TestMethod]
    public void Get_QuandoAbrePeloAnuncio_DeveDestinarAoProprietario()
    {
        // Act
        var resultado = controller.Get(1, 2);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var mensagem = Assert.IsInstanceOfType<MensagemViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(2u, mensagem.IdPessoaEnviou);
        Assert.AreEqual(1u, mensagem.IdPessoaRecebeu);
        Assert.AreEqual("Apartamento 2 Quartos – Centro", viewResult.ViewData[MensagemController.ChaveNomeImovel]);

        var historico = Assert.IsInstanceOfType<List<MensagemViewModel>>(
            viewResult.ViewData[MensagemController.ChaveHistorico]);

        Assert.HasCount(2, historico);
        Assert.IsTrue(historico[0].Enviada);
        Assert.IsFalse(historico[1].Enviada);
        Assert.IsTrue(historico[0].DataEnvio < historico[1].DataEnvio);
        Assert.IsFalse(historico.Any(item => item.Texto.Contains("Ana")));

        mensagemServiceMock.Verify(
            service => service.GetConversa(2, 1),
            Times.Once);
        pessoaServiceMock.Verify(service => service.GetAll(), Times.Never);
    }

    [TestMethod]
    public void Get_QuandoMontaALista_DeveMostrarQuemApareceAUltimaMensagemEAOrdem()
    {
        // Act
        var resultado = controller.Get(1, 2);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var conversas = Assert.IsInstanceOfType<List<ConversaItemViewModel>>(
            viewResult.ViewData[MensagemController.ChaveConversas]);

        Assert.HasCount(3, conversas);
        Assert.IsFalse(conversas.Any(conversa => conversa.IdPessoa == 2));

        Assert.AreEqual(1u, conversas[0].IdPessoa);
        Assert.AreEqual("Maria José Santos", conversas[0].Nome);
        Assert.IsTrue(conversas[0].Ativa);
        Assert.AreEqual("Apartamento 2 Quartos – Centro", conversas[0].NomeImovel);
        Assert.AreEqual("Oi, João, obrigado pelo interesse! Sim, ainda está disponível.", conversas[0].Texto);

        Assert.AreEqual(3u, conversas[1].IdPessoa);
        Assert.AreEqual("Ana Souza", conversas[1].Nome);
        Assert.IsFalse(conversas[1].Ativa);
        Assert.IsNull(conversas[1].NomeImovel);
        Assert.AreEqual("Oi, João. Sou a Ana.", conversas[1].Texto);

        Assert.AreEqual(4u, conversas[2].IdPessoa);
        Assert.AreEqual("Carla Lima", conversas[2].Nome);
        Assert.IsFalse(conversas[2].Ativa);
        Assert.AreEqual("João, ainda tem a casa?", conversas[2].Texto);
        Assert.IsTrue(conversas[1].DataEnvio > conversas[2].DataEnvio);
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

        Assert.AreEqual(nameof(MensagemController.Get), redirectResult.ActionName);
        Assert.AreEqual(1u, Convert.ToUInt32(redirectResult.RouteValues!["idImovel"]));
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
            Texto = "",
            IdPessoaEnviou = 2,
            IdPessoaRecebeu = 1
        };

        var validacao = new ValidationContext(modelo);
        var resultados = new List<ValidationResult>();
        Assert.IsFalse(Validator.TryValidateObject(modelo, validacao, resultados, true));
        foreach (var item in resultados)
        {
            controller.ModelState.AddModelError(
                item.MemberNames.FirstOrDefault() ?? string.Empty,
                item.ErrorMessage ?? string.Empty);
        }

        // Act
        var resultado = controller.Create(modelo, 1);

        // Assert
        var viewResult = Assert.IsInstanceOfType<ViewResult>(resultado);
        var mensagem = Assert.IsInstanceOfType<MensagemViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(nameof(MensagemController.Get), viewResult.ViewName);
        Assert.AreEqual("", mensagem.Texto);

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
    public void Create_QuandoServicoRecusa_DeveManterNaTelaComMensagemDeErro()
    {
        // Arrange
        const string texto = "O destinatário deve ser diferente do remetente.";

        mensagemServiceMock
            .Setup(service => service.Create(It.IsAny<Mensagem>()))
            .Throws(new ServiceException(texto));

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
        var mensagem = Assert.IsInstanceOfType<MensagemViewModel>(
            viewResult.ViewData.Model);

        Assert.AreEqual(nameof(MensagemController.Get), viewResult.ViewName);
        Assert.AreEqual("Posso visitar amanhã?", mensagem.Texto);

        var erros = controller.ModelState.Values
            .SelectMany(entrada => entrada.Errors)
            .ToList();

        Assert.HasCount(1, erros);
        Assert.AreEqual(texto, erros[0].ErrorMessage);
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

    private static IEnumerable<Mensagem> ObterMensagens()
    {
        return ObterConversa().Append(new Mensagem
        {
            Id = 3,
            Texto = "Oi, João. Sou a Ana.",
            DataEnvio = new DateTime(2026, 11, 9, 15, 0, 0),
            IdPessoaEnviou = 3,
            IdPessoaRecebeu = 2
        }).Append(new Mensagem
        {
            Id = 4,
            Texto = "João, ainda tem a casa?",
            DataEnvio = new DateTime(2026, 11, 9, 13, 0, 0),
            IdPessoaEnviou = 4,
            IdPessoaRecebeu = 2
        });
    }

    private static IEnumerable<Mensagem> ObterConversa()
    {
        return
        [
            new Mensagem
            {
                Id = 1,
                Texto = "Olá! Vi seu anúncio do apartamento. Ainda está disponível?",
                DataEnvio = new DateTime(2026, 11, 9, 14, 10, 0),
                IdPessoaEnviou = 2,
                IdPessoaRecebeu = 1
            },
            new Mensagem
            {
                Id = 2,
                Texto = "Oi, João, obrigado pelo interesse! Sim, ainda está disponível.",
                DataEnvio = new DateTime(2026, 11, 9, 14, 15, 0),
                IdPessoaEnviou = 1,
                IdPessoaRecebeu = 2
            }
        ];
    }
}
