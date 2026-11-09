//////// Program.cs ////////
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SistemaPedidos
{
    class Program
    {
        static RegistroVentas registro;
        static string rutaDatos;

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            // Los datos se guardan en Documentos\Mateverso, fuera del proyecto,
            // así nunca se suben a GitHub por accidente.
            string documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            rutaDatos = Path.Combine(documentos, "Mateverso", "ventas.csv");

            try
            {
                registro = new RegistroVentas(ArchivoVentas.Cargar(rutaDatos));
            }
            catch (Exception ex)
            {
                Console.WriteLine("No pude leer el archivo de ventas: " + ex.Message);
                Console.WriteLine("Revisalo (" + rutaDatos + ") antes de seguir, para no pisar tus datos.");
                return;
            }

            Console.WriteLine("Datos: " + rutaDatos);
            Console.WriteLine("Ventas cargadas: " + registro.Cantidad);
            Console.WriteLine();

            int opcion = -1;
            while (opcion != 0)
            {
                MostrarMenu();
                opcion = LeerEntero("Opción: ");
                Console.WriteLine();

                switch (opcion)
                {
                    case 1: VerVentas(); break;
                    case 2: NuevaVenta(); break;
                    case 3: CambiarEstado(); break;
                    case 4: VerDetalle(); break;
                    case 5: Resumen(); break;
                    case 6: ImportarPlanilla(); break;
                    case 7: ExportarCopia(); break;
                    case 0: Console.WriteLine("¡Hasta luego!"); break;
                    default: Console.WriteLine("Opción inválida."); break;
                }
                Console.WriteLine();
            }
        }

        static void MostrarMenu()
        {
            Console.WriteLine("===== VENTAS MATEVERSO =====");
            Console.WriteLine("1. Ver últimas ventas");
            Console.WriteLine("2. Nueva venta");
            Console.WriteLine("3. Cambiar estado de una venta");
            Console.WriteLine("4. Ver detalle de una venta");
            Console.WriteLine("5. Resumen y ganancias");
            Console.WriteLine("6. Importar planilla (CSV de Google Sheets)");
            Console.WriteLine("7. Exportar copia a CSV");
            Console.WriteLine("0. Salir");
        }

        // ---------- Entrada de datos ----------

        static int LeerEntero(string mensaje)
        {
            int numero;
            Console.Write(mensaje);
            string texto = Console.ReadLine();
            while (!int.TryParse(texto, out numero))
            {
                if (texto == null)
                {
                    return 0; // se cerró la entrada
                }
                Console.Write("Escribí un número válido: ");
                texto = Console.ReadLine();
            }
            return numero;
        }

        // Acepta 14990, 14.990 o $14.990
        static decimal LeerMonto(string mensaje, bool puedeSerVacio)
        {
            while (true)
            {
                Console.Write(mensaje);
                string texto = Console.ReadLine();
                if (texto == null)
                {
                    return 0;
                }
                if (texto.Trim() == "" && puedeSerVacio)
                {
                    return 0;
                }
                string limpio = texto.Replace("$", "").Replace(".", "").Replace(" ", "");
                decimal monto;
                if (decimal.TryParse(limpio, NumberStyles.None, CultureInfo.InvariantCulture, out monto))
                {
                    return monto;
                }
                Console.WriteLine("Escribí solo números, por ejemplo 14990.");
            }
        }

        // Fecha vacía = hoy
        static DateTime LeerFecha(string mensaje)
        {
            while (true)
            {
                Console.Write(mensaje);
                string texto = Console.ReadLine();
                if (texto == null || texto.Trim() == "")
                {
                    return DateTime.Today;
                }
                DateTime fecha;
                if (DateTime.TryParseExact(texto.Trim(), new string[] { "d/M/yyyy", "d/M/yy" },
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
                {
                    return fecha;
                }
                Console.WriteLine("Usá el formato día/mes/año, por ejemplo 6/1/2026.");
            }
        }

        // ---------- Opciones del menú ----------

        static void VerVentas()
        {
            if (registro.Cantidad == 0)
            {
                Console.WriteLine("Todavía no hay ventas cargadas.");
                return;
            }
            int cuantas = LeerEntero("¿Cuántas querés ver? (0 = todas): ");
            int desde = 0;
            if (cuantas > 0 && cuantas < registro.Cantidad)
            {
                desde = registro.Cantidad - cuantas;
            }
            for (int i = desde; i < registro.Cantidad; i++)
            {
                Console.WriteLine(registro.Ventas[i].ALinea());
            }
        }

        static void NuevaVenta()
        {
            Console.Write("Productos (texto libre): ");
            string productos = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(productos))
            {
                Console.WriteLine("Se canceló: falta el detalle de productos.");
                return;
            }

            decimal costoProductos = LeerMonto("Costo de los productos: ", false);
            decimal bolsa = LeerMonto("Costo de la bolsa (Enter si no hay): ", true);
            decimal sticker = LeerMonto("Costo del sticker (Enter si no hay): ", true);
            decimal tarjeta = LeerMonto("Costo de la tarjeta de curado (Enter si no hay): ", true);
            decimal precioVenta = LeerMonto("Precio de venta: ", false);

            Console.WriteLine("Medio de pago: 1. Mercado Pago  2. Efectivo  3. Transferencia  4. Tarjeta web");
            int medio = LeerEntero("Medio (0 = Mercado Pago): ");
            if (medio < 1 || medio > 4)
            {
                medio = 1;
            }

            Console.WriteLine("Estado: 1. Pedido  2. Pagado  3. Entregado");
            int estado = LeerEntero("Estado: ");
            if (estado < 1 || estado > 3)
            {
                estado = 1;
            }

            DateTime fecha = LeerFecha("Fecha (Enter = hoy): ");

            Venta venta = new Venta(registro.ProximoNumero(), fecha, productos.Trim(),
                costoProductos + bolsa + sticker + tarjeta, precioVenta,
                (MedioPago)medio, (EstadoVenta)estado);
            venta.Bolsa = bolsa;
            venta.Sticker = sticker;
            venta.Tarjeta = tarjeta;

            registro.Agregar(venta);
            Guardar();

            Console.WriteLine();
            Console.WriteLine("Venta guardada:");
            venta.Mostrar();
        }

        static void CambiarEstado()
        {
            int numero = LeerEntero("Número de venta: ");
            Venta venta = registro.Buscar(numero);
            if (venta == null)
            {
                Console.WriteLine("No existe esa venta.");
                return;
            }

            Console.WriteLine("Estado actual: " + venta.Estado);
            Console.WriteLine("1. Pedido  2. Pagado  3. Entregado");
            int nuevo = LeerEntero("Nuevo estado: ");

            if (nuevo < 1 || nuevo > 3)
            {
                Console.WriteLine("Estado inválido.");
            }
            else if (venta.CambiarEstado((EstadoVenta)nuevo))
            {
                Guardar();
                Console.WriteLine("La venta #" + venta.Numero + " ahora está " + venta.Estado + ".");
            }
            else
            {
                Console.WriteLine("Solo se puede avanzar de estado, no volver para atrás.");
            }
        }

        static void VerDetalle()
        {
            int numero = LeerEntero("Número de venta: ");
            Venta venta = registro.Buscar(numero);
            if (venta == null)
            {
                Console.WriteLine("No existe esa venta.");
                return;
            }
            venta.Mostrar();
        }

        static void Resumen()
        {
            if (registro.Cantidad == 0)
            {
                Console.WriteLine("Todavía no hay ventas cargadas.");
                return;
            }

            Console.WriteLine("--- Por mes ---");
            registro.MostrarResumenPorMes();

            Console.WriteLine();
            Console.WriteLine("--- Totales ---");
            Console.WriteLine("Facturado:     " + Formato.Pesos(registro.TotalVentas()));
            Console.WriteLine("Costos:        " + Formato.Pesos(registro.TotalCostos()));
            Console.WriteLine("Ganancia:      " + Formato.Pesos(registro.GananciaTotal()));
            Console.WriteLine();
            Console.WriteLine("Cobrado:       " + Formato.Pesos(registro.TotalCobrado()));
            Console.WriteLine("Por cobrar:    " + Formato.Pesos(registro.TotalPorCobrar()));
            Console.WriteLine("Por entregar:  " + registro.CantidadConEstado(EstadoVenta.Pagado) + " venta(s) pagadas sin entregar");

            Console.WriteLine();
            Console.WriteLine("--- Por medio de pago ---");
            registro.MostrarResumenPorMedio();
        }

        static void ImportarPlanilla()
        {
            Console.WriteLine("Exportá la hoja VENTAS desde Google Sheets: Archivo > Descargar > Valores separados por comas (.csv).");
            Console.Write("Ruta del archivo CSV: ");
            string ruta = Console.ReadLine();
            if (ruta == null)
            {
                return;
            }
            ruta = ruta.Trim().Trim('"');

            if (!File.Exists(ruta))
            {
                Console.WriteLine("No encontré ese archivo.");
                return;
            }

            if (registro.Cantidad > 0)
            {
                Console.Write("Ya hay " + registro.Cantidad + " ventas cargadas. Importar las REEMPLAZA. ¿Seguro? (s/n): ");
                string respuesta = Console.ReadLine();
                if (respuesta == null || respuesta.Trim().ToLower() != "s")
                {
                    Console.WriteLine("Se canceló la importación.");
                    return;
                }
            }

            ResultadoImportacion resultado = ImportadorPlanilla.Importar(ruta);
            if (resultado.Error != "")
            {
                Console.WriteLine(resultado.Error);
                return;
            }

            registro = new RegistroVentas(resultado.Ventas);
            Guardar();

            Console.WriteLine();
            Console.WriteLine("Ventas importadas: " + resultado.Ventas.Count);
            Console.WriteLine("Filas ignoradas (vacías o leyenda): " + resultado.Ignoradas);
            if (resultado.Avisos.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Para revisar (" + resultado.Avisos.Count + "):");
                foreach (string aviso in resultado.Avisos)
                {
                    Console.WriteLine("  - " + aviso);
                }
            }
        }

        static void ExportarCopia()
        {
            string documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string ruta = Path.Combine(documentos, "Mateverso", "copia-ventas-" + DateTime.Today.ToString("yyyy-MM-dd") + ".csv");
            ArchivoVentas.Guardar(registro.Ventas, ruta);
            Console.WriteLine("Copia guardada en: " + ruta);
            Console.WriteLine("Se abre con Excel o Google Sheets.");
        }

        static void Guardar()
        {
            try
            {
                ArchivoVentas.Guardar(registro.Ventas, rutaDatos);
            }
            catch (IOException ex)
            {
                Console.WriteLine("¡Ojo! No se pudo guardar: " + ex.Message);
                Console.WriteLine("Si tenés el archivo abierto en Excel, cerralo.");
            }
        }
    }
}