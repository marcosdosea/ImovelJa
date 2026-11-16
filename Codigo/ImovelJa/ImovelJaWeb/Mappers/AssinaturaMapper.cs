using Core;
using ImovelJaWeb.Models;
using Mapster;

namespace ImovelJaWeb.Mappers
{
    public class AssinaturaMapper
    {
        public static AssinaturaViewModel ToViewModel(Assinatura assinatura)
        {
            return assinatura.Adapt<AssinaturaViewModel>();
        }

        public static Assinatura ToEntity(AssinaturaViewModel assinaturaViewModel)
        {
            return assinaturaViewModel.Adapt<Assinatura>();
        }

        public static IEnumerable<AssinaturaViewModel> ToViewModel(IEnumerable<Assinatura> assinaturas)
        {
            return assinaturas.Adapt<IEnumerable<AssinaturaViewModel>>();
        }

        public static IEnumerable<Assinatura> ToEntity(IEnumerable<AssinaturaViewModel> assinaturasViewModel)
        {
            return assinaturasViewModel.Adapt<IEnumerable<Assinatura>>();
        }
    }
}
