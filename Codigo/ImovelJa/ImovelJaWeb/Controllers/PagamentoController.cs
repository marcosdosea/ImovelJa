using Core;
using Core.Service;
using ImovelJaWeb.Mappers;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImovelJaWeb.Controllers
{
    public class PagamentoController : Controller
    {
        private readonly IPagamentoService pagamentoService;
        private readonly IAluguelService aluguelService;
        private readonly IImovelService imovelService;

        public PagamentoController(IPagamentoService pagamentoService, IAluguelService aluguelService, IImovelService imovelService)
        {
            this.pagamentoService = pagamentoService;
            this.aluguelService = aluguelService;
            this.imovelService = imovelService;
        }

        // GET: Pagamento/Details/5?idPessoa=2
        public ActionResult Details(uint id, uint idPessoa)
        {
            ViewData[AluguelController.ChaveIdPessoa] = idPessoa;

            try
            {
                var pagamento = pagamentoService.Get(id);
                if (pagamento == null || !PodeAcessar(pagamento, idPessoa))
                    return NotFound();

                ViewData["Dono"] = !DoInquilino(pagamento, idPessoa);
                return View(MontarModelo(pagamento));
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(new PagamentoViewModel { Id = id });
            }
        }

        // GET: Pagamento/Create?idAluguel=4&mes=2026-12-01&idPessoa=3
        public ActionResult Create(uint idAluguel, DateTime mes, uint idPessoa)
        {
            ViewData[AluguelController.ChaveIdPessoa] = idPessoa;

            var aluguel = aluguelService.GetDoProprietario(idAluguel, idPessoa);
            if (aluguel == null)
                return NotFound();

            var referencia = mes == default ? DateTime.Today : mes;
            var mesReferencia = new DateTime(referencia.Year, referencia.Month, 1);

            var registrado = pagamentoService.GetByAluguel(idAluguel)
                .FirstOrDefault(pagamento => pagamento.MesReferencia.Year == mesReferencia.Year
                                          && pagamento.MesReferencia.Month == mesReferencia.Month);
            if (registrado != null)
                return RedirectToAction(nameof(Edit), new { id = registrado.Id, idPessoa });

            return View(new PagamentoViewModel
            {
                IdAluguel = idAluguel,
                MesReferencia = mesReferencia,
                ValorAluguel = aluguel.Valor,
                NomeImovel = NomeDoImovel(aluguel)
            });
        }

        // POST: Pagamento/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PagamentoViewModel pagamentoViewModel, IFormFile? comprovante, uint idPessoa, bool pago)
        {
            ViewData[AluguelController.ChaveIdPessoa] = idPessoa;

            pagamentoViewModel.Id = 0;
            pagamentoViewModel.Status = pago ? "PA" : "PE";

            if (!ModelState.IsValid)
                return View(pagamentoViewModel);

            try
            {
                if (aluguelService.GetDoProprietario(pagamentoViewModel.IdAluguel, idPessoa) == null)
                    return NotFound();

                if (comprovante != null && comprovante.Length > 0)
                    pagamentoViewModel.Comprovante = LerArquivo(comprovante);

                pagamentoService.Create(PagamentoMapper.ToEntity(pagamentoViewModel));
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(pagamentoViewModel);
            }

            return RedirectToAction(nameof(AluguelController.Details), "Aluguel", new { id = pagamentoViewModel.IdAluguel, idPessoa });
        }

        // GET: Pagamento/Edit/5?idPessoa=2
        public ActionResult Edit(uint id, uint idPessoa)
        {
            ViewData[AluguelController.ChaveIdPessoa] = idPessoa;

            try
            {
                var pagamento = pagamentoService.Get(id);
                if (pagamento == null || !PodeAcessar(pagamento, idPessoa))
                    return NotFound();

                return View(MontarModelo(pagamento));
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(new PagamentoViewModel { Id = id });
            }
        }

        // POST: Pagamento/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(PagamentoViewModel pagamentoViewModel, IFormFile? comprovante, uint idPessoa, bool pago)
        {
            ViewData[AluguelController.ChaveIdPessoa] = idPessoa;

            if (pago)
                pagamentoViewModel.Status = "PA";
            else if (pagamentoViewModel.Status == "PA")
                pagamentoViewModel.Status = "PE";

            if (!ModelState.IsValid)
                return View(pagamentoViewModel);

            try
            {
                var atual = pagamentoService.Get(pagamentoViewModel.Id);
                if (atual == null || !PodeAcessar(atual, idPessoa))
                    return NotFound();

                pagamentoViewModel.IdAluguel = atual.IdAluguel;
                pagamentoViewModel.MesReferencia = atual.MesReferencia;

                if (comprovante != null && comprovante.Length > 0)
                    pagamentoViewModel.Comprovante = LerArquivo(comprovante);

                pagamentoService.Edit(PagamentoMapper.ToEntity(pagamentoViewModel));
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(pagamentoViewModel);
            }

            return RedirectToAction(nameof(AluguelController.Details), "Aluguel", new { id = pagamentoViewModel.IdAluguel, idPessoa });
        }

        // GET: Pagamento/Comprovante/5?idPessoa=2
        public ActionResult Comprovante(uint id, uint idPessoa)
        {
            try
            {
                var pagamento = pagamentoService.Get(id);
                if (pagamento == null || !PodeAcessar(pagamento, idPessoa)
                    || pagamento.Comprovante == null || pagamento.Comprovante.Length == 0)
                    return NotFound();

                return File(pagamento.Comprovante, TipoDoArquivo(pagamento.Comprovante));
            }
            catch (ServiceException)
            {
                return NotFound();
            }
        }

        private PagamentoViewModel MontarModelo(Pagamento pagamento)
        {
            var modelo = PagamentoMapper.ToViewModel(pagamento);
            modelo.NomeImovel = NomeDoImovel(pagamento.IdAluguelNavigation);
            return modelo;
        }

        private string NomeDoImovel(Aluguel? aluguel)
        {
            if (aluguel == null)
                return string.Empty;

            var imovel = aluguel.IdImovelNavigation != null && aluguel.IdImovelNavigation.Id != 0
                ? aluguel.IdImovelNavigation
                : aluguel.IdImovel == 0 ? null : imovelService.Get(aluguel.IdImovel);

            if (imovel == null || string.IsNullOrWhiteSpace(imovel.Nome))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(imovel.Cidade)
                || imovel.Nome.Contains(imovel.Cidade, StringComparison.CurrentCultureIgnoreCase))
                return imovel.Nome;

            return $"{imovel.Nome} — {imovel.Cidade}";
        }

        /// <summary>
        /// Confere se a pessoa pode ver ou alterar o pagamento: o inquilino do aluguel ou o dono do imóvel alugado
        /// </summary>
        /// <param name="pagamento">pagamento com o aluguel carregado</param>
        /// <param name="idPessoa">id da pessoa</param>
        /// <returns>verdadeiro se a pessoa for o inquilino ou o dono</returns>
        private bool PodeAcessar(Pagamento pagamento, uint idPessoa)
        {
            return DoInquilino(pagamento, idPessoa)
                || aluguelService.GetDoProprietario(pagamento.IdAluguel, idPessoa) != null;
        }

        private static bool DoInquilino(Pagamento pagamento, uint idPessoa)
        {
            return pagamento.IdAluguelNavigation != null
                && pagamento.IdAluguelNavigation.IdPessoaInquilino == idPessoa;
        }

        private static byte[] LerArquivo(IFormFile arquivo)
        {
            using var memoria = new MemoryStream();
            arquivo.CopyTo(memoria);
            return memoria.ToArray();
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
