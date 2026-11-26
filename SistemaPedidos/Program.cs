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
        static CatalogoProductos catalogo;
        static string rutaDatos;
        static string rutaProductos;
 
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
 
            // Los datos se guardan en Documentos\Mateverso, fuera del proyecto,
            // así nunca se suben a GitHub por accidente.
            string documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            rutaDatos = Path.Combine(documentos, "Mateverso", "ventas.csv");
            rutaProductos = Path.Combine(documentos, "Mateverso", "productos.csv");
 
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
 
            try
            {
                catalogo = new CatalogoProductos(ArchivoProductos.Cargar(rutaProductos));
            }
            catch (Exception ex)
            {
                Console.WriteLine("No pude leer el archivo de productos: " + ex.Message);
                Console.WriteLine("Revisalo (" + rutaProductos + ") antes de seguir, para no pisar tus datos.");
                return;
            }

            Console.WriteLine("Datos: " + rutaDatos);
            Console.WriteLine("Ventas cargadas: " + registro.Cantidad);
            Console.WriteLine("Productos en el catálogo: " + catalogo.Cantidad);
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
                    case 8: BuscarVentas(); break;
                    case 9: CorregirVenta(); break;
                    case 10: EliminarVenta(); break;
                    case 11: MenuCatalogo(); break;
                    case 12: ProductosMasVendidos(); break;
                    case 13: MenuCopias(); break;
                    case 14: MenuReportes(); break;
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
            Console.WriteLine("8. Buscar y filtrar ventas");
            Console.WriteLine("9. Corregir una venta");
            Console.WriteLine("10. Eliminar una venta");
            Console.WriteLine("11. Catálogo de productos");
            Console.WriteLine("12. Productos más vendidos");
            Console.WriteLine("13. Copias de seguridad");
            Console.WriteLine("14. Reportes (por mes, por producto, pendientes, Excel)");
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
        static bool TryLeerMonto(string texto, out decimal monto)
        {
            string limpio = texto.Replace("$", "").Replace(".", "").Replace(" ", "");
            return decimal.TryParse(limpio, NumberStyles.None, CultureInfo.InvariantCulture, out monto);
        }
 
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
                decimal monto;
                if (TryLeerMonto(texto, out monto))
                {
                    return monto;
                }
                Console.WriteLine("Escribí solo números, por ejemplo 14990.");
            }
        }
 
        // Acepta 6/1/2026 y 6/1/26
        static bool TryLeerFecha(string texto, out DateTime fecha)
        {
            return DateTime.TryParseExact(texto.Trim(), new string[] { "d/M/yyyy", "d/M/yy" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
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
                if (TryLeerFecha(texto, out fecha))
                {
                    return fecha;
                }
                Console.WriteLine("Usá el formato día/mes/año, por ejemplo 6/1/2026.");
            }
        }
 
        // ---------- Lectura con valor actual (Enter = dejarlo igual) ----------
 
        static string LeerTextoConActual(string etiqueta, string actual)
        {
            Console.Write(etiqueta + " [" + actual + "]: ");
            string texto = Console.ReadLine();
            if (texto == null || texto.Trim() == "")
            {
                return actual;
            }
            return texto.Trim();
        }
 
        static decimal LeerMontoConActual(string etiqueta, decimal actual)
        {
            while (true)
            {
                Console.Write(etiqueta + " [" + Formato.Pesos(actual) + "]: ");
                string texto = Console.ReadLine();
                if (texto == null || texto.Trim() == "")
                {
                    return actual;
                }
                decimal monto;
                if (TryLeerMonto(texto, out monto))
                {
                    return monto;
                }
                Console.WriteLine("Escribí solo números, por ejemplo 14990.");
            }
        }
 
        static DateTime LeerFechaConActual(string etiqueta, DateTime actual)
        {
            while (true)
            {
                Console.Write(etiqueta + " [" + actual.ToString("dd/MM/yyyy") + "]: ");
                string texto = Console.ReadLine();
                if (texto == null || texto.Trim() == "")
                {
                    return actual;
                }
                DateTime fecha;
                if (TryLeerFecha(texto, out fecha))
                {
                    return fecha;
                }
                Console.WriteLine("Usá el formato día/mes/año, por ejemplo 6/1/2026.");
            }
        }
 
        // Fecha opcional para los filtros: Enter = sin fecha
        static DateTime? LeerFechaOpcional(string mensaje)
        {
            while (true)
            {
                Console.Write(mensaje);
                string texto = Console.ReadLine();
                if (texto == null || texto.Trim() == "")
                {
                    return null;
                }
                DateTime fecha;
                if (TryLeerFecha(texto, out fecha))
                {
                    return fecha;
                }
                Console.WriteLine("Usá el formato día/mes/año, por ejemplo 6/1/2026.");
            }
        }
 
        // Número entre minimo y maximo; Enter = valor por defecto
        static int LeerEnteroConActual(string mensaje, int porDefecto, int minimo, int maximo)
        {
            while (true)
            {
                Console.Write(mensaje);
                string texto = Console.ReadLine();
                if (texto == null || texto.Trim() == "")
                {
                    return porDefecto;
                }
                int numero;
                if (int.TryParse(texto, out numero) && numero >= minimo && numero <= maximo)
                {
                    return numero;
                }
                Console.WriteLine("Elegí un número entre " + minimo + " y " + maximo + ".");
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
            string productos;
            decimal costoProductos = 0;
            decimal precioSugerido = 0;
            List<ItemVenta> items = new List<ItemVenta>();

            if (UsarCatalogo())
            {
                items = ElegirProductosDeVenta();
                if (items.Count == 0)
                {
                    Console.WriteLine("Se canceló: no agregaste ningún producto.");
                    return;
                }
                productos = TextoDeProductos(items);
                foreach (ItemVenta item in items)
                {
                    costoProductos += item.CostoTotal;
                    precioSugerido += item.PrecioTotal;
                }
                Console.WriteLine();
                Console.WriteLine("Productos: " + productos);
                Console.WriteLine("Costo de los productos: " + Formato.Pesos(costoProductos));
            }
            else
            {
                Console.Write("Productos (texto libre): ");
                productos = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(productos))
                {
                    Console.WriteLine("Se canceló: falta el detalle de productos.");
                    return;
                }
                costoProductos = LeerMonto("Costo de los productos: ", false);
            }

            decimal bolsa = LeerMonto("Costo de la bolsa (Enter si no hay): ", true);
            decimal sticker = LeerMonto("Costo del sticker (Enter si no hay): ", true);
            decimal tarjeta = LeerMonto("Costo de la tarjeta de curado (Enter si no hay): ", true);

            decimal precioVenta;
            if (items.Count > 0)
            {
                // Con el catálogo se sugiere la suma de precios; se puede cambiar (descuentos, combos)
                precioVenta = LeerMontoConActual("Precio de venta (Enter = el sugerido)", precioSugerido);
            }
            else
            {
                precioVenta = LeerMonto("Precio de venta: ", false);
            }

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
            foreach (ItemVenta item in items)
            {
                venta.Items.Add(item);
            }
 
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
 
        static void BuscarVentas()
        {
            Console.WriteLine("Dejá en blanco (Enter) los filtros que no quieras usar.");
            FiltroVentas filtro = new FiltroVentas();
 
            Console.Write("Texto en productos o nota: ");
            string texto = Console.ReadLine();
            if (texto != null)
            {
                filtro.Texto = texto.Trim();
            }
            filtro.Desde = LeerFechaOpcional("Desde (d/m/aaaa): ");
            filtro.Hasta = LeerFechaOpcional("Hasta (d/m/aaaa): ");
 
            Console.WriteLine("Estado: 1. Pedido  2. Pagado  3. Entregado");
            int estado = LeerEnteroConActual("Estado (Enter = todos): ", 0, 1, 3);
            if (estado > 0)
            {
                filtro.Estado = (EstadoVenta)estado;
            }
 
            Console.WriteLine("Medio: 1. Mercado Pago  2. Efectivo  3. Transferencia  4. Tarjeta web");
            int medio = LeerEnteroConActual("Medio (Enter = todos): ", 0, 1, 4);
            if (medio > 0)
            {
                filtro.Medio = (MedioPago)medio;
            }
 
            List<Venta> resultado = registro.Filtrar(filtro);
            Console.WriteLine();
            if (resultado.Count == 0)
            {
                Console.WriteLine("No hay ventas con esos filtros.");
                return;
            }
 
            const int MAXIMO_MOSTRAR = 50;
            int desde = resultado.Count > MAXIMO_MOSTRAR ? resultado.Count - MAXIMO_MOSTRAR : 0;
            if (desde > 0)
            {
                Console.WriteLine("(Se muestran las últimas " + MAXIMO_MOSTRAR + " de " + resultado.Count + ")");
            }
 
            decimal facturado = 0;
            decimal costos = 0;
            foreach (Venta v in resultado)
            {
                facturado += v.PrecioVenta;
                costos += v.CostoTotal;
            }
            for (int i = desde; i < resultado.Count; i++)
            {
                Console.WriteLine(resultado[i].ALinea());
            }
 
            Console.WriteLine();
            Console.WriteLine("Ventas encontradas: " + resultado.Count);
            Console.WriteLine("Facturado: " + Formato.Pesos(facturado)
                + "   Costos: " + Formato.Pesos(costos)
                + "   Ganancia: " + Formato.Pesos(facturado - costos));
        }
 
        static void CorregirVenta()
        {
            int numero = LeerEntero("Número de venta a corregir: ");
            Venta venta = registro.Buscar(numero);
            if (venta == null)
            {
                Console.WriteLine("No existe esa venta.");
                return;
            }
 
            venta.Mostrar();
            Console.WriteLine();
            Console.WriteLine("Enter deja cada dato como está.");
            if (venta.Items.Count > 0)
            {
                Console.WriteLine("(El detalle de productos del catálogo no cambia al corregir; solo los datos de abajo.)");
            }
 
            DateTime fecha = LeerFechaConActual("Fecha", venta.Fecha);
            string productos = LeerTextoConActual("Productos", venta.Productos);
            decimal costo = LeerMontoConActual("Costo total", venta.CostoTotal);
            decimal precio = LeerMontoConActual("Precio de venta", venta.PrecioVenta);
 
            Console.WriteLine("Medio de pago: 1. Mercado Pago  2. Efectivo  3. Transferencia  4. Tarjeta web");
            int medio = LeerEnteroConActual("Medio [" + venta.Medio + "]: ", (int)venta.Medio, 1, 4);
 
            Console.WriteLine("Estado: 1. Pedido  2. Pagado  3. Entregado");
            int estado = LeerEnteroConActual("Estado [" + venta.Estado + "]: ", (int)venta.Estado, 1, 3);
 
            Console.Write("Nota [" + (venta.Nota == "" ? "vacía" : venta.Nota) + "] (- para borrarla): ");
            string nota = Console.ReadLine();
            if (nota == null || nota.Trim() == "")
            {
                nota = venta.Nota;
            }
            else if (nota.Trim() == "-")
            {
                nota = "";
            }
            else
            {
                nota = nota.Trim();
            }
 
            Console.Write("¿Guardar los cambios? (s/n): ");
            string respuesta = Console.ReadLine();
            if (respuesta == null || respuesta.Trim().ToLower() != "s")
            {
                Console.WriteLine("No se guardó ningún cambio.");
                return;
            }
 
            venta.Corregir(fecha, productos, costo, precio, (MedioPago)medio, (EstadoVenta)estado);
            venta.Nota = nota;
            Guardar();
 
            Console.WriteLine();
            Console.WriteLine("Venta corregida:");
            venta.Mostrar();
        }
 
        static void EliminarVenta()
        {
            int numero = LeerEntero("Número de venta a eliminar: ");
            Venta venta = registro.Buscar(numero);
            if (venta == null)
            {
                Console.WriteLine("No existe esa venta.");
                return;
            }
 
            venta.Mostrar();
            Console.WriteLine();
            Console.Write("Para eliminarla definitivamente escribí ELIMINAR: ");
            string respuesta = Console.ReadLine();
            if (respuesta == null || respuesta.Trim().ToUpperInvariant() != "ELIMINAR")
            {
                Console.WriteLine("No se eliminó nada.");
                return;
            }
 
            registro.Eliminar(numero);
            Guardar();
            Console.WriteLine("Venta #" + numero + " eliminada.");
            Console.WriteLine("La versión anterior queda en las copias de seguridad (opción 13).");
        }
 
        // ---------- Catálogo de productos ----------

        static bool UsarCatalogo()
        {
            if (catalogo.Cantidad == 0)
            {
                Console.WriteLine("(Tip: cargá tus productos en la opción 11 y el costo y el precio se completan solos.)");
                return false;
            }
            Console.Write("¿Cargar los productos desde el catálogo? (s/n, Enter = s): ");
            string respuesta = Console.ReadLine();
            return respuesta == null || respuesta.Trim() == "" || respuesta.Trim().ToLower() == "s";
        }

        static string TextoDeProductos(List<ItemVenta> items)
        {
            List<string> partes = new List<string>();
            foreach (ItemVenta item in items)
            {
                partes.Add(item.ATexto());
            }
            return string.Join(" + ", partes);
        }

        // Pide productos uno por uno (por número o por parte del nombre) hasta un Enter vacío
        static List<ItemVenta> ElegirProductosDeVenta()
        {
            List<ItemVenta> items = new List<ItemVenta>();
            Console.WriteLine("Escribí el número o parte del nombre del producto. Enter vacío = terminar.");

            while (true)
            {
                Console.Write("Producto: ");
                string texto = Console.ReadLine();
                if (texto == null || texto.Trim() == "")
                {
                    break;
                }
                texto = texto.Trim();

                Producto elegido = null;
                int id;
                if (int.TryParse(texto, out id))
                {
                    elegido = catalogo.Buscar(id);
                }
                if (elegido == null)
                {
                    List<Producto> coincidencias = catalogo.BuscarPorTexto(texto);
                    if (coincidencias.Count == 1)
                    {
                        elegido = coincidencias[0];
                    }
                    else if (coincidencias.Count > 1)
                    {
                        Console.WriteLine("Hay " + coincidencias.Count + " productos así. Escribí el número del que querés:");
                        for (int i = 0; i < coincidencias.Count && i < 15; i++)
                        {
                            Console.WriteLine("  " + coincidencias[i].ALinea());
                        }
                        continue;
                    }
                }

                if (elegido != null)
                {
                    int cantidad = LeerEnteroConActual("Cantidad (Enter = 1): ", 1, 1, 99);
                    items.Add(new ItemVenta(elegido.Nombre, cantidad, elegido.Costo, elegido.Precio));
                }
                else
                {
                    Console.Write("No está en el catálogo. ¿Cargarlo a mano en esta venta? (s/n): ");
                    string respuesta = Console.ReadLine();
                    if (respuesta == null || respuesta.Trim().ToLower() != "s")
                    {
                        continue;
                    }
                    string nombre = LeerTextoConActual("Nombre", texto);
                    decimal costo = LeerMonto("Costo (por unidad): ", false);
                    decimal precio = LeerMonto("Precio (por unidad): ", false);
                    int cantidad = LeerEnteroConActual("Cantidad (Enter = 1): ", 1, 1, 99);
                    items.Add(new ItemVenta(nombre, cantidad, costo, precio));
                }

                ItemVenta ultimo = items[items.Count - 1];
                Console.WriteLine("  + " + ultimo.ATexto() + "  (costo " + Formato.Pesos(ultimo.CostoTotal)
                    + ", precio " + Formato.Pesos(ultimo.PrecioTotal) + ")");
            }
            return items;
        }

        static void MenuCatalogo()
        {
            int opcion = -1;
            while (opcion != 0)
            {
                Console.WriteLine("--- CATÁLOGO DE PRODUCTOS (" + catalogo.Cantidad + ") ---");
                Console.WriteLine("1. Ver productos");
                Console.WriteLine("2. Agregar un producto");
                Console.WriteLine("3. Editar un producto");
                Console.WriteLine("4. Eliminar un producto");
                Console.WriteLine("0. Volver");
                opcion = LeerEntero("Opción: ");
                Console.WriteLine();

                switch (opcion)
                {
                    case 1: VerCatalogo(); break;
                    case 2: AgregarProducto(); break;
                    case 3: EditarProducto(); break;
                    case 4: EliminarProducto(); break;
                    case 0: break;
                    default: Console.WriteLine("Opción inválida."); break;
                }
                Console.WriteLine();
            }
        }

        static void VerCatalogo()
        {
            if (catalogo.Cantidad == 0)
            {
                Console.WriteLine("Todavía no hay productos en el catálogo.");
                return;
            }
            foreach (Producto p in catalogo.Productos)
            {
                Console.WriteLine(p.ALinea());
            }
        }

        static void AgregarProducto()
        {
            Console.Write("Nombre del producto: ");
            string nombre = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                Console.WriteLine("Se canceló: falta el nombre.");
                return;
            }
            nombre = nombre.Trim();
            if (catalogo.ExisteNombre(nombre))
            {
                Console.WriteLine("Ya hay un producto con ese nombre.");
                return;
            }

            decimal costo = LeerMonto("Costo: ", false);
            decimal precio = LeerMonto("Precio de venta: ", false);

            Producto producto = catalogo.Agregar(nombre, costo, precio);
            GuardarCatalogo();
            Console.WriteLine("Producto agregado:");
            Console.WriteLine(producto.ALinea());
        }

        static void EditarProducto()
        {
            int id = LeerEntero("Número de producto a editar: ");
            Producto producto = catalogo.Buscar(id);
            if (producto == null)
            {
                Console.WriteLine("No existe ese producto.");
                return;
            }

            Console.WriteLine(producto.ALinea());
            Console.WriteLine("Enter deja cada dato como está. Las ventas anteriores no cambian.");
            string nombre = LeerTextoConActual("Nombre", producto.Nombre);
            decimal costo = LeerMontoConActual("Costo", producto.Costo);
            decimal precio = LeerMontoConActual("Precio de venta", producto.Precio);

            Console.Write("¿Guardar los cambios? (s/n): ");
            string respuesta = Console.ReadLine();
            if (respuesta == null || respuesta.Trim().ToLower() != "s")
            {
                Console.WriteLine("No se guardó ningún cambio.");
                return;
            }

            producto.Corregir(nombre, costo, precio);
            GuardarCatalogo();
            Console.WriteLine("Producto actualizado:");
            Console.WriteLine(producto.ALinea());
        }

        static void EliminarProducto()
        {
            int id = LeerEntero("Número de producto a eliminar: ");
            Producto producto = catalogo.Buscar(id);
            if (producto == null)
            {
                Console.WriteLine("No existe ese producto.");
                return;
            }

            Console.WriteLine(producto.ALinea());
            Console.Write("¿Eliminarlo del catálogo? Las ventas ya cargadas no cambian. (s/n): ");
            string respuesta = Console.ReadLine();
            if (respuesta == null || respuesta.Trim().ToLower() != "s")
            {
                Console.WriteLine("No se eliminó nada.");
                return;
            }

            catalogo.Eliminar(id);
            GuardarCatalogo();
            Console.WriteLine("Producto eliminado.");
        }

        static void ProductosMasVendidos()
        {
            int conDetalle = registro.CantidadConDetalle();
            if (conDetalle == 0)
            {
                Console.WriteLine("Todavía no hay ventas con productos del catálogo.");
                Console.WriteLine("Se cuentan las ventas nuevas cargadas con la opción 2 desde el catálogo.");
                return;
            }

            Console.WriteLine("Ordenar por: 1. Unidades vendidas  2. Ganancia");
            int orden = LeerEnteroConActual("Orden (Enter = 1): ", 1, 1, 2);
            Console.WriteLine();
            registro.MostrarProductosMasVendidos(orden, 15);
            Console.WriteLine();
            Console.WriteLine("Se cuentan " + conDetalle + " de " + registro.Cantidad
                + " ventas (las demás no tienen detalle de productos).");
            Console.WriteLine("La ganancia es de los productos, sin descontar bolsa, sticker ni tarjeta.");
        }

        // ---------- Reportes ----------

        static void MenuReportes()
        {
            if (registro.Cantidad == 0)
            {
                Console.WriteLine("Todavía no hay ventas cargadas.");
                return;
            }

            int opcion = -1;
            while (opcion != 0)
            {
                Console.WriteLine("--- REPORTES ---");
                Console.WriteLine("1. Ganancia por mes (con margen y variación)");
                Console.WriteLine("2. Rentabilidad por producto");
                Console.WriteLine("3. Pendientes: por cobrar y por entregar");
                Console.WriteLine("4. Exportar reportes para Excel");
                Console.WriteLine("0. Volver");
                opcion = LeerEntero("Opción: ");
                Console.WriteLine();

                switch (opcion)
                {
                    case 1: ReporteMensual(); break;
                    case 2: ReporteProductos(); break;
                    case 3: ReportePendientes(); break;
                    case 4: ExportarReportes(); break;
                    case 0: break;
                    default: Console.WriteLine("Opción inválida."); break;
                }
                if (opcion != 0)
                {
                    Console.WriteLine();
                }
            }
        }

        static string TextoVariacion(decimal? variacion)
        {
            if (!variacion.HasValue)
            {
                return "-";
            }
            return variacion.Value.ToString("+0;-0;0", CultureInfo.InvariantCulture) + "%";
        }

        static void ReporteMensual()
        {
            List<FilaMes> meses = Reportes.PorMes(registro.Ventas);
            Console.WriteLine("Mes      Ventas   Facturado     Costos    Ganancia  Margen  Vs. mes anterior");
            foreach (FilaMes f in meses)
            {
                Console.WriteLine("{0}  {1,6}  {2,10}  {3,10}  {4,10}  {5,5}%  {6,10}",
                    f.Mes, f.Ventas, Formato.Pesos(f.Facturado), Formato.Pesos(f.Costos),
                    Formato.Pesos(f.Ganancia), f.Margen.ToString("0", CultureInfo.InvariantCulture),
                    TextoVariacion(f.VariacionGanancia));
            }
            Console.WriteLine();
            Console.WriteLine("Margen = ganancia / facturado. \"Vs. mes anterior\" compara la ganancia");
            Console.WriteLine("solo si el mes anterior del calendario también tuvo ventas con ganancia.");
        }

        static void ReporteProductos()
        {
            int conDetalle = registro.CantidadConDetalle();
            if (conDetalle == 0)
            {
                Console.WriteLine("Todavía no hay ventas con productos del catálogo.");
                Console.WriteLine("Se cuentan las ventas nuevas cargadas con la opción 2 desde el catálogo.");
                return;
            }

            Console.WriteLine("Ordenar por: 1. Unidades  2. Ganancia  3. Margen");
            int orden = LeerEnteroConActual("Orden (Enter = 2): ", 2, 1, 3);
            Console.WriteLine();

            List<FilaProducto> filas = Reportes.PorProducto(registro.Ventas, (OrdenProductos)orden);
            int maximo = 20;
            Console.WriteLine("Producto                                  Unid.   Facturado    Ganancia  Margen");
            for (int i = 0; i < filas.Count && i < maximo; i++)
            {
                FilaProducto f = filas[i];
                string nombre = f.Nombre.Length > 40 ? f.Nombre.Substring(0, 37) + "..." : f.Nombre;
                Console.WriteLine("{0,-40} {1,6}  {2,10}  {3,10}  {4,5}%",
                    nombre, f.Unidades, Formato.Pesos(f.Facturado), Formato.Pesos(f.Ganancia),
                    f.Margen.ToString("0", CultureInfo.InvariantCulture));
            }
            if (filas.Count > maximo)
            {
                Console.WriteLine("(Se muestran los primeros " + maximo + " de " + filas.Count + ")");
            }
            Console.WriteLine();
            Console.WriteLine("Se cuentan " + conDetalle + " de " + registro.Cantidad
                + " ventas (las demás no tienen detalle de productos).");
            Console.WriteLine("La ganancia es de los productos, sin descontar bolsa, sticker ni tarjeta.");
        }

        static void ReportePendientes()
        {
            MostrarPendientes("POR COBRAR (pedidos sin pagar)", EstadoVenta.Pedido);
            Console.WriteLine();
            MostrarPendientes("POR ENTREGAR (pagadas, falta entregar)", EstadoVenta.Pagado);
        }

        static void MostrarPendientes(string titulo, EstadoVenta estado)
        {
            List<Venta> lista = Reportes.Pendientes(registro.Ventas, estado);
            decimal total = 0;
            Console.WriteLine("--- " + titulo + " ---");
            if (lista.Count == 0)
            {
                Console.WriteLine("No hay ventas en este estado.");
                return;
            }
            foreach (Venta v in lista)
            {
                int dias = Reportes.DiasDeEspera(v, DateTime.Today);
                string productos = v.Productos.Length > 38 ? v.Productos.Substring(0, 35) + "..." : v.Productos;
                Console.WriteLine("#{0,-3} {1:dd/MM/yyyy}  hace {2,3} días  {3,9}  {4}",
                    v.Numero, v.Fecha, dias, Formato.Pesos(v.PrecioVenta), productos);
                total += v.PrecioVenta;
            }
            Console.WriteLine("Total: " + Formato.Pesos(total) + " en " + lista.Count + " venta(s). Las más viejas van primero.");
        }

        static void ExportarReportes()
        {
            string documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string carpeta = Path.Combine(documentos, "Mateverso", "reportes");
            string fecha = DateTime.Today.ToString("yyyy-MM-dd");

            string rutaMensual = Path.Combine(carpeta, "reporte-mensual-" + fecha + ".csv");
            Reportes.ExportarMensual(Reportes.PorMes(registro.Ventas), rutaMensual);
            Console.WriteLine("Guardado: " + rutaMensual);

            if (registro.CantidadConDetalle() > 0)
            {
                string rutaProductos = Path.Combine(carpeta, "reporte-productos-" + fecha + ".csv");
                Reportes.ExportarProductos(Reportes.PorProducto(registro.Ventas, OrdenProductos.Ganancia), rutaProductos);
                Console.WriteLine("Guardado: " + rutaProductos);
            }
            else
            {
                Console.WriteLine("No se exportó el reporte de productos: todavía no hay ventas con detalle.");
            }
            Console.WriteLine("Se abren con Excel o Google Sheets (separados por punto y coma).");
        }

        // ---------- Copias de seguridad ----------

        static void MenuCopias()
        {
            Console.WriteLine("--- COPIAS DE SEGURIDAD ---");
            Console.WriteLine("Se guarda una copia por día, automática, en Documentos\\Mateverso\\copias");
            Console.WriteLine("(las últimas 30 de cada archivo).");
            Console.WriteLine("1. Restaurar ventas");
            Console.WriteLine("2. Restaurar catálogo de productos");
            Console.WriteLine("0. Volver");
            int opcion = LeerEntero("Opción: ");
            Console.WriteLine();
            if (opcion == 1)
            {
                Restaurar(rutaDatos, "ventas");
            }
            else if (opcion == 2)
            {
                Restaurar(rutaProductos, "catálogo");
            }
        }

        static void Restaurar(string ruta, string que)
        {
            List<string> copias = CopiasSeguridad.Listar(ruta);
            if (copias.Count == 0)
            {
                Console.WriteLine("Todavía no hay copias de " + que + ". Se crean solas al guardar.");
                return;
            }

            int mostrar = Math.Min(copias.Count, 15);
            for (int i = 0; i < mostrar; i++)
            {
                Console.WriteLine((i + 1) + ". " + Path.GetFileName(copias[i]));
            }
            int elegida = LeerEnteroConActual("Copia a restaurar (Enter = cancelar): ", 0, 0, mostrar);
            if (elegida == 0)
            {
                Console.WriteLine("No se restauró nada.");
                return;
            }
            string copia = copias[elegida - 1];

            // Se comprueba que la copia se pueda leer antes de tocar nada
            try
            {
                if (que == "ventas")
                {
                    ArchivoVentas.Cargar(copia);
                }
                else
                {
                    ArchivoProductos.Cargar(copia);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Esa copia no se puede leer (" + ex.Message + "). No se cambió nada.");
                return;
            }

            Console.Write("Esto reemplaza lo que hay ahora (" + que + "); lo actual se resguarda antes. Escribí RESTAURAR para seguir: ");
            string respuesta = Console.ReadLine();
            if (respuesta == null || respuesta.Trim().ToUpperInvariant() != "RESTAURAR")
            {
                Console.WriteLine("No se restauró nada.");
                return;
            }

            try
            {
                CopiasSeguridad.Restaurar(copia, ruta);
                if (que == "ventas")
                {
                    registro = new RegistroVentas(ArchivoVentas.Cargar(rutaDatos));
                    Console.WriteLine("Listo: " + registro.Cantidad + " ventas restauradas.");
                }
                else
                {
                    catalogo = new CatalogoProductos(ArchivoProductos.Cargar(rutaProductos));
                    Console.WriteLine("Listo: " + catalogo.Cantidad + " productos restaurados.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("No se pudo restaurar: " + ex.Message);
            }
        }

        static void GuardarCatalogo()
        {
            try
            {
                CopiasSeguridad.CopiaDelDia(rutaProductos);
                ArchivoProductos.Guardar(catalogo.Productos, rutaProductos);
            }
            catch (IOException ex)
            {
                Console.WriteLine("¡Ojo! No se pudo guardar el catálogo: " + ex.Message);
                Console.WriteLine("Si tenés el archivo abierto en Excel, cerralo.");
            }
        }

        static void Guardar()
        {
            try
            {
                CopiasSeguridad.CopiaDelDia(rutaDatos);
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