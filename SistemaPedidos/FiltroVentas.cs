//////// FiltroVentas.cs ////////
using System;
 
namespace SistemaPedidos
{
    // Reúne los criterios de búsqueda. Un criterio vacío no filtra nada.
    public class FiltroVentas
    {
        public string Texto = "";           // se busca en productos y nota
        public DateTime? Desde;             // fecha mínima (inclusive)
        public DateTime? Hasta;             // fecha máxima (inclusive)
        public EstadoVenta? Estado;
        public MedioPago? Medio;
 
        public bool Coincide(Venta venta)
        {
            if (Texto != "")
            {
                bool enProductos = venta.Productos.IndexOf(Texto, StringComparison.OrdinalIgnoreCase) >= 0;
                bool enNota = venta.Nota.IndexOf(Texto, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!enProductos && !enNota)
                {
                    return false;
                }
            }
            if (Desde.HasValue && venta.Fecha.Date < Desde.Value.Date)
            {
                return false;
            }
            if (Hasta.HasValue && venta.Fecha.Date > Hasta.Value.Date)
            {
                return false;
            }
            if (Estado.HasValue && venta.Estado != Estado.Value)
            {
                return false;
            }
            if (Medio.HasValue && venta.Medio != Medio.Value)
            {
                return false;
            }
            return true;
        }
    }
}