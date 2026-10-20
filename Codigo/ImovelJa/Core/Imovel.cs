using System;
using System.Collections.Generic;

namespace Core;

public partial class Imovel
{
    public uint Id { get; set; }

    public string Nome { get; set; } = null!;

    public string? Descricao { get; set; }

    /// <summary>
    /// C = CASA, A = APARTAMENTO, T = TERRENO, S = STUDIO, CO = COMERCIAL, O = OUTRO
    /// </summary>
    public string Tipo { get; set; } = null!;

    public decimal? AreaTotal { get; set; }

    public uint? QuantidadeBanheiros { get; set; }

    public uint? QuantidadeQuartos { get; set; }

    public decimal? AreaGaragem { get; set; }

    public decimal? AreaCozinha { get; set; }

    public decimal? AreaSala { get; set; }

    public decimal? AreaQuintal { get; set; }

    public decimal? AreaLavanderia { get; set; }

    public string Rua { get; set; } = null!;

    public string Bairro { get; set; } = null!;

    public string Cep { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public string Cidade { get; set; } = null!;

    public decimal Valor { get; set; }

    /// <summary>
    /// A = ALUGUEL, V = VENDA
    /// </summary>
    public string Finalidade { get; set; } = null!;

    /// <summary>
    /// D = DISPONIVEL, A = ALUGADO, V = VENDIDO, P = PAUSADO
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// P = PENDENTE, A = APROVADO, R = REPROVADO
    /// </summary>
    public string StatusValidacao { get; set; } = null!;

    public uint IdPessoaProprietario { get; set; }

    public virtual ICollection<Aluguel> Aluguels { get; set; } = new List<Aluguel>();

    public virtual Pessoa IdPessoaProprietarioNavigation { get; set; } = null!;
}
