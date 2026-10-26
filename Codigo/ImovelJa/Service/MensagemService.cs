using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service
{
    public class MensagemService : IMensagemService
    {
        private readonly ImovelJaContext context;

        public MensagemService(ImovelJaContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Registra uma nova mensagem enviada entre duas pessoas
        /// </summary>
        /// <param name="mensagem">dados da mensagem</param>
        /// <returns>id da mensagem criada</returns>
        /// <exception cref="ServiceException">mensagem em branco, destinatário não encontrado ou igual ao remetente</exception>
        public uint Create(Mensagem mensagem)
        {
            if (string.IsNullOrWhiteSpace(mensagem.Texto))
                throw new ServiceException("A mensagem não pode estar em branco.");

            if (mensagem.IdPessoaEnviou == mensagem.IdPessoaRecebeu)
                throw new ServiceException("O destinatário deve ser diferente do remetente.");

            if (!context.Pessoas.Any(p => p.Id == mensagem.IdPessoaRecebeu))
                throw new ServiceException("Destinatário não encontrado.");

            mensagem.DataEnvio = DateTime.Now;

            context.Add(mensagem);
            context.SaveChanges();
            return mensagem.Id;
        }

        /// <summary>
        /// Busca uma mensagem pelo id
        /// </summary>
        /// <param name="id">id da mensagem</param>
        /// <returns>dados da mensagem ou nulo se não for encontrada</returns>
        public Mensagem? Get(uint id)
        {
            return context.Mensagems.Find(id);
        }

        /// <summary>
        /// Busca todas as mensagens cadastradas
        /// </summary>
        /// <returns>lista de mensagens</returns>
        public IEnumerable<Mensagem> GetAll()
        {
            return context.Mensagems.AsNoTracking();
        }

        /// <summary>
        /// Busca as mensagens enviadas ou recebidas por uma pessoa, da mais antiga para a mais recente
        /// </summary>
        /// <param name="idPessoa">id da pessoa</param>
        /// <returns>lista de mensagens da pessoa</returns>
        public IEnumerable<Mensagem> GetByPessoa(uint idPessoa)
        {
            return context.Mensagems
                .Where(m => m.IdPessoaEnviou == idPessoa || m.IdPessoaRecebeu == idPessoa)
                .OrderBy(m => m.DataEnvio)
                .AsNoTracking();
        }
    }
}
