using System;
using System.Collections.Generic;

namespace Core;

public partial class Pagamento
{
    public uint Id { get; set; }

    public DateTime MesReferencia { get; set; }

    public decimal? ValorPago { get; set; }

    public DateTime? DataPagamento { get; set; }

    public byte[]? Comprovante { get; set; }

    /// <summary>
    /// PE = PENDENTE, EA = EM ATRASO, PA = PAGO
    /// </summary>
    public string Status { get; set; } = null!;

    public uint IdAluguel { get; set; }

    public virtual Aluguel IdAluguelNavigation { get; set; } = null!;
}
