using Core;
using Core.DTO;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service
{
    public class ImovelService : IImovelService
    {
        private readonly ImovelJaContext context;

        public ImovelService(ImovelJaContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Cadastra um novo anúncio de imóvel, que fica pendente de validação pelo administrador
        /// </summary>
        /// <param name="imovel">dados do imóvel</param>
        /// <returns>id do imóvel criado</returns>
        /// <exception cref="ServiceException">proprietário não encontrado ou limite de imóveis do plano atingido</exception>
        public uint Create(Imovel imovel)
        {
            if (!context.Pessoas.Any(p => p.Id == imovel.IdPessoaProprietario))
                throw new ServiceException("Proprietário não encontrado.");

            var plano = ObterPlanoAtual(imovel.IdPessoaProprietario);
            int quantidade = context.Imovels.Count(i => i.IdPessoaProprietario == imovel.IdPessoaProprietario);
            if (quantidade >= plano.LimiteImoveisAnunciados)
                throw new ServiceException($"O plano {plano.Nome} permite no máximo {plano.LimiteImoveisAnunciados} imóvel(is) anunciado(s).");

            imovel.Status = "D";
            imovel.StatusValidacao = "P";

            context.Add(imovel);
            context.SaveChanges();
            return imovel.Id;
        }

        /// <summary>
        /// Edita os dados do anúncio, mantendo o proprietário e a situação de validação
        /// </summary>
        /// <param name="imovel">dados do imóvel</param>
        /// <exception cref="ServiceException">imóvel não encontrado</exception>
        public void Edit(Imovel imovel)
        {
            var atual = context.Imovels.AsNoTracking().FirstOrDefault(i => i.Id == imovel.Id)
                ?? throw new ServiceException("Imóvel não encontrado.");

            imovel.IdPessoaProprietario = atual.IdPessoaProprietario;
            imovel.StatusValidacao = atual.StatusValidacao;

            context.Update(imovel);
            context.SaveChanges();
        }

        /// <summary>
        /// Remove o anúncio do imóvel
        /// </summary>
        /// <param name="id">id do imóvel</param>
        /// <exception cref="ServiceException">imóvel com histórico de aluguel</exception>
        public void Delete(uint id)
        {
            var imovel = context.Imovels.Find(id);
            if (imovel != null)
            {
                if (context.Aluguels.Any(a => a.IdImovel == id))
                    throw new ServiceException("Não é possível excluir um imóvel com histórico de aluguel.");

                context.Remove(imovel);
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Busca um imóvel pelo id
        /// </summary>
        /// <param name="id">id do imóvel</param>
        /// <returns>dados do imóvel ou nulo se não for encontrado</returns>
        public Imovel? Get(uint id)
        {
            return context.Imovels.Find(id);
        }

        /// <summary>
        /// Busca todos os imóveis cadastrados
        /// </summary>
        /// <returns>lista de imóveis</returns>
        public IEnumerable<Imovel> GetAll()
        {
            return context.Imovels.AsNoTracking();
        }

        /// <summary>
        /// Busca os imóveis de um proprietário
        /// </summary>
        /// <param name="idPessoa">id do proprietário</param>
        /// <returns>lista de imóveis do proprietário</returns>
        public IEnumerable<Imovel> GetByProprietario(uint idPessoa)
        {
            return context.Imovels.Where(i => i.IdPessoaProprietario == idPessoa).AsNoTracking();
        }

        /// <summary>
        /// Busca os imóveis por situação de validação (P = pendente, A = aprovado, R = reprovado)
        /// </summary>
        /// <param name="statusValidacao">código da situação de validação</param>
        /// <returns>lista de imóveis na situação informada</returns>
        public IEnumerable<Imovel> GetByValidacao(string statusValidacao)
        {
            return context.Imovels.Where(i => i.StatusValidacao == statusValidacao).AsNoTracking();
        }

        /// <summary>
        /// Busca pública de imóveis pelo nome: retorna apenas anúncios aprovados e disponíveis
        /// </summary>
        /// <param name="nome">termo de busca</param>
        /// <returns>lista de imóveis encontrados</returns>
        public IEnumerable<Imovel> GetByNome(string nome)
        {
            return context.Imovels
                .Where(i => i.Nome.Contains(nome) && i.StatusValidacao == "A" && i.Status == "D")
                .AsNoTracking();
        }

        /// <summary>
        /// Aprova ou reprova um anúncio pendente de validação
        /// </summary>
        /// <param name="id">id do imóvel</param>
        /// <param name="aprovado">verdadeiro para aprovar, falso para reprovar</param>
        /// <exception cref="ServiceException">imóvel não encontrado ou anúncio não está pendente</exception>
        public void Approve(uint id, bool aprovado)
        {
            var imovel = context.Imovels.Find(id)
                ?? throw new ServiceException("Imóvel não encontrado.");

            if (imovel.StatusValidacao != "P")
                throw new ServiceException("Somente anúncios pendentes podem ser validados.");

            imovel.StatusValidacao = aprovado ? "A" : "R";
            context.SaveChanges();
        }

        /// <summary>
        /// Obtém o plano em vigor do proprietário: o da assinatura ativa, ou cancelada ainda dentro do período pago,
        /// e o plano Básico quando não houver assinatura em vigor
        /// </summary>
        /// <param name="idPessoa">id do proprietário</param>
        /// <returns>plano em vigor</returns>
        /// <exception cref="ServiceException">plano Básico não cadastrado</exception>
        private Plano ObterPlanoAtual(uint idPessoa)
        {
            var assinatura = context.Assinaturas
                .Include(a => a.IdPlanoNavigation)
                .AsNoTracking()
                .FirstOrDefault(a => a.IdPessoa == idPessoa
                                  && (a.Status == "A" || a.DataProximaCobranca >= DateTime.Today));

            if (assinatura != null)
                return assinatura.IdPlanoNavigation;

            return context.Planos.AsNoTracking().FirstOrDefault(p => p.Nome == "Básico")
                ?? throw new ServiceException("Plano Básico não cadastrado.");
        }

        /// <summary>
        /// Busca os imoveis disponíveis para aluguel ou venda, de acordo com os critérios informados no filtro, 
        /// não incluindo imóveis que não estão aprovados ou que já foram alugados ou vendidos
        /// </summary>
        /// <param name="filtro">filtro de busca</param>
        /// <returns>coleção de imóveis que atendem aos critérios</returns>
        public IEnumerable<Imovel> GetByFiltro(ImovelFiltroDTO filtro)
        {
            var consulta = context.Imovels.Where(i => i.Status == "D" && i.StatusValidacao == "A");

            if (!string.IsNullOrEmpty(filtro.Termo))
            {
                consulta = consulta.Where(i => i.Nome.Contains(filtro.Termo) || (i.Descricao != null && i.Descricao.Contains(filtro.Termo)));
            }

            var local = filtro.Localizacao?.Trim();
            if (!string.IsNullOrWhiteSpace(local))
            {
                consulta = consulta.Where(i => i.Bairro.Contains(local)
                                            || i.Cidade.Contains(local)
                                            || i.Estado.Contains(local));
            }

            if (filtro.Tipos != null && filtro.Tipos.Any())
            {
                consulta = consulta.Where(i => filtro.Tipos.Contains(i.Tipo));
            }

            if (filtro.ValorMinimo.HasValue)
            {
                consulta = consulta.Where(i => i.Valor >= filtro.ValorMinimo.Value);
            }

            if (filtro.ValorMaximo.HasValue)
            {
                consulta = consulta.Where(i => i.Valor <= filtro.ValorMaximo.Value);
            }

            if (filtro.QuantidadeQuartos.HasValue)
            {
                consulta = consulta.Where(i => i.QuantidadeQuartos == filtro.QuantidadeQuartos.Value);
            }
            
            if (filtro.AreaMinima.HasValue)
            {
                consulta = consulta.Where(i => i.AreaTotal >= filtro.AreaMinima.Value);
            }

            if (filtro.AreaMaxima.HasValue)
            {
                consulta = consulta.Where(i => i.AreaTotal <= filtro.AreaMaxima.Value);
            }
            
            return consulta.AsNoTracking();
        }
    }
}
