using ImovelJaWeb.Models;
using Mapster;
using Core;

namespace ImovelJaWeb.Mappers
{
    public class PagamentoMapper
    {
        public static Pagamento ToEntity(PagamentoViewModel viewModel)
        {
            return viewModel.Adapt<Pagamento>();
        }

        public static PagamentoViewModel ToViewModel(Pagamento entity)
        {
            var viewModel = entity.Adapt<PagamentoViewModel>();
            viewModel.ValorAluguel = entity.IdAluguelNavigation?.Valor ?? 0;
            return viewModel;
        }

        public static IEnumerable<PagamentoViewModel> ToViewModel(IEnumerable<Pagamento> entities)
        {
            return entities.Adapt<IEnumerable<PagamentoViewModel>>();
        }
    }
}
