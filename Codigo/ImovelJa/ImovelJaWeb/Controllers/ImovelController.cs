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
            if (imovel == null || imovel.StatusValidacao != "A" || imovel.Status != "D")
                return View(new ImovelViewModel());

            return View(ImovelMapper.ToViewModel(imovel));
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
    }
}