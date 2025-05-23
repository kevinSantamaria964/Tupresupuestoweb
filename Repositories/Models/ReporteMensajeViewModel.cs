using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;


namespace Tupresupuestoweb.Models
{
    public class ReporteMensajeViewModel
    {
        [Required(ErrorMessage = "El ID del usuario es obligatorio.")]
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        public string Titulo { get; set; }

        [Required(ErrorMessage = "El mensaje es obligatorio.")]
        public string Mensaje { get; set; }

        public List<HttpPostedFileBase> ImagenesAdjuntas { get; set; } // Lista de imágenes adjuntas
    }
}

