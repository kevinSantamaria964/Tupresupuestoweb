using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tupresupuestoweb.Repositories.Models
{
    public class ImagenesReporteViewModel
    {
        public List<string> Imagenes { get; set; }
        // Nuevas propiedades para el resumen financiero
        public decimal SaldoAcumulado { get; set; }
        public decimal IngresosAcumulados { get; set; }
        public decimal GastosAcumulados { get; set; }
    }
}