using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tupresupuestoweb.Repositories.Models
{
    public class UsuarioViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int IdRol { get; set; }
    }
}
