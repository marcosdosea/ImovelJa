using Core.DTO;
using Core.Service;
using ImovelJaWeb.Mappers;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImovelJaWeb.Controllers
{
    public class ImovelController : Controller
    {
        private readonly IImovelService imovelService;

        public ImovelController(IImovelService imovelService)
        {
            this.imovelService = imovelService;
        }

        // GET: ImovelController/Pendentes
        public ActionResult Pendentes()
        {
            var imoveis = imovelService.GetByValidacao("P");
            var imoveisViewModel = ImovelMapper.ToViewModel(imoveis);
            return View(imoveisViewModel);
        }

        // GET: ImovelController/Detalhes/5
        public ActionResult Detalhes(uint id)
        {
            var imovel = imovelService.Get(id);
            var imovelViewModel = imovel != null ? ImovelMapper.ToViewModel(imovel) : new ImovelViewModel();
            return View(imovelViewModel);
        }

        // GET: ImovelController/Validar/5
        public ActionResult Validar(uint id)
        {
            var imovel = imovelService.Get(id);
            var imovelViewModel = imovel != null ? ImovelMapper.ToViewModel(imovel) : new ImovelViewModel();
            return View(imovelViewModel);
        }

        // POST: ImovelController/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(ImovelViewModel imovelViewModel, bool aprovado)
        {
            try
            {
                imovelService.Approve(imovelViewModel.Id, aprovado);
            }
            catch (ServiceException ex)
            {
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, ex.Message);

                var imovel = imovelService.Get(imovelViewModel.Id);
                var modelo = imovel != null ? ImovelMapper.ToViewModel(imovel) : imovelViewModel;
                return View(nameof(Validar), modelo);
            }

            return RedirectToAction(nameof(Pendentes));
        }

        // GET: ImovelController/Buscar?Termo=casa&ValorMaximo=2000
        public ActionResult Buscar(ImovelFiltroViewModel filtro)
        {
            ValidarIntervalos(filtro);
            ViewData["Filtro"] = filtro;

            if (!ModelState.IsValid)
            {
                return View(Array.Empty<ImovelViewModel>());
            }

            var imoveis = imovelService.GetByFiltro(ImovelMapper.ToDTO(filtro));
            return View(ImovelMapper.ToViewModel(imoveis));
        }

        // GET: ImovelController/Detalhes/5
        public ActionResult Detalhes(uint id)
        {
            var imovel = imovelService.Get(id);

            if (imovel == null || imovel.StatusValidacao != "A" || imovel.Status != "D")
            {
                return NotFound();
            }

            return View(ImovelMapper.ToViewModel(imovel));
        }

        private void ValidarIntervalos(ImovelFiltroViewModel filtro)
        {
            if (filtro.ValorMinimo > filtro.ValorMaximo)
            {
                ModelState.AddModelError(
                    nameof(filtro.ValorMaximo),
                    "O valor máximo deve ser maior ou igual ao valor mínimo");
            }

            if (filtro.AreaMinima > filtro.AreaMaxima)
            {
                ModelState.AddModelError(
                    nameof(filtro.AreaMaxima),
                    "A área máxima deve ser maior ou igual à área mínima");
            }
        }
    }
}