using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tupresupuestoweb.Repositories.Models
{
    public class NotificacionViewModel
    {
        public string Mensaje { get; set; }
        public DateTime Fecha { get; set; }
        public bool Leida { get; set; }
        public string Categoria { get; set; }
        public decimal Cantidad { get; set; }
    }
}