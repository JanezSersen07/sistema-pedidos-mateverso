using System;
using System.Collections.Generic;
using System.IO;
using SistemaPedidos;
using Xunit;

namespace SistemaPedidos.Tests
{
    public class RepositorioSqliteTests : IDisposable
    {
        private readonly string carpeta;
        private readonly string ruta;

        public RepositorioSqliteTests()
        {
            carpeta = Path.Combine(Path.GetTempPath(), "mateverso-sqlite-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ruta = Path.Combine(carpeta, "mateverso.db");
        }

        public void Dispose()
        {
            Directory.Delete(carpeta, true);
        }

        private static Venta V(int numero, string productos = "Imperial", decimal costo = 100, decimal precio = 200)
        {
            return new Venta(numero, new DateTime(2026, 11, 5), productos, costo, precio,
                MedioPago.MercadoPago, EstadoVenta.Entregado);
        }

        // ---------- Base nueva ----------

        [Fact]
        public void BaseNueva_SinDatos_DevuelveListasVaciasYCreaElArchivo()
        {
            Assert.Empty(RepositorioSqlite.CargarVentas(ruta));
            Assert.Empty(RepositorioSqlite.CargarProductos(ruta));
            Assert.True(File.Exists(ruta));
        }

        [Fact]
        public void CreaLaCarpetaSiNoExiste()
        {
            string rutaNueva = Path.Combine(carpeta, "otra", "carpeta", "mateverso.db");
            RepositorioSqlite.GuardarVentas(new List<Venta> { V(1) }, rutaNueva);
            Assert.True(File.Exists(rutaNueva));
        }

        // ---------- Productos ----------

        [Fact]
        public void Productos_SeGuardanYSeLeenIguales()
        {
            List<Producto> productos = new List<Producto>
            {
                new Producto(1, "Algarrobo Roma 2", 26950, 53900),
                new Producto(5, "Bombilla Ñandú \"Pico\"; chata", 1795.5m, 3590.25m)
            };

            RepositorioSqlite.GuardarProductos(productos, ruta);
            List<Producto> leidos = RepositorioSqlite.CargarProductos(ruta);

            Assert.Equal(2, leidos.Count);
            Assert.Equal(1, leidos[0].Id);
            Assert.Equal("Algarrobo Roma 2", leidos[0].Nombre);
            Assert.Equal(26950m, leidos[0].Costo);
            Assert.Equal(53900m, leidos[0].Precio);
            Assert.Equal(5, leidos[1].Id);
            Assert.Equal("Bombilla Ñandú \"Pico\"; chata", leidos[1].Nombre);
            Assert.Equal(1795.5m, leidos[1].Costo);
            Assert.Equal(3590.25m, leidos[1].Precio);
        }

        [Fact]
        public void Productos_GuardarDeNuevoReemplazaSinDuplicar()
        {
            RepositorioSqlite.GuardarProductos(new List<Producto>
            {
                new Producto(1, "A", 1, 2), new Producto(2, "B", 3, 4)
            }, ruta);

            RepositorioSqlite.GuardarProductos(new List<Producto> { new Producto(2, "B", 30, 40) }, ruta);

            List<Producto> leidos = RepositorioSqlite.CargarProductos(ruta);
            Assert.Equal(1, leidos.Count);
            Assert.Equal(30m, leidos[0].Costo);
        }

        // ---------- Ventas ----------

        [Fact]
        public void Venta_ConTodosLosCampos_SeLeeIgual()
        {
            Venta v = new Venta(7, new DateTime(2026, 1, 11), "Imperial + Bombilla", 12345.67m, 23456.89m,
                MedioPago.Transferencia, EstadoVenta.Pagado);
            v.FechaSegundoPago = new DateTime(2026, 2, 3);
            v.Bolsa = 400;
            v.Sticker = 200;
            v.Tarjeta = 180.5m;
            v.EsRegalo = true;
            v.Nota = "Cliente: Ana; paga \"el resto\" mañana\ny retira";

            RepositorioSqlite.GuardarVentas(new List<Venta> { v }, ruta);
            Venta leida = RepositorioSqlite.CargarVentas(ruta)[0];

            Assert.Equal(7, leida.Numero);
            Assert.Equal(new DateTime(2026, 1, 11), leida.Fecha);
            Assert.Equal("Imperial + Bombilla", leida.Productos);
            Assert.Equal(12345.67m, leida.CostoTotal);
            Assert.Equal(23456.89m, leida.PrecioVenta);
            Assert.Equal(MedioPago.Transferencia, leida.Medio);
            Assert.Equal(EstadoVenta.Pagado, leida.Estado);
            Assert.Equal(new DateTime(2026, 2, 3), leida.FechaSegundoPago.Value);
            Assert.Equal(400m, leida.Bolsa);
            Assert.Equal(200m, leida.Sticker);
            Assert.Equal(180.5m, leida.Tarjeta);
            Assert.True(leida.EsRegalo);
            Assert.Equal("Cliente: Ana; paga \"el resto\" mañana\ny retira", leida.Nota);
        }

        [Fact]
        public void Venta_SinSegundoPago_QuedaSinFecha()
        {
            RepositorioSqlite.GuardarVentas(new List<Venta> { V(1) }, ruta);
            Venta leida = RepositorioSqlite.CargarVentas(ruta)[0];
            Assert.False(leida.FechaSegundoPago.HasValue);
            Assert.False(leida.EsRegalo);
            Assert.Equal(0, leida.Items.Count);
        }

        [Fact]
        public void Venta_LosMontosConCentavosSeGuardanExactos()
        {
            // Con decimales comunes (double) 0,1 + 0,2 daría 0,30000000000000004
            Venta v = V(1, "x", 0.1m + 0.2m, 1234.56m);
            RepositorioSqlite.GuardarVentas(new List<Venta> { v }, ruta);
            Venta leida = RepositorioSqlite.CargarVentas(ruta)[0];
            Assert.Equal(0.3m, leida.CostoTotal);
            Assert.Equal(1234.56m, leida.PrecioVenta);
        }

        [Fact]
        public void Items_SeGuardanEnOrdenYConSusValores()
        {
            Venta v = V(1, "A + B + C");
            v.Items.Add(new ItemVenta("Segundo", 2, 100.5m, 250.25m));
            v.Items.Add(new ItemVenta("Primero", 1, 10, 20));
            v.Items.Add(new ItemVenta("Tercero; \"raro\"", 3, 1, 2));

            RepositorioSqlite.GuardarVentas(new List<Venta> { v }, ruta);
            Venta leida = RepositorioSqlite.CargarVentas(ruta)[0];

            Assert.Equal(3, leida.Items.Count);
            Assert.Equal("Segundo", leida.Items[0].Nombre);
            Assert.Equal(2, leida.Items[0].Cantidad);
            Assert.Equal(100.5m, leida.Items[0].CostoUnitario);
            Assert.Equal(250.25m, leida.Items[0].PrecioUnitario);
            Assert.Equal("Primero", leida.Items[1].Nombre);
            Assert.Equal("Tercero; \"raro\"", leida.Items[2].Nombre);
        }

        [Fact]
        public void Items_CadaVentaConservaLosSuyos()
        {
            Venta a = V(1);
            a.Items.Add(new ItemVenta("De A", 1, 1, 2));
            Venta b = V(2);
            b.Items.Add(new ItemVenta("De B 1", 1, 1, 2));
            b.Items.Add(new ItemVenta("De B 2", 1, 1, 2));

            RepositorioSqlite.GuardarVentas(new List<Venta> { a, b }, ruta);
            List<Venta> leidas = RepositorioSqlite.CargarVentas(ruta);

            Assert.Equal(1, leidas[0].Items.Count);
            Assert.Equal(2, leidas[1].Items.Count);
            Assert.Equal("De A", leidas[0].Items[0].Nombre);
        }

        [Fact]
        public void Ventas_SeLeenOrdenadasPorNumero()
        {
            RepositorioSqlite.GuardarVentas(new List<Venta> { V(3), V(1), V(2) }, ruta);
            List<Venta> leidas = RepositorioSqlite.CargarVentas(ruta);
            Assert.Equal(1, leidas[0].Numero);
            Assert.Equal(2, leidas[1].Numero);
            Assert.Equal(3, leidas[2].Numero);
        }

        [Fact]
        public void Ventas_GuardarDeNuevoReemplazaYNoDejaItemsHuerfanos()
        {
            Venta a = V(1);
            a.Items.Add(new ItemVenta("De A", 1, 1, 2));
            Venta b = V(2);
            b.Items.Add(new ItemVenta("De B", 1, 1, 2));
            RepositorioSqlite.GuardarVentas(new List<Venta> { a, b }, ruta);

            // Se borra la venta 1 y se vuelve a guardar
            RepositorioSqlite.GuardarVentas(new List<Venta> { b }, ruta);

            List<Venta> leidas = RepositorioSqlite.CargarVentas(ruta);
            Assert.Equal(1, leidas.Count);
            Assert.Equal(2, leidas[0].Numero);
            Assert.Equal(1, leidas[0].Items.Count);
        }

        [Fact]
        public void Ventas_GuardarListaVaciaBorraTodo()
        {
            RepositorioSqlite.GuardarVentas(new List<Venta> { V(1), V(2) }, ruta);
            RepositorioSqlite.GuardarVentas(new List<Venta>(), ruta);
            Assert.Empty(RepositorioSqlite.CargarVentas(ruta));
        }

        [Fact]
        public void Ventas_UnTextoConComillasYCodigoSqlSeGuardaComoTexto()
        {
            Venta v = V(1, "Robert'); DROP TABLE ventas;--");
            v.Nota = "' OR '1'='1";

            RepositorioSqlite.GuardarVentas(new List<Venta> { v }, ruta);
            Venta leida = RepositorioSqlite.CargarVentas(ruta)[0];

            Assert.Equal("Robert'); DROP TABLE ventas;--", leida.Productos);
            Assert.Equal("' OR '1'='1", leida.Nota);
            Assert.Equal(1, RepositorioSqlite.CargarVentas(ruta).Count);
        }

        [Fact]
        public void Ventas_MuchasVentasSeGuardanYSeLeen()
        {
            List<Venta> ventas = new List<Venta>();
            for (int i = 1; i <= 500; i++)
            {
                Venta v = V(i, "Producto " + i, i, i * 2);
                v.Items.Add(new ItemVenta("Item " + i, 1, i, i * 2));
                ventas.Add(v);
            }

            RepositorioSqlite.GuardarVentas(ventas, ruta);
            List<Venta> leidas = RepositorioSqlite.CargarVentas(ruta);

            Assert.Equal(500, leidas.Count);
            Assert.Equal("Item 500", leidas[499].Items[0].Nombre);
            Assert.Equal(1000m, leidas[499].PrecioVenta);
        }

        // ---------- Transacción: todo o nada ----------

        [Fact]
        public void Ventas_SiFallaAMitadDeCamino_LaBaseQuedaComoEstaba()
        {
            RepositorioSqlite.GuardarVentas(new List<Venta> { V(1, "Original") }, ruta);

            // Dos ventas con el mismo número: la segunda no se puede insertar
            List<Venta> conError = new List<Venta> { V(5, "Nueva"), V(5, "Repetida") };
            Assert.ThrowsAny<Exception>(() => RepositorioSqlite.GuardarVentas(conError, ruta));

            List<Venta> leidas = RepositorioSqlite.CargarVentas(ruta);
            Assert.Equal(1, leidas.Count);
            Assert.Equal("Original", leidas[0].Productos);
        }

        [Fact]
        public void Productos_SiFallaAMitadDeCamino_ElCatalogoQuedaComoEstaba()
        {
            RepositorioSqlite.GuardarProductos(new List<Producto> { new Producto(1, "Original", 1, 2) }, ruta);

            List<Producto> conError = new List<Producto>
            {
                new Producto(9, "Nuevo", 1, 2), new Producto(9, "Repetido", 1, 2)
            };
            Assert.ThrowsAny<Exception>(() => RepositorioSqlite.GuardarProductos(conError, ruta));

            List<Producto> leidos = RepositorioSqlite.CargarProductos(ruta);
            Assert.Equal(1, leidos.Count);
            Assert.Equal("Original", leidos[0].Nombre);
        }

        [Fact]
        public void VentasYProductos_ConviveEnLaMismaBaseSinPisarse()
        {
            RepositorioSqlite.GuardarProductos(new List<Producto> { new Producto(1, "P", 1, 2) }, ruta);
            RepositorioSqlite.GuardarVentas(new List<Venta> { V(1) }, ruta);
            RepositorioSqlite.GuardarVentas(new List<Venta> { V(1), V(2) }, ruta);

            Assert.Equal(1, RepositorioSqlite.CargarProductos(ruta).Count);
            Assert.Equal(2, RepositorioSqlite.CargarVentas(ruta).Count);
        }

        // ---------- Archivo y copias de seguridad ----------

        [Fact]
        public void ElArchivoQuedaLibreDespuesDeUsarlo()
        {
            RepositorioSqlite.GuardarVentas(new List<Venta> { V(1) }, ruta);
            RepositorioSqlite.CargarVentas(ruta);

            // En Windows esto falla si la conexión dejó el archivo tomado
            string movido = Path.Combine(carpeta, "movido.db");
            File.Move(ruta, movido);
            Assert.True(File.Exists(movido));
        }

        [Fact]
        public void CopiaDeSeguridad_SeAbreYTieneLosMismosDatos()
        {
            Venta v = V(1, "Imperial");
            v.Items.Add(new ItemVenta("Imperial", 1, 100, 200));
            RepositorioSqlite.GuardarVentas(new List<Venta> { v }, ruta);
            RepositorioSqlite.GuardarProductos(new List<Producto> { new Producto(1, "Imperial", 100, 200) }, ruta);

            CopiasSeguridad.CopiaDelDia(ruta);

            List<string> copias = CopiasSeguridad.Listar(ruta);
            Assert.Equal(1, copias.Count);
            Assert.EndsWith(".db", copias[0]);
            Assert.Equal(1, RepositorioSqlite.CargarVentas(copias[0]).Count);
            Assert.Equal(1, RepositorioSqlite.CargarVentas(copias[0])[0].Items.Count);
            Assert.Equal(1, RepositorioSqlite.CargarProductos(copias[0]).Count);
        }

        [Fact]
        public void Restaurar_DevuelveLaBaseAlEstadoDeLaCopia()
        {
            RepositorioSqlite.GuardarVentas(new List<Venta> { V(1, "Antes") }, ruta);
            CopiasSeguridad.CopiaDelDia(ruta);
            RepositorioSqlite.GuardarVentas(new List<Venta> { V(1, "Antes"), V(2, "Despues") }, ruta);
            Assert.Equal(2, RepositorioSqlite.CargarVentas(ruta).Count);

            CopiasSeguridad.Restaurar(CopiasSeguridad.Listar(ruta)[0], ruta);

            List<Venta> leidas = RepositorioSqlite.CargarVentas(ruta);
            Assert.Equal(1, leidas.Count);
            Assert.Equal("Antes", leidas[0].Productos);
        }
    }
}
