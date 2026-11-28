//////// RepositorioSqlite.cs ////////
using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace SistemaPedidos
{
    // Lee y guarda ventas y productos en la base de datos SQLite.
    // Cada guardado es una transacción: o se guarda todo, o no se cambia nada
    // (si se corta la luz a mitad de camino, la base queda como estaba).
    public static class RepositorioSqlite
    {
        private const string FORMATO_FECHA = "yyyy-MM-dd";

        // ---------- Productos ----------

        public static List<Producto> CargarProductos(string ruta)
        {
            List<Producto> productos = new List<Producto>();
            using (SqliteConnection conexion = BaseDatos.Abrir(ruta))
            using (SqliteCommand comando = conexion.CreateCommand())
            {
                comando.CommandText = "SELECT id, nombre, costo, precio FROM productos ORDER BY id";
                using (SqliteDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        productos.Add(new Producto(
                            (int)lector.GetInt64(0),
                            lector.GetString(1),
                            Pesos(lector.GetInt64(2)),
                            Pesos(lector.GetInt64(3))));
                    }
                }
            }
            return productos;
        }

        public static void GuardarProductos(List<Producto> productos, string ruta)
        {
            using (SqliteConnection conexion = BaseDatos.Abrir(ruta))
            using (SqliteTransaction transaccion = conexion.BeginTransaction())
            {
                BaseDatos.Ejecutar(conexion, "DELETE FROM productos", transaccion);
                foreach (Producto p in productos)
                {
                    using (SqliteCommand comando = conexion.CreateCommand())
                    {
                        comando.Transaction = transaccion;
                        comando.CommandText =
                            "INSERT INTO productos (id, nombre, costo, precio) VALUES ($id, $nombre, $costo, $precio)";
                        comando.Parameters.AddWithValue("$id", (long)p.Id);
                        comando.Parameters.AddWithValue("$nombre", p.Nombre);
                        comando.Parameters.AddWithValue("$costo", Centavos(p.Costo));
                        comando.Parameters.AddWithValue("$precio", Centavos(p.Precio));
                        comando.ExecuteNonQuery();
                    }
                }
                transaccion.Commit();
            }
        }

        // ---------- Ventas ----------

        public static List<Venta> CargarVentas(string ruta)
        {
            List<Venta> ventas = new List<Venta>();
            Dictionary<int, Venta> porNumero = new Dictionary<int, Venta>();

            using (SqliteConnection conexion = BaseDatos.Abrir(ruta))
            {
                using (SqliteCommand comando = conexion.CreateCommand())
                {
                    comando.CommandText =
                        "SELECT numero, fecha, fecha_segundo_pago, productos, costo_total, bolsa, sticker, tarjeta, "
                        + "precio_venta, medio, estado, es_regalo, nota FROM ventas ORDER BY numero";
                    using (SqliteDataReader lector = comando.ExecuteReader())
                    {
                        while (lector.Read())
                        {
                            MedioPago medio = (MedioPago)Enum.Parse(typeof(MedioPago), lector.GetString(9));
                            EstadoVenta estado = (EstadoVenta)Enum.Parse(typeof(EstadoVenta), lector.GetString(10));
                            Venta v = new Venta(
                                (int)lector.GetInt64(0),
                                ParsearFecha(lector.GetString(1)),
                                lector.GetString(3),
                                Pesos(lector.GetInt64(4)),
                                Pesos(lector.GetInt64(8)),
                                medio,
                                estado);
                            if (!lector.IsDBNull(2))
                            {
                                v.FechaSegundoPago = ParsearFecha(lector.GetString(2));
                            }
                            v.Bolsa = Pesos(lector.GetInt64(5));
                            v.Sticker = Pesos(lector.GetInt64(6));
                            v.Tarjeta = Pesos(lector.GetInt64(7));
                            v.EsRegalo = lector.GetInt64(11) != 0;
                            v.Nota = lector.GetString(12);
                            ventas.Add(v);
                            porNumero[v.Numero] = v;
                        }
                    }
                }

                using (SqliteCommand comando = conexion.CreateCommand())
                {
                    comando.CommandText =
                        "SELECT venta_numero, nombre, cantidad, costo_unitario, precio_unitario "
                        + "FROM items_venta ORDER BY venta_numero, posicion";
                    using (SqliteDataReader lector = comando.ExecuteReader())
                    {
                        while (lector.Read())
                        {
                            Venta duenoDelItem;
                            if (porNumero.TryGetValue((int)lector.GetInt64(0), out duenoDelItem))
                            {
                                duenoDelItem.Items.Add(new ItemVenta(
                                    lector.GetString(1),
                                    (int)lector.GetInt64(2),
                                    Pesos(lector.GetInt64(3)),
                                    Pesos(lector.GetInt64(4))));
                            }
                        }
                    }
                }
            }
            return ventas;
        }

        public static void GuardarVentas(List<Venta> ventas, string ruta)
        {
            using (SqliteConnection conexion = BaseDatos.Abrir(ruta))
            using (SqliteTransaction transaccion = conexion.BeginTransaction())
            {
                // Los items se borran solos junto con su venta (ON DELETE CASCADE),
                // pero se borran explícitamente para no depender de eso.
                BaseDatos.Ejecutar(conexion, "DELETE FROM items_venta", transaccion);
                BaseDatos.Ejecutar(conexion, "DELETE FROM ventas", transaccion);

                foreach (Venta v in ventas)
                {
                    using (SqliteCommand comando = conexion.CreateCommand())
                    {
                        comando.Transaction = transaccion;
                        comando.CommandText =
                            "INSERT INTO ventas (numero, fecha, fecha_segundo_pago, productos, costo_total, bolsa, "
                            + "sticker, tarjeta, precio_venta, medio, estado, es_regalo, nota) VALUES "
                            + "($numero, $fecha, $segundo, $productos, $costo, $bolsa, $sticker, $tarjeta, "
                            + "$precio, $medio, $estado, $regalo, $nota)";
                        comando.Parameters.AddWithValue("$numero", (long)v.Numero);
                        comando.Parameters.AddWithValue("$fecha", v.Fecha.ToString(FORMATO_FECHA, CultureInfo.InvariantCulture));
                        if (v.FechaSegundoPago.HasValue)
                        {
                            comando.Parameters.AddWithValue("$segundo",
                                v.FechaSegundoPago.Value.ToString(FORMATO_FECHA, CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            comando.Parameters.AddWithValue("$segundo", DBNull.Value);
                        }
                        comando.Parameters.AddWithValue("$productos", v.Productos);
                        comando.Parameters.AddWithValue("$costo", Centavos(v.CostoTotal));
                        comando.Parameters.AddWithValue("$bolsa", Centavos(v.Bolsa));
                        comando.Parameters.AddWithValue("$sticker", Centavos(v.Sticker));
                        comando.Parameters.AddWithValue("$tarjeta", Centavos(v.Tarjeta));
                        comando.Parameters.AddWithValue("$precio", Centavos(v.PrecioVenta));
                        comando.Parameters.AddWithValue("$medio", v.Medio.ToString());
                        comando.Parameters.AddWithValue("$estado", v.Estado.ToString());
                        comando.Parameters.AddWithValue("$regalo", v.EsRegalo ? 1L : 0L);
                        comando.Parameters.AddWithValue("$nota", v.Nota ?? "");
                        comando.ExecuteNonQuery();
                    }

                    int posicion = 0;
                    foreach (ItemVenta item in v.Items)
                    {
                        using (SqliteCommand comando = conexion.CreateCommand())
                        {
                            comando.Transaction = transaccion;
                            comando.CommandText =
                                "INSERT INTO items_venta (venta_numero, posicion, nombre, cantidad, costo_unitario, precio_unitario) "
                                + "VALUES ($venta, $posicion, $nombre, $cantidad, $costo, $precio)";
                            comando.Parameters.AddWithValue("$venta", (long)v.Numero);
                            comando.Parameters.AddWithValue("$posicion", (long)posicion);
                            comando.Parameters.AddWithValue("$nombre", item.Nombre);
                            comando.Parameters.AddWithValue("$cantidad", (long)item.Cantidad);
                            comando.Parameters.AddWithValue("$costo", Centavos(item.CostoUnitario));
                            comando.Parameters.AddWithValue("$precio", Centavos(item.PrecioUnitario));
                            comando.ExecuteNonQuery();
                        }
                        posicion++;
                    }
                }
                transaccion.Commit();
            }
        }

        // ---------- Conversiones ----------

        private static long Centavos(decimal pesos)
        {
            return (long)Math.Round(pesos * 100m, 0, MidpointRounding.AwayFromZero);
        }

        private static decimal Pesos(long centavos)
        {
            return centavos / 100m;
        }

        private static DateTime ParsearFecha(string texto)
        {
            return DateTime.ParseExact(texto, FORMATO_FECHA, CultureInfo.InvariantCulture);
        }
    }
}
