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
 
        public bool Eliminar(int numero)
        {
            Venta venta = Buscar(numero);
            if (venta == null)
            {
                return false;
            }
            ventas.Remove(venta);
            return true;
        }
 
        // Devuelve las ventas que cumplen todos los criterios del filtro
        public List<Venta> Filtrar(FiltroVentas filtro)
        {
            List<Venta> resultado = new List<Venta>();
            foreach (Venta v in ventas)
            {
                if (filtro.Coincide(v))
                {
                    resultado.Add(v);
                }
            }
            return resultado;
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
 
        // Cuenta las ventas que tienen el detalle de productos del catálogo
        public int CantidadConDetalle()
        {
            int cantidad = 0;
            foreach (Venta v in ventas)
            {
                if (v.Items.Count > 0)
                {
                    cantidad++;
                }
            }
            return cantidad;
        }

        // Productos más vendidos: suma unidades, facturado y ganancia de cada producto.
        // Solo cuentan las ventas que tienen detalle. Orden: 1 = unidades, 2 = ganancia.
        public void MostrarProductosMasVendidos(int orden, int maximo)
        {
            Dictionary<string, decimal[]> totales = new Dictionary<string, decimal[]>();
            foreach (Venta v in ventas)
            {
                foreach (ItemVenta item in v.Items)
                {
                    if (!totales.ContainsKey(item.Nombre))
                    {
                        totales[item.Nombre] = new decimal[] { 0, 0, 0 }; // unidades, facturado, ganancia
                    }
                    totales[item.Nombre][0] += item.Cantidad;
                    totales[item.Nombre][1] += item.PrecioTotal;
                    totales[item.Nombre][2] += item.Ganancia;
                }
            }

            List<KeyValuePair<string, decimal[]>> lista = new List<KeyValuePair<string, decimal[]>>(totales);
            int columna = orden == 2 ? 2 : 0;
            // Ordena de mayor a menor; si empatan, queda primero el de más facturado
            lista.Sort((a, b) =>
            {
                int comparacion = b.Value[columna].CompareTo(a.Value[columna]);
                return comparacion != 0 ? comparacion : b.Value[1].CompareTo(a.Value[1]);
            });

            Console.WriteLine("Producto                                  Unid.   Facturado    Ganancia");
            int mostrados = 0;
            foreach (KeyValuePair<string, decimal[]> par in lista)
            {
                if (mostrados >= maximo)
                {
                    break;
                }
                string nombre = par.Key.Length > 40 ? par.Key.Substring(0, 37) + "..." : par.Key;
                Console.WriteLine("{0,-40} {1,6}  {2,10}  {3,10}",
                    nombre, par.Value[0], Formato.Pesos(par.Value[1]), Formato.Pesos(par.Value[2]));
                mostrados++;
            }
            if (lista.Count > maximo)
            {
                Console.WriteLine("(Se muestran los primeros " + maximo + " de " + lista.Count + ")");
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