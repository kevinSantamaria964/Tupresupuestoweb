using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;
using Tupresupuestoweb.Repositories.Models;
using Tupresupuestoweb.Utilities;

namespace Tupresupuestoweb.Controllers
{
    public class ChatController : Controller
    {
        // Verifica que haya usuario autenticado antes de ejecutar cualquier acción
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["IdUsuario"] == null)
            {
                filterContext.Result = RedirectToAction("Login", "Account");
                return;
            }
            base.OnActionExecuting(filterContext);
        }

        // Obtiene el Id del usuario logueado desde la sesión, lanza excepción si no está autenticado
        private int ObtenerUsuarioLogueadoId()
        {
            if (Session["IdUsuario"] == null)
                throw new UnauthorizedAccessException("Usuario no autenticado");

            return (int)Session["IdUsuario"];
        }

        // Acción para listar todos los asesores financieros (rol 2)
        public ActionResult ListaAsesores()
        {
            var asesores = new List<Usuario>();

            string query = "SELECT Id, Nombre FROM Usuarios WHERE IdRol = 2";

            using (var connection = DBContextUtility.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            asesores.Add(new Usuario
                            {
                                Id = reader.GetInt32(0),
                                Nombre = reader.GetString(1)
                            });
                        }
                    }
                }
            }

            return View(asesores);
        }

        // Acción para que el asesor financiero vea las conversaciones que tiene (usuarios con quienes ha chateado)
        public ActionResult MisConversaciones()
        {
            int asesorId = ObtenerUsuarioLogueadoId();
            var usuariosConversacion = new List<Usuario>();

            string query = @"
                SELECT DISTINCT u.Id, u.Nombre
                FROM Usuarios u
                INNER JOIN Mensajes m ON (u.Id = m.id_emisor OR u.Id = m.id_receptor)
                WHERE (m.id_emisor = @AsesorId OR m.id_receptor = @AsesorId)
                AND u.Id != @AsesorId";

            using (var connection = DBContextUtility.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AsesorId", asesorId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            usuariosConversacion.Add(new Usuario
                            {
                                Id = reader.GetInt32(0),
                                Nombre = reader.GetString(1)
                            });
                        }
                    }
                }
            }

            return View(usuariosConversacion);
        }

        // Acción para mostrar la conversación entre el usuario logueado y otro usuario (receptor)
        public ActionResult Conversacion(int? id)
        {
            if (id == null)
            {
                // Redirige a lista de asesores si no se especifica un interlocutor
                return RedirectToAction("ListaAsesores");
            }

            int usuarioId = ObtenerUsuarioLogueadoId();
            var mensajes = new List<MensajeChat>();

            string query = @"
                SELECT id, id_emisor, id_receptor, contenido, fecha_envio, leido
                FROM Mensajes
                WHERE (id_emisor = @UsuarioId AND id_receptor = @Id) OR (id_emisor = @Id AND id_receptor = @UsuarioId)
                ORDER BY fecha_envio";

            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@UsuarioId", usuarioId),
                new SqlParameter("@Id", id.Value)
            };

            using (var connection = DBContextUtility.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddRange(parameters.ToArray());
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            mensajes.Add(new MensajeChat
                            {
                                Id = reader.GetInt32(0),
                                IdEmisor = reader.GetInt32(1),
                                IdReceptor = reader.GetInt32(2),
                                Contenido = reader.GetString(3),
                                FechaEnvio = reader.GetDateTime(4),
                                Leido = reader.GetBoolean(5)
                            });
                        }
                    }
                }
            }

            ViewBag.ReceptorId = id.Value;
            return View(mensajes);
        }

        // Acción para enviar mensaje al receptor
        [HttpPost]
        public ActionResult EnviarMensaje(int receptorId, string contenido)
        {
            int emisorId = ObtenerUsuarioLogueadoId();

            if (string.IsNullOrWhiteSpace(contenido))
            {
                TempData["Error"] = "El mensaje no puede estar vacío.";
                return RedirectToAction("Conversacion", new { id = receptorId });
            }

            string insertQuery = @"
                INSERT INTO Mensajes (id_emisor, id_receptor, contenido, fecha_envio, leido)
                VALUES (@IdEmisor, @IdReceptor, @Contenido, @FechaEnvio, 0)";

            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@IdEmisor", emisorId),
                new SqlParameter("@IdReceptor", receptorId),
                new SqlParameter("@Contenido", contenido),
                new SqlParameter("@FechaEnvio", DateTime.Now)
            };

            DBContextUtility.ExecuteQueryWithParameters(insertQuery, parameters);

            return RedirectToAction("Conversacion", new { id = receptorId });
        }
    }
}



