using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tupresupuestoweb.Repositories.Models
{
    public class Transaccion
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public string Tipo { get; set; }  // "Ingreso" o "Gasto"
        public decimal Cantidad { get; set; }
        public DateTime Fecha { get; set; }
    }
}
