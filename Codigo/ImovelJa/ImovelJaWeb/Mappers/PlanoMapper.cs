using Core;
using ImovelJaWeb.Models;
using Mapster;

namespace ImovelJaWeb.Mappers
{
    public class PlanoMapper
    {
        public static PlanoViewModel ToViewModel(Plano plano)
        {
            return plano.Adapt<PlanoViewModel>();
        }

        public static Plano ToEntity(PlanoViewModel planoViewModel)
        {
            return planoViewModel.Adapt<Plano>();
        }

        public static IEnumerable<PlanoViewModel> ToViewModel(IEnumerable<Plano> planos)
        {
            return planos.Adapt<IEnumerable<PlanoViewModel>>();
        }

        public static IEnumerable<Plano> ToEntity(IEnumerable<PlanoViewModel> planosViewModel)
        {
            return planosViewModel.Adapt<IEnumerable<Plano>>();
        }
    }
}
