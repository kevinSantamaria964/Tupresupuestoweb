using System;
using System.Data.SqlClient;
using System.Web.Mvc;
using Tupresupuestoweb.Utilities; // Asegúrate de que esta línea esté correcta

namespace Tupresupuestoweb.Controllers
{
    public class TestController : Controller
    {
        public ActionResult Index()
        {
            string mensaje = "";
            try
            {
                // Obtiene la conexión usando el método GetConnection de DBContextUtility
                using (SqlConnection connection = DBContextUtility.GetConnection())
                {
                    // Intenta abrir la conexión
                    connection.Open();
                    mensaje = "Conexión exitosa a la base de datos.";
                }
            }
            catch (Exception ex)
            {
                // Si hay un error, captura y muestra el mensaje
                mensaje = "Error al conectar a la base de datos: " + ex.Message;
            }

            // Devuelve la vista con el mensaje de prueba
            ViewBag.Mensaje = mensaje;
            return View();
        }
    }
}


