using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tupresupuestoweb.Repositories.Models
{
    public class ReporteEnviadoViewModel
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Mensaje { get; set; }
        public string RutaArchivo { get; set; }
        public DateTime Fecha { get; set; }
        public bool Leido { get; set; }
    }
}