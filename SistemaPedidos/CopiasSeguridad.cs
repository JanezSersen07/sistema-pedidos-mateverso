using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SistemaPedidos
{
    // Copias de seguridad con fecha, en la subcarpeta "copias" junto al archivo de datos.
    // - Una copia por día (la del primer guardado del día, o sea cómo quedó el día anterior).
    // - Se conservan las últimas 30 de cada archivo; las más viejas se borran solas.
    // - Antes de restaurar, el estado actual también se resguarda.
    public static class CopiasSeguridad
    {
        private const int MAXIMO_POR_ARCHIVO = 30;

        private static string Carpeta(string ruta)
        {
            return Path.Combine(Path.GetDirectoryName(ruta) ?? "", "copias");
        }

        // Llamar justo antes de guardar. Nunca debe impedir que se guarde.
        public static void CopiaDelDia(string ruta)
        {
            try
            {
                if (!File.Exists(ruta))
                {
                    return;
                }
                string carpeta = Carpeta(ruta);
                Directory.CreateDirectory(carpeta);
                string nombre = Path.GetFileNameWithoutExtension(ruta);
                string ext = Path.GetExtension(ruta);
                string destino = Path.Combine(carpeta,
                    nombre + "-" + DateTime.Today.ToString("yyyy-MM-dd") + ext);
                if (!File.Exists(destino))
                {
                    File.Copy(ruta, destino);
                    LimpiarViejas(carpeta, nombre, ext);
                }
            }
            catch (Exception)
            {
                // Si falla la copia, igual se guarda: el .bak sigue funcionando.
            }
        }

        private static void LimpiarViejas(string carpeta, string nombre, string ext)
        {
            // Solo las copias diarias (nombre-aaaa-mm-dd.ext); los nombres ordenan por fecha
            List<string> diarias = Directory.GetFiles(carpeta, nombre + "-????-??-??" + ext)
                .OrderByDescending(f => f, StringComparer.Ordinal).ToList();
            for (int i = MAXIMO_POR_ARCHIVO; i < diarias.Count; i++)
            {
                File.Delete(diarias[i]);
            }
        }

        // Copias disponibles de un archivo, la más reciente primero
        public static List<string> Listar(string ruta)
        {
            string carpeta = Carpeta(ruta);
            if (!Directory.Exists(carpeta))
            {
                return new List<string>();
            }
            string nombre = Path.GetFileNameWithoutExtension(ruta);
            string ext = Path.GetExtension(ruta);
            return Directory.GetFiles(carpeta, nombre + "-*" + ext)
                .OrderByDescending(f => Path.GetFileName(f), StringComparer.Ordinal).ToList();
        }

        // Pone la copia en lugar del archivo actual, resguardando antes el actual
        public static void Restaurar(string copia, string ruta)
        {
            if (File.Exists(ruta))
            {
                string carpeta = Carpeta(ruta);
                Directory.CreateDirectory(carpeta);
                string nombre = Path.GetFileNameWithoutExtension(ruta);
                string ext = Path.GetExtension(ruta);
                string previa = Path.Combine(carpeta, nombre + "-antes-de-restaurar-"
                    + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ext);
                File.Copy(ruta, previa, true);
            }
            File.Copy(copia, ruta, true);
        }
    }
}
