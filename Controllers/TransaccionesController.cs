using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Tupresupuestoweb.Repositories;
using Tupresupuestoweb.Repositories.Models;
using Tupresupuestoweb.Utilities;
using Tupresupuestoweb.Filters;
using System.Data;
using System.Web;
using System.IO;              // <---- Esta línea es la importante



namespace Tupresupuestoweb.Controllers
{
    [CustomAuthorize(3)]


    public class TransaccionesController : Controller
    {

        // GET: Transacciones/AgregarIngreso
        public ActionResult AgregarIngreso()
        {
            var model = new IngresoViewModel
            {
                CategoriasDisponibles = ObtenerCategoriasIngreso()
            };
            return View(model);
        }

        // POST: Transacciones/AgregarIngreso
        [HttpPost]
        public ActionResult AgregarIngreso(IngresoViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString))
                    {
                        conn.Open();

                        string query = @"INSERT INTO Ingresos (IdUsuario, Cantidad, Categoria, Fecha, Descripcion)
                                 VALUES (@IdUsuario, @Cantidad, @Categoria, @Fecha, @Descripcion)";

                        using (var cmd = new SqlCommand(query, conn))
                        {
                            int idUsuario = ObtenerIdUsuarioActual();

                            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            cmd.Parameters.AddWithValue("@Cantidad", model.Cantidad);
                            cmd.Parameters.AddWithValue("@Categoria", model.Categoria);
                            cmd.Parameters.AddWithValue("@Fecha", model.Fecha);
                            cmd.Parameters.AddWithValue("@Descripcion", (object)model.Descripcion ?? DBNull.Value);

                            cmd.ExecuteNonQuery();

                            // Guardar notificación
                            string mensaje = $"Se ha registrado un ingreso de {model.Cantidad:C}.";
                            new NotificacionesRepository().GuardarNotificacion(idUsuario, mensaje, model.Cantidad, model.Categoria);
                        }
                    }

                    return RedirectToAction("DashboardUsuario", "Home");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar el ingreso: " + ex.Message);
                }
            }

            model.CategoriasDisponibles = ObtenerCategoriasIngreso();
            return View(model);
        }


        // GET: Transacciones/AgregarGasto
        public ActionResult AgregarGasto()
        {
            var model = new GastoViewModel
            {
                CategoriasDisponibles = ObtenerCategoriasGasto()
            };
            return View(model);
        }

        [HttpPost]
        public ActionResult AgregarGasto(GastoViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString))
                    {
                        conn.Open();

                        string insertQuery = @"INSERT INTO Gastos (IdUsuario, Cantidad, Categoria, Fecha, Descripcion)
                                       VALUES (@IdUsuario, @Cantidad, @Categoria, @Fecha, @Descripcion)";

                        using (var cmd = new SqlCommand(insertQuery, conn))
                        {
                            int idUsuario = ObtenerIdUsuarioActual();

                            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            cmd.Parameters.AddWithValue("@Cantidad", model.Cantidad);
                            cmd.Parameters.AddWithValue("@Categoria", model.Categoria);
                            cmd.Parameters.AddWithValue("@Fecha", model.Fecha);
                            cmd.Parameters.AddWithValue("@Descripcion", (object)model.Descripcion ?? DBNull.Value);

                            cmd.ExecuteNonQuery();

                            // Guardar notificación
                            string mensaje = $"Se ha registrado un gasto de {model.Cantidad:C}.";
                            new NotificacionesRepository().GuardarNotificacion(idUsuario, mensaje, model.Cantidad, model.Categoria);
                        }
                    }

                    return RedirectToAction("DashboardUsuario", "Home");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar el gasto: " + ex.Message);
                }
            }

            model.CategoriasDisponibles = ObtenerCategoriasGasto();
            return View(model);
        }


        public ActionResult Historial() => View();
        public ActionResult Resumen() => View();

        [HttpGet]
        public JsonResult ResumenFinanciero(string periodo, DateTime? desde, DateTime? hasta)
        {
            int idUsuario = ObtenerIdUsuarioActual();
            DateTime hoy = DateTime.Today;
            DateTime fechaInicio, fechaFin;

            switch (periodo)
            {
                case "dia":
                    fechaInicio = hoy;
                    fechaFin = hoy;
                    break;
                case "semana":
                    fechaInicio = hoy.AddDays(-(int)hoy.DayOfWeek + 1);
                    fechaFin = hoy;
                    break;
                case "anio":
                    fechaInicio = new DateTime(hoy.Year, 1, 1);
                    fechaFin = hoy;
                    break;
                case "personalizado":
                    fechaInicio = desde ?? hoy;
                    fechaFin = hasta ?? hoy;
                    break;
                default:
                    fechaInicio = new DateTime(hoy.Year, hoy.Month, 1);
                    fechaFin = hoy;
                    break;
            }

            decimal ingresosTotales = 0;
            decimal gastosTotales = 0;
            var labels = new List<string>();
            var ingresosPorPeriodo = new List<decimal>();
            var gastosPorPeriodo = new List<decimal>();

            try
            {
                var ingresosPorFecha = new Dictionary<string, decimal>();
                var gastosPorFecha = new Dictionary<string, decimal>();

                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString))
                {
                    conn.Open();

                    // INGRESOS
                    string queryIngresos = @"SELECT Fecha, Cantidad FROM Ingresos 
                                     WHERE IdUsuario = @IdUsuario AND Fecha BETWEEN @Desde AND @Hasta";

                    using (var cmd = new SqlCommand(queryIngresos, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        cmd.Parameters.AddWithValue("@Desde", fechaInicio);
                        cmd.Parameters.AddWithValue("@Hasta", fechaFin);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime fecha = reader.GetDateTime(0);
                                decimal monto = reader.GetDecimal(1);
                                ingresosTotales += monto;

                                string label = fecha.ToShortDateString();
                                if (!ingresosPorFecha.ContainsKey(label))
                                    ingresosPorFecha[label] = 0;
                                ingresosPorFecha[label] += monto;
                            }
                        }
                    }

                    // GASTOS
                    string queryGastos = @"SELECT Fecha, Cantidad FROM Gastos 
                                   WHERE IdUsuario = @IdUsuario AND Fecha BETWEEN @Desde AND @Hasta";

                    using (var cmd = new SqlCommand(queryGastos, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        cmd.Parameters.AddWithValue("@Desde", fechaInicio);
                        cmd.Parameters.AddWithValue("@Hasta", fechaFin);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime fecha = reader.GetDateTime(0);
                                decimal monto = reader.GetDecimal(1);
                                gastosTotales += monto;

                                string label = fecha.ToShortDateString();
                                if (!gastosPorFecha.ContainsKey(label))
                                    gastosPorFecha[label] = 0;
                                gastosPorFecha[label] += monto;
                            }
                        }
                    }

                    // Unificar fechas
                    var todasLasFechas = ingresosPorFecha.Keys
                        .Union(gastosPorFecha.Keys)
                        .Select(f => DateTime.Parse(f))
                        .OrderBy(f => f)
                        .Select(f => f.ToShortDateString())
                        .ToList();

                    foreach (var fecha in todasLasFechas)
                    {
                        labels.Add(fecha);
                        ingresosPorPeriodo.Add(ingresosPorFecha.ContainsKey(fecha) ? ingresosPorFecha[fecha] : 0);
                        gastosPorPeriodo.Add(gastosPorFecha.ContainsKey(fecha) ? gastosPorFecha[fecha] : 0);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error en la base de datos: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }

            // Porcentajes
            var totalMovimientos = ingresosTotales + gastosTotales;
            var porcentajeIngresos = totalMovimientos > 0 ? (ingresosTotales / totalMovimientos) * 100 : 0;
            var porcentajeGastos = totalMovimientos > 0 ? (gastosTotales / totalMovimientos) * 100 : 0;

            var balance = ingresosTotales - gastosTotales;

            return Json(new
            {
                ingresos = ingresosTotales,
                gastos = gastosTotales,
                balance,
                labels,
                ingresosPorPeriodo,
                gastosPorPeriodo,
                porcentajeIngresos,
                porcentajeGastos
            }, JsonRequestBehavior.AllowGet);
        }


        private int ObtenerIdUsuarioActual()
        {
            if (Session["IdUsuario"] == null)
            {
                throw new InvalidOperationException("Usuario no autenticado.");
            }
            return (int)Session["IdUsuario"];
        }



        public ActionResult ObtenerTransaccionesRecientes()
        {
            int idUsuario = ObtenerIdUsuarioActual();
            var transacciones = new List<TransaccionUnificada>();

            using (SqlConnection conn = DBContextUtility.GetConnection())
            {
                conn.Open();

                // INGRESOS del usuario
                string queryIngresos = "SELECT Id, Fecha, Categoria, Cantidad FROM Ingresos WHERE IdUsuario = @IdUsuario";
                using (SqlCommand cmd = new SqlCommand(queryIngresos, conn))
                {
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // INGRESOS
                            transacciones.Add(new TransaccionUnificada
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Fecha = Convert.ToDateTime(reader["Fecha"]),
                                Categoria = reader["Categoria"].ToString(),
                                Cantidad = Convert.ToDecimal(reader["Cantidad"]),
                                Tipo = "Ingreso"
                            });

                        }
                    }
                }

                // GASTOS del usuario
                string queryGastos = "SELECT Id, Fecha, Categoria, Cantidad FROM Gastos WHERE IdUsuario = @IdUsuario";
                using (SqlCommand cmd = new SqlCommand(queryGastos, conn))
                {
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // GASTOS
                            transacciones.Add(new TransaccionUnificada
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Fecha = Convert.ToDateTime(reader["Fecha"]),
                                Categoria = reader["Categoria"].ToString(),
                                Cantidad = -Convert.ToDecimal(reader["Cantidad"]),
                                Tipo = "Gasto"
                            });

                        }
                    }
                }
            }

            var recientes = transacciones
                .OrderByDescending(t => t.Fecha)
                .Take(10)
                .ToList();

            return Json(recientes, JsonRequestBehavior.AllowGet);
        }



        // Métodos auxiliares para categorías
        private List<SelectListItem> ObtenerCategoriasIngreso()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Sueldo", Text = "💼 Sueldo" },
    new SelectListItem { Value = "Freelance", Text = "🖥️ Freelance" },
    new SelectListItem { Value = "Inversiones", Text = "📈 Inversiones" },
    new SelectListItem { Value = "Regalos", Text = "🎁 Regalos" },
    new SelectListItem { Value = "Ventas", Text = "💰 Ventas" },
    new SelectListItem { Value = "Bonos", Text = "🎉 Bonos" },
    new SelectListItem { Value = "Devoluciones", Text = "↩️ Devoluciones" },
    new SelectListItem { Value = "Premios", Text = "🏆 Premios" },
    new SelectListItem { Value = "Alquileres", Text = "🏡 Alquileres" },
    new SelectListItem { Value = "Dividendos", Text = "💹 Dividendos" },
    new SelectListItem { Value = "Intereses", Text = "🏦 Intereses" },
    new SelectListItem { Value = "Otros", Text = "🔄 Otros" }
            };
        }

        private List<SelectListItem> ObtenerCategoriasGasto()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Alimentación", Text = "🍔 Alimentación" },
    new SelectListItem { Value = "Transporte", Text = "🚗 Transporte" },
    new SelectListItem { Value = "Salud", Text = "🏥 Salud" },
    new SelectListItem { Value = "Educación", Text = "📚 Educación" },
    new SelectListItem { Value = "Ocio", Text = "🎮 Ocio" },
    new SelectListItem { Value = "Casa", Text = "🏠 Casa" },
    new SelectListItem { Value = "Familia", Text = "👨‍👩‍👧‍👦 Familia" },
    new SelectListItem { Value = "Regalos", Text = "🎁 Regalos" },
    new SelectListItem { Value = "Ropa", Text = "👕 Ropa" },
    new SelectListItem { Value = "Mascotas", Text = "🐶 Mascotas" },
    new SelectListItem { Value = "Viajes", Text = "✈️ Viajes" },
    new SelectListItem { Value = "Impuestos", Text = "💸 Impuestos" },
    new SelectListItem { Value = "Tecnología", Text = "💻 Tecnología" },
    new SelectListItem { Value = "Rutina", Text = "🔁 Rutina" },
    new SelectListItem { Value = "Otros", Text = "🔧 Otros" }
            };
        }

        [HttpPost]
        public ActionResult EliminarTransaccion(int Id, string Tipo)
        {
            int idUsuario = Convert.ToInt32(Session["IdUsuario"]);

            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString))
            {
                connection.Open();
                string tabla = Tipo == "ingreso" ? "Ingresos" : "Gastos";
                string query = $"DELETE FROM {tabla} WHERE Id = @Id AND IdUsuario = @IdUsuario";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", Id);
                    command.Parameters.AddWithValue("@IdUsuario", idUsuario);

                    int filasAfectadas = command.ExecuteNonQuery();
                    if (filasAfectadas == 0)
                        return new HttpStatusCodeResult(404, "No se encontró la transacción o no te pertenece");
                }
            }

            return new HttpStatusCodeResult(200);

        }
        public ActionResult Movimientos(string fecha)
        {
            DateTime fechaBase = DateTime.Now;
            if (!string.IsNullOrEmpty(fecha))
            {
                DateTime.TryParse(fecha, out fechaBase);
            }

            int idUsuario = Convert.ToInt32(Session["IdUsuario"]);
            var movimientos = MovimientoService.ObtenerMovimientosPorUsuario(idUsuario);

            // Generar los eventos para FullCalendar
            var eventos = movimientos.Select(m => new
            {
                title = m.TotalIngresos > 0 ? "Ingreso" : "Gasto",  // Título basado en el tipo de movimiento
                start = m.Fecha.ToString("yyyy-MM-dd"),
                description = m.TotalIngresos > 0 ? $"Ingreso de {m.TotalIngresos:C}" : $"Gasto de {m.TotalGastos:C}", // Descripción con detalles del monto
                color = m.TotalIngresos > 0 ? "green" : "red", // Color verde para ingresos, rojo para gastos
                allDay = true  // Esto asegura que el evento se muestre durante todo el día
            }).ToList();

            ViewBag.Eventos = eventos;
            ViewBag.FechaBase = fechaBase;

            return View(movimientos);
        }

        public ActionResult VerReportes()
        {
            int idUsuario = (int)Session["IdUsuario"];
            List<ReporteEnviadoViewModel> reportes = new List<ReporteEnviadoViewModel>();

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString))
            {
                conn.Open();
                var query = "SELECT Id, Titulo, Mensaje, RutaArchivo, Fecha, Leido FROM ReportesEnviados WHERE IdUsuario = @IdUsuario ORDER BY Fecha DESC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reportes.Add(new ReporteEnviadoViewModel
                            {
                                Id = (int)reader["Id"],
                                Titulo = reader["Titulo"].ToString(),
                                Mensaje = reader["Mensaje"].ToString(),
                                RutaArchivo = reader["RutaArchivo"].ToString(),
                                Fecha = (DateTime)reader["Fecha"],
                                Leido = (bool)reader["Leido"]
                            });
                        }
                    }
                }
            }

            return View(reportes);
        }
        public ActionResult DescargarReporte(string nombreArchivo)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivo))
                return HttpNotFound();

            var rutaCarpeta = Server.MapPath("~/Content/Reportes");
            var rutaCompleta = Path.Combine(rutaCarpeta, nombreArchivo); // Esto ahora funciona correctamente

            if (!System.IO.File.Exists(rutaCompleta))
            {
                TempData["Mensaje"] = "El archivo no se encuentra disponible.";
                return RedirectToAction("VerReportes");
            }

            return File(rutaCompleta, "application/pdf", nombreArchivo);
        }
    }
}









