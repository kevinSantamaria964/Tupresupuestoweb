using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tupresupuestoweb.Repositories.Models
{
    public class AuditoriaNota
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public string Accion { get; set; }
        public DateTime Fecha { get; set; }
    }
}