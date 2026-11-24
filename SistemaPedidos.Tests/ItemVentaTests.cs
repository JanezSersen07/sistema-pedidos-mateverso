using System;
using SistemaPedidos;
using Xunit;

namespace SistemaPedidos.Tests
{
    public class ItemVentaTests
    {
        [Fact]
        public void Totales_MultiplicanPorCantidad()
        {
            ItemVenta item = new ItemVenta("Bombilla", 3, 2000, 4000);
            Assert.Equal(6000m, item.CostoTotal);
            Assert.Equal(12000m, item.PrecioTotal);
            Assert.Equal(6000m, item.Ganancia);
        }

        [Fact]
        public void ATexto_ConUnaUnidad_MuestraSoloElNombre()
        {
            Assert.Equal("Mate Imperial", new ItemVenta("Mate Imperial", 1, 1, 2).ATexto());
        }

        [Fact]
        public void ATexto_ConVariasUnidades_MuestraLaCantidad()
        {
            Assert.Equal("2 x Bombilla", new ItemVenta("Bombilla", 2, 1, 2).ATexto());
        }

        [Fact]
        public void SerializarYLeer_DevuelveElMismoItem()
        {
            ItemVenta original = new ItemVenta("Termo 1,5lt", 2, 22000.5m, 44000);
            ItemVenta leido = ItemVenta.Leer(original.Serializar());
            Assert.Equal("Termo 1,5lt", leido.Nombre);
            Assert.Equal(2, leido.Cantidad);
            Assert.Equal(22000.5m, leido.CostoUnitario);
            Assert.Equal(44000m, leido.PrecioUnitario);
        }

        [Fact]
        public void Nombre_NoGuardaLosSimbolosReservadosDelArchivo()
        {
            ItemVenta item = new ItemVenta("A|B~C", 1, 1, 2);
            Assert.DoesNotContain("|", item.Nombre);
            Assert.DoesNotContain("~", item.Nombre);
        }

        [Fact]
        public void Leer_ConFormatoInvalido_LanzaFormatException()
        {
            Assert.Throws<FormatException>(() => ItemVenta.Leer("solo~tres~partes"));
        }
    }
}
