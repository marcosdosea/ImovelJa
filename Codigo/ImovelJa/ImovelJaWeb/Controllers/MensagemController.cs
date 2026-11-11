using Core;
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
        public const string ChaveConversas = "Conversas";
        public const string ChaveNomeDestinatario = "NomeDestinatario";
        public const string ChaveNomePessoa = "NomePessoa";
        public const string ChaveConversaAberta = "ConversaAberta";

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

        // GET: MensagemController/Get?idImovel=1&idPessoa=2
        public ActionResult Get(uint idImovel, uint? idPessoa, uint? idDestinatario = null)
        {
            var modelo = Montar(idImovel, idPessoa ?? 0, idDestinatario ?? 0, null);
            return View(modelo);
        }

        // POST: MensagemController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MensagemViewModel mensagem, uint? idImovel)
        {
            if (!ModelState.IsValid)
                return View(nameof(Get), Montar(idImovel ?? 0, mensagem.IdPessoaEnviou, mensagem.IdPessoaRecebeu, mensagem));

            try
            {
                mensagemService.Create(MensagemMapper.ToEntity(mensagem));
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(nameof(Get), Montar(idImovel ?? 0, mensagem.IdPessoaEnviou, mensagem.IdPessoaRecebeu, mensagem));
            }

            return RedirectToAction(nameof(Get), new
            {
                idImovel,
                idPessoa = mensagem.IdPessoaEnviou,
                idDestinatario = mensagem.IdPessoaRecebeu
            });
        }

        private MensagemViewModel Montar(uint idImovel, uint idPessoa, uint idDestinatario, MensagemViewModel? enviada)
        {
            var imovel = idImovel == 0 ? null : imovelService.Get(idImovel);
            var destinatario = idDestinatario != 0
                ? idDestinatario
                : enviada?.IdPessoaRecebeu ?? 0;

            if (destinatario == 0 && imovel != null && imovel.IdPessoaProprietario != idPessoa)
                destinatario = imovel.IdPessoaProprietario;

            if (destinatario == idPessoa)
                destinatario = 0;

            var mensagens = idPessoa == 0
                ? new List<Mensagem>()
                : mensagemService.GetByPessoa(idPessoa).ToList();
            var pessoas = BuscarPessoas(Participantes(mensagens, idPessoa, destinatario));

            ViewData[ChaveIdImovel] = imovel?.Id;
            ViewData[ChaveNomeImovel] = imovel != null && imovel.IdPessoaProprietario == destinatario
                ? imovel.Nome
                : null;
            ViewData[ChaveNomePessoa] = Nome(pessoas, idPessoa);
            ViewData[ChaveNomeDestinatario] = Nome(pessoas, destinatario);
            ViewData[ChaveConversaAberta] = destinatario != 0;
            ViewData[ChaveHistorico] = Historico(idPessoa, destinatario);
            ViewData[ChaveConversas] = MontarConversas(mensagens, pessoas, idPessoa, destinatario, imovel);

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

        private List<MensagemViewModel> Historico(uint idPessoa, uint destinatario)
        {
            return mensagemService.GetConversa(idPessoa, destinatario)
                .Select(mensagem =>
                {
                    var item = MensagemMapper.ToViewModel(mensagem);
                    item.Enviada = mensagem.IdPessoaEnviou == idPessoa;
                    return item;
                })
                .ToList();
        }

        private Dictionary<uint, Pessoa> BuscarPessoas(IEnumerable<uint> ids)
        {
            var pessoas = new Dictionary<uint, Pessoa>();

            foreach (var id in ids.Distinct())
            {
                if (id == 0)
                    continue;

                var pessoa = pessoaService.Get(id);
                if (pessoa != null)
                    pessoas[id] = pessoa;
            }

            return pessoas;
        }

        private static List<uint> Participantes(List<Mensagem> mensagens, uint idPessoa, uint destinatario)
        {
            var participantes = mensagens
                .Select(mensagem => mensagem.IdPessoaEnviou == idPessoa
                    ? mensagem.IdPessoaRecebeu
                    : mensagem.IdPessoaEnviou)
                .Where(id => id != 0 && id != idPessoa)
                .Distinct()
                .ToList();

            if (idPessoa != 0)
                participantes.Add(idPessoa);

            if (destinatario != 0 && !participantes.Contains(destinatario))
                participantes.Add(destinatario);

            return participantes;
        }

        private static List<ConversaItemViewModel> MontarConversas(
            List<Mensagem> mensagens,
            Dictionary<uint, Pessoa> pessoas,
            uint idPessoa,
            uint destinatario,
            Imovel? imovel)
        {
            return Participantes(mensagens, idPessoa, destinatario)
                .Where(id => id != idPessoa)
                .Select(id =>
                {
                    var ultima = mensagens
                        .Where(mensagem => mensagem.IdPessoaEnviou == id || mensagem.IdPessoaRecebeu == id)
                        .OrderByDescending(mensagem => mensagem.DataEnvio)
                        .FirstOrDefault();

                    return new ConversaItemViewModel
                    {
                        IdPessoa = id,
                        Nome = Nome(pessoas, id),
                        NomeImovel = imovel != null && imovel.IdPessoaProprietario == id ? imovel.Nome : null,
                        Texto = ultima?.Texto ?? string.Empty,
                        DataEnvio = ultima?.DataEnvio,
                        Ativa = id == destinatario
                    };
                })
                .OrderByDescending(conversa => conversa.Ativa)
                .ThenByDescending(conversa => conversa.DataEnvio ?? DateTime.MinValue)
                .ToList();
        }

        private static string Nome(Dictionary<uint, Pessoa> pessoas, uint id)
        {
            if (id == 0 || !pessoas.TryGetValue(id, out var pessoa) || string.IsNullOrWhiteSpace(pessoa.Nome))
                return string.Empty;

            return pessoa.Nome;
        }
    }
}
