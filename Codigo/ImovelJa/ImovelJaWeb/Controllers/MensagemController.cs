using Core.Service;
using ImovelJaWeb.Mappers;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImovelJaWeb.Controllers
{
    public class MensagemController : Controller
    {
        public const string ChaveHistorico = "Historico";
        public const string ChaveIdImovel = "IdImovel";
        public const string ChaveNomeImovel = "NomeImovel";

        private const string ChaveTexto = "Texto";

        private readonly IMensagemService mensagemService;
        private readonly IImovelService imovelService;

        public MensagemController(IMensagemService mensagemService, IImovelService imovelService)
        {
            this.mensagemService = mensagemService;
            this.imovelService = imovelService;
        }

        // GET: MensagemController/Conversa?idImovel=1&idPessoa=2
        public ActionResult Conversa(uint idImovel, uint? idPessoa)
        {
            var modelo = Montar(idImovel, idPessoa ?? 0, null);
            return View(modelo);
        }

        // POST: MensagemController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MensagemViewModel mensagem, uint? idImovel)
        {
            var emBranco = string.IsNullOrWhiteSpace(mensagem.Texto);
            if (emBranco && ModelState[ChaveTexto]?.Errors.Count is null or 0)
                ModelState.AddModelError(ChaveTexto, "A mensagem não pode estar em branco.");

            if (!ModelState.IsValid || emBranco)
                return View(nameof(Conversa), Montar(idImovel ?? 0, mensagem.IdPessoaEnviou, mensagem));

            try
            {
                mensagemService.Create(MensagemMapper.ToEntity(mensagem));
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(nameof(Conversa), Montar(idImovel ?? 0, mensagem.IdPessoaEnviou, mensagem));
            }

            return RedirectToAction(nameof(Conversa), new
            {
                idImovel,
                idPessoa = mensagem.IdPessoaEnviou
            });
        }

        private MensagemViewModel Montar(uint idImovel, uint idPessoa, MensagemViewModel? enviada)
        {
            var imovel = idImovel == 0 ? null : imovelService.Get(idImovel);
            var destinatario = enviada?.IdPessoaRecebeu ?? 0;

            if (destinatario == 0 && imovel != null && imovel.IdPessoaProprietario != idPessoa)
                destinatario = imovel.IdPessoaProprietario;

            ViewData[ChaveIdImovel] = imovel?.Id;
            ViewData[ChaveNomeImovel] = imovel?.Nome;
            ViewData[ChaveHistorico] = ObterHistorico(idPessoa, destinatario);

            if (enviada != null)
            {
                enviada.IdPessoaRecebeu = destinatario;
                return enviada;
            }

            return new MensagemViewModel
            {
                IdPessoaEnviou = idPessoa,
                IdPessoaRecebeu = destinatario
            };
        }

        private List<MensagemViewModel> ObterHistorico(uint idPessoa, uint destinatario)
        {
            if (idPessoa == 0 || destinatario == 0)
                return [];

            return mensagemService.GetByPessoa(idPessoa)
                .Where(mensagem =>
                    (mensagem.IdPessoaEnviou == idPessoa && mensagem.IdPessoaRecebeu == destinatario)
                    || (mensagem.IdPessoaEnviou == destinatario && mensagem.IdPessoaRecebeu == idPessoa))
                .OrderBy(mensagem => mensagem.DataEnvio)
                .Select(mensagem =>
                {
                    var item = MensagemMapper.ToViewModel(mensagem);
                    item.Enviada = mensagem.IdPessoaEnviou == idPessoa;
                    return item;
                })
                .ToList();
        }
    }
}
