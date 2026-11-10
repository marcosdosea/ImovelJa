using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service.Tests;

[TestClass]
public class MensagemGetConversaTests
{
    private ImovelJaContext contexto = null!;
    private IMensagemService servicoMensagem = null!;

    [TestInitialize]
    public void Inicializar()
    {
        var opcoes = new DbContextOptionsBuilder<ImovelJaContext>()
            .UseInMemoryDatabase($"ImovelJa_{Guid.NewGuid()}")
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
            },
            new Pessoa
            {
                Id = 3,
                Tipo = "F",
                Nome = "Ana Souza",
                Email = "ana.souza@gmail.com",
                Telefone = "79997776655",
                Cpf = "15350946056",
                DataNascimento = new DateTime(1998, 1, 9),
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
            },
            new Mensagem
            {
                Id = 4,
                Texto = "Maria, posso ver outro imóvel?",
                DataEnvio = new DateTime(2026, 11, 9, 11, 0, 0),
                IdPessoaEnviou = 3,
                IdPessoaRecebeu = 1
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
    public void GetConversa_QuandoHaMensagensEntreAsDuas_DeveRetornarEnviadasERecebidasEmOrdem()
    {
        // Act
        var mensagens = servicoMensagem.GetConversa(1, 2).ToList();

        // Assert
        Assert.HasCount(3, mensagens);
        Assert.IsTrue(mensagens.Any(mensagem => mensagem.IdPessoaEnviou == 1 && mensagem.IdPessoaRecebeu == 2));
        Assert.IsTrue(mensagens.Any(mensagem => mensagem.IdPessoaEnviou == 2 && mensagem.IdPessoaRecebeu == 1));
        CollectionAssert.AreEqual(
            new[]
            {
                "Olá! O imóvel ainda está disponível?",
                "Sim, João. Pode vir à tarde.",
                "Perfeito, até mais!"
            },
            mensagens.Select(mensagem => mensagem.Texto).ToList());
    }

    [TestMethod]
    public void GetConversa_QuandoHaMensagemComOutraPessoa_DeveDeixarDeFora()
    {
        // Act
        var mensagens = servicoMensagem.GetConversa(1, 2).ToList();

        // Assert
        Assert.IsFalse(mensagens.Any(mensagem => mensagem.IdPessoaEnviou == 3 || mensagem.IdPessoaRecebeu == 3));
    }

    [TestMethod]
    public void GetConversa_QuandoPessoaIgualOuAusente_DeveRetornarVazio()
    {
        // Act
        var mesmaPessoa = servicoMensagem.GetConversa(1, 1).ToList();
        var semDestinatario = servicoMensagem.GetConversa(1, 0).ToList();

        // Assert
        Assert.HasCount(0, mesmaPessoa);
        Assert.HasCount(0, semDestinatario);
    }
}
