//////// Reportes.cs ////////
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SistemaPedidos
{
    // Una fila del reporte mensual
    public class FilaMes
    {
        public string Mes { get; set; }               // "2026-11"
        public int Ventas { get; set; }
        public decimal Facturado { get; set; }
        public decimal Costos { get; set; }
        // Cuánto subió o bajó la ganancia respecto del mes anterior (en %).
        // Es null si no hay mes anterior o si ese mes no tuvo ganancia para comparar.
        public decimal? VariacionGanancia { get; set; }

        public decimal Ganancia
        {
            get { return Facturado - Costos; }
        }

        public decimal Margen
        {
            get { return Facturado == 0 ? 0 : Ganancia / Facturado * 100; }
        }
    }

    // Una fila del reporte de productos
    public class FilaProducto
    {
        public string Nombre { get; set; }
        public int Unidades { get; set; }
        public decimal Facturado { get; set; }
        public decimal Costos { get; set; }

        public decimal Ganancia
        {
            get { return Facturado - Costos; }
        }

        public decimal Margen
        {
            get { return Facturado == 0 ? 0 : Ganancia / Facturado * 100; }
        }
    }

    // Cómo ordenar el reporte de productos
    public enum OrdenProductos
    {
        Unidades = 1,
        Ganancia = 2,
        Margen = 3
    }

    // Reportes del negocio. Calculan los datos (sin imprimir nada) para poder probarlos;
    // Program.cs se encarga de mostrarlos en pantalla.
    public static class Reportes
    {
        private static readonly NumberFormatInfo numeroExcel = CrearFormatoExcel();

        // Coma como separador decimal, que es lo que espera el Excel en español
        private static NumberFormatInfo CrearFormatoExcel()
        {
            NumberFormatInfo formato = new NumberFormatInfo();
            formato.NumberDecimalSeparator = ",";
            formato.NumberGroupSeparator = "";
            return formato;
        }

        // Ventas, costos y ganancia de cada mes, ordenados del más viejo al más nuevo
        public static List<FilaMes> PorMes(IEnumerable<Venta> ventas)
        {
            SortedDictionary<string, FilaMes> meses = new SortedDictionary<string, FilaMes>(StringComparer.Ordinal);
            foreach (Venta v in ventas)
            {
                string clave = v.Fecha.ToString("yyyy-MM", CultureInfo.InvariantCulture);
                if (!meses.ContainsKey(clave))
                {
                    meses[clave] = new FilaMes { Mes = clave };
                }
                FilaMes fila = meses[clave];
                fila.Ventas += 1;
                fila.Facturado += v.PrecioVenta;
                fila.Costos += v.CostoTotal;
            }

            List<FilaMes> resultado = new List<FilaMes>(meses.Values);
            for (int i = 1; i < resultado.Count; i++)
            {
                FilaMes anterior = resultado[i - 1];
                // Solo se compara si el mes anterior es justo el mes de antes en el calendario
                // y tuvo ganancia positiva (si no, el porcentaje no tiene sentido).
                if (EsMesAnterior(anterior.Mes, resultado[i].Mes) && anterior.Ganancia > 0)
                {
                    resultado[i].VariacionGanancia =
                        (resultado[i].Ganancia - anterior.Ganancia) / anterior.Ganancia * 100;
                }
            }
            return resultado;
        }

        private static bool EsMesAnterior(string anterior, string actual)
        {
            DateTime a = DateTime.ParseExact(anterior, "yyyy-MM", CultureInfo.InvariantCulture);
            DateTime b = DateTime.ParseExact(actual, "yyyy-MM", CultureInfo.InvariantCulture);
            return a.AddMonths(1) == b;
        }

        // Unidades, facturado y ganancia de cada producto (solo ventas con detalle)
        public static List<FilaProducto> PorProducto(IEnumerable<Venta> ventas, OrdenProductos orden)
        {
            Dictionary<string, FilaProducto> productos = new Dictionary<string, FilaProducto>();
            foreach (Venta v in ventas)
            {
                foreach (ItemVenta item in v.Items)
                {
                    if (!productos.ContainsKey(item.Nombre))
                    {
                        productos[item.Nombre] = new FilaProducto { Nombre = item.Nombre };
                    }
                    FilaProducto fila = productos[item.Nombre];
                    fila.Unidades += item.Cantidad;
                    fila.Facturado += item.PrecioTotal;
                    fila.Costos += item.CostoTotal;
                }
            }

            List<FilaProducto> lista = new List<FilaProducto>(productos.Values);
            lista.Sort((a, b) =>
            {
                int comparacion;
                if (orden == OrdenProductos.Ganancia)
                {
                    comparacion = b.Ganancia.CompareTo(a.Ganancia);
                }
                else if (orden == OrdenProductos.Margen)
                {
                    comparacion = b.Margen.CompareTo(a.Margen);
                }
                else
                {
                    comparacion = b.Unidades.CompareTo(a.Unidades);
                }
                // Si empatan: primero el que más facturó, y después por nombre
                if (comparacion == 0)
                {
                    comparacion = b.Facturado.CompareTo(a.Facturado);
                }
                if (comparacion == 0)
                {
                    comparacion = string.CompareOrdinal(a.Nombre, b.Nombre);
                }
                return comparacion;
            });
            return lista;
        }

        // Ventas con un estado dado (Pedido = por cobrar, Pagado = por entregar),
        // de la más vieja a la más nueva, para atender primero lo que más espera.
        public static List<Venta> Pendientes(IEnumerable<Venta> ventas, EstadoVenta estado)
        {
            List<Venta> lista = new List<Venta>();
            foreach (Venta v in ventas)
            {
                if (v.Estado == estado)
                {
                    lista.Add(v);
                }
            }
            lista.Sort((a, b) =>
            {
                int comparacion = a.Fecha.CompareTo(b.Fecha);
                return comparacion != 0 ? comparacion : a.Numero.CompareTo(b.Numero);
            });
            return lista;
        }

        // Días que pasaron desde la fecha de la venta (nunca negativo)
        public static int DiasDeEspera(Venta venta, DateTime hoy)
        {
            int dias = (hoy.Date - venta.Fecha.Date).Days;
            return dias < 0 ? 0 : dias;
        }

        // ---------- Exportar para Excel ----------

        public static void ExportarMensual(List<FilaMes> filas, string ruta)
        {
            List<string[]> datos = new List<string[]>();
            datos.Add(new string[] { "Mes", "Ventas", "Facturado", "Costos", "Ganancia", "Margen %", "Variacion ganancia %" });
            foreach (FilaMes f in filas)
            {
                datos.Add(new string[]
                {
                    f.Mes,
                    f.Ventas.ToString(CultureInfo.InvariantCulture),
                    Numero(f.Facturado),
                    Numero(f.Costos),
                    Numero(f.Ganancia),
                    Numero(f.Margen),
                    f.VariacionGanancia.HasValue ? Numero(f.VariacionGanancia.Value) : ""
                });
            }
            Escribir(datos, ruta);
        }

        public static void ExportarProductos(List<FilaProducto> filas, string ruta)
        {
            List<string[]> datos = new List<string[]>();
            datos.Add(new string[] { "Producto", "Unidades", "Facturado", "Costos", "Ganancia", "Margen %" });
            foreach (FilaProducto f in filas)
            {
                datos.Add(new string[]
                {
                    f.Nombre,
                    f.Unidades.ToString(CultureInfo.InvariantCulture),
                    Numero(f.Facturado),
                    Numero(f.Costos),
                    Numero(f.Ganancia),
                    Numero(f.Margen)
                });
            }
            Escribir(datos, ruta);
        }

        // Máximo dos decimales, con coma
        private static string Numero(decimal valor)
        {
            return Math.Round(valor, 2).ToString("0.##", numeroExcel);
        }

        private static void Escribir(List<string[]> filas, string ruta)
        {
            string carpeta = Path.GetDirectoryName(ruta);
            if (!string.IsNullOrEmpty(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            StringBuilder sb = new StringBuilder();
            foreach (string[] fila in filas)
            {
                string[] campos = new string[fila.Length];
                for (int i = 0; i < fila.Length; i++)
                {
                    campos[i] = LectorCsv.Escapar(fila[i], ';');
                }
                sb.AppendLine(string.Join(";", campos));
            }
            // El BOM hace que Excel reconozca bien las tildes
            File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(true));
        }
    }
}
