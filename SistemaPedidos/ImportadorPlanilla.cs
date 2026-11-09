//////// ImportadorPlanilla.cs ////////
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SistemaPedidos
{
    public class ResultadoImportacion
    {
        public List<Venta> Ventas = new List<Venta>();
        public List<string> Avisos = new List<string>();   // cosas para revisar
        public int Ignoradas;                              // filas vacías o de leyenda
        public string Error = "";                          // si no se pudo leer el archivo
    }

    // Lee la hoja VENTAS exportada como CSV desde Google Sheets.
    // No adivina: lo que no puede leer con seguridad lo informa para que lo revises.
    public static class ImportadorPlanilla
    {
        public static ResultadoImportacion Importar(string ruta)
        {
            ResultadoImportacion resultado = new ResultadoImportacion();

            string texto = File.ReadAllText(ruta);
            List<List<string>> filas = LectorCsv.Leer(texto, LectorCsv.DetectarSeparador(texto));

            // 1) Buscar la fila de encabezados (la que tiene una celda que dice PRODUCTO)
            int filaEncabezado = -1;
            for (int i = 0; i < filas.Count && filaEncabezado == -1; i++)
            {
                foreach (string celda in filas[i])
                {
                    if (celda.Trim().ToUpperInvariant() == "PRODUCTO")
                    {
                        filaEncabezado = i;
                    }
                }
            }
            if (filaEncabezado == -1)
            {
                resultado.Error = "No encontré la fila de encabezados (PRODUCTO, PRECIO TOTAL COSTO...). ¿Es la hoja VENTAS?";
                return resultado;
            }

            // 2) Ubicar cada columna por el nombre de su encabezado
            List<string> encabezado = filas[filaEncabezado];
            int iProducto = BuscarColumna(encabezado, "PRODUCTO");
            int iCosto = BuscarColumna(encabezado, "TOTAL COSTO");
            int iBolsa = BuscarColumna(encabezado, "BOLSA");
            int iStick = BuscarColumna(encabezado, "STICK");
            int iTarjeta = BuscarColumna(encabezado, "TARJETA");
            int iVenta = BuscarColumna(encabezado, "TOTAL VENTA");
            int iMedio = BuscarColumna(encabezado, "METODO");
            if (iMedio == -1) iMedio = BuscarColumna(encabezado, "MÉTODO");
            int iFecha = BuscarColumna(encabezado, "FECHA");
            int iEstado = BuscarColumna(encabezado, "ESTADO");

            if (iProducto == -1 || iCosto == -1 || iVenta == -1 || iMedio == -1 || iFecha == -1 || iEstado == -1)
            {
                resultado.Error = "Faltan columnas. Necesito: PRODUCTO, PRECIO TOTAL COSTO, PRECIO TOTAL VENTA, METODO DE PAGO, FECHA y ESTADO.";
                return resultado;
            }

            // 3) Leer cada fila de datos
            for (int i = filaEncabezado + 1; i < filas.Count; i++)
            {
                List<string> fila = filas[i];
                int numeroFilaPlanilla = i + 1;

                string productos = Celda(fila, iProducto).Trim();
                string costoTexto = Celda(fila, iCosto).Trim();
                string ventaTexto = Celda(fila, iVenta).Trim();

                // Filas vacías o las de la leyenda de colores (no tienen montos)
                if (costoTexto == "" && ventaTexto == "")
                {
                    resultado.Ignoradas++;
                    continue;
                }

                // Costo total
                List<decimal> costos = ExtraerMontos(costoTexto);
                if (costos.Count == 0)
                {
                    resultado.Avisos.Add("Fila " + numeroFilaPlanilla + ": NO importada, no pude leer el costo total (\"" + costoTexto + "\").");
                    continue;
                }
                decimal costoTotal = costos[costos.Count - 1];

                // Precio de venta, con los casos especiales
                string nota = "";
                bool esRegalo = ventaTexto.ToUpperInvariant().Contains("REGALO");
                decimal precioVenta;
                List<decimal> montosVenta = ExtraerMontos(ventaTexto);

                if (esRegalo)
                {
                    precioVenta = 0;
                    nota = ventaTexto;
                    resultado.Avisos.Add("Fila " + numeroFilaPlanilla + ": es un regalo, se importó con venta $0 y el costo como gasto.");
                }
                else if (montosVenta.Count == 0)
                {
                    resultado.Avisos.Add("Fila " + numeroFilaPlanilla + ": NO importada, no pude leer el precio de venta (\"" + ventaTexto + "\").");
                    continue;
                }
                else if (montosVenta.Count > 1)
                {
                    precioVenta = montosVenta[montosVenta.Count - 1];
                    nota = ventaTexto;
                    resultado.Avisos.Add("Fila " + numeroFilaPlanilla + ": el precio de venta tenía varios montos, tomé el último (" + Formato.Pesos(precioVenta) + "). Revisalo.");
                }
                else
                {
                    precioVenta = montosVenta[0];
                }

                // Fechas: una sola, o dos (pago en dos partes)
                string[] partesFecha = Celda(fila, iFecha).Split('-');
                DateTime fecha;
                if (!LeerFecha(partesFecha[0], out fecha))
                {
                    resultado.Avisos.Add("Fila " + numeroFilaPlanilla + ": NO importada, no pude leer la fecha (\"" + Celda(fila, iFecha) + "\").");
                    continue;
                }
                DateTime? segundaFecha = null;
                if (partesFecha.Length > 1)
                {
                    DateTime otra;
                    if (LeerFecha(partesFecha[1], out otra))
                    {
                        segundaFecha = otra;
                    }
                    else
                    {
                        resultado.Avisos.Add("Fila " + numeroFilaPlanilla + ": no pude leer la segunda fecha, solo se guardó la primera.");
                    }
                }

                // Estado y medio de pago
                EstadoVenta estado = LeerEstado(Celda(fila, iEstado), numeroFilaPlanilla, resultado);
                MedioPago medio = LeerMedio(Celda(fila, iMedio), numeroFilaPlanilla, resultado);

                Venta venta = new Venta(resultado.Ventas.Count + 1, fecha, productos, costoTotal, precioVenta, medio, estado);
                venta.FechaSegundoPago = segundaFecha;
                venta.Bolsa = PrimerMonto(Celda(fila, iBolsa));
                venta.Sticker = PrimerMonto(Celda(fila, iStick));
                venta.Tarjeta = PrimerMonto(Celda(fila, iTarjeta));
                venta.EsRegalo = esRegalo;
                venta.Nota = nota;
                resultado.Ventas.Add(venta);
            }
            return resultado;
        }

        // Devuelve el contenido de una celda, o "" si la columna no existe
        private static string Celda(List<string> fila, int indice)
        {
            if (indice < 0 || indice >= fila.Count)
            {
                return "";
            }
            return fila[indice];
        }

        private static int BuscarColumna(List<string> encabezado, string palabra)
        {
            for (int i = 0; i < encabezado.Count; i++)
            {
                if (encabezado[i].ToUpperInvariant().Contains(palabra))
                {
                    return i;
                }
            }
            return -1;
        }

        // Saca todos los montos con formato "$14.990" de un texto.
        // Los puntos son separadores de miles, por eso se ignoran.
        public static List<decimal> ExtraerMontos(string texto)
        {
            List<decimal> montos = new List<decimal>();
            int i = 0;
            while (i < texto.Length)
            {
                if (texto[i] == '$')
                {
                    int j = i + 1;
                    while (j < texto.Length && texto[j] == ' ')
                    {
                        j++;
                    }
                    string digitos = "";
                    while (j < texto.Length && (char.IsDigit(texto[j]) || texto[j] == '.'))
                    {
                        if (char.IsDigit(texto[j]))
                        {
                            digitos = digitos + texto[j];
                        }
                        j++;
                    }
                    if (digitos != "")
                    {
                        montos.Add(decimal.Parse(digitos, CultureInfo.InvariantCulture));
                    }
                    i = j;
                }
                else
                {
                    i++;
                }
            }
            return montos;
        }

        private static decimal PrimerMonto(string texto)
        {
            List<decimal> montos = ExtraerMontos(texto);
            return montos.Count > 0 ? montos[0] : 0;
        }

        // Acepta 6/1/2026 y 30/1/26
        private static bool LeerFecha(string texto, out DateTime fecha)
        {
            string[] formatos = new string[] { "d/M/yyyy", "d/M/yy" };
            return DateTime.TryParseExact(texto.Trim(), formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
        }

        private static EstadoVenta LeerEstado(string texto, int fila, ResultadoImportacion resultado)
        {
            string t = texto.Trim().ToUpperInvariant();
            if (t == "ENTREGADO") return EstadoVenta.Entregado;
            if (t == "PAGADO") return EstadoVenta.Pagado;
            if (t == "PEDIDO" || t == "PENDIENTE" || t == "SIN PAGAR") return EstadoVenta.Pedido;

            resultado.Avisos.Add("Fila " + fila + ": estado desconocido (\"" + texto + "\"), se importó como Pedido.");
            return EstadoVenta.Pedido;
        }

        private static MedioPago LeerMedio(string texto, int fila, ResultadoImportacion resultado)
        {
            string t = texto.Trim().ToUpperInvariant();
            if (t.Contains("MERCADO")) return MedioPago.MercadoPago;
            if (t.Contains("EFECTIVO")) return MedioPago.Efectivo;
            if (t.Contains("TRANSFER")) return MedioPago.Transferencia;
            if (t.Contains("TARJETA")) return MedioPago.TarjetaWeb;

            resultado.Avisos.Add("Fila " + fila + ": medio de pago desconocido (\"" + texto + "\"), se importó como Mercado Pago.");
            return MedioPago.MercadoPago;
        }
    }
}