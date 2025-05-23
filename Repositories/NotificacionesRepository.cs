using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using Tupresupuestoweb.Repositories.Models;

namespace Tupresupuestoweb.Repositories
{
    public class NotificacionesRepository
    {
        private readonly string connectionString;

        public NotificacionesRepository()
        {
            connectionString = ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString;
        }

        // ✔ Guarda notificación con todos los campos
        public void GuardarNotificacion(int idUsuario, string mensaje, decimal cantidad, string categoria)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var query = @"INSERT INTO Notificaciones (IdUsuario, Mensaje, Fecha, Categoria, Cantidad, Leida)
                              VALUES (@IdUsuario, @Mensaje, @Fecha, @Categoria, @Cantidad, 0)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    command.Parameters.AddWithValue("@Mensaje", mensaje);
                    command.Parameters.AddWithValue("@Fecha", DateTime.Now);
                    command.Parameters.AddWithValue("@Categoria", categoria);
                    command.Parameters.AddWithValue("@Cantidad", cantidad);

                    command.ExecuteNonQuery();
                }
            }
        }

        // ✔ Obtener todas las notificaciones del usuario
        public List<NotificacionViewModel> ObtenerNotificaciones(int idUsuario)
        {
            var notificaciones = new List<NotificacionViewModel>();

            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"SELECT Mensaje, Fecha, Leida, Categoria, Cantidad 
                                 FROM Notificaciones 
                                 WHERE IdUsuario = @IdUsuario 
                                 ORDER BY Fecha DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdUsuario", idUsuario);

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            notificaciones.Add(new NotificacionViewModel
                            {
                                Mensaje = reader["Mensaje"]?.ToString(),
                                Fecha = reader["Fecha"] != DBNull.Value ? Convert.ToDateTime(reader["Fecha"]) : DateTime.MinValue,
                                Leida = reader["Leida"] != DBNull.Value ? Convert.ToBoolean(reader["Leida"]) : false,
                                Categoria = reader["Categoria"]?.ToString(),
                                Cantidad = reader["Cantidad"] != DBNull.Value ? Convert.ToDecimal(reader["Cantidad"]) : 0m
                            });

                        }
                    }
                }
            }

            return notificaciones;
        }
    }
}


