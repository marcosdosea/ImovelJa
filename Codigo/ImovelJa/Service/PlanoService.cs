using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service
{
    public class PlanoService : IPlanoService
    {
        private readonly ImovelJaContext context;

        public PlanoService(ImovelJaContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Edita o valor e os benefícios de um plano (CSU12)
        /// </summary>
        /// <param name="plano">dados do plano</param>
        /// <exception cref="ServiceException">plano não encontrado, valor negativo ou valor diferente de zero no plano Básico</exception>
        public void Edit(Plano plano)
        {
            var atual = context.Planos.Find(plano.Id)
                ?? throw new ServiceException("Plano não encontrado.");

            if (plano.Valor < 0)
                throw new ServiceException("O valor do plano não pode ser negativo.");

            if (atual.Nome == "Básico" && plano.Valor != 0)
                throw new ServiceException("O plano Básico deve ter valor zero.");

            atual.Valor = plano.Valor;
            atual.Beneficios = plano.Beneficios;
            context.SaveChanges();
        }

        /// <summary>
        /// Busca um plano pelo id
        /// </summary>
        /// <param name="id">id do plano</param>
        /// <returns>dados do plano ou nulo se não for encontrado</returns>
        public Plano? Get(uint id)
        {
            return context.Planos.Find(id);
        }

        /// <summary>
        /// Busca todos os planos cadastrados
        /// </summary>
        /// <returns>lista de planos</returns>
        public IEnumerable<Plano> GetAll()
        {
            return context.Planos.AsNoTracking();
        }

        /// <summary>
        /// Ativa ou desativa um plano; desativar impede novas contratações, sem alterar as assinaturas existentes
        /// </summary>
        /// <param name="id">id do plano</param>
        /// <param name="ativo">verdadeiro para ativar, falso para desativar</param>
        /// <exception cref="ServiceException">plano não encontrado ou tentativa de desativar o plano Básico</exception>
        public void Activate(uint id, bool ativo)
        {
            var plano = context.Planos.Find(id)
                ?? throw new ServiceException("Plano não encontrado.");

            if (!ativo && plano.Nome == "Básico")
                throw new ServiceException("O plano Básico não pode ser desativado.");

            plano.Status = ativo ? "A" : "I";
            context.SaveChanges();
        }
    }
}