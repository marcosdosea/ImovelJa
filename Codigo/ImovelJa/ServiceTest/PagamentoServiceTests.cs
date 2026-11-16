using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;

namespace Service.Tests;

[TestClass]
public class PagamentoServiceTests
{
    private const string MensagemMesJaRegistrado = "Já existe um pagamento registrado para este mês.";
    private const string MensagemDadosIncompletos = "Informe o valor e a data do pagamento.";
    private const string MensagemAluguelNaoEncontrado = "Aluguel não encontrado.";
    private const string MensagemPagamentoNaoEncontrado = "Pagamento não encontrado.";

    private ImovelJaContext contexto = null!;
    private IPagamentoService servicoPagamento = null!;

    // Datas relativas a hoje, porque o vencimento (RN02) depende de DateTime.Today.
    // Aluguel 1 vence no dia 10: MesPassado já venceu e MesFuturo ainda não.
    private static DateTime MesAtual => new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private static DateTime MesPassado => MesAtual.AddMonths(-2);
    private static DateTime MesFuturo => MesAtual.AddMonths(1);

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
                Status = "D",
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
                DiaPagamento = 31,
                Valor = 900m,
                Status = "A",
                IdImovel = 2,
                IdPessoaInquilino = 2
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

    private static Pagamento NovoPagamento(
        uint idAluguel,
        DateTime mesReferencia,
        string status = "PE",
        decimal? valorPago = null,
        DateTime? dataPagamento = null)
    {
        return new Pagamento
        {
            IdAluguel = idAluguel,
            MesReferencia = mesReferencia,
            Status = status,
            ValorPago = valorPago,
            DataPagamento = dataPagamento
        };
    }

    private uint Semear(Pagamento pagamento)
    {
        contexto.Pagamentos.Add(pagamento);
        contexto.SaveChanges();
        contexto.ChangeTracker.Clear();
        return pagamento.Id;
    }

    private Pagamento Gravado(uint id)
    {
        return contexto.Pagamentos.AsNoTracking().Single(pagamento => pagamento.Id == id);
    }

    [TestMethod]
    public void Create_QuandoMesReferenciaNaoEPrimeiroDia_DeveGravarNoPrimeiroDiaDoMes()
    {
        // Arrange
        var mesNoDia17 = MesPassado.AddDays(16);

        // Act
        var id = servicoPagamento.Create(NovoPagamento(1, mesNoDia17));

        // Assert
        var gravado = servicoPagamento.Get(id);

        Assert.IsNotNull(gravado);
        Assert.AreEqual(MesPassado, gravado.MesReferencia);
        Assert.AreEqual(1, gravado.MesReferencia.Day);
        Assert.AreEqual(1u, gravado.IdAluguel);
    }

    [TestMethod]
    public void Create_QuandoMesJaRegistradoParaOMesmoAluguel_DeveLancarServiceException()
    {
        // Arrange
        Semear(NovoPagamento(1, MesPassado, "EA"));
        var quantidade = contexto.Pagamentos.Count();

        // Act (outro dia do mesmo mês: o serviço normaliza para o dia 1 antes de comparar)
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoPagamento.Create(NovoPagamento(1, MesPassado.AddDays(9))));

        // Assert
        Assert.AreEqual(MensagemMesJaRegistrado, excecao.Message);
        Assert.AreEqual(quantidade, contexto.Pagamentos.Count());
    }

    [TestMethod]
    public void Create_QuandoMesJaRegistradoEmOutroAluguel_DeveGravar()
    {
        // Arrange
        Semear(NovoPagamento(1, MesPassado, "EA"));

        // Act
        var id = servicoPagamento.Create(NovoPagamento(2, MesPassado));

        // Assert
        var gravado = Gravado(id);

        Assert.AreEqual(2u, gravado.IdAluguel);
        Assert.AreEqual(MesPassado, gravado.MesReferencia);
        Assert.AreEqual(2, contexto.Pagamentos.Count());
    }

    [TestMethod]
    public void Edit_QuandoMesJaRegistradoEmOutroPagamentoDoAluguel_DeveLancarServiceException()
    {
        // Arrange
        var mesDoSegundo = MesAtual.AddMonths(-1);
        Semear(NovoPagamento(1, MesPassado, "EA"));
        var idSegundo = Semear(NovoPagamento(1, mesDoSegundo, "EA"));

        var edicao = NovoPagamento(1, MesPassado, "EA");
        edicao.Id = idSegundo;

        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoPagamento.Edit(edicao));

        // Assert
        Assert.AreEqual(MensagemMesJaRegistrado, excecao.Message);
        Assert.AreEqual(mesDoSegundo, Gravado(idSegundo).MesReferencia);
    }

    [TestMethod]
    public void Create_QuandoMarcadoComoPagoComValorParcial_DeveGravarComoPagoMesmoVencido()
    {
        // Act
        var id = servicoPagamento.Create(NovoPagamento(1, MesPassado, "PA", 500m, DateTime.Today));

        // Assert
        Assert.AreEqual("PA", Gravado(id).Status);
    }

    [TestMethod]
    public void Create_QuandoValorPagoIgualAoDoAluguel_DeveGravarComoPago()
    {
        // Act
        var id = servicoPagamento.Create(NovoPagamento(1, MesFuturo, "PE", 1200m, DateTime.Today));

        // Assert
        Assert.AreEqual("PA", Gravado(id).Status);
    }

    [TestMethod]
    public void Create_QuandoValorPagoMaiorQueODoAluguel_DeveGravarComoPago()
    {
        // Act
        var id = servicoPagamento.Create(NovoPagamento(1, MesFuturo, "PE", 1500m, DateTime.Today));

        // Assert
        Assert.AreEqual("PA", Gravado(id).Status);
    }

    [TestMethod]
    public void Create_QuandoValorParcialEVencimentoPassou_DeveGravarEmAtraso()
    {
        // Act
        var id = servicoPagamento.Create(NovoPagamento(1, MesPassado, "PE", 600m, DateTime.Today));

        // Assert
        Assert.AreEqual("EA", Gravado(id).Status);
    }

    [TestMethod]
    public void Create_QuandoSemPagamentoEVencimentoPassou_DeveGravarEmAtraso()
    {
        // Act
        var id = servicoPagamento.Create(NovoPagamento(1, MesPassado));

        // Assert
        Assert.AreEqual("EA", Gravado(id).Status);
    }

    [TestMethod]
    public void Create_QuandoVencimentoAindaNaoPassou_DeveGravarComoPendente()
    {
        // Act
        var id = servicoPagamento.Create(NovoPagamento(1, MesFuturo));

        // Assert
        Assert.AreEqual("PE", Gravado(id).Status);
    }

    [TestMethod]
    public void Create_QuandoMesCorrenteAntesDoVencimento_DeveGravarComoPendente()
    {
        // Act 
        var id = servicoPagamento.Create(NovoPagamento(2, MesAtual));

        // Assert
        Assert.AreEqual("PE", Gravado(id).Status);
    }

    [TestMethod]
    public void Create_QuandoMarcadoComoPagoSemValor_DeveLancarServiceException()
    {
        // Arrange
        var quantidade = contexto.Pagamentos.Count();

        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoPagamento.Create(NovoPagamento(1, MesPassado, "PA", null, DateTime.Today)));

        // Assert
        Assert.AreEqual(MensagemDadosIncompletos, excecao.Message);
        Assert.AreEqual(quantidade, contexto.Pagamentos.Count());
    }

    [TestMethod]
    public void Create_QuandoMarcadoComoPagoSemData_DeveLancarServiceException()
    {
        // Arrange
        var quantidade = contexto.Pagamentos.Count();

        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoPagamento.Create(NovoPagamento(1, MesPassado, "PA", 1200m, null)));

        // Assert
        Assert.AreEqual(MensagemDadosIncompletos, excecao.Message);
        Assert.AreEqual(quantidade, contexto.Pagamentos.Count());
    }

    [TestMethod]
    public void Create_QuandoValorAtingeOAluguelSemData_DeveLancarServiceException()
    {
        // Arrange
        var quantidade = contexto.Pagamentos.Count();

        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoPagamento.Create(NovoPagamento(1, MesFuturo, "PE", 1200m, null)));

        // Assert
        Assert.AreEqual(MensagemDadosIncompletos, excecao.Message);
        Assert.AreEqual(quantidade, contexto.Pagamentos.Count());
    }

    [TestMethod]
    public void Edit_QuandoMarcadoComoPagoSemValorOuData_DeveLancarServiceException()
    {
        // Arrange
        var id = Semear(NovoPagamento(1, MesPassado, "EA"));

        var edicao = NovoPagamento(1, MesPassado, "PA");
        edicao.Id = id;

        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoPagamento.Edit(edicao));

        // Assert
        Assert.AreEqual(MensagemDadosIncompletos, excecao.Message);
        Assert.AreEqual("EA", Gravado(id).Status);
    }

    [TestMethod]
    public void Create_QuandoAluguelNaoExiste_DeveLancarServiceException()
    {
        // Arrange
        var quantidade = contexto.Pagamentos.Count();

        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoPagamento.Create(NovoPagamento(99, MesPassado)));

        // Assert
        Assert.AreEqual(MensagemAluguelNaoEncontrado, excecao.Message);
        Assert.AreEqual(quantidade, contexto.Pagamentos.Count());
    }

    [TestMethod]
    public void Edit_QuandoAluguelNaoExiste_DeveLancarServiceException()
    {
        // Arrange
        var id = Semear(NovoPagamento(1, MesPassado, "EA"));

        var edicao = NovoPagamento(99, MesPassado, "EA");
        edicao.Id = id;

        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoPagamento.Edit(edicao));

        // Assert
        Assert.AreEqual(MensagemAluguelNaoEncontrado, excecao.Message);
        Assert.AreEqual(1u, Gravado(id).IdAluguel);
    }

    [TestMethod]
    public void Edit_QuandoPagamentoNaoExiste_DeveLancarServiceException()
    {
        // Arrange
        var edicao = NovoPagamento(1, MesPassado);
        edicao.Id = 999;

        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoPagamento.Edit(edicao));

        // Assert
        Assert.AreEqual(MensagemPagamentoNaoEncontrado, excecao.Message);
    }

    [TestMethod]
    public void Edit_QuandoDadosValidos_DeveAtualizarPagamentoERecalcularStatus()
    {
        // Arrange
        var id = Semear(NovoPagamento(1, MesPassado, "EA"));

        var edicao = NovoPagamento(1, MesPassado.AddDays(4), "EA", 1200m, DateTime.Today);
        edicao.Id = id;

        // Act (mantém o mesmo mês do próprio pagamento, o que não é conflito de RN12)
        servicoPagamento.Edit(edicao);

        // Assert
        var gravado = Gravado(id);

        Assert.AreEqual("PA", gravado.Status);
        Assert.AreEqual<decimal?>(1200m, gravado.ValorPago);
        Assert.AreEqual<DateTime?>(DateTime.Today, gravado.DataPagamento);
        Assert.AreEqual(MesPassado, gravado.MesReferencia);
        Assert.AreEqual(1, contexto.Pagamentos.Count());
    }

    [TestMethod]
    public void Edit_QuandoDesmarcaPagoEValorFicaAbaixoDoAluguel_DeveVoltarParaPendente()
    {
        // Arrange
        var id = Semear(NovoPagamento(1, MesFuturo, "PA", 1200m, DateTime.Today));

        var edicao = NovoPagamento(1, MesFuturo, "PE", 500m, DateTime.Today);
        edicao.Id = id;

        // Act
        servicoPagamento.Edit(edicao);

        // Assert
        var gravado = Gravado(id);

        Assert.AreEqual("PE", gravado.Status);
        Assert.AreEqual<decimal?>(500m, gravado.ValorPago);
    }

    [TestMethod]
    public void GetByAluguel_QuandoHaPagamentos_DeveRetornarSomenteDoAluguelEmOrdemDeMes()
    {
        // Arrange
        Semear(NovoPagamento(1, MesFuturo));
        Semear(NovoPagamento(1, MesPassado, "EA"));
        Semear(NovoPagamento(2, MesPassado, "EA"));
        Semear(NovoPagamento(1, MesAtual.AddMonths(-1), "EA"));

        // Act
        var pagamentos = servicoPagamento.GetByAluguel(1).ToList();

        // Assert
        Assert.HasCount(3, pagamentos);
        Assert.IsTrue(pagamentos.All(pagamento => pagamento.IdAluguel == 1));
        CollectionAssert.AreEqual(
            new[] { MesPassado, MesAtual.AddMonths(-1), MesFuturo },
            pagamentos.Select(pagamento => pagamento.MesReferencia).ToList());
    }

    [TestMethod]
    public void GetByAluguel_QuandoPendenteVencido_DeveRetornarEmAtrasoSemGravarNoBanco()
    {
        // Arrange
        var mesPago = MesAtual.AddMonths(-3);
        var mesVencido = MesAtual.AddMonths(-2);
        var mesEmAtraso = MesAtual.AddMonths(-1);
        var mesFuturo = MesFuturo;

        Semear(NovoPagamento(1, mesPago, "PA", 1200m, DateTime.Today));
        var idVencido = Semear(NovoPagamento(1, mesVencido, "PE"));
        Semear(NovoPagamento(1, mesEmAtraso, "EA"));
        Semear(NovoPagamento(1, mesFuturo, "PE"));

        // Act
        var pagamentos = servicoPagamento.GetByAluguel(1).ToList();

        // Assert
        Assert.HasCount(4, pagamentos);
        Assert.AreEqual("PA", pagamentos.Single(pagamento => pagamento.MesReferencia == mesPago).Status);
        Assert.AreEqual("EA", pagamentos.Single(pagamento => pagamento.MesReferencia == mesVencido).Status);
        Assert.AreEqual("EA", pagamentos.Single(pagamento => pagamento.MesReferencia == mesEmAtraso).Status);
        Assert.AreEqual("PE", pagamentos.Single(pagamento => pagamento.MesReferencia == mesFuturo).Status);

        Assert.AreEqual("PE", Gravado(idVencido).Status);
    }

    [TestMethod]
    public void GetByAluguel_QuandoAluguelNaoTemPagamentos_DeveRetornarListaVazia()
    {
        // Arrange
        Semear(NovoPagamento(1, MesPassado, "EA"));

        // Act
        var pagamentos = servicoPagamento.GetByAluguel(2).ToList();

        // Assert
        Assert.HasCount(0, pagamentos);
    }
}