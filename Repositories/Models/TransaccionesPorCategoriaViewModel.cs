using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tupresupuestoweb.Repositories.Models
{
    public class TransaccionesPorCategoriaViewModel
    {
        public string Categoria { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalGastos { get; set; }
    }
}
