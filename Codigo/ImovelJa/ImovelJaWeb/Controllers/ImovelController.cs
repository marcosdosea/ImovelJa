using Core;
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

        // GET: ImovelController/Review
        public ActionResult Review()
        {
            var imoveis = imovelService.GetByValidacao("P");
            var imoveisViewModel = ImovelMapper.ToViewModel(imoveis);
            return View(imoveisViewModel);
        }

        // GET: ImovelController/Validate/5
        public ActionResult Validate(uint id)
        {
            var imovel = imovelService.Get(id);
            var imovelViewModel = imovel != null ? ImovelMapper.ToViewModel(imovel) : new ImovelViewModel();
            return View(imovelViewModel);
        }

        // POST: ImovelController/Validate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Validate(ImovelViewModel imovelViewModel, bool aprovado)
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
                return View(nameof(Validate), modelo);
            }

            return RedirectToAction(nameof(Review));
        }

        // GET: ImovelController/Search?Termo=casa&ValorMaximo=2000
        public ActionResult Search(ImovelFiltroViewModel filtro)
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

        // GET: Imovel/Index?idPessoa=1
        public ActionResult Index(uint idPessoa)
        {
            ViewData["IdPessoa"] = idPessoa;
            return View(Listar(idPessoa));
        }

        // GET: Imovel/Create?idPessoa=1
        public ActionResult Create(uint idPessoa)
        {
            ViewData["IdPessoa"] = idPessoa;
            return View(new ImovelViewModel { IdPessoaProprietario = idPessoa });
        }

        // POST: Imovel/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ImovelViewModel imovelViewModel)
        {
            ViewData["IdPessoa"] = imovelViewModel.IdPessoaProprietario;

            if (!ModelState.IsValid)
            {
                return View(imovelViewModel);
            }

            try
            {
                imovelService.Create(ImovelMapper.ToEntity(imovelViewModel));
            }
            catch (ServiceException ex)
            {
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(imovelViewModel);
            }

            return RedirectToAction(nameof(Index), new { idPessoa = imovelViewModel.IdPessoaProprietario });
        }

        // GET: Imovel/Edit/5?idPessoa=1
        public ActionResult Edit(uint id, uint idPessoa)
        {
            ViewData["IdPessoa"] = idPessoa;
            var imovel = imovelService.GetDoProprietario(id, idPessoa);
            var imovelViewModel = imovel != null
                ? ImovelMapper.ToViewModel(imovel)
                : new ImovelViewModel { IdPessoaProprietario = idPessoa };
            return View(imovelViewModel);
        }

        // POST: Imovel/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ImovelViewModel imovelViewModel)
        {
            ViewData["IdPessoa"] = imovelViewModel.IdPessoaProprietario;

            if (!ModelState.IsValid)
            {
                return View(imovelViewModel);
            }

            if (imovelService.GetDoProprietario(imovelViewModel.Id, imovelViewModel.IdPessoaProprietario) == null)
            {
                ModelState.AddModelError(string.Empty, "Imóvel não encontrado.");
                return View(imovelViewModel);
            }

            try
            {
                imovelService.Edit(ImovelMapper.ToEntity(imovelViewModel));
            }
            catch (ServiceException ex)
            {
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(imovelViewModel);
            }

            return RedirectToAction(nameof(Index), new { idPessoa = imovelViewModel.IdPessoaProprietario });
        }

        // GET: Imovel/Delete/5?idPessoa=1
        public ActionResult Delete(uint id, uint idPessoa)
        {
            ViewData["IdPessoa"] = idPessoa;
            var imovel = imovelService.GetDoProprietario(id, idPessoa);
            var imovelViewModel = imovel != null
                ? ImovelMapper.ToViewModel(imovel)
                : new ImovelViewModel { IdPessoaProprietario = idPessoa };
            return View(imovelViewModel);
        }

        // POST: Imovel/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(uint id, uint idPessoa)
        {
            ViewData["IdPessoa"] = idPessoa;

            if (!ModelState.IsValid)
            {
                var invalido = imovelService.GetDoProprietario(id, idPessoa);
                var modeloInvalido = invalido != null
                    ? ImovelMapper.ToViewModel(invalido)
                    : new ImovelViewModel { Id = id, IdPessoaProprietario = idPessoa };
                return View(modeloInvalido);
            }

            var imovel = imovelService.GetDoProprietario(id, idPessoa);
            if (imovel == null)
            {
                ModelState.AddModelError(string.Empty, "Imóvel não encontrado.");
                return View(new ImovelViewModel { IdPessoaProprietario = idPessoa });
            }

            try
            {
                imovelService.Delete(id);
            }
            catch (ServiceException ex)
            {
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(ImovelMapper.ToViewModel(imovel));
            }

            return RedirectToAction(nameof(Index), new { idPessoa });
        }

        // POST: Imovel/Pause
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Pause(uint id, bool pausado, uint idPessoa)
        {
            ViewData["IdPessoa"] = idPessoa;

            if (!ModelState.IsValid)
            {
                return View(nameof(Index), Listar(idPessoa));
            }

            if (imovelService.GetDoProprietario(id, idPessoa) == null)
            {
                ModelState.AddModelError(string.Empty, "Imóvel não encontrado.");
                return View(nameof(Index), Listar(idPessoa));
            }

            try
            {
                imovelService.Pause(id, pausado);
            }
            catch (ServiceException ex)
            {
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(nameof(Index), Listar(idPessoa));
            }

            return RedirectToAction(nameof(Index), new { idPessoa });
        }

        // GET: ImovelController/Details/5
        public ActionResult Details(uint id, uint idPessoa = 0)
        {
            if (idPessoa != 0)
            {
                var doDono = imovelService.GetDoProprietario(id, idPessoa);
                if (doDono != null)
                {
                    ViewData["Dono"] = true;
                    ViewData["IdPessoa"] = idPessoa;
                    return View(ImovelMapper.ToViewModel(doDono));
                }
            }

            var publico = imovelService.GetPublico(id);
            if (publico == null)
            {
                return NotFound();
            }

            return View(ImovelMapper.ToViewModel(publico));
        }

        private IEnumerable<ImovelViewModel> Listar(uint idPessoa)
        {
            var imoveis = imovelService.GetByProprietario(idPessoa).OrderBy(imovel => imovel.Id);
            return ImovelMapper.ToViewModel(imoveis);
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