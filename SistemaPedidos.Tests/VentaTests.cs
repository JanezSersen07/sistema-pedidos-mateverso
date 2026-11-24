using System;
using SistemaPedidos;
using Xunit;

namespace SistemaPedidos.Tests
{
    public class VentaTests
    {
        private static Venta NuevaVenta(decimal costo, decimal precio, EstadoVenta estado = EstadoVenta.Pedido)
        {
            return new Venta(1, new DateTime(2026, 11, 1), "Mate Imperial", costo, precio,
                             MedioPago.MercadoPago, estado);
        }

        [Fact]
        public void Ganancia_EsPrecioMenosCosto()
        {
            Venta v = NuevaVenta(12000, 22000);
            Assert.Equal(10000m, v.Ganancia);
        }

        [Fact]
        public void Margen_EsGananciaSobrePrecio()
        {
            Venta v = NuevaVenta(12000, 24000);
            Assert.Equal(50m, v.MargenPorcentaje);
        }

        [Fact]
        public void Margen_ConPrecioCero_NoDividePorCero()
        {
            Venta v = NuevaVenta(500, 0);
            Assert.Equal(0m, v.MargenPorcentaje);
        }

        [Fact]
        public void CambiarEstado_PermiteAvanzar()
        {
            Venta v = NuevaVenta(1, 2, EstadoVenta.Pedido);
            Assert.True(v.CambiarEstado(EstadoVenta.Pagado));
            Assert.Equal(EstadoVenta.Pagado, v.Estado);
            Assert.True(v.CambiarEstado(EstadoVenta.Entregado));
            Assert.Equal(EstadoVenta.Entregado, v.Estado);
        }

        [Fact]
        public void CambiarEstado_NoPermiteRetroceder()
        {
            Venta v = NuevaVenta(1, 2, EstadoVenta.Entregado);
            Assert.False(v.CambiarEstado(EstadoVenta.Pagado));
            Assert.Equal(EstadoVenta.Entregado, v.Estado);
        }

        [Fact]
        public void CambiarEstado_AlMismoEstado_DevuelveFalse()
        {
            Venta v = NuevaVenta(1, 2, EstadoVenta.Pagado);
            Assert.False(v.CambiarEstado(EstadoVenta.Pagado));
        }

        [Fact]
        public void Corregir_PuedeVolverElEstadoAtras()
        {
            Venta v = NuevaVenta(1, 2, EstadoVenta.Entregado);
            v.Corregir(new DateTime(2026, 11, 2), "Termo", 100, 300, MedioPago.Efectivo, EstadoVenta.Pedido);
            Assert.Equal(EstadoVenta.Pedido, v.Estado);
            Assert.Equal("Termo", v.Productos);
            Assert.Equal(200m, v.Ganancia);
            Assert.Equal(MedioPago.Efectivo, v.Medio);
        }
    }
}
