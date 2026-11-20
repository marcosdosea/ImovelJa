using Core;
using ImovelJaWeb.Models;
using Mapster;
using System.Collections.Generic;

namespace ImovelJaWeb.Mappers
{
    public class AluguelMapper
    {
        static AluguelMapper()
        {
            TypeAdapterConfig<Aluguel, AluguelViewModel>.NewConfig()
                .Ignore(d => d.Contrato)
                .Ignore(d => d.Imovel)
                .Ignore(d => d.Inquilino)
                .Ignore(d => d.Pagamentos);

            TypeAdapterConfig<AluguelViewModel, Aluguel>.NewConfig()
                .Ignore(d => d.Status)
                .Ignore(d => d.ArquivoContrato)
                .Ignore(d => d.IdImovelNavigation)
                .Ignore(d => d.IdPessoaInquilinoNavigation)
                .Ignore(d => d.Pagamentos);
        }

        public static AluguelViewModel ToViewModel(Aluguel aluguel, Imovel? imovel, Pessoa? inquilino)
        {
            var viewModel = aluguel.Adapt<AluguelViewModel>();
            viewModel.PossuiContrato = aluguel.ArquivoContrato is { Length: > 0 };
            viewModel.Imovel = imovel == null ? null : ImovelMapper.ToViewModel(imovel);
            viewModel.Inquilino = inquilino == null ? null : PessoaMapper.ToViewModel(inquilino);
            viewModel.Pagamentos = new List<PagamentoViewModel>();
            return viewModel;
        }

        public static Aluguel ToEntity(AluguelViewModel aluguelViewModel)
        {
            return aluguelViewModel.Adapt<Aluguel>();
        }

        public static void ToEntity(AluguelViewModel aluguelViewModel, Aluguel aluguel)
        {
            aluguelViewModel.Adapt(aluguel);
        }
    }
}
