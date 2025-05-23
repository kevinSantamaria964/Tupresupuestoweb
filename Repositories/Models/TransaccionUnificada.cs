using System;

namespace Tupresupuestoweb.Repositories.Models
{
    public class TransaccionUnificada
    {
        public int Id { get; set; } // 👈 Agregá esto
        public DateTime Fecha { get; set; }
        public string Categoria { get; set; }
        public decimal Cantidad { get; set; } // Positivo para ingresos, negativo para gastos
        public string Tipo { get; set; } // 👈 Opcional, para distinguir entre ingreso/gasto
    }
}
