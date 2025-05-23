using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace Tupresupuestoweb.Repositories.Models
{
    public class TransaccionContadorViewModel
    {


        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }  // Aquí va el nombre del usuario
        public string Tipo { get; set; }
        public decimal Cantidad { get; set; }
        public DateTime Fecha { get; set; }
        public string Categoria { get; set; }
        public string Descripcion { get; set; }
    }
}
