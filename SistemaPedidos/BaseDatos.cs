//////// BaseDatos.cs ////////
using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace SistemaPedidos
{
    // La base de datos SQLite del programa: un solo archivo (mateverso.db) con las ventas,
    // el detalle de productos de cada venta y el catálogo.
    public static class BaseDatos
    {
        // Abre la base (la crea si no existe) y deja las tablas listas.
        public static SqliteConnection Abrir(string ruta)
        {
            string carpeta = Path.GetDirectoryName(ruta);
            if (!string.IsNullOrEmpty(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            SqliteConnectionStringBuilder datos = new SqliteConnectionStringBuilder();
            datos.DataSource = ruta;
            datos.Mode = SqliteOpenMode.ReadWriteCreate;
            // Sin "pool" de conexiones: así el archivo queda libre apenas se cierra,
            // y se puede copiar (copias de seguridad) o reemplazar (restaurar).
            datos.Pooling = false;

            SqliteConnection conexion = new SqliteConnection(datos.ToString());
            try
            {
                conexion.Open();
                Ejecutar(conexion, "PRAGMA foreign_keys = ON;");
                CrearTablas(conexion);
            }
            catch
            {
                conexion.Dispose();
                throw;
            }
            return conexion;
        }

        // Si hay una transacción en curso, hay que pasarla: todos los comandos que
        // se ejecutan dentro de ella tienen que declararla.
        public static void Ejecutar(SqliteConnection conexion, string sql, SqliteTransaction? transaccion = null)
        {
            using (SqliteCommand comando = conexion.CreateCommand())
            {
                comando.Transaction = transaccion;
                comando.CommandText = sql;
                comando.ExecuteNonQuery();
            }
        }

        // Los montos se guardan en centavos (números enteros) para que nunca
        // aparezcan errores de redondeo, como pasaría con decimales comunes.
        private static void CrearTablas(SqliteConnection conexion)
        {
            Ejecutar(conexion, @"
CREATE TABLE IF NOT EXISTS productos (
    id      INTEGER PRIMARY KEY,
    nombre  TEXT    NOT NULL,
    costo   INTEGER NOT NULL,
    precio  INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS ventas (
    numero              INTEGER PRIMARY KEY,
    fecha               TEXT    NOT NULL,
    fecha_segundo_pago  TEXT,
    productos           TEXT    NOT NULL,
    costo_total         INTEGER NOT NULL,
    bolsa               INTEGER NOT NULL DEFAULT 0,
    sticker             INTEGER NOT NULL DEFAULT 0,
    tarjeta             INTEGER NOT NULL DEFAULT 0,
    precio_venta        INTEGER NOT NULL,
    medio               TEXT    NOT NULL,
    estado              TEXT    NOT NULL,
    es_regalo           INTEGER NOT NULL DEFAULT 0,
    nota                TEXT    NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS items_venta (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    venta_numero     INTEGER NOT NULL REFERENCES ventas(numero) ON DELETE CASCADE,
    posicion         INTEGER NOT NULL,
    nombre           TEXT    NOT NULL,
    cantidad         INTEGER NOT NULL,
    costo_unitario   INTEGER NOT NULL,
    precio_unitario  INTEGER NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_items_venta ON items_venta (venta_numero);

PRAGMA user_version = 1;
");
        }
    }
}
