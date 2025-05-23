using System;
using System.Web.Mvc;
using Tupresupuestoweb.Repositories.Models;
using Tupresupuestoweb.Utilities;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace Tupresupuestoweb.Controllers
{
    public class UserController : Controller
    {
        // GET: Perfil
        public ActionResult Perfil()
        {
            if (Session["Username"] == null)
                return RedirectToAction("Login", "Account");

            var usuario = new PerfilViewModel
            {
                Nombre = Session["Nombre"].ToString(),
                Correo = Session["Correo"]?.ToString(),
                Username = Session["Username"].ToString()
            };

            return View(usuario);
        }

        // GET: EditarPerfil
        public ActionResult EditarPerfil()
        {
            if (Session["Username"] == null || Session["Nombre"] == null || Session["Correo"] == null)
                return RedirectToAction("Login", "Account");

            var usuario = new PerfilViewModel
            {
                Nombre = Session["Nombre"].ToString(),
                Correo = Session["Correo"]?.ToString(),
                Username = Session["Username"].ToString()
            };

            return View(usuario);
        }

        [HttpPost]
        public ActionResult EditarPerfil(PerfilViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Asegura que se use el Username correcto desde la sesión
                    string usernameSesion = Session["Username"]?.ToString();

                    // Mostrar en la consola de depuración
                    System.Diagnostics.Debug.WriteLine("Username en sesión: " + usernameSesion);
                    System.Diagnostics.Debug.WriteLine("Username en modelo: " + model.Username);

                    if (!string.IsNullOrEmpty(usernameSesion))
                    {
                        string query = "UPDATE Usuarios SET Nombre = @Nombre, Correo = @Correo WHERE Username = @Username";

                        var parameters = new[]
                        {
                    new SqlParameter("@Nombre", model.Nombre),
                    new SqlParameter("@Correo", model.Correo),
                    new SqlParameter("@Username", usernameSesion)
                };

                        DBContextUtility.ExecuteQueryWithParameters(query, parameters);

                        // Actualiza la sesión con los nuevos datos
                        Session["Nombre"] = model.Nombre;
                        Session["Correo"] = model.Correo;

                        return RedirectToAction("Perfil", "Account");
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "No se pudo encontrar el usuario en sesión.";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "Error al actualizar tu perfil. Detalle: " + ex.Message;
                }
            }

            return View(model);
        }

    }

}

