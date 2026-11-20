using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service
{
    public class AluguelService : IAluguelService
    {
        private readonly ImovelJaContext context;

        public AluguelService(ImovelJaContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Registra um novo aluguel para o imóvel e marca o imóvel como alugado.
        /// O inquilino é a pessoa informada pelo id ou, quando o id não é informado, pelos dados em IdPessoaInquilinoNavigation:
        /// se o CPF ou CNPJ já estiver cadastrado, o aluguel é vinculado a essa pessoa; senão, a pessoa é cadastrada
        /// junto com o aluguel, na mesma transação (RN04)
        /// </summary>
        /// <param name="aluguel">dados do aluguel e do inquilino</param>
        /// <returns>id do aluguel criado</returns>
        /// <exception cref="ServiceException">imóvel não encontrado, com aluguel ativo, não anunciado para aluguel, não aprovado ou indisponível,
        /// inquilino não encontrado ou dados do inquilino incompletos</exception>
        public uint Create(Aluguel aluguel)
        {
            var imovel = context.Imovels.Find(aluguel.IdImovel)
                ?? throw new ServiceException("Imóvel não encontrado.");

            if (context.Aluguels.Any(a => a.IdImovel == aluguel.IdImovel && a.Status == "A"))
                throw new ServiceException("O imóvel já possui um aluguel ativo.");

            if (imovel.Finalidade != "A")
                throw new ServiceException("O imóvel não é anunciado para aluguel.");

            if (imovel.StatusValidacao != "A")
                throw new ServiceException("O anúncio do imóvel ainda não foi aprovado.");

            if (imovel.Status != "D")
                throw new ServiceException("O imóvel não está disponível para aluguel.");

            if (aluguel.IdPessoaInquilino != 0)
            {
                if (!context.Pessoas.Any(p => p.Id == aluguel.IdPessoaInquilino))
                    throw new ServiceException("Inquilino não encontrado.");
                aluguel.IdPessoaInquilinoNavigation = null!;
            }
            else
            {
                var inquilino = aluguel.IdPessoaInquilinoNavigation
                    ?? throw new ServiceException("Informe os dados do inquilino.");

                if (inquilino.Tipo != "F" && inquilino.Tipo != "J")
                    throw new ServiceException("Tipo de pessoa inválido.");

                string? documento = inquilino.Tipo == "F" ? inquilino.Cpf : inquilino.Cnpj;
                if (string.IsNullOrWhiteSpace(documento) || string.IsNullOrWhiteSpace(inquilino.Nome))
                    throw new ServiceException("Informe o nome e o CPF ou CNPJ do inquilino.");

                var cadastrada = context.Pessoas.FirstOrDefault(p => p.Cpf == documento || p.Cnpj == documento);
                if (cadastrada != null)
                {
                    aluguel.IdPessoaInquilinoNavigation = cadastrada;
                }
                else
                {
                    if (inquilino.Tipo == "F")
                        inquilino.Cnpj = null;
                    else
                        inquilino.Cpf = null;
                    inquilino.Status = "A";
                    context.Add(inquilino);
                }
            }

            aluguel.Status = "A";
            imovel.Status = "A";

            context.Add(aluguel);
            context.SaveChanges();
            return aluguel.Id;
        }

        /// <summary>
        /// Edita os dados do aluguel e, quando informados em IdPessoaInquilinoNavigation, atualiza o nome,
        /// o CPF ou CNPJ (conforme o tipo da pessoa) e o telefone do inquilino vinculado, na mesma transação
        /// do aluguel (RN16). O inquilino vinculado ao aluguel não muda. Se o documento informado pertencer
        /// a outra pessoa, nada é gravado (RN11)
        /// </summary>
        /// <param name="aluguel">dados do aluguel e, opcionalmente, do inquilino</param>
        /// <exception cref="ServiceException">aluguel ou inquilino não encontrado, aluguel cancelado, nome ou documento do inquilino ausente, ou documento pertencente a outra pessoa</exception>
        public void Edit(Aluguel aluguel)
        {
            var atual = context.Aluguels.AsNoTracking().FirstOrDefault(a => a.Id == aluguel.Id)
                ?? throw new ServiceException("Aluguel não encontrado.");

            if (atual.Status != "A")
                throw new ServiceException("Não é possível editar um aluguel cancelado.");

            var dados = aluguel.IdPessoaInquilinoNavigation;
            Pessoa? inquilino = null;
            string? documento = null;

            if (dados != null)
            {
                inquilino = context.Pessoas.Find(atual.IdPessoaInquilino)
                    ?? throw new ServiceException("Inquilino não encontrado.");

                documento = inquilino.Tipo == "F" ? dados.Cpf : dados.Cnpj;
                if (string.IsNullOrWhiteSpace(dados.Nome) || string.IsNullOrWhiteSpace(documento))
                    throw new ServiceException("Informe o nome e o CPF ou CNPJ do inquilino.");

                if (context.Pessoas.Any(p => p.Id != inquilino.Id && (p.Cpf == documento || p.Cnpj == documento)))
                    throw new ServiceException("O CPF ou CNPJ informado pertence a outra pessoa.");
            }

            aluguel.IdPessoaInquilino = atual.IdPessoaInquilino;
            aluguel.IdPessoaInquilinoNavigation = null!;

            if (inquilino != null && dados != null)
            {
                inquilino.Nome = dados.Nome;
                inquilino.Telefone = dados.Telefone;
                if (inquilino.Tipo == "F")
                    inquilino.Cpf = documento;
                else
                    inquilino.Cnpj = documento;
            }

            context.Update(aluguel);
            context.SaveChanges();
        }

        /// <summary>
        /// Cancela o aluguel, preservando o histórico, e devolve o imóvel para disponível
        /// </summary>
        /// <param name="id">id do aluguel</param>
        public void Delete(uint id)
        {
            var aluguel = context.Aluguels.Find(id);
            if (aluguel != null)
            {
                aluguel.Status = "C";
                var imovel = context.Imovels.Find(aluguel.IdImovel);
                if (imovel != null)
                    imovel.Status = "D";
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Busca um aluguel pelo id
        /// </summary>
        /// <param name="id">id do aluguel</param>
        /// <returns>dados do aluguel ou nulo se não for encontrado</returns>
        public Aluguel? Get(uint id)
        {
            return context.Aluguels.Find(id);
        }

        /// <summary>
        /// Busca um aluguel pelo id apenas se ele estiver vinculado ao inquilino informado
        /// </summary>
        /// <param name="id">id do aluguel</param>
        /// <param name="idPessoa">id da pessoa inquilina</param>
        /// <returns>dados do aluguel ou nulo se não for encontrado ou não pertencer ao inquilino</returns>
        public Aluguel? GetDoInquilino(uint id, uint idPessoa)
        {
            return context.Aluguels
                .AsNoTracking()
                .FirstOrDefault(a => a.Id == id && a.IdPessoaInquilino == idPessoa);
        }

        /// <summary>
        /// Busca um aluguel pelo id apenas se o imóvel alugado pertencer à pessoa informada
        /// </summary>
        /// <param name="id">id do aluguel</param>
        /// <param name="idPessoa">id da pessoa dona do imóvel</param>
        /// <returns>dados do aluguel ou nulo se não for encontrado ou o imóvel não pertencer à pessoa</returns>
        public Aluguel? GetDoProprietario(uint id, uint idPessoa)
        {
            return context.Aluguels
                .AsNoTracking()
                .FirstOrDefault(a => a.Id == id && a.IdImovelNavigation.IdPessoaProprietario == idPessoa);
        }

        /// <summary>
        /// Busca todos os aluguéis cadastrados
        /// </summary>
        /// <returns>lista de aluguéis</returns>
        public IEnumerable<Aluguel> GetAll()
        {
            return context.Aluguels.AsNoTracking();
        }

        /// <summary>
        /// Busca os aluguéis de um imóvel, incluindo os cancelados
        /// </summary>
        /// <param name="idImovel">id do imóvel</param>
        /// <returns>lista de aluguéis do imóvel</returns>
        public IEnumerable<Aluguel> GetByImovel(uint idImovel)
        {
            return context.Aluguels.Where(a => a.IdImovel == idImovel).AsNoTracking();
        }

        /// <summary>
        /// Busca os aluguéis vinculados a um inquilino, com os dados do imóvel de cada aluguel
        /// </summary>
        /// <param name="idPessoa">id da pessoa inquilina</param>
        /// <returns>lista de aluguéis do inquilino</returns>
        public IEnumerable<Aluguel> GetByInquilino(uint idPessoa)
        {
            return context.Aluguels
                .Include(a => a.IdImovelNavigation)
                .Where(a => a.IdPessoaInquilino == idPessoa)
                .AsNoTracking();
        }
    }
}
