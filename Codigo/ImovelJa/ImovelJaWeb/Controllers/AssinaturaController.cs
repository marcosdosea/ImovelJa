using Core.Service;
using ImovelJaWeb.Mappers;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImovelJaWeb.Controllers
{
    public class AssinaturaController : Controller
    {
        private readonly IAssinaturaService assinaturaService;
        private readonly IPlanoService planoService;

        public AssinaturaController(IAssinaturaService assinaturaService, IPlanoService planoService)
        {
            this.assinaturaService = assinaturaService;
            this.planoService = planoService;
        }

        // GET: Assinatura?idPessoa=5
        public ActionResult Index(uint idPessoa)
        {
            var assinatura = assinaturaService.GetByPessoa(idPessoa);
            var assinaturaViewModel = assinatura != null
                ? AssinaturaMapper.ToViewModel(assinatura)
                : new AssinaturaViewModel { IdPessoa = idPessoa };

            PrepararPlanoAtual(assinaturaViewModel.IdPlano);
            return View(assinaturaViewModel);
        }

        // GET: Assinatura/Create?idPessoa=5
        public ActionResult Create(uint idPessoa)
        {
            PrepararPlanosDisponiveis();
            return View(new AssinaturaViewModel { IdPessoa = idPessoa });
        }

        // POST: Assinatura/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AssinaturaViewModel assinaturaViewModel)
        {
            if (!ModelState.IsValid)
            {
                PrepararPlanosDisponiveis();
                return View(assinaturaViewModel);
            }

            try
            {
                assinaturaService.Create(AssinaturaMapper.ToEntity(assinaturaViewModel));
            }
            catch (ServiceException ex)
            {
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, ex.Message);
                PrepararPlanosDisponiveis();
                return View(assinaturaViewModel);
            }

            return RedirectToAction(nameof(Index), new { idPessoa = assinaturaViewModel.IdPessoa });
        }

        // GET: Assinatura/Edit/5
        public ActionResult Edit(uint id)
        {
            var assinatura = assinaturaService.Get(id);
            var assinaturaViewModel = assinatura != null ? AssinaturaMapper.ToViewModel(assinatura) : new AssinaturaViewModel();

            PrepararPlanosDisponiveis();
            return View(assinaturaViewModel);
        }

        // POST: Assinatura/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(AssinaturaViewModel assinaturaViewModel)
        {
            if (!ModelState.IsValid)
            {
                PrepararPlanosDisponiveis();
                return View(assinaturaViewModel);
            }

            try
            {
                assinaturaService.Edit(AssinaturaMapper.ToEntity(assinaturaViewModel));
            }
            catch (ServiceException ex)
            {
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, ex.Message);
                PrepararPlanosDisponiveis();
                return View(assinaturaViewModel);
            }

            return RedirectToAction(nameof(Index), new { idPessoa = assinaturaViewModel.IdPessoa });
        }

        // GET: Assinatura/Cancel/5
        public ActionResult Cancel(uint id)
        {
            var assinatura = assinaturaService.Get(id);
            var assinaturaViewModel = assinatura != null ? AssinaturaMapper.ToViewModel(assinatura) : new AssinaturaViewModel();

            PrepararPlanoAtual(assinaturaViewModel.IdPlano);
            return View(assinaturaViewModel);
        }

        // POST: Assinatura/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancel(AssinaturaViewModel assinaturaViewModel)
        {
            try
            {
                assinaturaService.Cancel(assinaturaViewModel.Id);
            }
            catch (ServiceException ex)
            {
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, ex.Message);

                var assinatura = assinaturaService.Get(assinaturaViewModel.Id);
                var modelo = assinatura != null ? AssinaturaMapper.ToViewModel(assinatura) : assinaturaViewModel;

                PrepararPlanoAtual(modelo.IdPlano);
                return View(modelo);
            }

            return RedirectToAction(nameof(Index), new { idPessoa = assinaturaViewModel.IdPessoa });
        }

        private void PrepararPlanoAtual(uint idPlano)
        {
            var plano = idPlano != 0 ? planoService.Get(idPlano) : null;
            ViewBag.PlanoAtual = plano != null ? PlanoMapper.ToViewModel(plano) : null;
        }

        private void PrepararPlanosDisponiveis()
        {
            var planos = planoService.GetAll()
                .Where(plano => plano.Status == "A")
                .OrderBy(plano => plano.Valor);

            ViewBag.PlanosDisponiveis = PlanoMapper.ToViewModel(planos);
        }
    }
}
