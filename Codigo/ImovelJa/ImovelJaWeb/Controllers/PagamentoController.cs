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
        private readonly IImovelService imovelService;

        public PagamentoController(IPagamentoService pagamentoService, IImovelService imovelService)
        {
            this.pagamentoService = pagamentoService;
            this.imovelService = imovelService;
        }

        // GET: Pagamento/Edit/5?idPessoa=2
        public ActionResult Edit(uint id, uint idPessoa)
        {
            ViewData[AluguelController.ChaveIdPessoa] = idPessoa;

            try
            {
                var pagamento = pagamentoService.Get(id);
                if (pagamento == null || DeOutroInquilino(pagamento, idPessoa))
                    return NotFound();

                var modelo = PagamentoMapper.ToViewModel(pagamento);
                modelo.NomeImovel = NomeDoImovel(pagamento);
                return View(modelo);
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
                if (atual == null || DeOutroInquilino(atual, idPessoa))
                    return NotFound();

                if (comprovante != null && comprovante.Length > 0)
                    pagamentoViewModel.Comprovante = LerArquivo(comprovante);

                pagamentoService.Edit(PagamentoMapper.ToEntity(pagamentoViewModel));
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(pagamentoViewModel);
            }

            return RedirectToAction(nameof(AluguelController.Index), "Aluguel", new { idPessoa });
        }

        // GET: Pagamento/Comprovante/5
        public ActionResult Comprovante(uint id)
        {
            try
            {
                var pagamento = pagamentoService.Get(id);
                if (pagamento?.Comprovante == null || pagamento.Comprovante.Length == 0)
                    return NotFound();

                return File(pagamento.Comprovante, TipoDoArquivo(pagamento.Comprovante));
            }
            catch (ServiceException)
            {
                return NotFound();
            }
        }

        private string NomeDoImovel(Pagamento pagamento)
        {
            var aluguel = pagamento.IdAluguelNavigation;
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

        private static bool DeOutroInquilino(Pagamento pagamento, uint idPessoa)
        {
            if (idPessoa == 0)
                return false;

            var inquilino = pagamento.IdAluguelNavigation?.IdPessoaInquilino ?? 0;
            return inquilino != 0 && inquilino != idPessoa;
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
