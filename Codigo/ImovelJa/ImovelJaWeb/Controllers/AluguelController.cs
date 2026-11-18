using Core;
using Core.Service;
using ImovelJaWeb.Mappers;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImovelJaWeb.Controllers
{
    public class AluguelController : Controller
    {
        private readonly IAluguelService aluguelService;
        private readonly IImovelService imovelService;
        private readonly IPessoaService pessoaService;

        public AluguelController(IAluguelService aluguelService, IImovelService imovelService, IPessoaService pessoaService)
        {
            this.aluguelService = aluguelService;
            this.imovelService = imovelService;
            this.pessoaService = pessoaService;
        }

        // GET: Aluguel/Create?idImovel=5
        public ActionResult Create(uint idImovel)
        {
            var imovel = imovelService.Get(idImovel);
            if(imovel == null)
            {
                return NotFound();
            }
            string? erro = VerificarImovel(imovel);
            if(erro != null)
            {
                TempData["Erro"] = erro;
                return RedirectToAction(nameof(Details), new { idImovel });
            }

            var model = new AluguelViewModel();
            model.IdImovel = idImovel;
            model.Valor = imovel.Valor;
            model.Inquilino = new PessoaViewModel { Tipo = "F" };
            return View(model);
        }
        // POST: Aluguel/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(AluguelViewModel model)
        {
            var imovel = imovelService.Get(model.IdImovel);
            if (imovel == null)
                return NotFound();

            if (model.IdPessoaInquilino != 0)
                IgnorarErrosDoInquilino();

            string? erro = VerificarImovel(imovel);
            if (erro != null)
                ModelState.AddModelError(string.Empty, erro);

            if (!ModelState.IsValid)
                return View(model);

            var aluguel = AluguelMapper.ToEntity(model);

            if (model.IdPessoaInquilino == 0 && model.Inquilino != null)
            {
                model.Inquilino.Email = null;
                aluguel.IdPessoaInquilinoNavigation = PessoaMapper.ToEntity(model.Inquilino);
            }

            if (model.Contrato != null && model.Contrato.Length > 0)
                aluguel.ArquivoContrato = LerArquivo(model.Contrato);

            try
            {
                uint id = aluguelService.Create(aluguel);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        // GET: Aluguel/Details/5
        public IActionResult Details(uint id)
        {
            var aluguel = aluguelService.Get(id);
            if (aluguel == null)
                return NotFound();

            return View(MontarModelo(aluguel));
        }

        // POST: Aluguel/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(AluguelViewModel model)
        {
            var aluguel = aluguelService.Get(model.Id);
            if (aluguel == null)
                return NotFound();

         
            IgnorarErrosDoInquilino();

            if (aluguel.Status != "A")
                ModelState.AddModelError(string.Empty, "Não é possível editar um aluguel cancelado.");

            if (!ModelState.IsValid)
                return View("Details", CompletarModelo(model, aluguel));

         
            uint idImovel = aluguel.IdImovel;
            uint idInquilino = aluguel.IdPessoaInquilino;

         
            AluguelMapper.ToEntity(model, aluguel);

            aluguel.IdImovel = idImovel;
            aluguel.IdPessoaInquilino = idInquilino;

           
            if (model.Contrato != null && model.Contrato.Length > 0)
                aluguel.ArquivoContrato = LerArquivo(model.Contrato);

            try
            {
                aluguelService.Edit(aluguel);
                return RedirectToAction(nameof(Details), new { id = aluguel.Id });
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Details", CompletarModelo(model, aluguel));
            }
        }

        // GET: Aluguel/Delete/5
        public IActionResult Delete(uint id)
        {
            var aluguel = aluguelService.Get(id);
            if (aluguel == null)
                return NotFound();

            if (aluguel.Status != "A")
            {
                TempData["Erro"] = "O aluguel já está cancelado.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(MontarModelo(aluguel));
        }

        // POST: Aluguel/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(uint id)
        {
            var aluguel = aluguelService.Get(id);
            if (aluguel == null)
                return NotFound();

            
            if (aluguel.Status != "A")
            {
                TempData["Erro"] = "O aluguel já está cancelado.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                aluguelService.Delete(id);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(MontarModelo(aluguel));
            }
        }


        public static string? VerificarImovel(Imovel imovel) 
        {
            if(imovel.Finalidade != "A")
            {
                return "O imóvel não é anunciado para aluguel";
            }
            if(imovel.Status != "A")
            {
                return "O imóvel não foi aprovado";
            }
            if(imovel.Status != "D")
            {
                return "O imóvel não está disponivel";
            }

            return null;
        }
        private void IgnorarErrosDoInquilino()
        {
            var chaves = ModelState.Keys.Where(k => k.StartsWith("Inquilino")).ToList();
            foreach (var chave in chaves)
                ModelState.Remove(chave);
        }
        private static byte[] LerArquivo(IFormFile arquivo)
        {
            using var memoria = new MemoryStream();
            arquivo.CopyTo(memoria);
            return memoria.ToArray();
        }
        
        private PessoaViewModel? BuscarInquilino(uint idPessoa)
        {
            var pessoa = pessoaService.Get(idPessoa);
            if (pessoa == null)
                return null;

            return PessoaMapper.ToViewModel(pessoa);
        }

        
        private AluguelViewModel MontarModelo(Aluguel aluguel)
        {
            var model = AluguelMapper.ToViewModel(aluguel);
            model.Inquilino = BuscarInquilino(aluguel.IdPessoaInquilino);
            return model;
        }

        
        private AluguelViewModel CompletarModelo(AluguelViewModel model, Aluguel aluguel)
        {
            model.IdImovel = aluguel.IdImovel;
            model.IdPessoaInquilino = aluguel.IdPessoaInquilino;
            model.Status = aluguel.Status;
            model.TemContrato = aluguel.ArquivoContrato != null && aluguel.ArquivoContrato.Length > 0;
            model.Inquilino = BuscarInquilino(aluguel.IdPessoaInquilino);
            return model;
        }

    }
}
