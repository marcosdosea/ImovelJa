using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ImovelJaWeb.Controllers
{
    public class PagamentoController : Controller
    {
        // GET: PagamentoController
        public ActionResult Index()
        {
            return View();
        }

        // GET: PagamentoController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: PagamentoController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: PagamentoController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: PagamentoController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: PagamentoController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: PagamentoController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: PagamentoController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
