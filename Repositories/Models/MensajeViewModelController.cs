using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Tupresupuestoweb.Repositories.Models
{
    public class MensajeViewModel
    {
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Asunto { get; set; }
        public string Contenido { get; set; }
    }
}
