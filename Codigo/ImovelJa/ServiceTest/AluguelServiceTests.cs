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
                Email = "joao.conceicao@gmail.com",
                Telefone = "79998887766",
                Cpf = "39053344705",
                DataNascimento = new DateTime(1992, 8, 21),
                Status = "A"
            });

        contexto.Imovels.AddRange(
            new Imovel
            {
                Id = 1,
                Nome = "Casa - Itabaiana",
                Descricao = "Casa térrea próxima ao Rotary Club.",
                Tipo = "C",
                AreaTotal = 38m,
                QuantidadeBanheiros = 1,
                QuantidadeQuartos = 1,
                QuantidadeVagasGaragem = 0,
                Rua = "Rua do Rotary, 120",
                Bairro = "Rotary Club",
                Cep = "49500000",
                Cidade = "Itabaiana",
                Estado = "SE",
                Valor = 700m,
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
                Estado = "SE",
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
                DiaPagamento = 5,
                DataVencimentoProximo = new DateTime(2026, 12, 5),
                Valor = 700m,
                Status = "A",
                IdImovel = 1,
                IdPessoaInquilino = 2
            },
            new Aluguel
            {
                Id = 2,
                DiaPagamento = 10,
                Valor = 900m,
                Status = "C",
                IdImovel = 2,
                IdPessoaInquilino = 1
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
    public void GetByInquilino_QuandoInquilinoPossuiAluguel_DeveTrazerOsDadosDoImovel()
    {
        // Act
        var alugueis = servicoAluguel.GetByInquilino(2).ToList();

        // Assert
        Assert.HasCount(1, alugueis);
        Assert.AreEqual(1u, alugueis[0].Id);
        Assert.AreEqual(2u, alugueis[0].IdPessoaInquilino);
        Assert.IsNotNull(alugueis[0].IdImovelNavigation);
        Assert.AreEqual("Casa - Itabaiana", alugueis[0].IdImovelNavigation.Nome);
        Assert.AreEqual("Rotary Club", alugueis[0].IdImovelNavigation.Bairro);
        Assert.AreEqual("Itabaiana", alugueis[0].IdImovelNavigation.Cidade);
        Assert.AreEqual("SE", alugueis[0].IdImovelNavigation.Estado);
        Assert.AreEqual(1u, alugueis[0].IdImovelNavigation.QuantidadeQuartos);
        Assert.AreEqual(38m, alugueis[0].IdImovelNavigation.AreaTotal);
        Assert.AreEqual(700m, alugueis[0].IdImovelNavigation.Valor);
        Assert.IsFalse(alugueis.Any(aluguel => aluguel.IdPessoaInquilino != 2));
    }
}
