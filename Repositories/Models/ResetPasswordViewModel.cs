using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tupresupuestoweb.Repositories.Models
{
    public class ResetPasswordViewModel
    {
        public string Token { get; set; }
        public string NuevaContrasena { get; set; }
    }
}