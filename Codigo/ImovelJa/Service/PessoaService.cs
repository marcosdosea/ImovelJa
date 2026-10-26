using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service
{
    public class PessoaService : IPessoaService
    {
        private readonly ImovelJaContext context;

        public PessoaService(ImovelJaContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Cadastra uma nova pessoa e vincula os aluguéis já registrados com o mesmo documento (RN04)
        /// </summary>
        /// <param name="pessoa">dados da pessoa</param>
        /// <returns>id da pessoa criada</returns>
        /// <exception cref="ServiceException">campos obrigatórios do tipo de pessoa ausentes ou e-mail, CPF ou CNPJ já cadastrado</exception>
        public uint Create(Pessoa pessoa)
        {
            VerificarDados(pessoa);
            pessoa.Status = "A";

            string documento = (pessoa.Tipo == "F" ? pessoa.Cpf : pessoa.Cnpj)!;
            var alugueis = context.Aluguels
                .Where(a => a.DocumentoInquilino == documento && a.IdPessoaInquilino == null)
                .ToList();
            foreach (var aluguel in alugueis)
                aluguel.IdPessoaInquilinoNavigation = pessoa;

            context.Add(pessoa);
            context.SaveChanges();
            return pessoa.Id;
        }

        /// <summary>
        /// Edita os dados cadastrais da pessoa, mantendo a situação da conta
        /// </summary>
        /// <param name="pessoa">dados da pessoa</param>
        /// <exception cref="ServiceException">pessoa não encontrada, campos obrigatórios ausentes ou e-mail, CPF ou CNPJ já cadastrado</exception>
        public void Edit(Pessoa pessoa)
        {
            var atual = context.Pessoas.AsNoTracking().FirstOrDefault(p => p.Id == pessoa.Id)
                ?? throw new ServiceException("Pessoa não encontrada.");

            VerificarDados(pessoa);
            pessoa.Status = atual.Status;

            context.Update(pessoa);
            context.SaveChanges();
        }

        /// <summary>
        /// Busca uma pessoa pelo id
        /// </summary>
        /// <param name="id">id da pessoa</param>
        /// <returns>dados da pessoa ou nulo se não for encontrada</returns>
        public Pessoa? Get(uint id)
        {
            return context.Pessoas.Find(id);
        }

        /// <summary>
        /// Busca todas as pessoas cadastradas
        /// </summary>
        /// <returns>lista de pessoas</returns>
        public IEnumerable<Pessoa> GetAll()
        {
            return context.Pessoas.AsNoTracking();
        }

        /// <summary>
        /// Busca uma pessoa pelo CPF ou CNPJ
        /// </summary>
        /// <param name="documento">CPF ou CNPJ, somente números</param>
        /// <returns>dados da pessoa ou nulo se não for encontrada</returns>
        public Pessoa? GetByDocumento(string documento)
        {
            return context.Pessoas.AsNoTracking()
                .FirstOrDefault(p => p.Cpf == documento || p.Cnpj == documento);
        }

        /// <summary>
        /// Ativa ou bloqueia a conta de uma pessoa
        /// </summary>
        /// <param name="id">id da pessoa</param>
        /// <param name="ativo">verdadeiro para ativar, falso para bloquear</param>
        /// <exception cref="ServiceException">pessoa não encontrada</exception>
        public void Activate(uint id, bool ativo)
        {
            var pessoa = context.Pessoas.Find(id)
                ?? throw new ServiceException("Pessoa não encontrada.");

            pessoa.Status = ativo ? "A" : "B";
            context.SaveChanges();
        }

        /// <summary>
        /// Verifica os campos obrigatórios conforme o tipo de pessoa (RN05) e a unicidade de e-mail, CPF e CNPJ
        /// </summary>
        /// <param name="pessoa">dados da pessoa</param>
        /// <exception cref="ServiceException">regra não cumprida</exception>
        private void VerificarDados(Pessoa pessoa)
        {
            if (pessoa.Tipo == "F")
            {
                if (string.IsNullOrWhiteSpace(pessoa.Cpf) || pessoa.DataNascimento == null)
                    throw new ServiceException("Pessoa física deve informar CPF e data de nascimento.");
                pessoa.Cnpj = null;
            }
            else if (pessoa.Tipo == "J")
            {
                if (string.IsNullOrWhiteSpace(pessoa.Cnpj))
                    throw new ServiceException("Pessoa jurídica deve informar CNPJ.");
                pessoa.Cpf = null;
                pessoa.DataNascimento = null;
            }
            else
            {
                throw new ServiceException("Tipo de pessoa inválido.");
            }

            if (context.Pessoas.Any(p => p.Email == pessoa.Email && p.Id != pessoa.Id))
                throw new ServiceException("E-mail já cadastrado.");

            if (pessoa.Cpf != null && context.Pessoas.Any(p => p.Cpf == pessoa.Cpf && p.Id != pessoa.Id))
                throw new ServiceException("CPF já cadastrado.");

            if (pessoa.Cnpj != null && context.Pessoas.Any(p => p.Cnpj == pessoa.Cnpj && p.Id != pessoa.Id))
                throw new ServiceException("CNPJ já cadastrado.");
        }
    }
}