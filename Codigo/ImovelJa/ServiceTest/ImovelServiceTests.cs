using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using Core.DTO;

namespace Service.Tests;

[TestClass]
public class ImovelServiceTests
{
    private ImovelJaContext contexto = null!;
    private IImovelService servicoImovel = null!;

    [TestInitialize]
    public void Inicializar()
    {
        var nomeBanco = $"ImovelJa_{Guid.NewGuid()}";

        var opcoes = new DbContextOptionsBuilder<ImovelJaContext>()
            .UseInMemoryDatabase(nomeBanco)
            .Options;

        contexto = new ImovelJaContext(opcoes);

        contexto.Pessoas.Add(new Pessoa
        {
            Id = 1,
            Tipo = "F",
            Nome = "Maria José Santos",
            Email = "maria.santos@gmail.com",
            Telefone = "79998123456",
            Cpf = "52998224725",
            DataNascimento = new DateTime(1985, 3, 14),
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
                StatusValidacao = "P",
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
                StatusValidacao = "P",
                IdPessoaProprietario = 1
            },
            new Imovel
            {
                Id = 3,
                Nome = "Studio no bairro São Cristóvão",
                Descricao = "Studio compacto com cozinha americana.",
                Tipo = "S",
                AreaTotal = 32m,
                QuantidadeBanheiros = 1,
                QuantidadeQuartos = 1,
                QuantidadeVagasGaragem = 0,
                Rua = "Rua Doutor Airton Teles, 88",
                Bairro = "São Cristóvão",
                Cep = "49504200",
                Cidade = "Itabaiana",
                Estado = "Sergipe",
                Valor = 650m,
                Finalidade = "A",
                Status = "D",
                StatusValidacao = "A",
                IdPessoaProprietario = 1
            },
            new Imovel
            {
                Id = 4,
                Nome = "Terreno no Marianga",
                Descricao = "Terreno plano de esquina.",
                Tipo = "T",
                AreaTotal = 360m,
                Rua = "Rua Projetada B, s/n",
                Bairro = "Marianga",
                Cep = "49507000",
                Cidade = "Itabaiana",
                Estado = "Sergipe",
                Valor = 85000m,
                Finalidade = "V",
                Status = "D",
                StatusValidacao = "R",
                IdPessoaProprietario = 1
            },


            new Imovel
            {
                Id = 5,
                Nome = "Apartamento 2 quartos nos Jardins",
                Tipo = "A",
                AreaTotal = 62.5m,
                QuantidadeQuartos = 2,
                Rua = "Rua Itabaianinha, 410",
                Bairro = "Jardins",
                Cep = "49025000",
                Cidade = "Aracaju",
                Estado = "Sergipe",
                Valor = 1450.50m,
                Finalidade = "A",
                Status = "D",
                StatusValidacao = "A",
                IdPessoaProprietario = 1
            },

            new Imovel
            {
                Id = 6,
                Nome = "Apartamento 3 quartos nos Jardins",
                Descricao = "Apartamento no ultimo andar.",
                Tipo = "A",
                AreaTotal = 82.5m,
                QuantidadeQuartos = 3,
                Rua = "Rua Itabaianinha, 430",
                Bairro = "Jardins",
                Cep = "49025000",
                Cidade = "Aracaju",
                Estado = "Sergipe",
                Valor = 1450.50m,
                Finalidade = "A",
                Status = "P",
                StatusValidacao = "A",
                IdPessoaProprietario = 1
            }

            );

        contexto.SaveChanges();

        servicoImovel = new ImovelService(contexto);
    }

    [TestCleanup]
    public void Finalizar()
    {
        contexto.Dispose();
    }

    [TestMethod]
    public void GetByValidacao_QuandoExistemAnunciosPendentes_DeveRetornarSomenteOsPendentes()
    {
        // Act
        var imoveis = servicoImovel.GetByValidacao("P").ToList();

        // Assert
        Assert.HasCount(2, imoveis);
        Assert.IsTrue(imoveis.All(imovel => imovel.StatusValidacao == "P"));

        CollectionAssert.AreEquivalent(
            new uint[] { 1, 2 },
            imoveis.Select(imovel => imovel.Id).ToList());
    }

    [TestMethod]
    public void GetByValidacao_QuandoNaoHaAnunciosPendentes_DeveRetornarColecaoVazia()
    {
        // Arrange
        contexto.Imovels.RemoveRange(
            contexto.Imovels.Where(imovel => imovel.StatusValidacao == "P"));
        contexto.SaveChanges();

        // Act
        var imoveis = servicoImovel.GetByValidacao("P").ToList();

        // Assert
        Assert.IsNotNull(imoveis);
        Assert.IsEmpty(imoveis);
    }

    [TestMethod]
    public void Get_QuandoIdExiste_DeveRetornarImovelCorrespondente()
    {
        // Act
        var imovel = servicoImovel.Get(1);

        // Assert
        Assert.IsNotNull(imovel);
        Assert.AreEqual(1u, imovel.Id);
        Assert.AreEqual("Casa com 3 quartos no Centro", imovel.Nome);
        Assert.AreEqual("C", imovel.Tipo);
        Assert.AreEqual("Rua Sete de Setembro, 245", imovel.Rua);
        Assert.AreEqual("Centro", imovel.Bairro);
        Assert.AreEqual("Itabaiana", imovel.Cidade);
        Assert.AreEqual(1200m, imovel.Valor);
        Assert.AreEqual("P", imovel.StatusValidacao);
        Assert.AreEqual(1u, imovel.IdPessoaProprietario);
    }

    [TestMethod]
    public void Get_QuandoIdNaoExiste_DeveRetornarNulo()
    {
        // Act
        var imovel = servicoImovel.Get(999);

        // Assert
        Assert.IsNull(imovel);
    }

    [TestMethod]
    public void Approve_QuandoAnuncioPendenteEAprovado_DeveAlterarStatusValidacaoParaAprovado()
    {
        // Act
        servicoImovel.Approve(1, true);

        // Assert
        var imovel = servicoImovel.Get(1);

        Assert.IsNotNull(imovel);
        Assert.AreEqual("A", imovel.StatusValidacao);
    }

    [TestMethod]
    public void Approve_QuandoAnuncioPendenteEReprovado_DeveAlterarStatusValidacaoParaReprovado()
    {
        // Act
        servicoImovel.Approve(2, false);

        // Assert
        var imovel = servicoImovel.Get(2);

        Assert.IsNotNull(imovel);
        Assert.AreEqual("R", imovel.StatusValidacao);
    }

    [TestMethod]
    public void Approve_QuandoAnuncioPendenteEValidado_DeveRemoverAnuncioDosPendentes()
    {
        // Act
        servicoImovel.Approve(1, true);

        // Assert
        var pendentes = servicoImovel.GetByValidacao("P").ToList();

        Assert.HasCount(1, pendentes);
        Assert.AreEqual(2u, pendentes[0].Id);
    }

    [TestMethod]
    public void Approve_QuandoAnuncioPendenteEAprovado_DeveExibirAnuncioNaBusca()
    {
        // Act
        servicoImovel.Approve(1, true);

        // Assert
        var imoveis = servicoImovel.GetByNome("Casa com 3 quartos").ToList();

        Assert.HasCount(1, imoveis);
        Assert.AreEqual(1u, imoveis[0].Id);
    }

    [TestMethod]
    public void Approve_QuandoAnuncioPendenteEReprovado_NaoDeveExibirAnuncioNaBusca()
    {
        // Act
        servicoImovel.Approve(2, false);

        // Assert
        var imoveis = servicoImovel.GetByNome("Apartamento mobiliado").ToList();

        Assert.IsNotNull(imoveis);
        Assert.IsEmpty(imoveis);
    }

    [TestMethod]
    public void Approve_QuandoAnuncioJaFoiAprovado_DeveLancarServiceException()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoImovel.Approve(3, false));

        // Assert
        Assert.AreEqual("Somente anúncios pendentes podem ser validados.", excecao.Message);

        var imovel = servicoImovel.Get(3);

        Assert.IsNotNull(imovel);
        Assert.AreEqual("A", imovel.StatusValidacao);
    }

    [TestMethod]
    public void Approve_QuandoAnuncioJaFoiReprovado_DeveLancarServiceException()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoImovel.Approve(4, true));

        // Assert
        Assert.AreEqual("Somente anúncios pendentes podem ser validados.", excecao.Message);

        var imovel = servicoImovel.Get(4);

        Assert.IsNotNull(imovel);
        Assert.AreEqual("R", imovel.StatusValidacao);
    }

    [TestMethod]
    public void Approve_QuandoImovelNaoExiste_DeveLancarServiceException()
    {
        // Act
        var excecao = Assert.ThrowsExactly<ServiceException>(
            () => servicoImovel.Approve(999, true));

        // Assert
        Assert.AreEqual("Imóvel não encontrado.", excecao.Message);
    }

    [TestMethod]
    public void GetByFiltro_QuandoNaoExistemFiltros_DeveRetornarColecaoComTodosImoveis()
    {
        // Act
        var filtro = new ImovelFiltroDTO();
        var imoveis = servicoImovel.GetByFiltro(filtro);

        // Assert
        Assert.HasCount(2, imoveis);
    }

    [TestMethod]
    public void GetByFiltro_QuandoExistemImoveisQueAtendamOsCriterios_DeveRetornarSomenteImoveisQueAtendam()
    {
        // Act
        var filtro = new ImovelFiltroDTO
        {
            Termo = "Apartamento",
            Localizacao = "Jardins",
            Tipos = new List<string> { "A" },
            ValorMinimo = 1000m,
            ValorMaximo = 2000m,
            QuantidadeQuartos = 2
        };
        var imoveis = servicoImovel.GetByFiltro(filtro);
        // Assert
        Assert.HasCount(1, imoveis);
        Assert.AreEqual(5u, imoveis.First().Id);
    }

    [TestMethod]
    public void GetByFiltro_QuandoNaoExistemImoveisQueAtendamOsCriterios_DeveRetornarColecaoVazia()
    {
        // Act
        var filtro = new ImovelFiltroDTO
        {
            Termo = "Casa",
            Localizacao = "Jardins",
            Tipos = new List<string> { "A" },
            ValorMinimo = 1000m,
            ValorMaximo = 2000m,
            QuantidadeQuartos = 2
        };
        var imoveis = servicoImovel.GetByFiltro(filtro);
        // Assert
        Assert.IsNotNull(imoveis);
        Assert.IsEmpty(imoveis);
    }


    [TestMethod]
    public void GetByFiltro_QuandoExistemImoveisQueAtendamOsCriteriosPorArea_DeveRetornarSomenteImoveisQueAtendam()
    {
        // Act
        var filtro = new ImovelFiltroDTO
        {
            AreaMinima = 60m,
            AreaMaxima = 90m
        };
        var imoveis = servicoImovel.GetByFiltro(filtro);
        // Assert
        Assert.HasCount(1, imoveis);
        CollectionAssert.AreEquivalent(
            new uint[] { 5 },
            imoveis.Select(imovel => imovel.Id).ToList());
    }

    [TestMethod]
    public void GetByFiltro_QuandoExistemImoveisQueAtendamOsCriteriosPorQuantidadeQuartos_DeveRetornarSomenteImoveisQueAtendam()
    {
        // Act
        var filtro = new ImovelFiltroDTO
        {
            QuantidadeQuartos = 1
        };
        var imoveis = servicoImovel.GetByFiltro(filtro);
        // Assert
        Assert.HasCount(1, imoveis);
        Assert.AreEqual(3u, imoveis.First().Id);
    }

    [TestMethod]
    public void GetByFiltro_QuandoExistemImoveisQueAtendamOsCriteriosPorValor_DeveRetornarSomenteImoveisQueAtendam()
    {
        // Act
        var filtro = new ImovelFiltroDTO
        {
            ValorMinimo = 600m,
            ValorMaximo = 1000m
        };
        var imoveis = servicoImovel.GetByFiltro(filtro);
        // Assert
        Assert.HasCount(1, imoveis);
        Assert.AreEqual(3u, imoveis.First().Id);
    }

    [TestMethod]
    public void GetByFiltro_QuandoExistemImoveisQueAtendamOsCriteriosPorTipo_DeveRetornarSomenteImoveisQueAtendam()
    {
        // Act
        var filtro = new ImovelFiltroDTO
        {
            Tipos = new List<string> { "A" }
        };
        var imoveis = servicoImovel.GetByFiltro(filtro);
        // Assert
        Assert.HasCount(1, imoveis);
        CollectionAssert.AreEquivalent(
            new uint[] { 5 },
            imoveis.Select(imovel => imovel.Id).ToList());
    }

    [TestMethod]
    public void GetByFiltro_QuandoExistemImoveisQueAtendamOsCriteriosPorTermo_DeveRetornarSomenteImoveisQueAtendam()
    {
        // Act
        var filtro = new ImovelFiltroDTO
        {
            Termo = "Studio"
        };
        var imoveis = servicoImovel.GetByFiltro(filtro);
        // Assert
        Assert.HasCount(1, imoveis);
        Assert.AreEqual(3u, imoveis.First().Id);
    }
}
