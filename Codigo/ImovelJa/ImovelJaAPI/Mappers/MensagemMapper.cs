using Core;
using ImovelJaAPI.Models;
using Mapster;

namespace ImovelJaAPI.Mappers
{
    public class MensagemMapper
    {
        public static MensagemViewModel ToViewModel(Mensagem mensagem)
        {
            return mensagem.Adapt<MensagemViewModel>();
        }

        public static Mensagem ToEntity(MensagemViewModel mensagemViewModel)
        {
            return mensagemViewModel.Adapt<Mensagem>();
        }

        public static IEnumerable<MensagemViewModel> ToViewModel(IEnumerable<Mensagem> mensagens)
        {
            return mensagens.Adapt<IEnumerable<MensagemViewModel>>();
        }
    }
}
