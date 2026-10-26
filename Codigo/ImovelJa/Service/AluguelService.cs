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
        /// Registra um novo aluguel para o imóvel e marca o imóvel como alugado
        /// </summary>
        /// <param name="aluguel">dados do aluguel</param>
        /// <returns>id do aluguel criado</returns>
        /// <exception cref="ServiceException">imóvel não encontrado ou com aluguel ativo</exception>
        public uint Create(Aluguel aluguel)
        {
            var imovel = context.Imovels.Find(aluguel.IdImovel)
                ?? throw new ServiceException("Imóvel não encontrado.");

            if (context.Aluguels.Any(a => a.IdImovel == aluguel.IdImovel && a.Status == "A"))
                throw new ServiceException("O imóvel já possui um aluguel ativo.");

            VincularInquilino(aluguel);
            aluguel.Status = "A";
            imovel.Status = "A";

            context.Add(aluguel);
            context.SaveChanges();
            return aluguel.Id;
        }

        /// <summary>
        /// Edita os dados do aluguel
        /// </summary>
        /// <param name="aluguel">dados do aluguel</param>
        /// <exception cref="ServiceException">aluguel não encontrado</exception>
        public void Edit(Aluguel aluguel)
        {
            if (!context.Aluguels.Any(a => a.Id == aluguel.Id))
                throw new ServiceException("Aluguel não encontrado.");

            VincularInquilino(aluguel);
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
        /// Busca os aluguéis vinculados a um inquilino
        /// </summary>
        /// <param name="idPessoa">id da pessoa inquilina</param>
        /// <returns>lista de aluguéis do inquilino</returns>
        public IEnumerable<Aluguel> GetByInquilino(uint idPessoa)
        {
            return context.Aluguels.Where(a => a.IdPessoaInquilino == idPessoa).AsNoTracking();
        }

        /// <summary>
        /// Vincula o inquilino ao aluguel quando o documento pertence a uma pessoa cadastrada (RN04)
        /// </summary>
        /// <param name="aluguel">dados do aluguel</param>
        private void VincularInquilino(Aluguel aluguel)
        {
            var pessoa = context.Pessoas.AsNoTracking()
                .FirstOrDefault(p => p.Cpf == aluguel.DocumentoInquilino || p.Cnpj == aluguel.DocumentoInquilino);

            if (pessoa == null)
            {
                aluguel.IdPessoaInquilino = null;
                return;
            }

            aluguel.IdPessoaInquilino = pessoa.Id;
            aluguel.NomeInquilino = pessoa.Nome;
            aluguel.TelefoneInquilino = pessoa.Telefone;
        }
    }
}
