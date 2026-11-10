using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service.Tests;

[TestClass]
public class MensagemServiceTests
{
    private ImovelJaContext contexto = null!;
    private IMensagemService servicoMensagem = null!;

    [TestInitialize]
    public void Inicializar()
    {
        var nomeBanco = $"ImovelJa_{Guid.NewGuid()}";

        var opcoes = new DbContextOptionsBuilder<ImovelJaContext>()
            .UseInMemoryDatabase(nomeBanco)
            .Options;

        contexto = new ImovelJaContext(opcoes);

        contexto.Pessoas.AddRange(
            new Pessoa
            {
                Id = 1,
                Tipo = "F",
                Nome = "Maria José Santos",
                Email = "maria.santos@gmail.com",
                Telefone = "79998123456",
                Cpf = "52998224725",
                DataNascimento = new DateTime(1985, 3, 14),
                Status = "A"
            },
            new Pessoa
            {
                Id = 2,
                Tipo = "F",
                Nome = "João da Conceição",
                Email = "joao.conceicao@gmail.com",
                Telefone = "79998887766",
                Cpf = "39053344705",
                DataNascimento = new DateTime(1992, 8, 21),
                Status = "A"
            });

        contexto.Mensagems.AddRange(
            new Mensagem
            {
                Id = 3,
                Texto = "Perfeito, até mais!",
                DataEnvio = new DateTime(2026, 11, 9, 10, 10, 0),
                IdPessoaEnviou = 2,
                IdPessoaRecebeu = 1
            },
            new Mensagem
            {
                Id = 1,
                Texto = "Olá! O imóvel ainda está disponível?",
                DataEnvio = new DateTime(2026, 11, 9, 10, 0, 0),
                IdPessoaEnviou = 2,
                IdPessoaRecebeu = 1
            },
            new Mensagem
            {
                Id = 2,
                Texto = "Sim, João. Pode vir à tarde.",
                DataEnvio = new DateTime(2026, 11, 9, 10, 5, 0),
                IdPessoaEnviou = 1,
                IdPessoaRecebeu = 2
            });

        contexto.SaveChanges();

        servicoMensagem = new MensagemService(contexto);
    }

    [TestCleanup]
    public void Finalizar()
    {
        contexto.Dispose();
    }

    [TestMethod]
    public void Create_QuandoMensagemValida_DeveGravarComADataDeEnvio()
    {
        // Arrange
        var antes = DateTime.Now;
        var mensagem = new Mensagem
        {
            Texto = "Podemos visitar amanhã cedo?",
            IdPessoaEnviou = 2,
            IdPessoaRecebeu = 1
        };

        // Act
        var id = servicoMensagem.Create(mensagem);
        var gravada = servicoMensagem.Get(id);

        // Assert
        Assert.IsNotNull(gravada);
        Assert.AreEqual("Podemos visitar amanhã cedo?", gravada.Texto);
        Assert.AreEqual(2u, gravada.IdPessoaEnviou);
        Assert.AreEqual(1u, gravada.IdPessoaRecebeu);
        Assert.IsTrue(gravada.DataEnvio >= antes.AddSeconds(-1));
        Assert.IsTrue(gravada.DataEnvio <= DateTime.Now.AddSeconds(1));
    }

    [TestMethod]
    public void Create_QuandoTextoEmBranco_DeveLancarServiceException()
    {
        // Arrange
        var quantidade = contexto.Mensagems.Count();
        var mensagem = new Mensagem
        {
            Texto = "   ",
            IdPessoaEnviou = 2,
            IdPessoaRecebeu = 1
        };

        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoMensagem.Create(mensagem));

        // Assert
        Assert.AreEqual("A mensagem não pode estar em branco.", excecao.Message);
        Assert.AreEqual(quantidade, contexto.Mensagems.Count());
    }

    [TestMethod]
    public void Create_QuandoRemetenteIgualAoDestinatario_DeveLancarServiceException()
    {
        // Arrange
        var mensagem = new Mensagem
        {
            Texto = "Vou falar comigo mesmo.",
            IdPessoaEnviou = 1,
            IdPessoaRecebeu = 1
        };

        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoMensagem.Create(mensagem));

        // Assert
        Assert.AreEqual("O destinatário deve ser diferente do remetente.", excecao.Message);
    }

    [TestMethod]
    public void Create_QuandoDestinatarioNaoExiste_DeveLancarServiceException()
    {
        // Arrange
        var mensagem = new Mensagem
        {
            Texto = "Olá, tem alguém aí?",
            IdPessoaEnviou = 1,
            IdPessoaRecebeu = 99
        };

        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoMensagem.Create(mensagem));

        // Assert
        Assert.AreEqual("Destinatário não encontrado.", excecao.Message);
    }

    [TestMethod]
    public void Get_QuandoMensagemExiste_DeveRetornarAMensagem()
    {
        // Act
        var mensagem = servicoMensagem.Get(1);

        // Assert
        Assert.IsNotNull(mensagem);
        Assert.AreEqual("Olá! O imóvel ainda está disponível?", mensagem.Texto);
        Assert.AreEqual(2u, mensagem.IdPessoaEnviou);
        Assert.AreEqual(1u, mensagem.IdPessoaRecebeu);
    }

    [TestMethod]
    public void Get_QuandoMensagemNaoExiste_DeveRetornarNulo()
    {
        // Act
        var mensagem = servicoMensagem.Get(99);

        // Assert
        Assert.IsNull(mensagem);
    }

    [TestMethod]
    public void GetByPessoa_QuandoPessoaTemMensagens_DeveRetornarEnviadasERecebidasEmOrdem()
    {
        // Act
        var mensagens = servicoMensagem.GetByPessoa(1).ToList();

        // Assert
        Assert.HasCount(3, mensagens);
        Assert.IsTrue(mensagens.Any(mensagem => mensagem.IdPessoaEnviou == 1));
        Assert.IsTrue(mensagens.Any(mensagem => mensagem.IdPessoaRecebeu == 1));
        CollectionAssert.AreEqual(
            new[]
            {
                "Olá! O imóvel ainda está disponível?",
                "Sim, João. Pode vir à tarde.",
                "Perfeito, até mais!"
            },
            mensagens.Select(mensagem => mensagem.Texto).ToList());
    }
}
