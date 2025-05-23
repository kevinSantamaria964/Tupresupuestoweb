using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using Tupresupuestoweb.Repositories.Models;
using Tupresupuestoweb.Filters;
using static Tupresupuestoweb.Repositories.Models.AdminDashboardViewModel;
using Tupresupuestoweb.Utilities;
using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout.Properties;
using System.IO;
using iText.Layout;
using iText.IO.Font.Constants;
using iText.Kernel.Font;



namespace Tupresupuestoweb.Controllers
{
    [CustomAuthorize(1)] // Por ejemplo, solo administradores (IdRol = 1)
    public class AdminController : Controller
    {

        public ActionResult UpdateUserPasswords()
        {
            // Llamar al método de actualización de contraseñas
            UpdateUserPasswordsInDatabase();
            return RedirectToAction("Usuarios"); // O redirigir a la vista que prefieras
        }

        private void UpdateUserPasswordsInDatabase()
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Seleccionar todos los usuarios cuya contraseña no tenga salt
                string query = "SELECT Id, Contraseña FROM Usuarios WHERE Salt IS NULL";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        int userId = (int)reader["Id"];
                        string oldHashedPassword = reader["Contraseña"].ToString();

                        // Generar un nuevo salt y hashear la contraseña con PBKDF2
                        string newSalt;
                        string newHashedPassword = HashPasswordWithSalt(oldHashedPassword, out newSalt);

                        // Actualizar la contraseña con PBKDF2 y el nuevo salt
                        string updateQuery = "UPDATE Usuarios SET Contraseña = @Contraseña, Salt = @Salt WHERE Id = @Id";
                        using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@Contraseña", newHashedPassword);
                            updateCommand.Parameters.AddWithValue("@Salt", newSalt);
                            updateCommand.Parameters.AddWithValue("@Id", userId);
                            updateCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
        private string HashPasswordWithSalt(string password, out string salt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                // Generar un salt único para cada usuario
                salt = Convert.ToBase64String(hmac.Key);

                // Hashear la contraseña con el salt
                byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
                byte[] saltedPasswordBytes = passwordBytes.Concat(Convert.FromBase64String(salt)).ToArray();
                byte[] hash = hmac.ComputeHash(saltedPasswordBytes);

                return Convert.ToBase64String(hash);
            }
        }

        private string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString;

        // Vista principal (Dashboard + Usuarios)
        public ActionResult Usuarios()
        {
            AdminDashboardViewModel viewModel = new AdminDashboardViewModel
            {
                TotalUsuarios = ObtenerTotalUsuarios(),
                TotalIngresos = ObtenerTotalIngresos(),
                CargaSistema = ObtenerCargaSistema(),
                ActividadUsuarios = ObtenerActividadUsuarios() ?? new Dictionary<string, int>(),
                ActividadesRecientes = ObtenerActividadesRecientes(),
                Usuarios = ObtenerUsuarios(),
                DistribucionRoles = ObtenerDistribucionRoles(),
                ActividadPorDia = ObtenerActividadPorDia(),
                EvolucionIngresosGastos = ObtenerEvolucionIngresosGastos(),


                // <--- Agregado aquí
            };

            return View(viewModel); // Va a Usuarios.cshtml
        }


        // POST: Actualizar rol de usuario
        [HttpPost]
        public ActionResult ActualizarRol(FormCollection collection)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                foreach (var key in collection.AllKeys)
                {
                    if (key.StartsWith("Rol_"))
                    {
                        string username = key.Substring(4);
                        string nuevoRol = collection[key];

                        int idRol;
                        switch (nuevoRol)
                        {
                            case "Administrador":
                                idRol = 1;
                                break;
                            case "Asesor Financiero":
                                idRol = 2;
                                break;
                            case "Usuario Básico":
                                idRol = 3;
                                break;
                            default:
                                idRol = 3;
                                break;
                        }

                        string query = "UPDATE Usuarios SET IdRol = @IdRol WHERE Username = @Username";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@IdRol", idRol);
                            command.Parameters.AddWithValue("@Username", username);

                            int rowsAffected = command.ExecuteNonQuery();
                            if (rowsAffected <= 0)
                            {
                                ModelState.AddModelError("", "Hubo un error al actualizar el rol del usuario.");
                            }
                        }
                    }
                }
            }

            return RedirectToAction("Usuarios");
        }

        // ========== MÉTODOS AUXILIARES ==========

        private List<PerfilViewModel> ObtenerUsuarios()
        {
            List<PerfilViewModel> usuarios = new List<PerfilViewModel>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Username, Nombre, Correo, IdRol FROM Usuarios";
                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        usuarios.Add(new PerfilViewModel
                        {
                            Username = reader["Username"].ToString(),
                            Nombre = reader["Nombre"].ToString(),
                            Correo = reader["Correo"].ToString(),
                            Rol = GetRoleName(Convert.ToInt32(reader["IdRol"]))
                        });
                    }
                }
            }

            return usuarios;
        }

        private string GetRoleName(int roleId)
        {
            switch (roleId)
            {
                case 1: return "Administrador";
                case 2: return "Asesor Financiero";
                case 3: return "Usuario Básico";
                default: return "Desconocido";
            }
        }

        private int ObtenerTotalUsuarios()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM Usuarios";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    return (int)command.ExecuteScalar();
                }
            }
        }

        private decimal ObtenerTotalIngresos()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT ISNULL(SUM(Cantidad), 0) FROM Ingresos";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    return Convert.ToDecimal(command.ExecuteScalar());
                }
            }
        }

        private string ObtenerCargaSistema()
        {
            return "24.3%"; // Valor simulado, puedes sustituirlo por lectura real si lo deseas
        }

        private Dictionary<string, int> ObtenerActividadUsuarios()
        {
            var resultado = new Dictionary<string, int>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT U.Username, 
                               ISNULL(G.CantidadGastos, 0) + ISNULL(I.CantidadIngresos, 0) AS TotalTransacciones
                        FROM Usuarios U
                        LEFT JOIN (
                            SELECT IdUsuario, COUNT(*) AS CantidadGastos
                            FROM Gastos
                            GROUP BY IdUsuario
                        ) G ON U.Id = G.IdUsuario
                        LEFT JOIN (
                            SELECT IdUsuario, COUNT(*) AS CantidadIngresos
                            FROM Ingresos
                            GROUP BY IdUsuario
                        ) I ON U.Id = I.IdUsuario";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string username = reader["Username"].ToString();
                            int total = Convert.ToInt32(reader["TotalTransacciones"]);
                            resultado[username] = total;
                        }
                    }
                }
            }
            catch
            {
                // Registrar el error si lo deseas
            }

            return resultado;
        }

        private List<string> ObtenerActividadesRecientes()
        {
            var actividades = new List<string>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
            SELECT TOP 10 *
            FROM (
                SELECT U.Username, G.Cantidad, G.Fecha, 'Gasto' AS Tipo
                FROM Gastos G
                INNER JOIN Usuarios U ON G.IdUsuario = U.Id
                UNION ALL
                SELECT U.Username, I.Cantidad, I.Fecha, 'Ingreso' AS Tipo
                FROM Ingresos I
                INNER JOIN Usuarios U ON I.IdUsuario = U.Id
            ) AS Actividades
            ORDER BY Fecha DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string username = reader["Username"].ToString();
                        decimal cantidad = Convert.ToDecimal(reader["Cantidad"]);
                        DateTime fecha = Convert.ToDateTime(reader["Fecha"]);
                        string tipo = reader["Tipo"].ToString();

                        actividades.Add($"{username} registró un {tipo.ToLower()} de ${cantidad:N2} el {fecha.ToShortDateString()}");
                    }
                }
            }

            return actividades;
        }

        private Dictionary<string, int> ObtenerDistribucionRoles()
        {
            var resultado = new Dictionary<string, int>
    {
        { "Administrador", 0 },
        { "Asesor Financiero", 0 },
        { "Usuario Básico", 0 }
    };

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT IdRol, COUNT(*) AS Cantidad FROM Usuarios GROUP BY IdRol";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int idRol = Convert.ToInt32(reader["IdRol"]);
                        int cantidad = Convert.ToInt32(reader["Cantidad"]);

                        string rol = GetRoleName(idRol);
                        if (resultado.ContainsKey(rol))
                        {
                            resultado[rol] = cantidad;
                        }
                    }
                }
            }

            return resultado;
        }

        private Dictionary<string, int> ObtenerActividadPorDia()
        {
            var actividadPorDia = new Dictionary<string, int>();
            var hoy = DateTime.Today;

            // Inicializar con 0 para los últimos 7 días
            for (int i = 6; i >= 0; i--)
            {
                var dia = hoy.AddDays(-i).ToString("dd MMM");
                actividadPorDia[dia] = 0;
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = @"
            SELECT CONVERT(date, Fecha) AS Dia, COUNT(*) AS Total
            FROM (
                SELECT Fecha FROM Ingresos
                UNION ALL
                SELECT Fecha FROM Gastos
            ) AS Transacciones
            WHERE Fecha >= @FechaInicio
            GROUP BY CONVERT(date, Fecha)
            ORDER BY Dia;
        ";

                var fechaInicio = hoy.AddDays(-6);

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FechaInicio", fechaInicio);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var dia = Convert.ToDateTime(reader["Dia"]).ToString("dd MMM");
                            var total = Convert.ToInt32(reader["Total"]);
                            if (actividadPorDia.ContainsKey(dia))
                                actividadPorDia[dia] = total;
                        }
                    }
                }
            }

            return actividadPorDia;
        }
        private List<EvolucionFinanciera> ObtenerEvolucionIngresosGastos()
        {
            var resultado = new List<EvolucionFinanciera>();
            var connectionString = ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString;

            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    var query = @"
                SELECT 
                    CONVERT(date, Fecha) AS Fecha,
                    SUM(CASE WHEN Tipo = 'Ingreso' THEN Cantidad ELSE 0 END) AS TotalIngresos,
                    SUM(CASE WHEN Tipo = 'Gasto' THEN Cantidad ELSE 0 END) AS TotalGastos
                FROM (
                    SELECT Fecha, Cantidad, 'Ingreso' AS Tipo FROM Ingresos
                    UNION ALL
                    SELECT Fecha, Cantidad, 'Gasto' AS Tipo FROM Gastos
                ) AS TransaccionesUnificadas
                GROUP BY CONVERT(date, Fecha)
                ORDER BY CONVERT(date, Fecha);
            ";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var fecha = reader["Fecha"] != DBNull.Value ? Convert.ToDateTime(reader["Fecha"]) : DateTime.MinValue;
                            var ingresos = reader["TotalIngresos"] != DBNull.Value ? Convert.ToDecimal(reader["TotalIngresos"]) : 0;
                            var gastos = reader["TotalGastos"] != DBNull.Value ? Convert.ToDecimal(reader["TotalGastos"]) : 0;

                            resultado.Add(new EvolucionFinanciera
                            {
                                Fecha = fecha.ToString("dd MMM"),
                                TotalIngresos = ingresos,
                                TotalGastos = gastos
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al obtener evolución de ingresos y gastos: " + ex.ToString());

                }
            }

            return resultado;
        }
        [HttpPost]
        public ActionResult ActualizarUsuarios(FormCollection form)
        {
            string accion = form["accion"];
            if (string.IsNullOrEmpty(accion))
                return RedirectToAction("Usuarios");

            string[] partes = accion.Split('_');
            if (partes.Length != 2)
                return RedirectToAction("Usuarios");

            string tipoAccion = partes[0];
            string username = partes[1];

            int idUsuario = ObtenerIdPorUsername(username);
            if (idUsuario == 0)
                return RedirectToAction("Usuarios");

            switch (tipoAccion)
            {
                case "guardar":
                    string nuevoRol = form["Rol_" + username];
                    ActualizarRolUsuario(idUsuario, nuevoRol);
                    break;
                case "activar":
                    ActivarUsuario(idUsuario);
                    break;
                case "desactivar":
                    DesactivarUsuario(idUsuario);
                    break;
                case "eliminar":
                    EliminarUsuario(idUsuario);
                    break;
            }

            return RedirectToAction("Usuarios");
        }

        private int ObtenerIdPorUsername(string username)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT Id FROM Usuarios WHERE Username = @Username";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);
                connection.Open();
                var result = command.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        private void ActualizarRolUsuario(int id, string nuevoRol)
        {
            var roles = new Dictionary<string, int>
    {
        { "Usuario Básico", 3 },
        { "Asesor Financiero", 2 },
        { "Administrador", 1 }
    };

            if (!roles.TryGetValue(nuevoRol, out int idRol))
            {
                throw new ArgumentException("Rol inválido: " + nuevoRol);
            }

            using (var connection = new SqlConnection(connectionString))
            {
                var query = "UPDATE Usuarios SET IdRol = @IdRol WHERE Id = @Id";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@IdRol", idRol);
                command.Parameters.AddWithValue("@Id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }



        [HttpPost]
        public ActionResult ActivarUsuario(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = "UPDATE Usuarios SET Activo = 1 WHERE Id = @Id";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
            return RedirectToAction("Usuarios");
        }

        [HttpPost]
        public ActionResult DesactivarUsuario(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = "UPDATE Usuarios SET Activo = 0 WHERE Id = @Id";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
            return RedirectToAction("Usuarios");
        }

        [HttpPost]
        public ActionResult EliminarUsuario(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = "DELETE FROM Usuarios WHERE Id = @Id";
                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
            return RedirectToAction("Usuarios");
        }

        [HttpGet]
        public ActionResult ExportarUsuariosPdf(int[] rolesSeleccionados)
        {
            try
            {
                if (rolesSeleccionados == null || rolesSeleccionados.Length == 0)
                {
                    return new HttpStatusCodeResult(400, "Debe seleccionar al menos un rol.");
                }

                var usuarios = new List<UsuarioExportadoViewModel>();

                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString))
                {
                    connection.Open();

                    // Construir una lista segura de parámetros para SQL IN (usar parámetros para evitar SQL Injection)
                    var parametros = new List<string>();
                    var command = new SqlCommand();
                    command.Connection = connection;

                    for (int i = 0; i < rolesSeleccionados.Length; i++)
                    {
                        string paramName = "@rol" + i;
                        parametros.Add(paramName);
                        command.Parameters.AddWithValue(paramName, rolesSeleccionados[i]);
                    }

                    var query = $@"SELECT 
                            u.Id, 
                            u.Nombre, 
                            u.Username, 
                            u.Correo, 
                            r.NombreRol AS Rol, 
                            u.Activo
                          FROM 
                            Usuarios u
                          INNER JOIN 
                            Roles r ON u.IdRol = r.Id
                          WHERE u.IdRol IN ({string.Join(",", parametros)})";

                    command.CommandText = query;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            usuarios.Add(new UsuarioExportadoViewModel
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Nombre = reader["Nombre"].ToString(),
                                Username = reader["Username"].ToString(),
                                Correo = reader["Correo"].ToString(),
                                Rol = reader["Rol"].ToString(),
                                Activo = Convert.ToBoolean(reader["Activo"])
                            });
                        }
                    }
                }

                // ... Generación PDF igual que antes (no cambio) ...

                // Retornar el PDF, por ejemplo:
                using (var memoryStream = new MemoryStream())
                {
                    var writer = new PdfWriter(memoryStream);
                    var pdf = new PdfDocument(writer);
                    var doc = new Document(pdf);

                    // Añadir título, fecha, tabla... (igual que tu código anterior)

                    // Por ejemplo tabla:
                    Table table = new Table(6, false);
                    string[] encabezados = { "ID", "Nombre", "Username", "Correo", "Rol", "Activo" };
                    PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                    PdfFont regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                    foreach (var h in encabezados)
                    {
                        table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(boldFont)));
                    }

                    foreach (var u in usuarios)
                    {
                        table.AddCell(new Paragraph(u.Id.ToString()).SetFont(regularFont));
                        table.AddCell(new Paragraph(u.Nombre).SetFont(regularFont));
                        table.AddCell(new Paragraph(u.Username).SetFont(regularFont));
                        table.AddCell(new Paragraph(u.Correo).SetFont(regularFont));
                        table.AddCell(new Paragraph(u.Rol).SetFont(regularFont));
                        table.AddCell(new Paragraph(u.Activo ? "Sí" : "No").SetFont(regularFont));
                    }

                    doc.Add(table);
                    doc.Close();

                    return File(memoryStream.ToArray(), "application/pdf", "Usuarios.pdf");
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, "Error generando PDF: " + ex.Message);
            }
        }
    }
}








