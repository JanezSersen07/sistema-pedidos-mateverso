//////// ArchivoVentas.cs ////////
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SistemaPedidos
{
    // Guarda y carga las ventas en un archivo CSV propio del programa.
    // Se separa con punto y coma para que el Excel en español lo abra bien.
    public static class ArchivoVentas
    {
        private const char SEP = ';';
        private const string FORMATO_FECHA = "yyyy-MM-dd";

        private const string ENCABEZADO =
            "Numero;Fecha;FechaSegundoPago;Productos;CostoTotal;Bolsa;Sticker;Tarjeta;PrecioVenta;MedioPago;Estado;EsRegalo;Nota";

        public static void Guardar(List<Venta> ventas, string ruta)
        {
            string carpeta = Path.GetDirectoryName(ruta);
            if (!string.IsNullOrEmpty(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(ENCABEZADO);
            foreach (Venta v in ventas)
            {
                string[] campos = new string[]
                {
                    v.Numero.ToString(),
                    v.Fecha.ToString(FORMATO_FECHA),
                    v.FechaSegundoPago.HasValue ? v.FechaSegundoPago.Value.ToString(FORMATO_FECHA) : "",
                    v.Productos,
                    Numero(v.CostoTotal),
                    Numero(v.Bolsa),
                    Numero(v.Sticker),
                    Numero(v.Tarjeta),
                    Numero(v.PrecioVenta),
                    v.Medio.ToString(),
                    v.Estado.ToString(),
                    v.EsRegalo ? "Si" : "No",
                    v.Nota
                };
                for (int i = 0; i < campos.Length; i++)
                {
                    campos[i] = LectorCsv.Escapar(campos[i], SEP);
                }
                sb.AppendLine(string.Join(SEP.ToString(), campos));
            }

            // Primero una copia de seguridad del archivo anterior
            if (File.Exists(ruta))
            {
                File.Copy(ruta, ruta + ".bak", true);
            }
            // El BOM hace que Excel reconozca bien las tildes
            File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(true));
        }

        public static List<Venta> Cargar(string ruta)
        {
            List<Venta> ventas = new List<Venta>();
            if (!File.Exists(ruta))
            {
                return ventas;
            }

            List<List<string>> filas = LectorCsv.Leer(File.ReadAllText(ruta), SEP);
            for (int i = 1; i < filas.Count; i++) // la fila 0 es el encabezado
            {
                List<string> f = filas[i];
                if (f.Count < 13)
                {
                    continue;
                }

                try
                {
                    DateTime fecha = DateTime.ParseExact(f[1], FORMATO_FECHA, CultureInfo.InvariantCulture);
                    MedioPago medio = (MedioPago)Enum.Parse(typeof(MedioPago), f[9]);
                    EstadoVenta estado = (EstadoVenta)Enum.Parse(typeof(EstadoVenta), f[10]);

                    Venta v = new Venta(int.Parse(f[0]), fecha, f[3], Leer(f[4]), Leer(f[8]), medio, estado);
                    if (f[2] != "")
                    {
                        v.FechaSegundoPago = DateTime.ParseExact(f[2], FORMATO_FECHA, CultureInfo.InvariantCulture);
                    }
                    v.Bolsa = Leer(f[5]);
                    v.Sticker = Leer(f[6]);
                    v.Tarjeta = Leer(f[7]);
                    v.EsRegalo = f[11] == "Si";
                    v.Nota = f[12];
                    ventas.Add(v);
                }
                catch (FormatException)
                {
                    throw new FormatException("La fila " + (i + 1) + " del archivo de ventas tiene un dato inválido.");
                }
                catch (ArgumentException)
                {
                    throw new FormatException("La fila " + (i + 1) + " del archivo de ventas tiene un dato inválido.");
                }
            }
            return ventas;
        }

        private static string Numero(decimal valor)
        {
            return valor.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static decimal Leer(string texto)
        {
            if (texto == "")
            {
                return 0;
            }
            return decimal.Parse(texto, CultureInfo.InvariantCulture);
        }
    }
}
