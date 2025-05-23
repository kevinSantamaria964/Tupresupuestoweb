using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Configuration;
using Tupresupuestoweb.Repositories.Models;

namespace Tupresupuestoweb.Repositories
{
    public class TransaccionServicio
    {
        private readonly string _connectionString;

        public TransaccionServicio()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString;
        }

        public List<TransaccionContadorViewModel> ObtenerTransaccionesPorUsuario(int idUsuario)
        {
            var lista = new List<TransaccionContadorViewModel>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string sql = @"
                   SELECT 
                        i.Id,
                        i.IdUsuario,
                        u.Nombre,           -- nombre del usuario
                        'Ingreso' AS Tipo,
                        i.Cantidad,
                        i.Fecha,
                        i.Categoria,
                        i.Descripcion
                    FROM Ingresos i
                    INNER JOIN Usuarios u ON i.IdUsuario = u.Id
                    WHERE i.IdUsuario = @IdUsuario

                    UNION ALL

                    SELECT 
                        g.Id,
                        g.IdUsuario,
                        u.Nombre,          -- nombre del usuario
                        'Gasto' AS Tipo,
                        g.Cantidad,
                        g.Fecha,
                        g.Categoria,
                        g.Descripcion
                    FROM Gastos g
                    INNER JOIN Usuarios u ON g.IdUsuario = u.Id
                    WHERE g.IdUsuario = @IdUsuario

                    ORDER BY Fecha DESC;
                ";

                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    lista.Add(new TransaccionContadorViewModel
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        IdUsuario = Convert.ToInt32(reader["IdUsuario"]),
                        Nombre = reader["Nombre"].ToString(), // Aquí asignamos el nombre correctamente
                        Tipo = reader["Tipo"].ToString(),
                        Cantidad = Convert.ToDecimal(reader["Cantidad"]),
                        Fecha = Convert.ToDateTime(reader["Fecha"]),
                        Categoria = reader["Categoria"].ToString(),
                        Descripcion = reader["Descripcion"].ToString()
                    });
                }
            }

            return lista;
        }
    }
}

