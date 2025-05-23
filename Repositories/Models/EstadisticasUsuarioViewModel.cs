using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tupresupuestoweb.Repositories.Models
{

    public class EstadisticasUsuarioViewModel
    {
        public List<string> CategoriasIngresos { get; set; } = new List<string>();
        public List<decimal> TotalesIngresos { get; set; } = new List<decimal>();

        public List<string> CategoriasGastos { get; set; } = new List<string>();
        public List<decimal> TotalesGastos { get; set; } = new List<decimal>();
        public List<string> Meses { get; set; } = new List<string>();
        public List<decimal> IngresosMensuales { get; set; } = new List<decimal>();
        public List<decimal> GastosMensuales { get; set; } = new List<decimal>();
        public List<decimal> SaldoMensual { get; set; } = new List<decimal>();
        public List<decimal> SaldoAcumulado { get; set; } = new List<decimal>();
        public List<decimal> PorcentajeAhorroMensual { get; set; } = new List<decimal>();
        public List<string> CategoriasFrecuentes { get; set; } = new List<string>();
        public List<int> CantidadesPorCategoria { get; set; } = new List<int>();
        public List<int> CantidadesIngresosPorCategoria { get; set; } = new List<int>();
        public List<int> CantidadesGastosPorCategoria { get; set; } = new List<int>();



    }
}