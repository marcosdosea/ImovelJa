using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service
{
    public class AssinaturaService : IAssinaturaService
    {
        private readonly ImovelJaContext context;

        public AssinaturaService(ImovelJaContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Registra a primeira assinatura de um plano para a pessoa
        /// </summary>
        /// <param name="assinatura">dados da assinatura</param>
        /// <returns>id da assinatura criada</returns>
        /// <exception cref="ServiceException">pessoa já possui assinatura, plano não encontrado ou inativo</exception>
        public uint Create(Assinatura assinatura)
        {
            if (context.Assinaturas.Any(a => a.IdPessoa == assinatura.IdPessoa))
                throw new ServiceException("A pessoa já possui uma assinatura. Para trocar de plano, altere a assinatura.");

            VerificarPlanoAtivo(assinatura.IdPlano);
            IniciarPeriodo(assinatura);

            context.Add(assinatura);
            context.SaveChanges();
            return assinatura.Id;
        }

        /// <summary>
        /// Troca o plano da assinatura (upgrade ou downgrade) ou reativa uma assinatura cancelada
        /// </summary>
        /// <param name="assinatura">dados da assinatura, com o novo plano</param>
        /// <exception cref="ServiceException">assinatura não encontrada, plano não encontrado ou inativo</exception>
        public void Edit(Assinatura assinatura)
        {
            var atual = context.Assinaturas.Find(assinatura.Id)
                ?? throw new ServiceException("Assinatura não encontrada.");

            VerificarPlanoAtivo(assinatura.IdPlano);
            atual.IdPlano = assinatura.IdPlano;
            IniciarPeriodo(atual);

            context.SaveChanges();
        }

        /// <summary>
        /// Cancela a assinatura; os benefícios do plano continuam até a data da próxima cobrança (CSU09)
        /// </summary>
        /// <param name="id">id da assinatura</param>
        /// <exception cref="ServiceException">assinatura não encontrada ou já cancelada</exception>
        public void Cancel(uint id)
        {
            var assinatura = context.Assinaturas.Find(id)
                ?? throw new ServiceException("Assinatura não encontrada.");

            if (assinatura.Status == "C")
                throw new ServiceException("A assinatura já está cancelada.");

            assinatura.Status = "C";
            context.SaveChanges();
        }

        /// <summary>
        /// Busca uma assinatura pelo id
        /// </summary>
        /// <param name="id">id da assinatura</param>
        /// <returns>dados da assinatura ou nulo se não for encontrada</returns>
        public Assinatura? Get(uint id)
        {
            return context.Assinaturas.Find(id);
        }

        /// <summary>
        /// Busca todas as assinaturas cadastradas
        /// </summary>
        /// <returns>lista de assinaturas</returns>
        public IEnumerable<Assinatura> GetAll()
        {
            return context.Assinaturas.AsNoTracking();
        }

        /// <summary>
        /// Busca a assinatura de uma pessoa
        /// </summary>
        /// <param name="idPessoa">id da pessoa</param>
        /// <returns>dados da assinatura ou nulo se a pessoa não tiver assinatura</returns>
        public Assinatura? GetByPessoa(uint idPessoa)
        {
            return context.Assinaturas.AsNoTracking().FirstOrDefault(a => a.IdPessoa == idPessoa);
        }

        /// <summary>
        /// Verifica se o plano existe e está disponível para contratação
        /// </summary>
        /// <param name="idPlano">id do plano</param>
        /// <exception cref="ServiceException">plano não encontrado ou inativo</exception>
        private void VerificarPlanoAtivo(uint idPlano)
        {
            var plano = context.Planos.Find(idPlano)
                ?? throw new ServiceException("Plano não encontrado.");

            if (plano.Status != "A")
                throw new ServiceException("Este plano não está disponível para contratação.");
        }

        /// <summary>
        /// Define a assinatura como ativa, com contratação hoje e próxima cobrança em um mês
        /// </summary>
        /// <param name="assinatura">dados da assinatura</param>
        private static void IniciarPeriodo(Assinatura assinatura)
        {
            assinatura.Status = "A";
            assinatura.DataContratacao = DateTime.Today;
            assinatura.DataProximaCobranca = DateTime.Today.AddMonths(1);
        }
    }
}