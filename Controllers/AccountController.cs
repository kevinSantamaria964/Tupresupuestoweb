using System;
using System.Data.SqlClient;
using System.Web.Mvc;
using Tupresupuestoweb.Repositories.Models;
using System.Security.Cryptography;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Configuration;

namespace Tupresupuestoweb.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Validación de longitud mínima
                if (model.Password.Length < 8)
                {
                    ModelState.AddModelError("", "La contraseña debe tener al menos 8 caracteres.");
                    return View(model);
                }

                // Validación de contraseña (solo letras y números)
                string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d]+$";
                if (!Regex.IsMatch(model.Password, passwordPattern))
                {
                    ModelState.AddModelError("", "La contraseña solo puede contener letras y números.");
                    return View(model);
                }

                // Verificar que las contraseñas coincidan
                if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError("", "Las contraseñas no coinciden.");
                    return View(model);
                }

                // Validar rol permitido (solo 2 o 3)

                if (model.IdRol != 2 && model.IdRol != 3)
                {
                    ModelState.AddModelError("", "Rol inválido seleccionado.");
                    return View(model);
                }

                // Generar hash y salt
                string salt;
                string hashedPassword = HashPasswordWithSalt(model.Password, out salt);

                string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "INSERT INTO Usuarios (Nombre, Username, Correo, Contraseña, Salt, IdRol) VALUES (@Nombre, @Username, @Correo, @Contraseña, @Salt, @IdRol)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Nombre", model.Username);
                        command.Parameters.AddWithValue("@Username", model.Username);
                        command.Parameters.AddWithValue("@Correo", model.Email);
                        command.Parameters.AddWithValue("@Contraseña", hashedPassword);
                        command.Parameters.AddWithValue("@Salt", salt);
                        command.Parameters.AddWithValue("@IdRol", model.IdRol);
                        // Usuario Básico
                       

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            try
                            {
                                var emailService = new EmailService();
                                string asunto = "Bienvenido a TuPresupuesto";
                                string cuerpo = $@"
<html>
<head>
  <style>
    body {{
      font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
      color: #333;
      background-color: #f9f9f9;
      padding: 20px;
    }}
    .container {{
      background-color: #ffffff;
      padding: 20px;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      max-width: 600px;
      margin: auto;
    }}
    .header {{
      font-size: 24px;
      font-weight: bold;
      margin-bottom: 15px;
      color: #2c3e50;
    }}
    .content {{
      font-size: 16px;
      line-height: 1.6;
    }}
    .footer {{
      margin-top: 30px;
      font-size: 14px;
      color: #888;
      border-top: 1px solid #eee;
      padding-top: 10px;
    }}
    a {{
      color: #3498db;
      text-decoration: none;
    }}
    .logo {{
      text-align: center;
      margin-bottom: 20px;
    }}
    .logo img {{
      max-width: 150px;
      height: auto;
    }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='logo'>
      <img src=""cid:LogoTuPresupuesto"" alt=""Logo TuPresupuesto"" />
    </div>
    <div class='header'>¡Bienvenido a TuPresupuesto, {model.Username}!</div>
    <div class='content'>
      <p>Gracias por registrarte en <strong>TuPresupuesto</strong>, la plataforma para gestionar tus finanzas personales de forma fácil y segura.</p>
      <p>Ya puedes iniciar sesión y comenzar a administrar tu presupuesto, ingresos y gastos para alcanzar tus metas financieras.</p>
      <p>Si tienes alguna duda o necesitas ayuda, no dudes en contactarnos.</p>
    </div>
    <div class='footer'>
      Atentamente,<br />
      El equipo de <strong>TuPresupuesto</strong><br />
      <a href='mailto:tupresupuestokas@gmail.com'>tupresupuestokas@gmail.com</a>
    </div>
  </div>
</body>
</html>";


                                emailService.EnviarCorreo(model.Email, asunto, cuerpo);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("Error al enviar correo: " + ex.Message);
                                // También podrías guardar este error en tus logs
                            }

                            return RedirectToAction("Login", "Account");
                        }
                        else
                        {
                            ModelState.AddModelError("", "Hubo un error al registrar el usuario.");
                        }
                    }
                }
            }

            return View(model);
        }

        // GET: Account/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Por favor complete todos los campos.";
                return View();
            }

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT * FROM Usuarios WHERE Username = @Username";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);

                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        string storedSalt = reader["Salt"].ToString();
                        string storedHashedPassword = reader["Contraseña"].ToString();

                        // Hashear la contraseña ingresada con el salt almacenado
                        string hashedPassword = HashPasswordWithSalt(password, storedSalt);

                        if (hashedPassword == storedHashedPassword)
                        {
                            // Si la contraseña coincide, iniciamos sesión
                            Session["IdUsuario"] = reader["Id"];
                            Session["Nombre"] = reader["Nombre"];
                            Session["Correo"] = reader["Correo"];
                            Session["Username"] = reader["Username"];
                            Session["IdRol"] = reader["IdRol"];

                            int idRol = (int)reader["IdRol"];
                            if (idRol == 1)
                                return RedirectToAction("Usuarios", "Admin");
                            else if (idRol == 2)
                                return RedirectToAction("DashboardContador", "Home");
                            else
                                return RedirectToAction("DashboardUsuario", "Home");
                        }
                        else
                        {
                            ViewBag.Error = "Usuario o contraseña incorrectos.";
                            return View();
                        }
                    }
                    else
                    {
                        ViewBag.Error = "Usuario no encontrado.";
                        return View();
                    }
                }
            }
        }

        // Método para hashear la contraseña con salt
        private string HashPasswordWithSalt(string password, out string salt)
        {
            using (var hmac = new HMACSHA512())
            {
                salt = Convert.ToBase64String(hmac.Key);
                byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
                byte[] saltedPasswordBytes = passwordBytes.Concat(Convert.FromBase64String(salt)).ToArray();
                byte[] hash = hmac.ComputeHash(saltedPasswordBytes);
                return Convert.ToBase64String(hash);
            }
        }

        // Sobrecarga para login
        private string HashPasswordWithSalt(string password, string salt)
        {
            using (var hmac = new HMACSHA512(Convert.FromBase64String(salt)))
            {
                byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
                byte[] saltedPasswordBytes = passwordBytes.Concat(Convert.FromBase64String(salt)).ToArray();
                byte[] hash = hmac.ComputeHash(saltedPasswordBytes);
                return Convert.ToBase64String(hash);
            }
        }




        // GET: Account/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();

            // También elimina cookies si estás usando FormsAuthentication
            if (Request.Cookies[".ASPXAUTH"] != null)
            {
                var cookie = new HttpCookie(".ASPXAUTH");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }

            return RedirectToAction("Login", "Account");
        }



        // GET: Account/Perfil
        public ActionResult Perfil()
        {
            if (Session["IdUsuario"] == null)
                return RedirectToAction("Login");

            int idUsuario = Convert.ToInt32(Session["IdUsuario"]);
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString;

            PerfilViewModel model = new PerfilViewModel();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Nombre, Username, Correo, IdRol FROM Usuarios WHERE Id = @IdUsuario";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdUsuario", idUsuario);

                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        model.Nombre = reader["Nombre"].ToString();
                        model.Username = reader["Username"].ToString();
                        model.Correo = reader["Correo"].ToString();
                        int idRol = Convert.ToInt32(reader["IdRol"]);
                        model.Rol = idRol == 1 ? "Administrador" :
                                    idRol == 2 ? "Contador" :
                                    "Usuario";
                    }
                    else
                    {
                        return HttpNotFound();
                    }
                }
            }

            return View(model);
        }
        // GET: User/EditarPerfil
        public ActionResult EditarPerfil()
        {
            if (Session["IdUsuario"] == null)
                return RedirectToAction("Login", "Account");

            int idUsuario = Convert.ToInt32(Session["IdUsuario"]);
            PerfilViewModel model = new PerfilViewModel();

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Nombre, Correo, Username FROM Usuarios WHERE Id = @Id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", idUsuario);
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        model.Nombre = reader["Nombre"].ToString();
                        model.Correo = reader["Correo"].ToString();
                        model.Username = reader["Username"].ToString();
                    }
                }
            }

            return View(model);
        }

        // POST: User/EditarPerfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarPerfil(PerfilViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            int idUsuario = Convert.ToInt32(Session["IdUsuario"]);
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "UPDATE Usuarios SET Nombre = @Nombre, Correo = @Correo WHERE Id = @Id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Nombre", model.Nombre);
                    command.Parameters.AddWithValue("@Correo", model.Correo);
                    command.Parameters.AddWithValue("@Id", idUsuario);

                    int rows = command.ExecuteNonQuery();
                    if (rows > 0)
                    {
                        TempData["Mensaje"] = "Perfil actualizado correctamente.";
                        return RedirectToAction("Perfil", "Account");
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo actualizar el perfil.");
                    }
                }
            }

            return View(model);
        }
        // GET: Account/ForgotPassword
        public ActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Debes ingresar un correo.");
                return View();
            }

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Correo FROM Usuarios WHERE Correo = @Correo";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Correo", email);
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        int userId = (int)reader["Id"];
                        string token = Guid.NewGuid().ToString();
                        reader.Close();

                        string insertToken = "INSERT INTO PasswordResetTokens (UserId, Token, Expiration) VALUES (@UserId, @Token, @Expiration)";
                        using (SqlCommand cmdInsert = new SqlCommand(insertToken, connection))
                        {
                            cmdInsert.Parameters.AddWithValue("@UserId", userId);
                            cmdInsert.Parameters.AddWithValue("@Token", token);
                            cmdInsert.Parameters.AddWithValue("@Expiration", DateTime.Now.AddHours(1));
                            cmdInsert.ExecuteNonQuery();
                        }

                        var emailService = new EmailService();
                        string resetLink = Url.Action("ResetPassword", "Account", new { token = token }, protocol: Request.Url.Scheme);
                        string asunto = "Restablece tu contraseña - TuPresupuesto";
                        string cuerpo = $@"
                    <p>Para restablecer tu contraseña haz clic en el siguiente enlace:</p>
                    <p><a href='{resetLink}'>Restablecer contraseña</a></p>
                    <p>Este enlace expirará en 1 hora.</p>";

                        emailService.EnviarCorreo(email, asunto, cuerpo);

                        ViewBag.Message = "Se ha enviado un correo para restablecer tu contraseña.";
                        return View();
                    }
                    else
                    {
                        ModelState.AddModelError("", "No existe un usuario con ese correo.");
                        return View();
                    }
                }
            }
        }
        // GET: Account/ResetPassword
        public ActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Token = token; // Importante que siempre tenga un valor válido
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(string token, string nuevaPassword, string confirmarPassword)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(nuevaPassword) || string.IsNullOrEmpty(confirmarPassword))
            {
                ModelState.AddModelError("", "Debes completar todos los campos.");
                ViewBag.Token = token;
                return View();
            }

            if (nuevaPassword != confirmarPassword)
            {
                ModelState.AddModelError("", "Las contraseñas no coinciden.");
                ViewBag.Token = token;
                return View();
            }

            if (nuevaPassword.Length < 8)
            {
                ModelState.AddModelError("", "La contraseña debe tener al menos 8 caracteres.");
                ViewBag.Token = token;
                return View();
            }

            string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d]+$";
            if (!Regex.IsMatch(nuevaPassword, passwordPattern))
            {
                ModelState.AddModelError("", "La contraseña debe contener letras mayúsculas, minúsculas y números.");
                ViewBag.Token = token;
                return View();
            }

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string queryToken = "SELECT UserId FROM PasswordResetTokens WHERE Token = @Token AND Expiration > @Now";
                using (SqlCommand command = new SqlCommand(queryToken, connection))
                {
                    command.Parameters.AddWithValue("@Token", token);
                    command.Parameters.AddWithValue("@Now", DateTime.Now);
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        int userId = (int)reader["UserId"];
                        reader.Close();

                        string salt;
                        string hashedPassword = HashPasswordWithSalt(nuevaPassword, out salt);

                        string updatePassword = "UPDATE Usuarios SET Contraseña = @Contraseña, Salt = @Salt WHERE Id = @UserId";
                        using (SqlCommand cmdUpdate = new SqlCommand(updatePassword, connection))
                        {
                            cmdUpdate.Parameters.AddWithValue("@Contraseña", hashedPassword);
                            cmdUpdate.Parameters.AddWithValue("@Salt", salt);
                            cmdUpdate.Parameters.AddWithValue("@UserId", userId);
                            cmdUpdate.ExecuteNonQuery();
                        }

                        string deleteToken = "DELETE FROM PasswordResetTokens WHERE Token = @Token";
                        using (SqlCommand cmdDelete = new SqlCommand(deleteToken, connection))
                        {
                            cmdDelete.Parameters.AddWithValue("@Token", token);
                            cmdDelete.ExecuteNonQuery();
                        }

                        TempData["SuccessMessage"] = "Tu contraseña ha sido restablecida correctamente.";
                        return RedirectToAction("Login", "Account");
                    }
                    else
                    {
                        ViewBag.Error = "Token inválido o expirado.";
                        return View("Error");
                    }
                }
            }
        }

    }
}
      
    








