using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Core;

public partial class ImovelJaContext : DbContext
{
    public ImovelJaContext()
    {
    }

    public ImovelJaContext(DbContextOptions<ImovelJaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Aluguel> Aluguels { get; set; }

    public virtual DbSet<Assinatura> Assinaturas { get; set; }

    public virtual DbSet<Imovel> Imovels { get; set; }

    public virtual DbSet<Mensagem> Mensagems { get; set; }

    public virtual DbSet<Pagamento> Pagamentos { get; set; }

    public virtual DbSet<Pessoa> Pessoas { get; set; }

    public virtual DbSet<Plano> Planos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Aluguel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("aluguel");

            entity.HasIndex(e => e.IdImovel, "FK_Aluguel_Imovel");

            entity.HasIndex(e => e.IdPessoaInquilino, "FK_Aluguel_PessoaInquilino");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ArquivoContrato)
                .HasColumnType("mediumblob")
                .HasColumnName("arquivoContrato");
            entity.Property(e => e.DataVencimentoProximo)
                .HasColumnType("date")
                .HasColumnName("dataVencimentoProximo");
            entity.Property(e => e.DiaPagamento).HasColumnName("diaPagamento");
            entity.Property(e => e.DocumentoInquilino)
                .HasMaxLength(14)
                .HasColumnName("documentoInquilino");
            entity.Property(e => e.IdImovel).HasColumnName("idImovel");
            entity.Property(e => e.IdPessoaInquilino).HasColumnName("idPessoaInquilino");
            entity.Property(e => e.NomeInquilino)
                .HasMaxLength(100)
                .HasColumnName("nomeInquilino");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'A'")
                .HasComment("A = ATIVO, C = CANCELADO")
                .HasColumnType("enum('A','C')")
                .HasColumnName("status");
            entity.Property(e => e.TelefoneInquilino)
                .HasMaxLength(20)
                .HasColumnName("telefoneInquilino");
            entity.Property(e => e.Valor)
                .HasPrecision(12)
                .HasColumnName("valor");

            entity.HasOne(d => d.IdImovelNavigation).WithMany(p => p.Aluguels)
                .HasForeignKey(d => d.IdImovel)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Aluguel_Imovel");

            entity.HasOne(d => d.IdPessoaInquilinoNavigation).WithMany(p => p.Aluguels)
                .HasForeignKey(d => d.IdPessoaInquilino)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Aluguel_PessoaInquilino");
        });

        modelBuilder.Entity<Assinatura>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("assinatura");

            entity.HasIndex(e => e.IdPlano, "FK_Assinatura_Plano");

            entity.HasIndex(e => e.IdPessoa, "IDX_AssinaturaIdPessoa").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DataContratacao)
                .HasColumnType("date")
                .HasColumnName("dataContratacao");
            entity.Property(e => e.DataProximaCobranca)
                .HasColumnType("date")
                .HasColumnName("dataProximaCobranca");
            entity.Property(e => e.IdPessoa).HasColumnName("idPessoa");
            entity.Property(e => e.IdPlano).HasColumnName("idPlano");
            entity.Property(e => e.Status)
                .HasComment("A = ATIVO, C = CANCELADO")
                .HasColumnType("enum('A','C')")
                .HasColumnName("status");

            entity.HasOne(d => d.IdPessoaNavigation).WithOne(p => p.Assinatura)
                .HasForeignKey<Assinatura>(d => d.IdPessoa)
                .HasConstraintName("FK_Assinatura_Pessoa");

            entity.HasOne(d => d.IdPlanoNavigation).WithMany(p => p.Assinaturas)
                .HasForeignKey(d => d.IdPlano)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Assinatura_Plano");
        });

        modelBuilder.Entity<Imovel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("imovel");

            entity.HasIndex(e => e.IdPessoaProprietario, "FK_Imovel_PessoaProprietario");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AreaCozinha)
                .HasPrecision(10)
                .HasColumnName("areaCozinha");
            entity.Property(e => e.AreaGaragem)
                .HasPrecision(10)
                .HasColumnName("areaGaragem");
            entity.Property(e => e.AreaLavanderia)
                .HasPrecision(10)
                .HasColumnName("areaLavanderia");
            entity.Property(e => e.AreaQuintal)
                .HasPrecision(10)
                .HasColumnName("areaQuintal");
            entity.Property(e => e.AreaSala)
                .HasPrecision(10)
                .HasColumnName("areaSala");
            entity.Property(e => e.AreaTotal)
                .HasPrecision(10)
                .HasColumnName("areaTotal");
            entity.Property(e => e.Bairro)
                .HasMaxLength(100)
                .HasColumnName("bairro");
            entity.Property(e => e.Cep)
                .HasMaxLength(8)
                .IsFixedLength()
                .HasColumnName("cep");
            entity.Property(e => e.Cidade)
                .HasMaxLength(100)
                .HasColumnName("cidade");
            entity.Property(e => e.Descricao)
                .HasColumnType("text")
                .HasColumnName("descricao");
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .HasColumnName("estado");
            entity.Property(e => e.Finalidade)
                .HasComment("A = ALUGUEL, V = VENDA")
                .HasColumnType("enum('A','V')")
                .HasColumnName("finalidade");
            entity.Property(e => e.IdPessoaProprietario).HasColumnName("idPessoaProprietario");
            entity.Property(e => e.Nome)
                .HasMaxLength(150)
                .HasColumnName("nome");
            entity.Property(e => e.QuantidadeBanheiros).HasColumnName("quantidadeBanheiros");
            entity.Property(e => e.QuantidadeQuartos).HasColumnName("quantidadeQuartos");
            entity.Property(e => e.Rua)
                .HasMaxLength(150)
                .HasColumnName("rua");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'D'")
                .HasComment("D = DISPONIVEL, A = ALUGADO, V = VENDIDO, P = PAUSADO")
                .HasColumnType("enum('D','A','V','P')")
                .HasColumnName("status");
            entity.Property(e => e.StatusValidacao)
                .HasDefaultValueSql("'P'")
                .HasComment("P = PENDENTE, A = APROVADO, R = REPROVADO")
                .HasColumnType("enum('P','A','R')")
                .HasColumnName("statusValidacao");
            entity.Property(e => e.Tipo)
                .HasComment("C = CASA, A = APARTAMENTO, T = TERRENO, S = STUDIO, CO = COMERCIAL, O = OUTRO")
                .HasColumnType("enum('C','A','T','S','CO','O')")
                .HasColumnName("tipo");
            entity.Property(e => e.Valor)
                .HasPrecision(12)
                .HasColumnName("valor");

            entity.HasOne(d => d.IdPessoaProprietarioNavigation).WithMany(p => p.Imovels)
                .HasForeignKey(d => d.IdPessoaProprietario)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Imovel_PessoaProprietario");
        });

        modelBuilder.Entity<Mensagem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("mensagem");

            entity.HasIndex(e => e.IdPessoaEnviou, "FK_Mensagem_PessoaEnviou");

            entity.HasIndex(e => e.IdPessoaRecebeu, "FK_Mensagem_PessoaRecebeu");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DataEnvio)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("dataEnvio");
            entity.Property(e => e.IdPessoaEnviou).HasColumnName("idPessoaEnviou");
            entity.Property(e => e.IdPessoaRecebeu).HasColumnName("idPessoaRecebeu");
            entity.Property(e => e.Texto)
                .HasColumnType("text")
                .HasColumnName("texto");

            entity.HasOne(d => d.IdPessoaEnviouNavigation).WithMany(p => p.MensagemIdPessoaEnviouNavigations)
                .HasForeignKey(d => d.IdPessoaEnviou)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Mensagem_PessoaEnviou");

            entity.HasOne(d => d.IdPessoaRecebeuNavigation).WithMany(p => p.MensagemIdPessoaRecebeuNavigations)
                .HasForeignKey(d => d.IdPessoaRecebeu)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Mensagem_PessoaRecebeu");
        });

        modelBuilder.Entity<Pagamento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("pagamento");

            entity.HasIndex(e => e.IdAluguel, "FK_Pagamento_Aluguel");

            entity.HasIndex(e => new { e.IdAluguel, e.MesReferencia }, "IDX_PagamentoIdAluguelMes").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Comprovante)
                .HasColumnType("mediumblob")
                .HasColumnName("comprovante");
            entity.Property(e => e.DataPagamento)
                .HasColumnType("date")
                .HasColumnName("dataPagamento");
            entity.Property(e => e.IdAluguel).HasColumnName("idAluguel");
            entity.Property(e => e.MesReferencia)
                .HasColumnType("date")
                .HasColumnName("mesReferencia");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'PE'")
                .HasComment("PE = PENDENTE, EA = EM ATRASO, PA = PAGO")
                .HasColumnType("enum('PE','EA','PA')")
                .HasColumnName("status");
            entity.Property(e => e.ValorPago)
                .HasPrecision(12)
                .HasColumnName("valorPago");

            entity.HasOne(d => d.IdAluguelNavigation).WithMany(p => p.Pagamentos)
                .HasForeignKey(d => d.IdAluguel)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Pagamento_Aluguel");
        });

        modelBuilder.Entity<Pessoa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("pessoa");

            entity.HasIndex(e => e.Cnpj, "IDX_PessoaCnpj").IsUnique();

            entity.HasIndex(e => e.Cpf, "IDX_PessoaCpf").IsUnique();

            entity.HasIndex(e => e.Email, "IDX_PessoaEmail").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Cnpj)
                .HasMaxLength(14)
                .IsFixedLength()
                .HasColumnName("cnpj");
            entity.Property(e => e.Cpf)
                .HasMaxLength(11)
                .IsFixedLength()
                .HasColumnName("cpf");
            entity.Property(e => e.DataNascimento)
                .HasColumnType("date")
                .HasColumnName("dataNascimento");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.FotoPerfil)
                .HasColumnType("mediumblob")
                .HasColumnName("fotoPerfil");
            entity.Property(e => e.Nome)
                .HasMaxLength(150)
                .HasColumnName("nome");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'A'")
                .HasComment("A = ATIVO, B = BLOQUEADO")
                .HasColumnType("enum('A','B')")
                .HasColumnName("status");
            entity.Property(e => e.Telefone)
                .HasMaxLength(20)
                .HasColumnName("telefone");
            entity.Property(e => e.Tipo)
                .HasComment("F = FISICA, J = JURIDICA")
                .HasColumnType("enum('F','J')")
                .HasColumnName("tipo");
        });

        modelBuilder.Entity<Plano>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("plano");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Beneficios)
                .HasColumnType("text")
                .HasColumnName("beneficios");
            entity.Property(e => e.LimiteImoveisAnunciados).HasColumnName("limiteImoveisAnunciados");
            entity.Property(e => e.Nome)
                .HasMaxLength(100)
                .HasColumnName("nome");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'A'")
                .HasComment("A = ATIVO, I = INATIVO")
                .HasColumnType("enum('A','I')")
                .HasColumnName("status");
            entity.Property(e => e.Valor)
                .HasPrecision(10)
                .HasColumnName("valor");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
