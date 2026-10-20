using System;
using System.Collections.Generic;

namespace Core;

public partial class Mensagem
{
    public uint Id { get; set; }

    public string Texto { get; set; } = null!;

    public DateTime DataEnvio { get; set; }

    public uint IdPessoaEnviou { get; set; }

    public uint IdPessoaRecebeu { get; set; }

    public virtual Pessoa IdPessoaEnviouNavigation { get; set; } = null!;

    public virtual Pessoa IdPessoaRecebeuNavigation { get; set; } = null!;
}
