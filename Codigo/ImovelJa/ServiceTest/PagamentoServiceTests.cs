using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service.Tests;

[TestClass]
public class PagamentoServiceTests
{
    private static readonly byte[] ComprovanteAnterior = [0x25, 0x50, 0x44, 0x46, 0x2D];

    private ImovelJaContext contexto = null!;
    private IPagamentoService servicoPagamento = null!;

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

        contexto.Imovels.Add(
            new Imovel
            {
                Id = 1,
                Nome = "Casa - Itabaiana",
                Descricao = "Casa térrea próxima ao Rotary Club.",
                Tipo = "C",
                AreaTotal = 38m,
                QuantidadeBanheiros = 1,
                QuantidadeQuartos = 1,
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
            });

        contexto.Aluguels.Add(
            new Aluguel
            {
                Id = 1,
                DiaPagamento = 5,
                DataVencimentoProximo = new DateTime(2026, 12, 5),
                Valor = 700m,
                Status = "A",
                IdImovel = 1,
                IdPessoaInquilino = 2
            });

        contexto.Pagamentos.Add(
            new Pagamento
            {
                Id = 1,
                MesReferencia = new DateTime(2026, 12, 1),
                ValorPago = 100m,
                DataPagamento = new DateTime(2026, 12, 3),
                Comprovante = ComprovanteAnterior,
                Status = "PE",
                IdAluguel = 1
            });

        contexto.SaveChanges();
        contexto.ChangeTracker.Clear();

        servicoPagamento = new PagamentoService(contexto);
    }

    [TestCleanup]
    public void Finalizar()
    {
        contexto.Dispose();
    }

    [TestMethod]
    public void Edit_QuandoNenhumComprovanteNovoEInformado_DeveManterOAnterior()
    {
        // Arrange
        var pagamento = PagamentoSemComprovanteNovo();

        // Act
        servicoPagamento.Edit(pagamento);
        var gravado = contexto.Pagamentos.AsNoTracking().Single(item => item.Id == 1);

        // Assert
        CollectionAssert.AreEqual(ComprovanteAnterior, gravado.Comprovante);
        Assert.AreEqual(200m, gravado.ValorPago);
    }

    [TestMethod]
    public void Edit_QuandoComprovanteNovoVemVazio_DeveManterOAnterior()
    {
        // Arrange
        var pagamento = PagamentoSemComprovanteNovo();
        pagamento.Comprovante = [];

        // Act
        servicoPagamento.Edit(pagamento);
        var gravado = contexto.Pagamentos.AsNoTracking().Single(item => item.Id == 1);

        // Assert
        CollectionAssert.AreEqual(ComprovanteAnterior, gravado.Comprovante);
    }

    [TestMethod]
    public void Edit_QuandoUmNovoComprovanteEEnviado_DeveSubstituirOAnterior()
    {
        // Arrange
        var novo = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var pagamento = PagamentoSemComprovanteNovo();
        pagamento.Comprovante = novo;

        // Act
        servicoPagamento.Edit(pagamento);
        var gravado = contexto.Pagamentos.AsNoTracking().Single(item => item.Id == 1);

        // Assert
        CollectionAssert.AreEqual(novo, gravado.Comprovante);
        CollectionAssert.AreNotEqual(ComprovanteAnterior, gravado.Comprovante);
    }

    private static Pagamento PagamentoSemComprovanteNovo()
    {
        return new Pagamento
        {
            Id = 1,
            MesReferencia = new DateTime(2026, 12, 15),
            ValorPago = 200m,
            DataPagamento = new DateTime(2026, 12, 4),
            Status = "PE",
            IdAluguel = 1
        };
    }
}
