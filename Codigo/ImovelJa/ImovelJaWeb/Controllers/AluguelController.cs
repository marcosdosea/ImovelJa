using Core;
using Core.Service;
using ImovelJaWeb.Mappers;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImovelJaWeb.Controllers
{
    public class AluguelController : Controller
    {
        public const string ChaveIdPessoa = "IdPessoa";

        private readonly IAluguelService aluguelService;
        private readonly IPagamentoService pagamentoService;
        private readonly IImovelService imovelService;
        private readonly IPessoaService pessoaService;

        public AluguelController(
            IAluguelService aluguelService,
            IPagamentoService pagamentoService,
            IImovelService imovelService,
            IPessoaService pessoaService)
        {
            this.aluguelService = aluguelService;
            this.pagamentoService = pagamentoService;
            this.imovelService = imovelService;
            this.pessoaService = pessoaService;
        }

        // GET: Aluguel?idPessoa=2
        public ActionResult Index(uint idPessoa)
        {
            PrepararPessoa(idPessoa);

            var alugueis = aluguelService.GetByInquilino(idPessoa)
                .Select(aluguel => AluguelMapper.ToViewModel(aluguel, BuscarImovel(aluguel), null))
                .ToList();

            return View(alugueis);
        }

        // GET: Aluguel/Details/5?idPessoa=2
        public ActionResult Details(uint id, uint idPessoa)
        {
            PrepararPessoa(idPessoa);

            try
            {
                var aluguel = aluguelService.GetDoInquilino(id, idPessoa);
                if (aluguel == null)
                    return NotFound();

                var modelo = AluguelMapper.ToViewModel(aluguel, BuscarImovel(aluguel), BuscarInquilino(aluguel));
                modelo.Pagamentos = PagamentoMapper.ToViewModel(pagamentoService.GetByAluguel(id)).ToList();
                return View(modelo);
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(new AluguelViewModel { Id = id, IdPessoaInquilino = idPessoa });
            }
        }

        // GET: Aluguel/Contrato/5?idPessoa=2
        public ActionResult Contrato(uint id, uint idPessoa)
        {
            try
            {
                var aluguel = aluguelService.GetDoInquilino(id, idPessoa);
                if (aluguel == null)
                    return NotFound();

                if (aluguel.ArquivoContrato == null || aluguel.ArquivoContrato.Length == 0)
                    return NotFound();

                var tipo = TipoDoArquivo(aluguel.ArquivoContrato);
                var nome = tipo == "application/pdf" ? "contrato.pdf" : "contrato";
                return File(aluguel.ArquivoContrato, tipo, nome);
            }
            catch (ServiceException)
            {
                return NotFound();
            }
        }

        private void PrepararPessoa(uint idPessoa)
        {
            ViewData[ChaveIdPessoa] = idPessoa;

            if (idPessoa == 0)
                return;

            var pessoa = pessoaService.Get(idPessoa);
            if (pessoa != null && !string.IsNullOrWhiteSpace(pessoa.Nome))
                ViewData["NomeUsuario"] = pessoa.Nome;
        }

        private Imovel? BuscarImovel(Aluguel aluguel)
        {
            if (aluguel.IdImovelNavigation != null && aluguel.IdImovelNavigation.Id != 0)
                return aluguel.IdImovelNavigation;

            return aluguel.IdImovel == 0 ? null : imovelService.Get(aluguel.IdImovel);
        }

        private Pessoa? BuscarInquilino(Aluguel aluguel)
        {
            if (aluguel.IdPessoaInquilinoNavigation != null && aluguel.IdPessoaInquilinoNavigation.Id != 0)
                return aluguel.IdPessoaInquilinoNavigation;

            return aluguel.IdPessoaInquilino == 0 ? null : pessoaService.Get(aluguel.IdPessoaInquilino);
        }

        private static string TipoDoArquivo(byte[] arquivo)
        {
            if (arquivo.Length >= 4
                && arquivo[0] == 0x25 && arquivo[1] == 0x50 && arquivo[2] == 0x44 && arquivo[3] == 0x46)
                return "application/pdf";

            if (arquivo.Length >= 3 && arquivo[0] == 0xFF && arquivo[1] == 0xD8 && arquivo[2] == 0xFF)
                return "image/jpeg";

            if (arquivo.Length >= 8
                && arquivo[0] == 0x89 && arquivo[1] == 0x50 && arquivo[2] == 0x4E && arquivo[3] == 0x47)
                return "image/png";

            return "application/octet-stream";
        }
    }
}
