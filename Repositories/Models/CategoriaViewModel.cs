using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Tupresupuestoweb.Repositories.Models
{
    public class CategoriaViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Emoji { get; set; }
        public string Tipo { get; set; }
    }
}



