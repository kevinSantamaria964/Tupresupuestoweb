using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Tupresupuestoweb.Repositories.Models
{
    public class IngresoViewModel
    {
        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Debe ser mayor a 0")]
        public decimal Cantidad { get; set; }

        [Required(ErrorMessage = "La categoría es obligatoria")]
        public string Categoria { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria")]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }

        public string Descripcion { get; set; }
        public List<SelectListItem> CategoriasDisponibles { get; internal set; }
    }
}

