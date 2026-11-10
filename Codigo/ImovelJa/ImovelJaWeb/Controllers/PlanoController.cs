using Core.Service;
using ImovelJaWeb.Mappers;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImovelJaWeb.Controllers
{
    public class PlanoController : Controller
    {
        private readonly IPlanoService planoService;

        public PlanoController(IPlanoService planoService)
        {
            this.planoService = planoService;
        }

        // GET: Plano
        public ActionResult Index()
        {
            var planos = planoService.GetAll().OrderBy(plano => plano.Id);
            var planosViewModel = PlanoMapper.ToViewModel(planos);
            return View(planosViewModel);
        }

        // GET: Plano/Details/5
        public ActionResult Details(uint id)
        {
            var plano = planoService.Get(id);
            var planoViewModel = plano != null ? PlanoMapper.ToViewModel(plano) : new PlanoViewModel();
            return View(planoViewModel);
        }

        // GET: Plano/Edit/5
        public ActionResult Edit(uint id)
        {
            PrepararDemaisPlanos(id);

            var plano = planoService.Get(id);
            var planoViewModel = plano != null ? PlanoMapper.ToViewModel(plano) : new PlanoViewModel();
            return View(planoViewModel);
        }

        // POST: Plano/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(PlanoViewModel planoViewModel)
        {
            if (!ModelState.IsValid)
            {
                PrepararDemaisPlanos(planoViewModel.Id);
                return View(planoViewModel);
            }

            try
            {
                planoService.Edit(PlanoMapper.ToEntity(planoViewModel));
            }
            catch (ServiceException ex)
            {
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, ex.Message);
                PrepararDemaisPlanos(planoViewModel.Id);
                return View(planoViewModel);
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Plano/Activate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Activate(uint id, bool ativo, string? retorno)
        {
            if (!ModelState.IsValid)
            {
                var planosInvalidos = planoService.GetAll().OrderBy(plano => plano.Id);
                return View(nameof(Index), PlanoMapper.ToViewModel(planosInvalidos));
            }

            try
            {
                planoService.Activate(id, ativo);
            }
            catch (ServiceException ex)
            {
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, ex.Message);

                if (retorno == nameof(Edit))
                {
                    PrepararDemaisPlanos(id);
                    var plano = planoService.Get(id);
                    var modelo = plano != null ? PlanoMapper.ToViewModel(plano) : new PlanoViewModel { Id = id };
                    return View(nameof(Edit), modelo);
                }

                var planos = planoService.GetAll().OrderBy(plano => plano.Id);
                return View(nameof(Index), PlanoMapper.ToViewModel(planos));
            }

            return RedirectToAction(nameof(Index));
        }

        private void PrepararDemaisPlanos(uint id)
        {
            var demais = planoService.GetAll()
                .Where(plano => plano.Id != id)
                .OrderBy(plano => plano.Id);

            ViewBag.DemaisPlanos = PlanoMapper.ToViewModel(demais);
        }
    }
}
