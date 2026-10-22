using System;
using System.Collections.Generic;

namespace Core;

public partial class Plano
{
    public uint Id { get; set; }

    public string Nome { get; set; } = null!;

    public decimal Valor { get; set; }

    public uint LimiteImoveisAnunciados { get; set; }

    public string? Beneficios { get; set; }

    /// <summary>
    /// A = ATIVO, I = INATIVO
    /// </summary>
    public string Status { get; set; } = null!;

    public virtual ICollection<Assinatura> Assinaturas { get; set; } = new List<Assinatura>();
}
