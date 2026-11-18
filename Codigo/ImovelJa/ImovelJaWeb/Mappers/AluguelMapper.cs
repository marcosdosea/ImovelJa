using Core;
using ImovelJaWeb.Models;
using Mapster;

namespace ImovelJaWeb.Mappers
{
    public class AluguelMapper
    {
        static AluguelMapper()
        {
            TypeAdapterConfig<Aluguel, AluguelViewModel>.NewConfig()
                .Map(d => d.TemContrato, s => s.ArquivoContrato != null && s.ArquivoContrato.Length > 0)
                .Ignore(d => d.Contrato)
                .Ignore(d => d.Inquilino);

            TypeAdapterConfig<AluguelViewModel, Aluguel>.NewConfig()
                .Ignore(d => d.Status)
                .Ignore(d => d.ArquivoContrato)
                .Ignore(d => d.IdImovelNavigation)
                .Ignore(d => d.IdPessoaInquilinoNavigation)
                .Ignore(d => d.Pagamentos);
        }

        public static AluguelViewModel ToViewModel(Aluguel aluguel)
        {
            return aluguel.Adapt<AluguelViewModel>();
        }

        public static Aluguel ToEntity(AluguelViewModel aluguelViewModel)
        {
            return aluguelViewModel.Adapt<Aluguel>();
        }

        public static void ToEntity(AluguelViewModel aluguelViewModel, Aluguel aluguel)
        {
            aluguelViewModel.Adapt(aluguel);
        }

        public static IEnumerable<AluguelViewModel> ToViewModel(IEnumerable<Aluguel> alugueis)
        {
            return alugueis.Adapt<IEnumerable<AluguelViewModel>>();
        }
    }
}