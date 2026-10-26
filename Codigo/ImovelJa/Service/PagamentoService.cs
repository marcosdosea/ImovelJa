using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service
{
    public class PagamentoService : IPagamentoService
    {
        private readonly ImovelJaContext context;

        public PagamentoService(ImovelJaContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Registra o pagamento de um mês do aluguel
        /// </summary>
        /// <param name="pagamento">dados do pagamento</param>
        /// <returns>id do pagamento criado</returns>
        /// <exception cref="ServiceException">aluguel não encontrado, mês já registrado ou dados do pagamento incompletos</exception>
        public uint Create(Pagamento pagamento)
        {
            var aluguel = context.Aluguels.Find(pagamento.IdAluguel)
                ?? throw new ServiceException("Aluguel não encontrado.");

            pagamento.MesReferencia = PrimeiroDiaDoMes(pagamento.MesReferencia);
            VerificarMesUnico(pagamento);
            pagamento.Status = CalcularStatus(pagamento, aluguel);

            context.Add(pagamento);
            context.SaveChanges();
            return pagamento.Id;
        }

        /// <summary>
        /// Edita os dados do pagamento de um mês do aluguel
        /// </summary>
        /// <param name="pagamento">dados do pagamento</param>
        /// <exception cref="ServiceException">pagamento ou aluguel não encontrado, mês já registrado ou dados do pagamento incompletos</exception>
        public void Edit(Pagamento pagamento)
        {
            if (!context.Pagamentos.Any(p => p.Id == pagamento.Id))
                throw new ServiceException("Pagamento não encontrado.");

            var aluguel = context.Aluguels.Find(pagamento.IdAluguel)
                ?? throw new ServiceException("Aluguel não encontrado.");

            pagamento.MesReferencia = PrimeiroDiaDoMes(pagamento.MesReferencia);
            VerificarMesUnico(pagamento);
            pagamento.Status = CalcularStatus(pagamento, aluguel);

            context.Update(pagamento);
            context.SaveChanges();
        }

        /// <summary>
        /// Busca um pagamento pelo id, com o status atualizado conforme a data de vencimento
        /// </summary>
        /// <param name="id">id do pagamento</param>
        /// <returns>dados do pagamento ou nulo se não for encontrado</returns>
        public Pagamento? Get(uint id)
        {
            var pagamento = context.Pagamentos
                .Include(p => p.IdAluguelNavigation)
                .AsNoTracking()
                .FirstOrDefault(p => p.Id == id);
            return pagamento == null ? null : AtualizarStatus(pagamento);
        }

        /// <summary>
        /// Busca todos os pagamentos cadastrados, com o status atualizado conforme a data de vencimento
        /// </summary>
        /// <returns>lista de pagamentos</returns>
        public IEnumerable<Pagamento> GetAll()
        {
            return context.Pagamentos
                .Include(p => p.IdAluguelNavigation)
                .AsNoTracking()
                .AsEnumerable()
                .Select(AtualizarStatus);
        }

        /// <summary>
        /// Busca os pagamentos de um aluguel, com o status atualizado conforme a data de vencimento
        /// </summary>
        /// <param name="idAluguel">id do aluguel</param>
        /// <returns>lista de pagamentos do aluguel</returns>
        public IEnumerable<Pagamento> GetByAluguel(uint idAluguel)
        {
            return context.Pagamentos
                .Include(p => p.IdAluguelNavigation)
                .Where(p => p.IdAluguel == idAluguel)
                .OrderBy(p => p.MesReferencia)
                .AsNoTracking()
                .AsEnumerable()
                .Select(AtualizarStatus);
        }

        /// <summary>
        /// Verifica se o mês de referência já foi registrado para o mesmo aluguel
        /// </summary>
        /// <param name="pagamento">dados do pagamento</param>
        /// <exception cref="ServiceException">mês de referência já registrado</exception>
        private void VerificarMesUnico(Pagamento pagamento)
        {
            if (context.Pagamentos.Any(p => p.IdAluguel == pagamento.IdAluguel
                                         && p.MesReferencia == pagamento.MesReferencia
                                         && p.Id != pagamento.Id))
                throw new ServiceException("Já existe um pagamento registrado para este mês.");
        }

        /// <summary>
        /// Calcula o status do pagamento (RN02): Pago quando marcado como pago ou quando o valor pago atinge o valor do aluguel;
        /// Em atraso quando a data de vencimento já passou; Pendente nos demais casos
        /// </summary>
        /// <param name="pagamento">dados do pagamento</param>
        /// <param name="aluguel">aluguel do pagamento</param>
        /// <returns>código do status (PA, EA ou PE)</returns>
        /// <exception cref="ServiceException">pagamento marcado como pago sem valor ou data do pagamento</exception>
        private static string CalcularStatus(Pagamento pagamento, Aluguel aluguel)
        {
            bool pago = pagamento.Status == "PA" || pagamento.ValorPago >= aluguel.Valor;
            if (pago)
            {
                if (pagamento.ValorPago == null || pagamento.DataPagamento == null)
                    throw new ServiceException("Informe o valor e a data do pagamento.");
                return "PA";
            }

            return Vencido(pagamento, aluguel) ? "EA" : "PE";
        }

        /// <summary>
        /// Verifica se a data de vencimento do mês de referência já passou
        /// </summary>
        /// <param name="pagamento">dados do pagamento</param>
        /// <param name="aluguel">aluguel do pagamento, com o dia de pagamento</param>
        /// <returns>verdadeiro se o vencimento já passou</returns>
        private static bool Vencido(Pagamento pagamento, Aluguel aluguel)
        {
            var mes = pagamento.MesReferencia;
            int dia = Math.Min(aluguel.DiaPagamento, DateTime.DaysInMonth(mes.Year, mes.Month));
            var vencimento = new DateTime(mes.Year, mes.Month, dia);
            return DateTime.Today > vencimento;
        }

        /// <summary>
        /// Marca como em atraso o pagamento pendente cujo vencimento já passou, sem gravar no banco (RN02)
        /// </summary>
        /// <param name="pagamento">pagamento com o aluguel carregado</param>
        /// <returns>pagamento com o status atualizado</returns>
        private static Pagamento AtualizarStatus(Pagamento pagamento)
        {
            if (pagamento.Status == "PE" && Vencido(pagamento, pagamento.IdAluguelNavigation))
                pagamento.Status = "EA";
            return pagamento;
        }

        /// <summary>
        /// Converte uma data para o primeiro dia do seu mês, formato usado no mês de referência
        /// </summary>
        /// <param name="data">data qualquer do mês</param>
        /// <returns>primeiro dia do mês</returns>
        private static DateTime PrimeiroDiaDoMes(DateTime data)
        {
            return new DateTime(data.Year, data.Month, 1);
        }
    }
}
