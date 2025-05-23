using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Image;
using iText.Layout.Properties;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Colors;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using Tupresupuestoweb.Models;
using Tupresupuestoweb.Repositories;
using Tupresupuestoweb.Repositories.Models;
using System.IO;
using Tupresupuestoweb.Filters;
using Tupresupuestoweb.Utilities;
using System.Web;




[CustomAuthorize(2)] // Solo asesores (IdRol = 2)




public class AsesorController : Controller
{
    private readonly NotificacionesRepository _notificacionesRepository;

    public AsesorController()
    {
        _notificacionesRepository = new NotificacionesRepository();
    }

    // GET: Asesor/Buscar
    public ActionResult Buscar()
    {
        return View("~/Views/Home/DashboardContador.cshtml");
    }

    // POST: Asesor/Buscar
    [HttpPost]
    public ActionResult Buscar(int idUsuario)
    {
        var notificaciones = _notificacionesRepository.ObtenerNotificaciones(idUsuario);
        ViewBag.IdUsuario = idUsuario;
        ViewBag.Notificaciones = notificaciones;
        return View("~/Views/Home/DashboardContador.cshtml");
    }

    public ActionResult EstadisticasUsuario(int id, DateTime? desde = null, DateTime? hasta = null, string rango = null)
    {
        if (!string.IsNullOrEmpty(rango))
        {
            DateTime hoy = DateTime.Today;
            switch (rango)
            {
                case "hoy":
                    desde = hoy;
                    hasta = hoy;
                    break;
                case "semana":
                    desde = hoy.AddDays(-7);
                    hasta = hoy;
                    break;
                case "mes":
                    desde = hoy.AddMonths(-1);
                    hasta = hoy;
                    break;
                case "anio":
                    desde = new DateTime(hoy.Year, 1, 1);
                    hasta = hoy;
                    break;
            }
        }

        var estadisticas = ObtenerEstadisticasUsuario(id, desde, hasta);
        ViewBag.IdUsuario = id;
        return View(estadisticas);
    }





    private EstadisticasUsuarioViewModel ObtenerEstadisticasUsuario(int idUsuario, DateTime? desde, DateTime? hasta)
    {
        var modelo = new EstadisticasUsuarioViewModel();
        string connectionString = ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString;

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            string filtroFecha = "";
            if (desde.HasValue && hasta.HasValue)
            {
                filtroFecha = " AND Fecha >= @Desde AND Fecha <= @Hasta";
            }

            // Ingresos por categoría
            using (SqlCommand cmd = new SqlCommand($@"
            SELECT Categoria, SUM(Cantidad) as Total 
            FROM Ingresos 
            WHERE IdUsuario = @Id {filtroFecha} 
            GROUP BY Categoria", connection))
            {
                cmd.Parameters.AddWithValue("@Id", idUsuario);
                if (desde.HasValue && hasta.HasValue)
                {
                    cmd.Parameters.AddWithValue("@Desde", desde.Value);
                    cmd.Parameters.AddWithValue("@Hasta", hasta.Value);
                }

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        modelo.CategoriasIngresos.Add(reader["Categoria"].ToString());
                        modelo.TotalesIngresos.Add(Convert.ToDecimal(reader["Total"]));
                    }
                }
            }

            // Gastos por categoría
            using (SqlCommand cmd = new SqlCommand($@"
            SELECT Categoria, SUM(Cantidad) as Total 
            FROM Gastos 
            WHERE IdUsuario = @Id {filtroFecha} 
            GROUP BY Categoria", connection))
            {
                cmd.Parameters.AddWithValue("@Id", idUsuario);
                if (desde.HasValue && hasta.HasValue)
                {
                    cmd.Parameters.AddWithValue("@Desde", desde.Value);
                    cmd.Parameters.AddWithValue("@Hasta", hasta.Value);
                }

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        modelo.CategoriasGastos.Add(reader["Categoria"].ToString());
                        modelo.TotalesGastos.Add(Convert.ToDecimal(reader["Total"]));
                    }
                }
            }

            // Ingresos mensuales
            using (SqlCommand cmd = new SqlCommand($@"
            SELECT FORMAT(Fecha, 'yyyy-MM') AS Mes, SUM(Cantidad) AS Total 
            FROM Ingresos 
            WHERE IdUsuario = @Id {filtroFecha}
            GROUP BY FORMAT(Fecha, 'yyyy-MM') 
            ORDER BY Mes", connection))
            {
                cmd.Parameters.AddWithValue("@Id", idUsuario);
                if (desde.HasValue && hasta.HasValue)
                {
                    cmd.Parameters.AddWithValue("@Desde", desde.Value);
                    cmd.Parameters.AddWithValue("@Hasta", hasta.Value);
                }

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        modelo.Meses.Add(reader["Mes"].ToString());
                        modelo.IngresosMensuales.Add(Convert.ToDecimal(reader["Total"]));
                    }
                }
            }

            // Gastos mensuales
            using (SqlCommand cmd = new SqlCommand($@"
            SELECT FORMAT(Fecha, 'yyyy-MM') AS Mes, SUM(Cantidad) AS Total 
            FROM Gastos 
            WHERE IdUsuario = @Id {filtroFecha}
            GROUP BY FORMAT(Fecha, 'yyyy-MM') 
            ORDER BY Mes", connection))
            {
                cmd.Parameters.AddWithValue("@Id", idUsuario);
                if (desde.HasValue && hasta.HasValue)
                {
                    cmd.Parameters.AddWithValue("@Desde", desde.Value);
                    cmd.Parameters.AddWithValue("@Hasta", hasta.Value);
                }

                using (var reader = cmd.ExecuteReader())
                {
                    var gastosPorMes = new Dictionary<string, decimal>();
                    while (reader.Read())
                    {
                        gastosPorMes[reader["Mes"].ToString()] = Convert.ToDecimal(reader["Total"]);
                    }

                    foreach (var mes in modelo.Meses)
                    {
                        modelo.GastosMensuales.Add(gastosPorMes.ContainsKey(mes) ? gastosPorMes[mes] : 0);
                    }
                }
            }
            // Transacciones por categoría y tipo (Ingreso/Gasto)
            using (SqlCommand cmd = new SqlCommand(@"
    SELECT Categoria, 'Ingreso' AS Tipo, COUNT(*) AS Total
    FROM Ingresos
    WHERE IdUsuario = @Id
    GROUP BY Categoria

    UNION ALL

    SELECT Categoria, 'Gasto' AS Tipo, COUNT(*) AS Total
    FROM Gastos
    WHERE IdUsuario = @Id
    GROUP BY Categoria", connection))
            {
                cmd.Parameters.AddWithValue("@Id", idUsuario);

                var categorias = new HashSet<string>();
                var ingresosDict = new Dictionary<string, int>();
                var gastosDict = new Dictionary<string, int>();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string categoria = reader["Categoria"].ToString();
                        string tipo = reader["Tipo"].ToString();
                        int total = Convert.ToInt32(reader["Total"]);

                        categorias.Add(categoria);

                        if (tipo == "Ingreso")
                            ingresosDict[categoria] = total;
                        else if (tipo == "Gasto")
                            gastosDict[categoria] = total;
                    }
                }

                foreach (var categoria in categorias.OrderBy(c => c))
                {
                    modelo.CategoriasFrecuentes.Add(categoria);
                    modelo.CantidadesIngresosPorCategoria.Add(ingresosDict.ContainsKey(categoria) ? ingresosDict[categoria] : 0);
                    modelo.CantidadesGastosPorCategoria.Add(gastosDict.ContainsKey(categoria) ? gastosDict[categoria] : 0);
                }
            }



            // Cálculo de saldo mensual y acumulado
            for (int i = 0; i < modelo.Meses.Count; i++)
            {
                decimal ingresos = modelo.IngresosMensuales[i];
                decimal gastos = i < modelo.GastosMensuales.Count ? modelo.GastosMensuales[i] : 0;
                modelo.SaldoMensual.Add(ingresos - gastos);
            }

            decimal acumulado = 0;
            for (int i = 0; i < modelo.Meses.Count; i++)
            {
                decimal saldo = modelo.IngresosMensuales[i] - modelo.GastosMensuales[i];
                acumulado += saldo;
                modelo.SaldoAcumulado.Add(acumulado);
            }

            // Calcular porcentaje de ahorro mensual
            for (int i = 0; i < modelo.Meses.Count; i++)
            {
                decimal ingreso = modelo.IngresosMensuales[i];
                decimal gasto = modelo.GastosMensuales[i];
                decimal ahorro = ingreso - gasto;

                decimal porcentaje = ingreso > 0 ? (ahorro / ingreso) * 100 : 0;
                modelo.PorcentajeAhorroMensual.Add(Math.Round(porcentaje, 2));
            }





            return modelo;

        }
    }
    public ActionResult VerUsuariosAsignados()
    {
        // Obtener el ID del asesor desde la sesión
        int idAsesor = (int)Session["IdUsuario"];

        // Obtener los usuarios asignados a este asesor
        var usuariosAsignados = ObtenerUsuariosAsignados(idAsesor);

        // Verifica que los usuarios se están recuperando correctamente
        if (usuariosAsignados == null || !usuariosAsignados.Any())
        {
            Debug.WriteLine("No se encontraron usuarios asignados.");
        }

        return View(usuariosAsignados);  // Pasa la lista de usuarios a la vista
    }


    private List<Usuario> ObtenerUsuariosAsignados(int idAsesor)
    {
        List<Usuario> usuariosAsignados = new List<Usuario>();

        // Conexión a la base de datos
        using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString))
        {
            conn.Open();

            // Consulta para obtener usuarios asignados a un asesor
            string query = @"
        SELECT u.Id, u.Nombre, u.Username, u.Correo, u.IdRol
        FROM Usuarios u
        INNER JOIN UsuariosAsesorados ua ON u.Id = ua.IdUsuario
        WHERE ua.IdAsesor = @IdAsesor";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                // Agregar el parámetro para el ID del asesor
                cmd.Parameters.AddWithValue("@IdAsesor", idAsesor);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    // Leer los resultados y agregar los usuarios a la lista
                    while (reader.Read())
                    {
                        usuariosAsignados.Add(new Usuario
                        {
                            Id = (int)reader["Id"],
                            Nombre = reader["Nombre"].ToString(),
                            Username = reader["Username"].ToString(),
                            Correo = reader["Correo"].ToString(),
                            IdRol = (int)reader["IdRol"]
                        });
                    }
                }
            }
        }

        return usuariosAsignados;  // Devolver la lista de usuarios
    }
    [HttpGet]
    public ActionResult GenerarReporte()
    {
        return View();
    }

    [HttpPost]
    public ActionResult GenerarReporte(ReporteMensajeViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        // Nombre y ruta del archivo
        var nombreArchivo = $"Reporte_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var rutaCarpeta = Server.MapPath("~/Content/Reportes");
        var rutaCompleta = Path.Combine(rutaCarpeta, nombreArchivo);

        if (!Directory.Exists(rutaCarpeta))
            Directory.CreateDirectory(rutaCarpeta);

        // Crear el PDF
        using (var writer = new PdfWriter(rutaCompleta))
        using (var pdf = new PdfDocument(writer))
        using (var doc = new Document(pdf))
        {
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Logo
            string rutaLogo = Server.MapPath("~/Content/Images/tupresupuestoinicio.png");
            if (System.IO.File.Exists(rutaLogo))
            {
                var logoData = ImageDataFactory.Create(rutaLogo);
                var logo = new Image(logoData)
                    .SetWidth(120)
                    .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                    .SetMarginBottom(10);
                doc.Add(logo);
            }

            // Título y contenido
            var titulo = new Paragraph("Reporte Personalizado")
                .SetFontSize(20)
                .SetFont(fontBold)
                .SetTextAlignment(TextAlignment.CENTER);
            doc.Add(titulo);

            doc.Add(new Paragraph($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}")
                .SetFontSize(10)
                .SetFont(fontRegular)
                .SetTextAlignment(TextAlignment.RIGHT));

            doc.Add(new Paragraph(" "));

            doc.Add(new Paragraph("Título:").SetFontSize(12).SetFont(fontBold));
            doc.Add(new Paragraph(modelo.Titulo).SetFont(fontRegular));

            doc.Add(new Paragraph("Mensaje:").SetFontSize(12).SetFont(fontBold));
            doc.Add(new Paragraph(modelo.Mensaje).SetFont(fontRegular));

            if (modelo.ImagenesAdjuntas != null && modelo.ImagenesAdjuntas.Count > 0)
            {
                foreach (var imagen in modelo.ImagenesAdjuntas)
                {
                    if (imagen != null && imagen.ContentLength > 0)
                    {
                        var imagenBytes = new byte[imagen.ContentLength];
                        imagen.InputStream.Read(imagenBytes, 0, imagen.ContentLength);

                        var imageData = ImageDataFactory.Create(imagenBytes);
                        var image = new Image(imageData)
                            .SetMaxHeight(300)
                            .SetAutoScale(true)
                            .SetHorizontalAlignment(HorizontalAlignment.CENTER);

                        doc.Add(image);
                    }
                }
            }
        }

        // ✅ Insertar en la base de datos
        try
        {
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString))
            {
                connection.Open();
                string query = @"INSERT INTO ReportesEnviados (IdUsuario, Titulo, Mensaje, RutaArchivo)
                             VALUES (@IdUsuario, @Titulo, @Mensaje, @RutaArchivo)";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@IdUsuario", modelo.UsuarioId);
                    // asegúrate de tener este campo en el ViewModel
                    cmd.Parameters.AddWithValue("@Titulo", modelo.Titulo);
                    cmd.Parameters.AddWithValue("@Mensaje", modelo.Mensaje);
                    cmd.Parameters.AddWithValue("@RutaArchivo", "/Content/Reportes/" + nombreArchivo);

                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al guardar en la base de datos: " + ex.Message;
            return View(modelo);
        }

        TempData["ArchivoGenerado"] = nombreArchivo;
        TempData["Mensaje"] = "Reporte generado correctamente: " + nombreArchivo;

        return RedirectToAction("GenerarReporte");
    }




    
    public ActionResult DescargarReporte(string nombreArchivo)
    {
        // Ruta del archivo generado
        var rutaCarpeta = Server.MapPath("~/Content/Reportes");
        var rutaCompleta = Path.Combine(rutaCarpeta, nombreArchivo);

        // Verificar si el archivo existe
        if (System.IO.File.Exists(rutaCompleta))
        {
            return File(rutaCompleta, "application/pdf", nombreArchivo);
        }

        // Si no existe, mostrar mensaje de error
        TempData["Mensaje"] = "El archivo no se encuentra disponible.";
        return RedirectToAction("GenerarReporte");
    }
    [HttpPost]
    public ActionResult GenerarReporteGraficas(EstadisticasUsuarioViewModel model, List<string> imagenes)
    {
        try
        {
            if (imagenes == null || imagenes.Count == 0)
            {
                return new HttpStatusCodeResult(400, "No se han recibido imágenes.");
            }

            using (var memoryStream = new MemoryStream())
            {
                var writer = new PdfWriter(memoryStream);
                var pdfDocument = new PdfDocument(writer);
                var document = new Document(pdfDocument);

                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                // Cargar logo
                string rutaLogo = Server.MapPath("~/Content/Images/tupresupuestoinicio.png");
                if (System.IO.File.Exists(rutaLogo))
                {
                    var logoData = ImageDataFactory.Create(rutaLogo);
                    var logo = new Image(logoData)
                        .SetWidth(120) // ancho ajustable
                        .SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER)
                        .SetMarginBottom(10);
                    document.Add(logo);
                }

                document.Add(new Paragraph("📊 Reporte Financiero Detallado")
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetFontSize(24)
                    .SetFontColor(iText.Kernel.Colors.ColorConstants.BLACK)
                    .SetFont(boldFont)
                    .SetMarginBottom(10));

                document.Add(new Paragraph($"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT)
                    .SetFontSize(10)
                    .SetFontColor(iText.Kernel.Colors.ColorConstants.GRAY)
                    .SetMarginBottom(20));

                document.Add(new Paragraph("A continuación se presentan los gráficos que resumen la información financiera del usuario. " +
                    "Estos gráficos incluyen ingresos y gastos por categoría, comparativas mensuales, saldo neto, porcentaje de ahorro y más.")
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.JUSTIFIED)
                    .SetFontSize(12)
                    .SetMarginBottom(20));

                float maxWidth = 450f;
                float maxHeight = 400f;

                foreach (var imagenBase64 in imagenes)
                {
                    if (string.IsNullOrWhiteSpace(imagenBase64))
                    {
                        return new HttpStatusCodeResult(400, "Una de las cadenas base64 está vacía.");
                    }

                    var imagenBytes = Convert.FromBase64String(imagenBase64);
                    var imagenData = ImageDataFactory.Create(imagenBytes);
                    var imagen = new Image(imagenData);

                    if (imagen.GetImageWidth() > maxWidth || imagen.GetImageHeight() > maxHeight)
                    {
                        imagen.ScaleToFit(maxWidth, maxHeight);
                    }

                    imagen.SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER);
                    imagen.SetMarginBottom(25);

                    document.Add(imagen);
                }

                document.Close();

                var byteArray = memoryStream.ToArray();

                if (byteArray.Length == 0)
                {
                    return new HttpStatusCodeResult(400, "El PDF generado está vacío.");
                }

                return File(byteArray, "application/pdf", "ReporteGraficas.pdf");
            }
        }
        catch (Exception ex)
        {
            return new HttpStatusCodeResult(500, $"Error interno: {ex.Message}");
        }
    }


    private TransaccionServicio _servicio = new TransaccionServicio();

    // GET: Asesor/DashboardContadorUsuarios?idUsuario=1
    public ActionResult DashboardContadorUsuarios(int? idUsuario)
    {
        if (!idUsuario.HasValue)
        {
            // Si no se proporciona idUsuario, envía una lista vacía a la vista
            return View(new List<TransaccionContadorViewModel>());
        }

        // Obtener transacciones filtradas por idUsuario
        var transacciones = _servicio.ObtenerTransaccionesPorUsuario(idUsuario.Value);

        // Retorna la vista con el modelo
        return View(transacciones);
    }
    private UsuarioServicio _usuarioServicio = new UsuarioServicio();

    public ActionResult ListadoUsuarios()
    {
        // Suponiendo que tienes un servicio que obtiene usuarios por rol
        var usuariosRol3 = new UsuarioServicio().ObtenerUsuariosPorRol(3);
        return View(usuariosRol3);
    }

}




























