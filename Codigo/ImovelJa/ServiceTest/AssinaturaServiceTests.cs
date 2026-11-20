using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service.Tests;

[TestClass]
public class AssinaturaServiceTests
{
    private const string MensagemPagamentoRecusado =
        "Pagamento não autorizado. Verifique os dados de pagamento e tente novamente.";

    private ImovelJaContext contexto = null!;
    private IAssinaturaService servicoAssinatura = null!;
    private IAssinaturaService servicoComPagamentoRecusado = null!;

    [TestInitialize]
    public void Inicializar()
    {
        var nomeBanco = $"ImovelJa_{Guid.NewGuid()}";

        var opcoes = new DbContextOptionsBuilder<ImovelJaContext>()
            .UseInMemoryDatabase(nomeBanco)
            .Options;

        contexto = new ImovelJaContext(opcoes);

        contexto.Pessoas.AddRange(
            new Pessoa { Id = 1, Tipo = "F", Nome = "Maria José Santos", Cpf = "52998224725", Status = "A" },
            new Pessoa { Id = 2, Tipo = "F", Nome = "Bernardo Santos Silva", Cpf = "12565478917", Status = "A" },
            new Pessoa { Id = 3, Tipo = "J", Nome = "Imobiliária Central", Cnpj = "11222333000181", Status = "A" });

        contexto.Planos.AddRange(
            new Plano { Id = 1, Nome = "Básico", Valor = 0m, LimiteImoveisAnunciados = 1, Status = "A" },
            new Plano { Id = 2, Nome = "Profissional", Valor = 49.90m, LimiteImoveisAnunciados = 10, Status = "A" },
            new Plano { Id = 3, Nome = "Imobiliária", Valor = 199.90m, LimiteImoveisAnunciados = 1_000_000, Status = "A" },
            new Plano { Id = 4, Nome = "Premium", Valor = 99.90m, LimiteImoveisAnunciados = 30, Status = "I" });

        contexto.Assinaturas.AddRange(
            new Assinatura
            {
                Id = 1,
                IdPessoa = 1,
                IdPlano = 2,
                Status = "A",
                DataContratacao = new DateTime(2026, 10, 10),
                DataProximaCobranca = new DateTime(2026, 11, 10)
            },
            new Assinatura
            {
                Id = 2,
                IdPessoa = 2,
                IdPlano = 2,
                Status = "C",
                DataContratacao = new DateTime(2026, 9, 5),
                DataProximaCobranca = new DateTime(2026, 10, 5)
            });

        contexto.SaveChanges();

        servicoAssinatura = new AssinaturaService(contexto, new CobrancaSimuladaService());
        servicoComPagamentoRecusado = new AssinaturaService(contexto, new CobrancaSimuladaService(aprovar: false));
    }

    [TestCleanup]
    public void Finalizar()
    {
        contexto.Dispose();
    }

    [TestMethod]
    public void Create_QuandoDadosSaoValidos_DeveRegistrarAssinaturaAtivaComPeriodoIniciadoHoje()
    {
        // Arrange
        var assinatura = new Assinatura { IdPessoa = 3, IdPlano = 3 };

        // Act
        var id = servicoAssinatura.Create(assinatura);

        // Assert
        var gravada = servicoAssinatura.Get(id);

        Assert.IsNotNull(gravada);
        Assert.AreEqual(3u, gravada.IdPessoa);
        Assert.AreEqual(3u, gravada.IdPlano);
        Assert.AreEqual("A", gravada.Status);
        Assert.AreEqual(DateTime.Today, gravada.DataContratacao);
        Assert.AreEqual(DateTime.Today.AddMonths(1), gravada.DataProximaCobranca);
        Assert.HasCount(3, servicoAssinatura.GetAll().ToList());
    }

    [TestMethod]
    public void Create_QuandoPlanoEGratuito_DeveRegistrarSemPassarPelaCobranca()
    {
        // Arrange
        var assinatura = new Assinatura { IdPessoa = 3, IdPlano = 1 };

        // Act
        var id = servicoComPagamentoRecusado.Create(assinatura);

        // Assert
        var gravada = servicoAssinatura.Get(id);

        Assert.IsNotNull(gravada);
        Assert.AreEqual(1u, gravada.IdPlano);
        Assert.AreEqual("A", gravada.Status);
    }

    [TestMethod]
    public void Create_QuandoPessoaJaPossuiAssinatura_DeveLancarServiceException()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoAssinatura.Create(new Assinatura { IdPessoa = 1, IdPlano = 3 }));

        // Assert
        Assert.AreEqual(
            "A pessoa já possui uma assinatura. Para trocar de plano, altere a assinatura.",
            excecao.Message);
        Assert.HasCount(2, servicoAssinatura.GetAll().ToList());
    }

    [TestMethod]
    public void Create_QuandoPlanoEstaInativo_DeveLancarServiceException()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoAssinatura.Create(new Assinatura { IdPessoa = 3, IdPlano = 4 }));

        // Assert
        Assert.AreEqual("Este plano não está disponível para contratação.", excecao.Message);
        Assert.IsNull(servicoAssinatura.GetByPessoa(3));
    }

    [TestMethod]
    public void Create_QuandoPlanoNaoExiste_DeveLancarServiceException()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoAssinatura.Create(new Assinatura { IdPessoa = 3, IdPlano = 99 }));

        // Assert
        Assert.AreEqual("Plano não encontrado.", excecao.Message);
        Assert.IsNull(servicoAssinatura.GetByPessoa(3));
    }

    [TestMethod]
    public void Create_QuandoPagamentoNaoEAutorizado_DeveLancarServiceExceptionSemGravar()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoComPagamentoRecusado.Create(new Assinatura { IdPessoa = 3, IdPlano = 3 }));

        // Assert
        Assert.AreEqual(MensagemPagamentoRecusado, excecao.Message);
        Assert.IsNull(servicoAssinatura.GetByPessoa(3));
    }

    [TestMethod]
    public void Edit_QuandoTrocaDePlano_DeveReiniciarOPeriodo()
    {
        // Act
        servicoAssinatura.Edit(new Assinatura { Id = 1, IdPlano = 3 });

        // Assert
        var assinatura = servicoAssinatura.Get(1);

        Assert.IsNotNull(assinatura);
        Assert.AreEqual(3u, assinatura.IdPlano);
        Assert.AreEqual(1u, assinatura.IdPessoa);
        Assert.AreEqual("A", assinatura.Status);
        Assert.AreEqual(DateTime.Today, assinatura.DataContratacao);
        Assert.AreEqual(DateTime.Today.AddMonths(1), assinatura.DataProximaCobranca);
    }

    [TestMethod]
    public void Edit_QuandoAssinaturaEstaCancelada_DeveReativarComONovoPlano()
    {
        // Act
        servicoAssinatura.Edit(new Assinatura { Id = 2, IdPlano = 3 });

        // Assert
        var assinatura = servicoAssinatura.Get(2);

        Assert.IsNotNull(assinatura);
        Assert.AreEqual("A", assinatura.Status);
        Assert.AreEqual(3u, assinatura.IdPlano);
        Assert.AreEqual(DateTime.Today.AddMonths(1), assinatura.DataProximaCobranca);
    }

    [TestMethod]
    public void Edit_QuandoPagamentoNaoEAutorizado_DeveManterAAssinaturaSemAlteracao()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoComPagamentoRecusado.Edit(new Assinatura { Id = 1, IdPlano = 3 }));

        // Assert
        Assert.AreEqual(MensagemPagamentoRecusado, excecao.Message);
        VerificarAssinaturaOriginal();
    }

    [TestMethod]
    public void Edit_QuandoPlanoEstaInativo_DeveLancarServiceException()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoAssinatura.Edit(new Assinatura { Id = 1, IdPlano = 4 }));

        // Assert
        Assert.AreEqual("Este plano não está disponível para contratação.", excecao.Message);
        VerificarAssinaturaOriginal();
    }

    [TestMethod]
    public void Edit_QuandoAssinaturaNaoExiste_DeveLancarServiceException()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoAssinatura.Edit(new Assinatura { Id = 99, IdPlano = 3 }));

        // Assert
        Assert.AreEqual("Assinatura não encontrada.", excecao.Message);
    }

    [TestMethod]
    public void Cancel_QuandoAssinaturaEstaAtiva_DeveCancelarMantendoOPlanoAteAProximaCobranca()
    {
        // Act
        servicoAssinatura.Cancel(1);

        // Assert
        var assinatura = servicoAssinatura.Get(1);

        Assert.IsNotNull(assinatura);
        Assert.AreEqual("C", assinatura.Status);
        Assert.AreEqual(2u, assinatura.IdPlano);
        Assert.AreEqual(new DateTime(2026, 11, 10), assinatura.DataProximaCobranca);
    }

    [TestMethod]
    public void Cancel_QuandoAssinaturaJaEstaCancelada_DeveLancarServiceException()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoAssinatura.Cancel(2));

        // Assert
        Assert.AreEqual("A assinatura já está cancelada.", excecao.Message);
    }

    [TestMethod]
    public void Cancel_QuandoAssinaturaNaoExiste_DeveLancarServiceException()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoAssinatura.Cancel(99));

        // Assert
        Assert.AreEqual("Assinatura não encontrada.", excecao.Message);
    }

    [TestMethod]
    public void GetByPessoa_QuandoPessoaPossuiAssinatura_DeveRetornarAAssinatura()
    {
        // Act
        var assinatura = servicoAssinatura.GetByPessoa(1);

        // Assert
        Assert.IsNotNull(assinatura);
        Assert.AreEqual(1u, assinatura.Id);
        Assert.AreEqual(2u, assinatura.IdPlano);
    }

    [TestMethod]
    public void GetByPessoa_QuandoPessoaNaoPossuiAssinatura_DeveRetornarNulo()
    {
        // Act
        var assinatura = servicoAssinatura.GetByPessoa(3);

        // Assert
        Assert.IsNull(assinatura);
    }

    private void VerificarAssinaturaOriginal()
    {
        var assinatura = servicoAssinatura.Get(1);

        Assert.IsNotNull(assinatura);
        Assert.AreEqual(2u, assinatura.IdPlano);
        Assert.AreEqual("A", assinatura.Status);
        Assert.AreEqual(new DateTime(2026, 10, 10), assinatura.DataContratacao);
        Assert.AreEqual(new DateTime(2026, 11, 10), assinatura.DataProximaCobranca);
    }
}
