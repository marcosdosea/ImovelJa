using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service.Tests;

[TestClass]
public class AluguelServiceTests
{
    private ImovelJaContext contexto = null!;
    private IAluguelService servicoAluguel = null!;

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
                Email = null,
                Telefone = "79998887766",
                Cpf = "39053344705",
                DataNascimento = new DateTime(1992, 8, 21),
                Status = "A"
            },
            new Pessoa
            { 
                Id = 3,
                Tipo = "F",
                Nome = "Carlos Inquilino",
                Email = null,
                Telefone = "79996389554",
                Cpf = "12565478917",
                DataNascimento = new DateTime(1990, 1, 25),
                Status = "A"
            },
            new Pessoa
            {
                Id = 4,
                Tipo = "J",
                Nome = "Posto Central de Lagarto",
                Email = "postoc@gmail.com",
                Telefone = "79998545126",
                Cnpj = "21326458000105",
                Status = "A"
            });

        contexto.Imovels.AddRange(
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
                Status = "A",
                StatusValidacao = "A",
                DataCadastro = new DateTime(2026, 7, 21, 14, 30, 0),
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
                DataCadastro = new DateTime(2026, 7, 22, 9, 15, 0),
                IdPessoaProprietario = 1
            });

        contexto.Aluguels.AddRange(
            new Aluguel
            {
                Id = 1,
                DiaPagamento = 10,
                Valor = 1200m,
                Status = "A",
                IdImovel = 1,
                IdPessoaInquilino = 2
            },
            new Aluguel
            {
                Id = 2,
                DiaPagamento = 5,
                Valor = 900m,
                Status = "C",
                IdImovel = 2,
                IdPessoaInquilino = 3
            });

        contexto.SaveChanges();
        contexto.ChangeTracker.Clear();
        servicoAluguel = new AluguelService(contexto);
    }

    [TestCleanup]
    public void Finalizar()
    {
        contexto.Dispose();
    }

    [TestMethod]
    public void Get_QuandoIdExiste_DeveRetornarAluguelCorrespondente()
    {
        // Act
        var aluguel = servicoAluguel.Get(1);

        // Assert
        Assert.IsNotNull(aluguel);
        Assert.AreEqual(1u, aluguel.Id);
        Assert.AreEqual(10, aluguel.DiaPagamento);
        Assert.AreEqual(1200m, aluguel.Valor);
        Assert.AreEqual("A", aluguel.Status);
        Assert.AreEqual(1u, aluguel.IdImovel);
        Assert.AreEqual(2u, aluguel.IdPessoaInquilino);
    }

    [TestMethod]
    public void Get_QuandoIdNaoExiste_DeveRetornarNulo()
    {
        // Act
        var aluguel = servicoAluguel.Get(999);

        // Assert
        Assert.IsNull(aluguel);
    }

    [TestMethod]
    public void GetByImovel_QuandoImovelTemAluguelCancelado_DeveIncluirOCancelado()
    {
        // Act
        var alugueis = servicoAluguel.GetByImovel(2).ToList();

        // Assert
        Assert.HasCount(1, alugueis);
        Assert.AreEqual(2u, alugueis[0].Id);
        Assert.AreEqual("C", alugueis[0].Status);
    }

    [TestMethod]
    public void GetByInquilino_QuandoInquilinoPossuiAluguel_DeveRetornarSomenteSeusAlugueis()
    {
        // Act
        var alugueis = servicoAluguel.GetByInquilino(2).ToList();

        // Assert
        Assert.HasCount(1, alugueis);
        Assert.AreEqual(1u, alugueis[0].Id);
        Assert.IsTrue(alugueis.All(aluguel => aluguel.IdPessoaInquilino == 2));
    }

    [TestMethod]
    public void Create_QuandoPessoaJaCadastrada_DeveVincularAPessoaExistente()
    {
        // Arrange
        var aluguel = new Aluguel
        {
            Id = 10,
            DiaPagamento = 5,
            Valor = 900m,
            IdImovel = 2,
            IdPessoaInquilinoNavigation = new Pessoa
            {
                Tipo = "F",
                Nome = "Bernardo",
                Cpf = "39053344705"
            }
        };

        // Act
        var id = servicoAluguel.Create(aluguel);

        // Assert
        var criado = servicoAluguel.Get(id);

        Assert.IsNotNull(criado);
        Assert.AreEqual(2u, criado.IdPessoaInquilino);
        Assert.AreEqual("A", criado.Status);
        Assert.HasCount(4, contexto.Pessoas.ToList());

        var inquilino = contexto.Pessoas.Find(2u);

        Assert.IsNotNull(inquilino);
        Assert.AreEqual("João da Conceição", inquilino.Nome);
    }

    [TestMethod]
    public void Create_QuandoPessoaNova_DeveCadastrarAPessoaNaMesmaTransacao()
    {
        // Arrange
        var aluguel = new Aluguel
        {
            Id = 10,
            DiaPagamento = 5,
            Valor = 900m,
            IdImovel = 2,
            IdPessoaInquilinoNavigation = new Pessoa
            {
                Id = 10,
                Tipo = "F",
                Nome = "Ana Paula Lima",
                Telefone = "79991112222",
                Cpf = "11144477735"
            }
        };

        // Act
        var id = servicoAluguel.Create(aluguel);

        // Assert
        var criado = servicoAluguel.Get(id);
        var pessoa = contexto.Pessoas.Single(p => p.Cpf == "11144477735");

        Assert.IsNotNull(criado);
        Assert.HasCount(5, contexto.Pessoas.ToList());
        Assert.AreEqual(pessoa.Id, criado.IdPessoaInquilino);
        Assert.AreEqual("Ana Paula Lima", pessoa.Nome);
        Assert.AreEqual("79991112222", pessoa.Telefone);
        Assert.AreEqual("A", pessoa.Status);
        Assert.IsNull(pessoa.Cnpj);
    }

    [TestMethod]
    public void Create_QuandoDadosDoInquilinoSaoIncompletos_DeveLancarServiceExceptionENaoGravarNada()
    {
        // Arrange
        var aluguel = new Aluguel
        {
            Id = 10,
            DiaPagamento = 5,
            Valor = 900m,
            IdImovel = 2,
            IdPessoaInquilinoNavigation = new Pessoa
            {
                Id = 10,
                Tipo = "F",
                Nome = "Ana Paula Lima"
            }
        };

        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoAluguel.Create(aluguel));

        // Assert
        Assert.AreEqual("Informe o nome e o CPF ou CNPJ do inquilino.", excecao.Message);
        Assert.HasCount(4, contexto.Pessoas.ToList());
        Assert.HasCount(2, servicoAluguel.GetAll().ToList());

        var imovel = contexto.Imovels.Find(2u);

        Assert.IsNotNull(imovel);
        Assert.AreEqual("D", imovel.Status);
    }

    [TestMethod]
    public void Create_QuandoImovelTemAluguelAtivo_DeveLancarServiceException()
    {
        // Arrange
        var aluguel = new Aluguel
        {
            Id = 10,
            DiaPagamento = 5,
            Valor = 900m,
            IdImovel = 1,
            IdPessoaInquilinoNavigation = new Pessoa
            {
                Tipo = "F",
                Nome = "Carlos Inquilino",
                Cpf = "12565478917"
            }
        };

        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoAluguel.Create(aluguel));

        // Assert
        Assert.AreEqual("O imóvel já possui um aluguel ativo.", excecao.Message);
        Assert.HasCount(1, servicoAluguel.GetByImovel(1).ToList());
    }

    [TestMethod]
    public void Create_QuandoImovelTemApenasAluguelCancelado_DeveRegistrarNovoAluguel()
    {
        // Arrange
        var aluguel = new Aluguel
        {
            Id = 10,
            DiaPagamento = 15,
            Valor = 950m,
            IdImovel = 2,
            IdPessoaInquilino = 3
        };

        // Act
        var id = servicoAluguel.Create(aluguel);

        // Assert
        var criado = servicoAluguel.Get(id);

        Assert.IsNotNull(criado);
        Assert.AreEqual("A", criado.Status);
        Assert.HasCount(2, servicoAluguel.GetByImovel(2).ToList());
    }

    [TestMethod]
    public void Create_QuandoAluguelEhRegistrado_DeveMarcarImovelComoAlugado()
    {
        // Arrange
        var aluguel = new Aluguel
        {
            Id = 10,
            DiaPagamento = 5,
            Valor = 900m,
            IdImovel = 2,
            IdPessoaInquilinoNavigation = new Pessoa
            {
                Tipo = "F",
                Nome = "Carlos Inquilino",
                Cpf = "12565478917"
            }
        };

        // Act
        servicoAluguel.Create(aluguel);

        // Assert
        var imovel = contexto.Imovels.Find(2u);

        Assert.IsNotNull(imovel);
        Assert.AreEqual("A", imovel.Status);
    }

    [TestMethod]
    public void Edit_QuandoInformadosDadosDoInquilino_DeveAtualizarAluguelEInquilino()
    {
        // Arrange
        var edicao = new Aluguel
        {
            Id = 1,
            IdImovel = 1,
            Status = "A",
            DiaPagamento = 20,
            Valor = 1300m,
            IdPessoaInquilinoNavigation = new Pessoa
            {
                Nome = "João da Conceição Filho",
                Cpf = "11144477735",
                Telefone = "79911112222"
            }
        };

        // Act
        servicoAluguel.Edit(edicao);

        // Assert
        var aluguel = servicoAluguel.Get(1);
        var inquilino = contexto.Pessoas.Find(2u);

        Assert.IsNotNull(aluguel);
        Assert.AreEqual(20, aluguel.DiaPagamento);
        Assert.AreEqual(1300m, aluguel.Valor);
        Assert.AreEqual(2u, aluguel.IdPessoaInquilino);

        Assert.IsNotNull(inquilino);
        Assert.AreEqual("João da Conceição Filho", inquilino.Nome);
        Assert.AreEqual("11144477735", inquilino.Cpf);
        Assert.AreEqual("79911112222", inquilino.Telefone);
        Assert.HasCount(4, contexto.Pessoas.ToList());
    }

    [TestMethod]
    public void Edit_QuandoMantemOProprioDocumento_DeveAtualizarSemLancarExcecao()
    {
        // Arrange
        var edicao = new Aluguel
        {
            Id = 1,
            IdImovel = 1,
            Status = "A",
            DiaPagamento = 10,
            Valor = 1200m,
            IdPessoaInquilinoNavigation = new Pessoa
            {
                Nome = "João C. da Conceição",
                Cpf = "39053344705", 
                Telefone = "79998887766"
            }
        };

        // Act
        servicoAluguel.Edit(edicao);

        // Assert
        var inquilino = contexto.Pessoas.Find(2u);

        Assert.IsNotNull(inquilino);
        Assert.AreEqual("João C. da Conceição", inquilino.Nome);
        Assert.AreEqual("39053344705", inquilino.Cpf);
    }

    [TestMethod]
    public void Edit_QuandoDocumentoPertenceAOutraPessoa_DeveLancarServiceExceptionENaoGravarNada()
    {
        // Arrange
        var edicao = new Aluguel
        {
            Id = 1,
            IdImovel = 1,
            Status = "A",
            DiaPagamento = 25,
            Valor = 9999m,
            IdPessoaInquilinoNavigation = new Pessoa
            {
                Nome = "Nome que não pode ser gravado",
                Cpf = "12565478917", // CPF do Carlos (id 3)
                Telefone = "79900000000"
            }
        };

        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoAluguel.Edit(edicao));

        // Assert
        Assert.AreEqual("O CPF ou CNPJ informado pertence a outra pessoa.", excecao.Message);

        var aluguel = servicoAluguel.Get(1);
        var inquilino = contexto.Pessoas.Find(2u);

        Assert.IsNotNull(aluguel);
        Assert.AreEqual(10, aluguel.DiaPagamento);
        Assert.AreEqual(1200m, aluguel.Valor);

        Assert.IsNotNull(inquilino);
        Assert.AreEqual("João da Conceição", inquilino.Nome);
        Assert.AreEqual("39053344705", inquilino.Cpf);
        Assert.AreEqual("79998887766", inquilino.Telefone);
    }

    [TestMethod]
    public void Edit_QuandoAluguelNaoExiste_DeveLancarServiceException()
    {
        // Arrange
        var edicao = new Aluguel { Id = 999, IdImovel = 1, Status = "A" };

        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoAluguel.Edit(edicao));

        // Assert
        Assert.AreEqual("Aluguel não encontrado.", excecao.Message);
    }

    [TestMethod]
    public void Delete_QuandoAluguelAtivo_DeveCancelarMantendoOVinculoEDevolverImovelParaDisponivel()
    {
        // Act
        servicoAluguel.Delete(1);

        // Assert
        var aluguel = servicoAluguel.Get(1);
        var imovel = contexto.Imovels.Find(1u);

        Assert.IsNotNull(aluguel);
        Assert.AreEqual("C", aluguel.Status);
        Assert.AreEqual(2u, aluguel.IdPessoaInquilino);

        Assert.IsNotNull(imovel);
        Assert.AreEqual("D", imovel.Status);

        var doInquilino = servicoAluguel.GetByInquilino(2).ToList();

        Assert.HasCount(1, doInquilino);
        Assert.AreEqual(1u, doInquilino[0].Id);
    }
}