using System;
using System.Collections.Generic;

namespace Core;

public partial class Pessoa
{
    public uint Id { get; set; }

    /// <summary>
    /// F = FISICA, J = JURIDICA
    /// </summary>
    public string Tipo { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Nome { get; set; } = null!;

    public string? Telefone { get; set; }

    public byte[]? FotoPerfil { get; set; }

    public string? Cpf { get; set; }

    public string? Cnpj { get; set; }

    public DateTime? DataNascimento { get; set; }

    /// <summary>
    /// A = ATIVO, B = BLOQUEADO
    /// </summary>
    public string Status { get; set; } = null!;

    public virtual ICollection<Aluguel> Aluguels { get; set; } = new List<Aluguel>();

    public virtual Assinatura? Assinatura { get; set; }

    public virtual ICollection<Imovel> Imovels { get; set; } = new List<Imovel>();

    public virtual ICollection<Mensagem> MensagemIdPessoaEnviouNavigations { get; set; } = new List<Mensagem>();

    public virtual ICollection<Mensagem> MensagemIdPessoaRecebeuNavigations { get; set; } = new List<Mensagem>();
}
