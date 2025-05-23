using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tupresupuestoweb.Repositories.Models;

namespace Tupresupuestoweb.Controllers
{
    public class CategoriasController : Controller
    {
        // GET: Categorias/Crear
        public ActionResult Crear()
        {
            return View();
        }

        // POST: Categorias/Crear
        [HttpPost]
        public ActionResult Crear(CategoriaViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Aquí más adelante guardarás en la base de datos
                TempData["Mensaje"] = "✅ Categoría creada correctamente (simulado)";
                return RedirectToAction("Crear");
            }

            return View(model);
        }
    }
}
