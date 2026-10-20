using System;
using System.Collections.Generic;

namespace Core;

public partial class Aluguel
{
    public uint Id { get; set; }

    public string? NomeInquilino { get; set; }

    public string DocumentoInquilino { get; set; } = null!;

    public string? TelefoneInquilino { get; set; }

    public int DiaPagamento { get; set; }

    public DateTime? DataVencimentoProximo { get; set; }

    public decimal Valor { get; set; }

    public byte[]? ArquivoContrato { get; set; }

    /// <summary>
    /// A = ATIVO, C = CANCELADO
    /// </summary>
    public string Status { get; set; } = null!;

    public uint IdImovel { get; set; }

    public uint? IdPessoaInquilino { get; set; }

    public virtual Imovel IdImovelNavigation { get; set; } = null!;

    public virtual Pessoa? IdPessoaInquilinoNavigation { get; set; }

    public virtual ICollection<Pagamento> Pagamentos { get; set; } = new List<Pagamento>();
}
