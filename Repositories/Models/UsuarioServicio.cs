using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Configuration;
using Tupresupuestoweb.Repositories.Models;

namespace Tupresupuestoweb.Repositories
{
    public class UsuarioServicio
    {
        private readonly string _connectionString;

        public UsuarioServicio()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString;
        }

        public List<UsuarioViewModel> ObtenerUsuariosPorRol(int idRol)
        {
            var lista = new List<UsuarioViewModel>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string sql = "SELECT Id, Nombre, IdRol FROM Usuarios WHERE IdRol = @IdRol";

                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@IdRol", idRol);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    lista.Add(new UsuarioViewModel
                    {
                        Id = (int)reader["Id"],
                        Nombre = reader["Nombre"].ToString(),
                        IdRol = (int)reader["IdRol"]
                    });
                }
            }
            return lista;
        }
    }
}
