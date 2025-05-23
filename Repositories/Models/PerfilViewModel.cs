using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tupresupuestoweb.Repositories.Models
{
    public class PerfilViewModel
    {
        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; }

        [Display(Name = "Correo electrónico")]
        [EmailAddress]
        public string Correo { get; set; }

        [Display(Name = "Nombre de usuario")]
        public string Username { get; set; }

        [Display(Name = "Rol")]
        public string Rol { get; set; }

        public bool Activo { get; set; }   // Por defecto activo

    }
}

