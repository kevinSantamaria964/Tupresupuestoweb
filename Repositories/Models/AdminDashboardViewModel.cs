using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tupresupuestoweb.Repositories.Models
{
    public class AdminDashboardViewModel
    {
        public List<PerfilViewModel> Usuarios { get; set; }
        public int TotalUsuarios { get; set; }
        public decimal TotalIngresos { get; set; }
        public int SesionesActivas { get; set; }
        public string CargaSistema { get; set; }
        public List<string> ActividadesRecientes { get; set; }
        public Dictionary<string, int> ActividadUsuarios { get; set; }

        // NUEVO: Distribución de roles para gráfico de dona
        public Dictionary<string, int> DistribucionRoles { get; set; }

        public Dictionary<string, int> ActividadPorDia { get; set; }


        public AdminDashboardViewModel()
        {
            Usuarios = new List<PerfilViewModel>();
            ActividadesRecientes = new List<string>();
            ActividadUsuarios = new Dictionary<string, int>();
            DistribucionRoles = new Dictionary<string, int>(); // Inicializamos aquí también
        }
        public class EvolucionFinanciera
        {
            public string Fecha { get; set; } // formato: "dd MMM"
            public decimal TotalIngresos { get; set; }
            public decimal TotalGastos { get; set; }
        }
        public List<EvolucionFinanciera> EvolucionIngresosGastos { get; set; }

    }
}
