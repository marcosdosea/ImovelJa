using Core;
using ImovelJaWeb.Models;
using Mapster;

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
            viewModel.PossuiComprovante = entity.Comprovante is { Length: > 0 };
            viewModel.Comprovante = null;
            return viewModel;
        }

        public static IEnumerable<PagamentoViewModel> ToViewModel(IEnumerable<Pagamento> entities)
        {
            return entities.Select(ToViewModel);
        }
    }
}
