using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Configuration;

namespace Tupresupuestoweb.Utilities
{
    public class DBContextUtility
    {
        // Método para obtener la conexión a la base de datos
        public static SqlConnection GetConnection()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["TuPresupuestoDB"].ConnectionString;
            return new SqlConnection(connectionString);
        }

        // Método para ejecutar una consulta SQL con parámetros
        public static void ExecuteQueryWithParameters(string query, List<SqlParameter> parameters)
        {
            using (SqlConnection connection = GetConnection())
            {
                connection.Open();  // Abrir la conexión

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Añadir los parámetros al comando
                    command.Parameters.AddRange(parameters.ToArray());

                    // Ejecutar el comando (en este caso, una consulta de actualización)
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void ExecuteQueryWithParameters(string query, SqlParameter[] parameters)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddRange(parameters);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}


