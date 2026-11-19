using Core;
using ImovelJaWeb.Models;
using Mapster;

namespace ImovelJaWeb.Mappers
{
    public class AluguelMapper
    {
        public static AluguelViewModel ToViewModel(Aluguel aluguel, Imovel? imovel, Pessoa? inquilino)
        {
            var viewModel = aluguel.Adapt<AluguelViewModel>();
            viewModel.PossuiContrato = aluguel.ArquivoContrato is { Length: > 0 };
            viewModel.Imovel = imovel == null ? null : ImovelMapper.ToViewModel(imovel);
            viewModel.Inquilino = inquilino == null ? null : PessoaMapper.ToViewModel(inquilino);
            viewModel.Pagamentos = new List<PagamentoViewModel>();
            return viewModel;
        }
    }
}
