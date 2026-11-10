using Core;
using Core.DTO;
using ImovelJaWeb.Models;
using Mapster;

namespace ImovelJaWeb.Mappers
{
    public class ImovelMapper
    {
        public static ImovelViewModel ToViewModel(Imovel imovel)
        {
            return imovel.Adapt<ImovelViewModel>();
        }

        public static Imovel ToEntity(ImovelViewModel imovelViewModel)
        {
            return imovelViewModel.Adapt<Imovel>();
        }

        public static IEnumerable<ImovelViewModel> ToViewModel(IEnumerable<Imovel> imoveis)
        {
            return imoveis.Adapt<IEnumerable<ImovelViewModel>>();
        }

        public static IEnumerable<Imovel> ToEntity(IEnumerable<ImovelViewModel> imoveisViewModel)
        {
            return imoveisViewModel.Adapt<IEnumerable<Imovel>>();
        }

        public static ImovelFiltroDTO ToDTO(ImovelFiltroViewModel filtroViewModel)
        {
            return filtroViewModel.Adapt<ImovelFiltroDTO>();
        }
    }
}
