using Core.Service;
using ImovelJaWeb.Mappers;
using Microsoft.AspNetCore.Mvc;
using Service;

namespace ImovelJaWeb.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly IPessoaService pessoaService;
        private readonly IImovelService imovelService;

        public UsuarioController(IPessoaService pessoaService, IImovelService imovelService)
        {
            this.pessoaService = pessoaService;
            this.imovelService = imovelService;
        }
        // GET: PessoaController/GetAll
        public ActionResult Index()
        {
            var pessoas = pessoaService.GetUsuario();
            var pessoasViewModel = PessoaMapper.ToViewModel(pessoas);
            return View(pessoasViewModel);
        }

        // GET: PessoaController/Details/5
        public ActionResult Details(uint id)
        {
            var pessoa = pessoaService.Get(id);
            if(pessoa == null)
            {
                return NotFound();
            }
            ViewBag.imoveis = imovelService.GetByProprietario(id)
                .Select(ImovelMapper.ToViewModel)
                .ToList();
            
            return View(PessoaMapper.ToViewModel(pessoa));
        }

        // POST: UsuarioController/Block/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Activate(uint id, bool ativo)
        {
            try
            {
                pessoaService.Activate(id, ativo);
                TempData["Mensagem"] = ativo ? "Usuário ativado com sucesso." : "Usuário bloqueado com sucesso.";
            }
            catch (ServiceException ex)
            {
                TempData["Erro"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
