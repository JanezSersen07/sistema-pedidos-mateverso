//////// ArchivoProductos.cs ////////
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SistemaPedidos
{
    // Guarda y carga el catálogo en un CSV propio, igual que ArchivoVentas.
    public static class ArchivoProductos
    {
        private const char SEP = ';';
        private const string ENCABEZADO = "Id;Nombre;Costo;Precio";

        public static void Guardar(List<Producto> productos, string ruta)
        {
            string carpeta = Path.GetDirectoryName(ruta);
            if (!string.IsNullOrEmpty(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(ENCABEZADO);
            foreach (Producto p in productos)
            {
                string[] campos = new string[]
                {
                    p.Id.ToString(),
                    p.Nombre,
                    p.Costo.ToString("0.##", CultureInfo.InvariantCulture),
                    p.Precio.ToString("0.##", CultureInfo.InvariantCulture)
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
            File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(true));
        }

        public static List<Producto> Cargar(string ruta)
        {
            List<Producto> productos = new List<Producto>();
            if (!File.Exists(ruta))
            {
                return productos;
            }

            List<List<string>> filas = LectorCsv.Leer(File.ReadAllText(ruta), SEP);
            for (int i = 1; i < filas.Count; i++) // la fila 0 es el encabezado
            {
                List<string> f = filas[i];
                if (f.Count < 4)
                {
                    continue;
                }

                try
                {
                    productos.Add(new Producto(
                        int.Parse(f[0], CultureInfo.InvariantCulture),
                        f[1],
                        decimal.Parse(f[2], CultureInfo.InvariantCulture),
                        decimal.Parse(f[3], CultureInfo.InvariantCulture)));
                }
                catch (FormatException)
                {
                    throw new FormatException("La fila " + (i + 1) + " del archivo de productos tiene un dato inválido.");
                }
            }
            return productos;
        }
    }
}
