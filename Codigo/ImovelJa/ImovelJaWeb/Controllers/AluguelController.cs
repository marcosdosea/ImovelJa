using Core;
using Core.Service;
using ImovelJaWeb.Mappers;
using ImovelJaWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Util;

namespace ImovelJaWeb.Controllers
{
    public class AluguelController : Controller
    {
        public const string ChaveIdPessoa = "IdPessoa";

        private readonly IAluguelService aluguelService;
        private readonly IPagamentoService pagamentoService;
        private readonly IImovelService imovelService;
        private readonly IPessoaService pessoaService;

        public AluguelController(
            IAluguelService aluguelService,
            IPagamentoService pagamentoService,
            IImovelService imovelService,
            IPessoaService pessoaService)
        {
            this.aluguelService = aluguelService;
            this.pagamentoService = pagamentoService;
            this.imovelService = imovelService;
            this.pessoaService = pessoaService;
        }

        // GET: Aluguel/Create?idImovel=5
        public ActionResult Create(uint idImovel)
        {
            var imovel = imovelService.Get(idImovel);
            if (imovel == null)
                return NotFound();

            string? erro = VerificarImovel(imovel);
            if (erro != null)
            {
                TempData["Erro"] = erro;
                return RedirectToAction("Details", "Imovel", new { id = idImovel, idPessoa = imovel.IdPessoaProprietario });
            }

            PrepararPessoa(imovel.IdPessoaProprietario);

            var model = new AluguelViewModel
            {
                IdImovel = idImovel,
                Valor = imovel.Valor,
                Imovel = ImovelMapper.ToViewModel(imovel),
                Inquilino = new PessoaViewModel { Tipo = "F" }
            };
            return View(model);
        }

        // POST: Aluguel/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AluguelViewModel model)
        {
            var imovel = imovelService.Get(model.IdImovel);
            if (imovel == null)
                return NotFound();

            PrepararPessoa(imovel.IdPessoaProprietario);
            model.Imovel = ImovelMapper.ToViewModel(imovel);

            if (model.IdPessoaInquilino != 0)
                IgnorarErrosDoInquilino();
            else if (model.Inquilino != null)
            {
                model.Inquilino.Cpf = SomenteNumeros(model.Inquilino.Cpf);
                model.Inquilino.Telefone = SomenteNumeros(model.Inquilino.Telefone);
            }

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
                return RedirectToAction(nameof(Details), new { id, idPessoa = imovel.IdPessoaProprietario });
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        // GET: Aluguel/Details/5?idPessoa=2
        public ActionResult Details(uint id, uint idPessoa)
        {
            PrepararPessoa(idPessoa);

            try
            {
                var aluguel = BuscarDaPessoa(id, idPessoa);
                if (aluguel == null)
                    return NotFound();

                ViewData["Dono"] = aluguel.IdPessoaInquilino != idPessoa;

                var modelo = AluguelMapper.ToViewModel(aluguel, BuscarImovel(aluguel), BuscarInquilino(aluguel));
                modelo.Pagamentos = PagamentoMapper.ToViewModel(pagamentoService.GetByAluguel(id)).ToList();
                return View(modelo);
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(new AluguelViewModel { Id = id, IdPessoaInquilino = idPessoa });
            }
        }

        // GET: Aluguel/Contrato/5?idPessoa=2
        public ActionResult Contrato(uint id, uint idPessoa)
        {
            try
            {
                var aluguel = BuscarDaPessoa(id, idPessoa);
                if (aluguel == null)
                    return NotFound();

                if (aluguel.ArquivoContrato == null || aluguel.ArquivoContrato.Length == 0)
                    return NotFound();

                var tipo = TipoDoArquivo(aluguel.ArquivoContrato);
                var nome = tipo == "application/pdf" ? "contrato.pdf" : "contrato";
                return File(aluguel.ArquivoContrato, tipo, nome);
            }
            catch (ServiceException)
            {
                return NotFound();
            }
        }

        // GET: Aluguel/Edit/5
        public ActionResult Edit(uint id)
        {
            var aluguel = aluguelService.Get(id);
            if (aluguel == null)
                return NotFound();

            if (aluguel.Status != "A")
            {
                TempData["Erro"] = "Não é possível editar um aluguel cancelado.";
                return RedirectToAction(nameof(Details), new { id, idPessoa = DonoDo(aluguel) });
            }

            return View(MontarModelo(aluguel));
        }

        // POST: Aluguel/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(AluguelViewModel model)
        {
            var aluguel = aluguelService.Get(model.Id);
            if (aluguel == null)
                return NotFound();

            IgnorarErrosDoInquilino();

            if (aluguel.Status != "A")
                ModelState.AddModelError(string.Empty, "Não é possível editar um aluguel cancelado.");

            if (!ModelState.IsValid)
                return View(CompletarModelo(model, aluguel));

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
                return RedirectToAction(nameof(Details), new { id = aluguel.Id, idPessoa = DonoDo(aluguel) });
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(CompletarModelo(model, aluguel));
            }
        }

        // GET: Aluguel/Delete/5
        public ActionResult Delete(uint id)
        {
            var aluguel = aluguelService.Get(id);
            if (aluguel == null)
                return NotFound();

            if (aluguel.Status != "A")
            {
                TempData["Erro"] = "O aluguel já está cancelado.";
                return RedirectToAction(nameof(Details), new { id, idPessoa = DonoDo(aluguel) });
            }

            return View(MontarModelo(aluguel));
        }

        // POST: Aluguel/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(uint id)
        {
            var aluguel = aluguelService.Get(id);
            if (aluguel == null)
                return NotFound();

            if (aluguel.Status != "A")
            {
                TempData["Erro"] = "O aluguel já está cancelado.";
                return RedirectToAction(nameof(Details), new { id, idPessoa = DonoDo(aluguel) });
            }

            try
            {
                aluguelService.Delete(id);
                return RedirectToAction(nameof(Details), new { id, idPessoa = DonoDo(aluguel) });
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(MontarModelo(aluguel));
            }
        }

        /// <summary>
        /// Confere se o imóvel pode ser alugado, para o formulário nem abrir quando não pode.
        /// O AluguelService.Create confere as mesmas regras antes de gravar
        /// </summary>
        /// <param name="imovel">imóvel do aluguel</param>
        /// <returns>mensagem de erro ou nulo se o imóvel pode ser alugado</returns>
        private static string? VerificarImovel(Imovel imovel)
        {
            if (imovel.Finalidade != "A")
                return "O imóvel não é anunciado para aluguel.";

            if (imovel.StatusValidacao != "A")
                return "O anúncio do imóvel ainda não foi aprovado.";

            if (imovel.Status != "D")
                return "O imóvel não está disponível para aluguel.";

            return null;
        }

        /// <summary>
        /// Busca o aluguel quando a pessoa é o inquilino ou o dono do imóvel
        /// </summary>
        /// <param name="id">id do aluguel</param>
        /// <param name="idPessoa">id da pessoa</param>
        /// <returns>dados do aluguel ou nulo se a pessoa não for o inquilino nem o dono</returns>
        private Aluguel? BuscarDaPessoa(uint id, uint idPessoa)
        {
            return aluguelService.GetDoInquilino(id, idPessoa)
                ?? aluguelService.GetDoProprietario(id, idPessoa);
        }

        private uint DonoDo(Aluguel aluguel)
        {
            return BuscarImovel(aluguel)?.IdPessoaProprietario ?? 0;
        }

        private void IgnorarErrosDoInquilino()
        {
            var chaves = ModelState.Keys.Where(chave => chave.StartsWith("Inquilino")).ToList();
            foreach (var chave in chaves)
                ModelState.Remove(chave);
        }

        private static byte[] LerArquivo(IFormFile arquivo)
        {
            using var memoria = new MemoryStream();
            arquivo.CopyTo(memoria);
            return memoria.ToArray();
        }

        private AluguelViewModel MontarModelo(Aluguel aluguel)
        {
            var modelo = AluguelMapper.ToViewModel(aluguel, BuscarImovel(aluguel), BuscarInquilino(aluguel));
            modelo.Pagamentos = PagamentoMapper.ToViewModel(pagamentoService.GetByAluguel(aluguel.Id)).ToList();
            return modelo;
        }

        /// <summary>
        /// Tira a pontuação do CPF e do telefone digitados com máscara, como o banco guarda
        /// </summary>
        /// <param name="valor">texto digitado</param>
        /// <returns>somente os números, ou nulo se nada foi digitado</returns>
        private static string? SomenteNumeros(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : Methods.RemoveNaoNumericos(valor);
        }

        private AluguelViewModel CompletarModelo(AluguelViewModel model, Aluguel aluguel)
        {
            var inquilino = BuscarInquilino(aluguel);

            model.IdImovel = aluguel.IdImovel;
            model.IdPessoaInquilino = aluguel.IdPessoaInquilino;
            model.Status = aluguel.Status;
            model.PossuiContrato = aluguel.ArquivoContrato is { Length: > 0 };
            model.Inquilino = inquilino == null ? null : PessoaMapper.ToViewModel(inquilino);
            model.Imovel = BuscarImovel(aluguel) is Imovel imovel ? ImovelMapper.ToViewModel(imovel) : null;
            model.Pagamentos = PagamentoMapper.ToViewModel(pagamentoService.GetByAluguel(aluguel.Id)).ToList();
            return model;
        }

        private void PrepararPessoa(uint idPessoa)
        {
            ViewData[ChaveIdPessoa] = idPessoa;

            if (idPessoa == 0)
                return;

            var pessoa = pessoaService.Get(idPessoa);
            if (pessoa != null && !string.IsNullOrWhiteSpace(pessoa.Nome))
                ViewData["NomeUsuario"] = pessoa.Nome;
        }

        private Imovel? BuscarImovel(Aluguel aluguel)
        {
            if (aluguel.IdImovelNavigation != null && aluguel.IdImovelNavigation.Id != 0)
                return aluguel.IdImovelNavigation;

            return aluguel.IdImovel == 0 ? null : imovelService.Get(aluguel.IdImovel);
        }

        private Pessoa? BuscarInquilino(Aluguel aluguel)
        {
            if (aluguel.IdPessoaInquilinoNavigation != null && aluguel.IdPessoaInquilinoNavigation.Id != 0)
                return aluguel.IdPessoaInquilinoNavigation;

            return aluguel.IdPessoaInquilino == 0 ? null : pessoaService.Get(aluguel.IdPessoaInquilino);
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
