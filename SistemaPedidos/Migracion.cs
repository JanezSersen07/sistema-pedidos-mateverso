//////// Migracion.cs ////////
using System;
using System.Collections.Generic;
using System.IO;

namespace SistemaPedidos
{
    // Pasa los datos de los archivos CSV (versiones anteriores del programa) a la base SQLite.
    // Los CSV no se tocan: quedan como estaban, de respaldo.
    public static class Migracion
    {
        // Devuelve null si no había nada que migrar; si migró, un texto contando qué pasó.
        // Solo migra cuando la base todavía no existe, así ocurre una única vez.
        public static string SiHaceFalta(string rutaBase, string csvVentas, string csvProductos)
        {
            if (File.Exists(rutaBase))
            {
                return null;
            }
            if (!File.Exists(csvVentas) && !File.Exists(csvProductos))
            {
                return null;
            }

            List<Venta> ventas = ArchivoVentas.Cargar(csvVentas);
            List<Producto> productos = ArchivoProductos.Cargar(csvProductos);

            // Se arma en un archivo temporal y recién al final se pone en su lugar:
            // si algo falla, no queda una base a medias.
            string temporal = rutaBase + ".nueva";
            if (File.Exists(temporal))
            {
                File.Delete(temporal);
            }
            try
            {
                RepositorioSqlite.GuardarProductos(productos, temporal);
                RepositorioSqlite.GuardarVentas(ventas, temporal);
                Verificar(ventas, productos, temporal);
                File.Move(temporal, rutaBase);
            }
            catch
            {
                if (File.Exists(temporal))
                {
                    File.Delete(temporal);
                }
                throw;
            }

            return "Se pasaron " + ventas.Count + " ventas y " + productos.Count
                + " productos a la base de datos (" + Path.GetFileName(rutaBase) + "). "
                + "Los archivos CSV quedaron como estaban.";
        }

        // Relee la base nueva y comprueba que tenga exactamente lo mismo que los CSV
        private static void Verificar(List<Venta> ventas, List<Producto> productos, string ruta)
        {
            List<Venta> guardadas = RepositorioSqlite.CargarVentas(ruta);
            List<Producto> productosGuardados = RepositorioSqlite.CargarProductos(ruta);

            if (guardadas.Count != ventas.Count || productosGuardados.Count != productos.Count
                || Resumen(guardadas) != Resumen(ventas))
            {
                throw new InvalidOperationException(
                    "La base nueva no coincide con los CSV. No se cambió nada.");
            }
        }

        // Cantidad de ventas, de items y los totales de costo y precio, en un solo texto
        private static string Resumen(List<Venta> ventas)
        {
            decimal costo = 0;
            decimal precio = 0;
            int items = 0;
            foreach (Venta v in ventas)
            {
                costo += v.CostoTotal;
                precio += v.PrecioVenta;
                items += v.Items.Count;
                foreach (ItemVenta item in v.Items)
                {
                    costo += item.CostoTotal;
                    precio += item.PrecioTotal;
                }
            }
            return ventas.Count + "|" + items + "|" + costo + "|" + precio;
        }
    }
}
