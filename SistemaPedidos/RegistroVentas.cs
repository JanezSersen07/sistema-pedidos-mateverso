//////// RegistroVentas.cs ////////
using System;
using System.Collections.Generic;

namespace SistemaPedidos
{
    // Guarda la lista de ventas y calcula los reportes.
    public class RegistroVentas
    {
        private List<Venta> ventas = new List<Venta>();

        public RegistroVentas(List<Venta> ventasIniciales)
        {
            ventas = ventasIniciales;
        }

        public List<Venta> Ventas
        {
            get { return ventas; }
        }

        public int Cantidad
        {
            get { return ventas.Count; }
        }

        public int ProximoNumero()
        {
            int mayor = 0;
            foreach (Venta v in ventas)
            {
                if (v.Numero > mayor)
                {
                    mayor = v.Numero;
                }
            }
            return mayor + 1;
        }

        public void Agregar(Venta venta)
        {
            ventas.Add(venta);
        }

        public Venta Buscar(int numero)
        {
            foreach (Venta v in ventas)
            {
                if (v.Numero == numero)
                {
                    return v;
                }
            }
            return null;
        }

        // Lo que ya está pagado (Pagado + Entregado)
        public decimal TotalCobrado()
        {
            decimal total = 0;
            foreach (Venta v in ventas)
            {
                if (v.Estado != EstadoVenta.Pedido)
                {
                    total += v.PrecioVenta;
                }
            }
            return total;
        }

        // Lo que falta cobrar (Pedido)
        public decimal TotalPorCobrar()
        {
            decimal total = 0;
            foreach (Venta v in ventas)
            {
                if (v.Estado == EstadoVenta.Pedido)
                {
                    total += v.PrecioVenta;
                }
            }
            return total;
        }

        public decimal TotalCostos()
        {
            decimal total = 0;
            foreach (Venta v in ventas)
            {
                total += v.CostoTotal;
            }
            return total;
        }

        public decimal TotalVentas()
        {
            decimal total = 0;
            foreach (Venta v in ventas)
            {
                total += v.PrecioVenta;
            }
            return total;
        }

        public decimal GananciaTotal()
        {
            return TotalVentas() - TotalCostos();
        }

        public int CantidadConEstado(EstadoVenta estado)
        {
            int cantidad = 0;
            foreach (Venta v in ventas)
            {
                if (v.Estado == estado)
                {
                    cantidad++;
                }
            }
            return cantidad;
        }

        // Muestra ventas, costos y ganancia agrupados por mes
        public void MostrarResumenPorMes()
        {
            SortedDictionary<string, decimal[]> meses = new SortedDictionary<string, decimal[]>();

            foreach (Venta v in ventas)
            {
                string clave = v.Fecha.ToString("yyyy-MM");
                if (!meses.ContainsKey(clave))
                {
                    meses[clave] = new decimal[] { 0, 0, 0 }; // cantidad, ventas, costos
                }
                meses[clave][0] += 1;
                meses[clave][1] += v.PrecioVenta;
                meses[clave][2] += v.CostoTotal;
            }

            Console.WriteLine("Mes      Ventas   Facturado     Costos    Ganancia");
            foreach (KeyValuePair<string, decimal[]> par in meses)
            {
                decimal[] d = par.Value;
                Console.WriteLine("{0}  {1,6}  {2,10}  {3,10}  {4,10}",
                    par.Key, d[0], Formato.Pesos(d[1]), Formato.Pesos(d[2]), Formato.Pesos(d[1] - d[2]));
            }
        }

        // Muestra cuánto se vendió con cada medio de pago
        public void MostrarResumenPorMedio()
        {
            Dictionary<MedioPago, decimal> totales = new Dictionary<MedioPago, decimal>();
            foreach (Venta v in ventas)
            {
                if (!totales.ContainsKey(v.Medio))
                {
                    totales[v.Medio] = 0;
                }
                totales[v.Medio] += v.PrecioVenta;
            }
            foreach (KeyValuePair<MedioPago, decimal> par in totales)
            {
                Console.WriteLine("  " + par.Key + ": " + Formato.Pesos(par.Value));
            }
        }
    }
}