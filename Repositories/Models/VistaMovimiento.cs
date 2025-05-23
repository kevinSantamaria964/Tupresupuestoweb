using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tupresupuestoweb.Repositories.Models
{
    public class VistaMovimiento
    {
        public int IdUsuario { get; set; }
        public DateTime Fecha { get; set; }
        public double TotalIngresos { get; set; }
        public double TotalGastos { get; set; }
    }
}