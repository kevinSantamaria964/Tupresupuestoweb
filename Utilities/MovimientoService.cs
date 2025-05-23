using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Tupresupuestoweb.Repositories.Models;

namespace Tupresupuestoweb.Utilities
{
    public class MovimientoService
    {
        public static List<VistaMovimiento> ObtenerMovimientosPorUsuario(int idUsuario)
        {
            List<VistaMovimiento> movimientos = new List<VistaMovimiento>();

            string query = "SELECT IdUsuario, Fecha, TotalIngresos, TotalGastos FROM VistaMovimientos WHERE IdUsuario = @IdUsuario";

            using (SqlConnection conn = DBContextUtility.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            movimientos.Add(new VistaMovimiento
                            {
                                IdUsuario = reader.GetInt32(0),
                                Fecha = reader.GetDateTime(1),
                                TotalIngresos = Convert.ToDouble(reader["TotalIngresos"]),
                                TotalGastos = Convert.ToDouble(reader["TotalGastos"])
                            });
                        }
                    }
                }
            }

            return movimientos;
        }
    }
}
