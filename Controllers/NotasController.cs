using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Web.Mvc;
using Tupresupuestoweb.Repositories.Models;

namespace Tupresupuestoweb.Controllers
{
    public class NotasController : Controller
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString;
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["IdUsuario"] == null)
            {
                filterContext.Result = RedirectToAction("Login", "Account");
                return;
            }

            base.OnActionExecuting(filterContext);
        }
        private int ObtenerUsuarioAutenticado()
        {
            if (Session["IdUsuario"] == null)
            {
                // Aquí podrías redirigir a login o mostrar error
                throw new UnauthorizedAccessException("Usuario no autenticado");
            }
            return (int)Session["IdUsuario"];
        }

        // Mostrar notas del usuario autenticado
        public ActionResult NotasUsuario()
        {
            int idUsuario = ObtenerUsuarioAutenticado();

            var notas = new List<Nota>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM Notas WHERE IdUsuario = @IdUsuario ORDER BY FechaCreacion DESC";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            notas.Add(new Nota
                            {
                                Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                IdUsuario = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                                Contenido = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                FechaCreacion = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3)
                            });
                        }
                    }
                }
            }

            ViewBag.IdUsuario = idUsuario; // opcional, para la vista
            return View(notas);
        }

        [HttpPost]
        public ActionResult Insertar(string contenido)
        {
            int idUsuario = ObtenerUsuarioAutenticado();

            if (string.IsNullOrWhiteSpace(contenido))
            {
                TempData["Error"] = "El contenido no puede estar vacío.";
                return RedirectToAction("NotasUsuario");
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("InsertarNota", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    command.Parameters.AddWithValue("@Contenido", contenido);
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction("NotasUsuario");
        }

        [HttpPost]
        public ActionResult Eliminar(int idNota)
        {
            int idUsuario = ObtenerUsuarioAutenticado();

            // Validar que la nota pertenezca al usuario
            if (!NotaPerteneceAlUsuario(idNota, idUsuario))
            {
                return new HttpStatusCodeResult(403); // Prohibido
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("EliminarNota", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@IdNota", idNota);
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction("NotasUsuario");
        }

        [HttpPost]
        public ActionResult Actualizar(int idNota, string contenido)
        {
            int idUsuario = ObtenerUsuarioAutenticado();

            if (string.IsNullOrWhiteSpace(contenido))
            {
                TempData["Error"] = "El contenido no puede estar vacío.";
                return RedirectToAction("NotasUsuario");
            }

            // Validar que la nota pertenezca al usuario
            if (!NotaPerteneceAlUsuario(idNota, idUsuario))
            {
                return new HttpStatusCodeResult(403); // Prohibido
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("ActualizarNota", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@IdNota", idNota);
                    command.Parameters.AddWithValue("@Contenido", contenido);
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction("NotasUsuario");
        }

        // Método para validar que la nota pertenece al usuario autenticado
        private bool NotaPerteneceAlUsuario(int idNota, int idUsuario)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT COUNT(*) FROM Notas WHERE Id = @IdNota AND IdUsuario = @IdUsuario";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdNota", idNota);
                    command.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }
    }
}

