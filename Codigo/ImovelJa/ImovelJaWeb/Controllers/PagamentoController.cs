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

        public PagamentoController(IPagamentoService pagamentoService, IAluguelService aluguelService)
        {
            this.pagamentoService = pagamentoService;
            this.aluguelService = aluguelService;
        }

        // GET: Pagamento/Create?idAluguel=1&mes=2026-11-01
        public ActionResult Create(uint idAluguel, DateTime mes)
        {
            var aluguel = aluguelService.Get(idAluguel);
            if (aluguel == null)
                return NotFound();

            var pagamentoViewModel = new PagamentoViewModel
            {
                IdAluguel = idAluguel,
                MesReferencia = new DateTime(mes.Year, mes.Month, 1),
                ValorAluguel = aluguel.Valor
            };
            return View(pagamentoViewModel);
        }

        // GET: Pagamento/Details/5
        public ActionResult Details(uint id)
        {
            var pagamento = pagamentoService.Get(id);
            if (pagamento == null)
                return NotFound();

            return View(PagamentoMapper.ToViewModel(pagamento));
        }

        // POST: Pagamento/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PagamentoViewModel pagamentoViewModel)
        {
            if (!ModelState.IsValid)
                return View(pagamentoViewModel);

            try
            {
                var id = pagamentoService.Create(PagamentoMapper.ToEntity(pagamentoViewModel));
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(pagamentoViewModel);
            }
        }

        // GET: Pagamento/Edit/5
        public ActionResult Edit(uint id)
        {
            var pagamento = pagamentoService.Get(id);
            if (pagamento == null)
                return NotFound();

            return View(PagamentoMapper.ToViewModel(pagamento));
        }

        // POST: Pagamento/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(PagamentoViewModel pagamentoViewModel)
        {
            if (!ModelState.IsValid)
                return View(pagamentoViewModel);

            try
            {
                pagamentoService.Edit(PagamentoMapper.ToEntity(pagamentoViewModel));
                return RedirectToAction(nameof(Details), new { id = pagamentoViewModel.Id });
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(pagamentoViewModel);
            }
        }
    }
}
