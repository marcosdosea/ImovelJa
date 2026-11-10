using Core.Service;
using ImovelJaWeb.Mappers;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImovelJaWeb.Controllers
{
    public class MensagemController : Controller
    {
        private const string ChaveTexto = "NovaMensagem.Texto";

        private readonly IMensagemService mensagemService;
        private readonly IImovelService imovelService;
        private readonly IPessoaService pessoaService;

        public MensagemController(
            IMensagemService mensagemService,
            IImovelService imovelService,
            IPessoaService pessoaService)
        {
            this.mensagemService = mensagemService;
            this.imovelService = imovelService;
            this.pessoaService = pessoaService;
        }

        // GET: MensagemController/Conversa?idImovel=1&idPessoa=2
        public ActionResult Conversa(uint? idImovel, uint? idDestinatario, uint? idPessoa)
        {
            var modelo = MontarConversa(idImovel, idDestinatario, idPessoa ?? 0, null);
            return View(modelo);
        }

        // POST: MensagemController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MensagemViewModel novaMensagem, uint? idImovel)
        {
            var emBranco = string.IsNullOrWhiteSpace(novaMensagem.Texto);
            if (emBranco && ModelState[ChaveTexto]?.Errors.Count is null or 0)
                ModelState.AddModelError(ChaveTexto, "A mensagem não pode estar em branco.");

            if (!ModelState.IsValid || emBranco)
                return View(nameof(Conversa), MontarConversa(idImovel, novaMensagem.IdPessoaRecebeu, novaMensagem.IdPessoaEnviou, novaMensagem));

            try
            {
                mensagemService.Create(MensagemMapper.ToEntity(novaMensagem));
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(nameof(Conversa), MontarConversa(idImovel, novaMensagem.IdPessoaRecebeu, novaMensagem.IdPessoaEnviou, novaMensagem));
            }

            return RedirectToAction(nameof(Conversa), new
            {
                idImovel,
                idDestinatario = novaMensagem.IdPessoaRecebeu,
                idPessoa = novaMensagem.IdPessoaEnviou
            });
        }

        private ConversaViewModel MontarConversa(uint? idImovel, uint? idDestinatario, uint idPessoa, MensagemViewModel? novaMensagem)
        {
            var pessoas = pessoaService.GetAll().ToDictionary(pessoa => pessoa.Id);
            var modelo = new ConversaViewModel
            {
                IdPessoa = idPessoa,
                NomePessoa = pessoas.GetValueOrDefault(idPessoa)?.Nome ?? string.Empty
            };

            var imovel = idImovel.HasValue ? imovelService.Get(idImovel.Value) : null;
            if (imovel != null)
                modelo.IdImovel = imovel.Id;

            var destinatario = idDestinatario.GetValueOrDefault();
            if (destinatario == 0 && imovel != null && imovel.IdPessoaProprietario != idPessoa)
                destinatario = imovel.IdPessoaProprietario;

            if (destinatario != 0 && destinatario != idPessoa)
            {
                modelo.IdDestinatario = destinatario;
                modelo.NomeDestinatario = pessoas.GetValueOrDefault(destinatario)?.Nome ?? string.Empty;
                modelo.ConversaAberta = true;
                if (imovel != null && imovel.IdPessoaProprietario == destinatario)
                    modelo.NomeImovel = imovel.Nome;
            }

            var mensagens = idPessoa == 0
                ? []
                : mensagemService.GetByPessoa(idPessoa).ToList();

            var participantes = mensagens
                .Select(mensagem => mensagem.IdPessoaEnviou == idPessoa ? mensagem.IdPessoaRecebeu : mensagem.IdPessoaEnviou)
                .Where(id => id != 0 && id != idPessoa)
                .ToList();

            if (modelo.IdDestinatario != 0 && !participantes.Contains(modelo.IdDestinatario))
                participantes.Add(modelo.IdDestinatario);

            modelo.Conversas = participantes
                .Distinct()
                .Select(id =>
                {
                    var ultima = mensagens
                        .Where(mensagem => mensagem.IdPessoaEnviou == id || mensagem.IdPessoaRecebeu == id)
                        .OrderByDescending(mensagem => mensagem.DataEnvio)
                        .FirstOrDefault();

                    return new ConversaItemViewModel
                    {
                        IdPessoa = id,
                        Nome = pessoas.GetValueOrDefault(id)?.Nome ?? "Usuário",
                        NomeImovel = imovel != null && imovel.IdPessoaProprietario == id ? imovel.Nome : null,
                        Texto = ultima?.Texto ?? string.Empty,
                        DataEnvio = ultima?.DataEnvio,
                        Ativa = id == modelo.IdDestinatario
                    };
                })
                .OrderByDescending(conversa => conversa.Ativa)
                .ThenByDescending(conversa => conversa.DataEnvio ?? DateTime.MinValue)
                .ToList();

            if (modelo.ConversaAberta)
            {
                modelo.Mensagens = mensagens
                    .Where(mensagem => mensagem.IdPessoaEnviou == modelo.IdDestinatario || mensagem.IdPessoaRecebeu == modelo.IdDestinatario)
                    .OrderBy(mensagem => mensagem.DataEnvio)
                    .Select(mensagem =>
                    {
                        var item = MensagemMapper.ToViewModel(mensagem);
                        item.Enviada = mensagem.IdPessoaEnviou == idPessoa;
                        return item;
                    })
                    .ToList();
            }

            modelo.NovaMensagem = novaMensagem ?? new MensagemViewModel
            {
                IdPessoaEnviou = idPessoa,
                IdPessoaRecebeu = modelo.IdDestinatario
            };

            if (modelo.NovaMensagem.IdPessoaRecebeu == 0)
                modelo.NovaMensagem.IdPessoaRecebeu = modelo.IdDestinatario;

            return modelo;
        }
    }
}
