using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Tupresupuestoweb.Controllers
{
    public class HomeController : Controller
    {
        // Página inicial: redirige al Dashboard principal
        public ActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        // Página "Acerca de"
        public ActionResult About()
        {
            ViewBag.Message = "Información sobre la aplicación TuPresupuesto.";
            return View();
        }

        // Página de contacto
        public ActionResult Contact()
        {
            ViewBag.Message = "¿Tienes preguntas? Contáctanos.";
            return View();
        }

        // ✅ Dashboard financiero general
        public ActionResult Dashboard()
        {
            return View();
        }

        // Reporte mensual del usuario
        public ActionResult ReporteMensual()
        {
            return View();
        }

        // Categorías de gastos e ingresos
        public ActionResult Categorias()
        {
            return View();
        }

        // Página de ayuda
        public ActionResult Ayuda()
        {
            return View();
        }

        // ✅ Dashboard del Administrador (Gestión de Usuarios)
        public ActionResult Usuarios()
        {
            if (Session["IdRol"] == null || (int)Session["IdRol"] != 1) // Verificar si el IdRol es 1 (Administrador)
                return RedirectToAction("Login", "Account"); // Redirige si no es admin

            return View(); // Vista de gestión de usuarios
        }
        public ActionResult Testimonios()
        {
            return View();
        }


        // ✅ Dashboard del Contador
        public ActionResult DashboardContador()
        {
            if (Session["IdRol"] == null || (int)Session["IdRol"] != 2) // Verificar si el IdRol es 2 (Contador)
                return RedirectToAction("Login", "Account"); // Redirige si no es contador

            return View(); // Vista del Dashboard del contador
        }

        // ✅ Dashboard del Usuario Básico
        public ActionResult DashboardUsuario()
        {
            if (Session["IdRol"] == null || (int)Session["IdRol"] != 3) // Verificar si el IdRol es 3 (Usuario Básico)
                return RedirectToAction("Login", "Account"); // Redirige si no es usuario básico

            return View(); // Vista del Dashboard del usuario básico
        }
    }
}
