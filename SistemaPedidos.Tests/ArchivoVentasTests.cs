using System;
using System.Collections.Generic;
using System.IO;
using SistemaPedidos;
using Xunit;

namespace SistemaPedidos.Tests
{
    // Estas pruebas escriben archivos reales, pero en una carpeta temporal que se borra al terminar.
    public class ArchivoVentasTests : IDisposable
    {
        private readonly string carpeta;

        public ArchivoVentasTests()
        {
            carpeta = Path.Combine(Path.GetTempPath(), "mateverso-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
        }

        public void Dispose()
        {
            Directory.Delete(carpeta, true);
        }

        private string Ruta(string nombre)
        {
            return Path.Combine(carpeta, nombre);
        }

        [Fact]
        public void GuardarYCargar_ConservaTodosLosDatos()
        {
            Venta v = new Venta(7, new DateTime(2026, 11, 5), "Imperial Negro + Bombilla", 14500, 27000,
                                MedioPago.Transferencia, EstadoVenta.Pagado);
            v.FechaSegundoPago = new DateTime(2026, 11, 8);
            v.Bolsa = 300; v.Sticker = 100; v.Tarjeta = 50;
            v.EsRegalo = true;
            v.Items.Add(new ItemVenta("Imperial Negro", 1, 12000, 22000));
            v.Items.Add(new ItemVenta("Bombilla", 2, 1000, 2500));

            string ruta = Ruta("ventas.csv");
            ArchivoVentas.Guardar(new List<Venta> { v }, ruta);
            List<Venta> cargadas = ArchivoVentas.Cargar(ruta);

            Assert.Single(cargadas);
            Venta c = cargadas[0];
            Assert.Equal(7, c.Numero);
            Assert.Equal(new DateTime(2026, 11, 5), c.Fecha);
            Assert.Equal(new DateTime(2026, 11, 8), c.FechaSegundoPago);
            Assert.Equal("Imperial Negro + Bombilla", c.Productos);
            Assert.Equal(14500m, c.CostoTotal);
            Assert.Equal(27000m, c.PrecioVenta);
            Assert.Equal(MedioPago.Transferencia, c.Medio);
            Assert.Equal(EstadoVenta.Pagado, c.Estado);
            Assert.True(c.EsRegalo);
            Assert.Equal(300m, c.Bolsa);
            Assert.Equal(2, c.Items.Count);
            Assert.Equal(2, c.Items[1].Cantidad);
        }

        [Fact]
        public void GuardarYCargar_ConservaNotasConPuntoYComaYComillas()
        {
            Venta v = new Venta(1, new DateTime(2026, 11, 5), "Mate; con \"detalle\"", 1, 2,
                                MedioPago.Efectivo, EstadoVenta.Pedido);
            v.Nota = "Entregar el jueves; llamar antes";

            string ruta = Ruta("ventas.csv");
            ArchivoVentas.Guardar(new List<Venta> { v }, ruta);
            Venta c = ArchivoVentas.Cargar(ruta)[0];

            Assert.Equal("Mate; con \"detalle\"", c.Productos);
            Assert.Equal("Entregar el jueves; llamar antes", c.Nota);
        }

        [Fact]
        public void Cargar_ArchivoInexistente_DevuelveListaVacia()
        {
            Assert.Empty(ArchivoVentas.Cargar(Ruta("no-existe.csv")));
        }

        [Fact]
        public void Cargar_ArchivoViejoSinColumnaDeItems_Funciona()
        {
            string encabezado = "Numero;Fecha;FechaSegundoPago;Productos;CostoTotal;Bolsa;Sticker;Tarjeta;PrecioVenta;MedioPago;Estado;EsRegalo;Nota";
            string fila = "3;2026-10-01;;Mate;1000;0;0;0;2000;MercadoPago;Entregado;No;";
            string ruta = Ruta("viejo.csv");
            File.WriteAllText(ruta, encabezado + "\n" + fila + "\n");

            List<Venta> ventas = ArchivoVentas.Cargar(ruta);

            Assert.Single(ventas);
            Assert.Empty(ventas[0].Items);
            Assert.Equal(1000m, ventas[0].Ganancia);
        }

        [Fact]
        public void Cargar_ConDatoInvalido_LanzaFormatException()
        {
            string encabezado = "Numero;Fecha;FechaSegundoPago;Productos;CostoTotal;Bolsa;Sticker;Tarjeta;PrecioVenta;MedioPago;Estado;EsRegalo;Nota;Items";
            string fila = "3;no-es-fecha;;Mate;1000;0;0;0;2000;MercadoPago;Entregado;No;;";
            string ruta = Ruta("roto.csv");
            File.WriteAllText(ruta, encabezado + "\n" + fila + "\n");

            Assert.Throws<FormatException>(() => ArchivoVentas.Cargar(ruta));
        }

        [Fact]
        public void Guardar_DejaUnBakDelArchivoAnterior()
        {
            string ruta = Ruta("ventas.csv");
            Venta a = new Venta(1, new DateTime(2026, 11, 1), "A", 1, 2, MedioPago.Efectivo, EstadoVenta.Pedido);
            Venta b = new Venta(2, new DateTime(2026, 11, 2), "B", 1, 2, MedioPago.Efectivo, EstadoVenta.Pedido);

            ArchivoVentas.Guardar(new List<Venta> { a }, ruta);
            ArchivoVentas.Guardar(new List<Venta> { a, b }, ruta);

            Assert.Equal(2, ArchivoVentas.Cargar(ruta).Count);
            Assert.Single(ArchivoVentas.Cargar(ruta + ".bak"));
        }
    }
}
