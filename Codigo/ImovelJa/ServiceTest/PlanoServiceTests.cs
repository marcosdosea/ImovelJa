using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service.Tests;

[TestClass]
public class PlanoServiceTests
{
    private const string BeneficiosBasico =
        "1 imóvel anunciado\nAnúncio de venda ou aluguel\nChat com interessados";

    private const string BeneficiosProfissional =
        "Até 10 imóveis anunciados\nDestaque na busca\nGestão de aluguéis\nSuporte por e-mail";

    private const string BeneficiosImobiliaria =
        "Imóveis ilimitados\nSuporte dedicado\nAPI de integração\nRelatórios avançados\nMúltiplos usuários";

    private ImovelJaContext contexto = null!;
    private IPlanoService servicoPlano = null!;

    [TestInitialize]
    public void Inicializar()
    {
        var nomeBanco = $"ImovelJa_{Guid.NewGuid()}";

        var opcoes = new DbContextOptionsBuilder<ImovelJaContext>()
            .UseInMemoryDatabase(nomeBanco)
            .Options;

        contexto = new ImovelJaContext(opcoes);

        contexto.Planos.AddRange(
            new Plano
            {
                Id = 1,
                Nome = "Básico",
                Valor = 0m,
                LimiteImoveisAnunciados = 1,
                Beneficios = BeneficiosBasico,
                Status = "A"
            },
            new Plano
            {
                Id = 2,
                Nome = "Profissional",
                Valor = 49.90m,
                LimiteImoveisAnunciados = 10,
                Beneficios = BeneficiosProfissional,
                Status = "A"
            },
            new Plano
            {
                Id = 3,
                Nome = "Imobiliária",
                Valor = 199.90m,
                LimiteImoveisAnunciados = 1_000_000,
                Beneficios = BeneficiosImobiliaria,
                Status = "A"
            });

        contexto.SaveChanges();

        servicoPlano = new PlanoService(contexto);
    }

    [TestCleanup]
    public void Finalizar()
    {
        contexto.Dispose();
    }

    [TestMethod]
    public void GetAll_QuandoExistemPlanos_DeveRetornarTodos()
    {
        // Act
        var planos = servicoPlano.GetAll().ToList();

        // Assert
        Assert.HasCount(3, planos);

        CollectionAssert.AreEquivalent(
            new uint[] { 1, 2, 3 },
            planos.Select(plano => plano.Id).ToList());

        var basico = planos.Single(plano => plano.Id == 1);
        var profissional = planos.Single(plano => plano.Id == 2);
        var imobiliaria = planos.Single(plano => plano.Id == 3);

        Assert.AreEqual("Básico", basico.Nome);
        Assert.AreEqual(0m, basico.Valor);
        Assert.AreEqual(1u, basico.LimiteImoveisAnunciados);
        Assert.AreEqual(BeneficiosBasico, basico.Beneficios);
        Assert.AreEqual("A", basico.Status);

        Assert.AreEqual("Profissional", profissional.Nome);
        Assert.AreEqual(49.90m, profissional.Valor);
        Assert.AreEqual(10u, profissional.LimiteImoveisAnunciados);
        Assert.AreEqual(BeneficiosProfissional, profissional.Beneficios);

        Assert.AreEqual("Imobiliária", imobiliaria.Nome);
        Assert.AreEqual(199.90m, imobiliaria.Valor);
        Assert.AreEqual(1_000_000u, imobiliaria.LimiteImoveisAnunciados);
        Assert.AreEqual(BeneficiosImobiliaria, imobiliaria.Beneficios);
    }

    [TestMethod]
    public void Get_QuandoIdExiste_DeveRetornarPlanoCorrespondente()
    {
        // Act
        var plano = servicoPlano.Get(2);

        // Assert
        Assert.IsNotNull(plano);
        Assert.AreEqual(2u, plano.Id);
        Assert.AreEqual("Profissional", plano.Nome);
        Assert.AreEqual(49.90m, plano.Valor);
        Assert.AreEqual(10u, plano.LimiteImoveisAnunciados);
        Assert.AreEqual(BeneficiosProfissional, plano.Beneficios);
        Assert.AreEqual("A", plano.Status);
    }

    [TestMethod]
    public void Get_QuandoIdNaoExiste_DeveRetornarNulo()
    {
        // Act
        var plano = servicoPlano.Get(999);

        // Assert
        Assert.IsNull(plano);
    }

    [TestMethod]
    public void Edit_QuandoValorEBeneficiosSaoValidos_DeveAtualizarSomenteEssesCampos()
    {
        // Arrange
        const string beneficios = "Até 10 imóveis anunciados\nRelatórios mensais";

        var plano = new Plano
        {
            Id = 2,
            Nome = "Nome que não deve ser gravado",
            Valor = 59.90m,
            LimiteImoveisAnunciados = 99,
            Beneficios = beneficios,
            Status = "I"
        };

        // Act
        servicoPlano.Edit(plano);

        // Assert
        var atualizado = servicoPlano.Get(2);

        Assert.IsNotNull(atualizado);
        Assert.AreEqual(59.90m, atualizado.Valor);
        Assert.AreEqual(beneficios, atualizado.Beneficios);
        Assert.AreEqual("Profissional", atualizado.Nome);
        Assert.AreEqual(10u, atualizado.LimiteImoveisAnunciados);
        Assert.AreEqual("A", atualizado.Status);

        var basico = servicoPlano.Get(1);

        Assert.IsNotNull(basico);
        Assert.AreEqual(0m, basico.Valor);
        Assert.AreEqual(BeneficiosBasico, basico.Beneficios);
    }

    [TestMethod]
    public void Edit_QuandoBasicoTemValorZero_DeveAtualizarBeneficios()
    {
        // Arrange
        const string beneficios = "1 imóvel anunciado\nAnúncio de venda ou aluguel";

        // Act
        servicoPlano.Edit(new Plano
        {
            Id = 1,
            Valor = 0m,
            Beneficios = beneficios
        });

        // Assert
        var plano = servicoPlano.Get(1);

        Assert.IsNotNull(plano);
        Assert.AreEqual(0m, plano.Valor);
        Assert.AreEqual(beneficios, plano.Beneficios);
        Assert.AreEqual("Básico", plano.Nome);
        Assert.AreEqual("A", plano.Status);
    }

    [TestMethod]
    public void Edit_QuandoValorENegativo_DeveLancarServiceException()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoPlano.Edit(new Plano { Id = 2, Valor = -1m }));

        // Assert
        Assert.AreEqual("O valor do plano não pode ser negativo.", excecao.Message);

        var plano = servicoPlano.Get(2);

        Assert.IsNotNull(plano);
        Assert.AreEqual(49.90m, plano.Valor);
        Assert.AreEqual(BeneficiosProfissional, plano.Beneficios);
    }

    [TestMethod]
    public void Edit_QuandoBasicoTemValorDiferenteDeZero_DeveLancarServiceException()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoPlano.Edit(new Plano { Id = 1, Valor = 10m, Beneficios = BeneficiosBasico }));

        // Assert
        Assert.AreEqual("O plano Básico deve ter valor zero.", excecao.Message);

        var plano = servicoPlano.Get(1);

        Assert.IsNotNull(plano);
        Assert.AreEqual(0m, plano.Valor);
        Assert.AreEqual(BeneficiosBasico, plano.Beneficios);
    }

    [TestMethod]
    public void Edit_QuandoPlanoNaoExiste_DeveLancarServiceException()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoPlano.Edit(new Plano { Id = 999, Valor = 10m }));

        // Assert
        Assert.AreEqual("Plano não encontrado.", excecao.Message);
    }

    [TestMethod]
    public void Activate_QuandoDesativaEAtivaPlano_DeveAlternarStatusSemAlterarOsDemaisDados()
    {
        // Act
        servicoPlano.Activate(2, false);

        // Assert
        var desativado = servicoPlano.Get(2);

        Assert.IsNotNull(desativado);
        Assert.AreEqual("I", desativado.Status);
        Assert.AreEqual("Profissional", desativado.Nome);
        Assert.AreEqual(49.90m, desativado.Valor);
        Assert.AreEqual(BeneficiosProfissional, desativado.Beneficios);
        var basico = servicoPlano.Get(1);
        var imobiliaria = servicoPlano.Get(3);

        Assert.IsNotNull(basico);
        Assert.IsNotNull(imobiliaria);
        Assert.AreEqual("A", basico.Status);
        Assert.AreEqual("A", imobiliaria.Status);

        // Act
        servicoPlano.Activate(2, true);

        // Assert
        var reativado = servicoPlano.Get(2);

        Assert.IsNotNull(reativado);
        Assert.AreEqual("A", reativado.Status);
        Assert.AreEqual(49.90m, reativado.Valor);
    }

    [TestMethod]
    public void Activate_QuandoDesativaBasico_DeveLancarServiceException()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoPlano.Activate(1, false));

        // Assert
        Assert.AreEqual("O plano Básico não pode ser desativado.", excecao.Message);

        var plano = servicoPlano.Get(1);

        Assert.IsNotNull(plano);
        Assert.AreEqual("A", plano.Status);
    }

    [TestMethod]
    public void Activate_QuandoPlanoNaoExiste_DeveLancarServiceException()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoPlano.Activate(999, false));

        // Assert
        Assert.AreEqual("Plano não encontrado.", excecao.Message);
    }
}
