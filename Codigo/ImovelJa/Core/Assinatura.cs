using System;
using System.Collections.Generic;

namespace Core;

public partial class Assinatura
{
    public uint Id { get; set; }

    public DateTime DataContratacao { get; set; }

    public DateTime DataProximaCobranca { get; set; }

    /// <summary>
    /// A = ATIVO, C = CANCELADO
    /// </summary>
    public string Status { get; set; } = null!;

    public uint IdPessoa { get; set; }

    public uint IdPlano { get; set; }

    public virtual Pessoa IdPessoaNavigation { get; set; } = null!;

    public virtual Plano IdPlanoNavigation { get; set; } = null!;
}
