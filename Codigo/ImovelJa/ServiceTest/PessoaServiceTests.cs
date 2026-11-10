using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service.Tests;

[TestClass]
public class PessoaServiceTests
{
    private ImovelJaContext contexto = null!;
    private IPessoaService servicoPessoa = null!;

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
                Nome = "Bernardo Santos Silva",
                Email = "bernardo.sa@gmail.com",
                Telefone = "79996389554",
                Cpf = "12565478917",
                DataNascimento = new DateTime(2000, 5, 15),
                Status = "A"
            },
            new Pessoa
            {
                Id = 3,
                Tipo = "J",
                Nome = "Posto Central de Lagarto",
                Email = "postoc@gmail.com",
                Telefone = "79998545126",
                Cnpj = "21326458000105",
                Status = "A"
            },
            new Pessoa
            {
                Id = 4,
                Tipo = "J",
                Nome = "Universidade Federal de Sergipe",
                Email = "ufs@gmail.com",
                Telefone = "79998545145",
                Cnpj = "11258365000137",
                Status = "A"
            },
            new Pessoa
            {
                // conta bloqueada
                Id = 5,
                Tipo = "F",
                Nome = "Eduarda Ávila Soltelo",
                Email = "eduarda@gmail.com",
                Telefone = "79998777145",
                Cpf = "12395874574",
                DataNascimento = new DateTime(1998, 8, 20),
                Status = "B"
            },
            new Pessoa
            {
                // inquilino cadastrado por um proprietário, sem conta (sem e-mail)
                Id = 6,
                Tipo = "F",
                Nome = "Carlos Inquilino",
                Email = null,
                Cpf = "39053344705",
                DataNascimento = new DateTime(1992, 1, 25),
                Status = "A"
            });

        contexto.SaveChanges();

        servicoPessoa = new PessoaService(contexto);
    }

    [TestCleanup]
    public void Finalizar()
    {
        contexto.Dispose();
    }

    [TestMethod]
    public void GetAll_QuandoExistemPessoas_DeveRetornarTodas()
    {
        // Act
        var pessoas = servicoPessoa.GetAll().ToList();

        // Assert
        Assert.HasCount(6, pessoas);

        CollectionAssert.AreEquivalent(
            new uint[] { 1, 2, 3, 4, 5, 6 },
            pessoas.Select(p => p.Id).ToList());
    }

    [TestMethod]
    public void GetAll_QuandoExistePessoaSemConta_DeveIncluirPessoaSemEmail()
    {
        // Act
        var pessoas = servicoPessoa.GetAll().ToList();

        // Assert
        var semConta = pessoas.Single(p => p.Email == null);

        Assert.AreEqual(6u, semConta.Id);
        Assert.AreEqual("Carlos Inquilino", semConta.Nome);
    }

    [TestMethod]
    public void Get_QuandoIdExiste_DeveRetornarPessoaCorrespondente()
    {
        // Act
        var pessoa = servicoPessoa.Get(1);

        // Assert
        Assert.IsNotNull(pessoa);
        Assert.AreEqual(1u, pessoa.Id);
        Assert.AreEqual("Maria José Santos", pessoa.Nome);
        Assert.AreEqual("F", pessoa.Tipo);
        Assert.AreEqual("maria.santos@gmail.com", pessoa.Email);
        Assert.AreEqual("52998224725", pessoa.Cpf);
    }

    [TestMethod]
    public void Get_QuandoIdNaoExiste_DeveRetornarNulo()
    {
        // Act
        var pessoa = servicoPessoa.Get(100);

        // Assert
        Assert.IsNull(pessoa);
    }

    [TestMethod]
    public void Activate_QuandoAtivoFalso_DeveBloquearConta()
    {
        // Act
        servicoPessoa.Activate(1, false);

        // Assert
        var pessoa = servicoPessoa.Get(1);

        Assert.IsNotNull(pessoa);
        Assert.AreEqual("B", pessoa.Status);
    }

    [TestMethod]
    public void Activate_QuandoAtivoVerdadeiro_DeveAtivarConta()
    {
        // Act
        servicoPessoa.Activate(5, true);

        // Assert
        var pessoa = servicoPessoa.Get(5);

        Assert.IsNotNull(pessoa);
        Assert.AreEqual("A", pessoa.Status);
    }

    [TestMethod]
    public void Activate_QuandoPessoaNaoExiste_DeveLancarServiceException()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoPessoa.Activate(100, true));

        // Assert
        Assert.AreEqual("Pessoa não encontrada.", excecao.Message);
    }
}