using System;
using System.Collections.Generic;
using System.IO;
using SistemaPedidos;
using Xunit;

namespace SistemaPedidos.Tests
{
    public class MigracionTests : IDisposable
    {
        private readonly string carpeta;
        private readonly string rutaBase;
        private readonly string csvVentas;
        private readonly string csvProductos;

        public MigracionTests()
        {
            carpeta = Path.Combine(Path.GetTempPath(), "mateverso-migracion-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            rutaBase = Path.Combine(carpeta, "mateverso.db");
            csvVentas = Path.Combine(carpeta, "ventas.csv");
            csvProductos = Path.Combine(carpeta, "productos.csv");
        }

        public void Dispose()
        {
            Directory.Delete(carpeta, true);
        }

        private void CrearCsvDeEjemplo()
        {
            Venta v1 = new Venta(1, new DateTime(2026, 10, 5), "Imperial + Bombilla", 12500, 20000,
                MedioPago.MercadoPago, EstadoVenta.Entregado);
            v1.Bolsa = 500;
            v1.Nota = "con; punto y coma";
            v1.Items.Add(new ItemVenta("Imperial", 1, 10000, 17000));
            v1.Items.Add(new ItemVenta("Bombilla", 1, 2000, 3000));
            Venta v2 = new Venta(2, new DateTime(2026, 11, 1), "Yerba", 3000, 6000,
                MedioPago.Efectivo, EstadoVenta.Pedido);
            v2.FechaSegundoPago = new DateTime(2026, 11, 8);

            ArchivoVentas.Guardar(new List<Venta> { v1, v2 }, csvVentas);
            ArchivoProductos.Guardar(new List<Producto>
            {
                new Producto(1, "Imperial", 10000, 17000),
                new Producto(2, "Bombilla", 2000, 3000),
                new Producto(3, "Yerba", 3000, 6000)
            }, csvProductos);
        }

        [Fact]
        public void SinCsvNiBase_NoHaceNadaYNoCreaLaBase()
        {
            Assert.Null(Migracion.SiHaceFalta(rutaBase, csvVentas, csvProductos));
            Assert.False(File.Exists(rutaBase));
        }

        [Fact]
        public void ConCsv_CreaLaBaseConLosMismosDatos()
        {
            CrearCsvDeEjemplo();

            string mensaje = Migracion.SiHaceFalta(rutaBase, csvVentas, csvProductos);

            Assert.NotNull(mensaje);
            Assert.Contains("2 ventas", mensaje);
            Assert.Contains("3 productos", mensaje);
            Assert.True(File.Exists(rutaBase));

            List<Venta> ventas = RepositorioSqlite.CargarVentas(rutaBase);
            Assert.Equal(2, ventas.Count);
            Assert.Equal("Imperial + Bombilla", ventas[0].Productos);
            Assert.Equal(12500m, ventas[0].CostoTotal);
            Assert.Equal(500m, ventas[0].Bolsa);
            Assert.Equal("con; punto y coma", ventas[0].Nota);
            Assert.Equal(2, ventas[0].Items.Count);
            Assert.Equal(new DateTime(2026, 11, 8), ventas[1].FechaSegundoPago.Value);
            Assert.Equal(EstadoVenta.Pedido, ventas[1].Estado);
            Assert.Equal(3, RepositorioSqlite.CargarProductos(rutaBase).Count);
        }

        [Fact]
        public void LosCsvQuedanIntactos()
        {
            CrearCsvDeEjemplo();
            string ventasAntes = File.ReadAllText(csvVentas);
            string productosAntes = File.ReadAllText(csvProductos);

            Migracion.SiHaceFalta(rutaBase, csvVentas, csvProductos);

            Assert.Equal(ventasAntes, File.ReadAllText(csvVentas));
            Assert.Equal(productosAntes, File.ReadAllText(csvProductos));
        }

        [Fact]
        public void NoDejaElArchivoTemporalDespuesDeMigrar()
        {
            CrearCsvDeEjemplo();
            Migracion.SiHaceFalta(rutaBase, csvVentas, csvProductos);
            Assert.False(File.Exists(rutaBase + ".nueva"));
        }

        [Fact]
        public void SiLaBaseYaExiste_NoMigraNiLaPisa()
        {
            CrearCsvDeEjemplo();
            RepositorioSqlite.GuardarVentas(new List<Venta>
            {
                new Venta(77, new DateTime(2026, 11, 20), "Ya estaba", 1, 2, MedioPago.Efectivo, EstadoVenta.Entregado)
            }, rutaBase);

            Assert.Null(Migracion.SiHaceFalta(rutaBase, csvVentas, csvProductos));

            List<Venta> ventas = RepositorioSqlite.CargarVentas(rutaBase);
            Assert.Equal(1, ventas.Count);
            Assert.Equal(77, ventas[0].Numero);
        }

        [Fact]
        public void MigrarDosVeces_LaSegundaNoHaceNada()
        {
            CrearCsvDeEjemplo();
            Assert.NotNull(Migracion.SiHaceFalta(rutaBase, csvVentas, csvProductos));
            Assert.Null(Migracion.SiHaceFalta(rutaBase, csvVentas, csvProductos));
            Assert.Equal(2, RepositorioSqlite.CargarVentas(rutaBase).Count);
        }

        [Fact]
        public void SoloConCsvDeProductos_MigraElCatalogoSinVentas()
        {
            ArchivoProductos.Guardar(new List<Producto> { new Producto(1, "Solo producto", 1, 2) }, csvProductos);

            string mensaje = Migracion.SiHaceFalta(rutaBase, csvVentas, csvProductos);

            Assert.Contains("0 ventas", mensaje);
            Assert.Equal(1, RepositorioSqlite.CargarProductos(rutaBase).Count);
            Assert.Empty(RepositorioSqlite.CargarVentas(rutaBase));
        }

        [Fact]
        public void ConVentasDuplicadasEnElCsv_FallaYNoDejaUnaBaseAMedias()
        {
            Venta a = new Venta(5, new DateTime(2026, 10, 1), "A", 1, 2, MedioPago.Efectivo, EstadoVenta.Entregado);
            Venta b = new Venta(5, new DateTime(2026, 10, 2), "B", 1, 2, MedioPago.Efectivo, EstadoVenta.Entregado);
            ArchivoVentas.Guardar(new List<Venta> { a, b }, csvVentas);

            Assert.ThrowsAny<Exception>(() => Migracion.SiHaceFalta(rutaBase, csvVentas, csvProductos));

            Assert.False(File.Exists(rutaBase));
            Assert.False(File.Exists(rutaBase + ".nueva"));
        }
    }
}
