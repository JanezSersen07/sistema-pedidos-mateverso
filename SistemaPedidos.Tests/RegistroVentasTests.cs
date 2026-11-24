using System;
using System.Collections.Generic;
using SistemaPedidos;
using Xunit;

namespace SistemaPedidos.Tests
{
    public class RegistroVentasTests
    {
        private static Venta V(int numero, string productos, decimal costo, decimal precio,
                               EstadoVenta estado, MedioPago medio = MedioPago.MercadoPago, int dia = 1)
        {
            return new Venta(numero, new DateTime(2026, 11, dia), productos, costo, precio, medio, estado);
        }

        private static RegistroVentas RegistroDeEjemplo()
        {
            return new RegistroVentas(new List<Venta>
            {
                V(1, "Imperial Negro", 12000, 22000, EstadoVenta.Entregado, MedioPago.MercadoPago, 3),
                V(2, "Termo Bolita", 18000, 36000, EstadoVenta.Pagado, MedioPago.Efectivo, 10),
                V(3, "Yerba Kurupi", 3000, 6000, EstadoVenta.Pedido, MedioPago.Transferencia, 20)
            });
        }

        [Fact]
        public void ProximoNumero_ConRegistroVacio_Es1()
        {
            Assert.Equal(1, new RegistroVentas(new List<Venta>()).ProximoNumero());
        }

        [Fact]
        public void ProximoNumero_ContinuaDesdeElMayor()
        {
            Assert.Equal(4, RegistroDeEjemplo().ProximoNumero());
        }

        [Fact]
        public void Agregar_Y_Buscar_Funcionan()
        {
            RegistroVentas r = RegistroDeEjemplo();
            r.Agregar(V(4, "Mesa Matera", 11000, 22000, EstadoVenta.Pedido));
            Assert.Equal(4, r.Cantidad);
            Assert.Equal("Mesa Matera", r.Buscar(4).Productos);
        }

        [Fact]
        public void Buscar_ConNumeroInexistente_DevuelveNull()
        {
            Assert.Null(RegistroDeEjemplo().Buscar(99));
        }

        [Fact]
        public void Eliminar_BorraLaVenta()
        {
            RegistroVentas r = RegistroDeEjemplo();
            Assert.True(r.Eliminar(2));
            Assert.Equal(2, r.Cantidad);
            Assert.Null(r.Buscar(2));
        }

        [Fact]
        public void Eliminar_ConNumeroInexistente_DevuelveFalse()
        {
            RegistroVentas r = RegistroDeEjemplo();
            Assert.False(r.Eliminar(99));
            Assert.Equal(3, r.Cantidad);
        }

        [Fact]
        public void TotalCobrado_SumaPagadasYEntregadas()
        {
            Assert.Equal(58000m, RegistroDeEjemplo().TotalCobrado());
        }

        [Fact]
        public void TotalPorCobrar_SumaSoloLosPedidos()
        {
            Assert.Equal(6000m, RegistroDeEjemplo().TotalPorCobrar());
        }

        [Fact]
        public void GananciaTotal_EsVentasMenosCostos()
        {
            // ventas 64000 - costos 33000
            Assert.Equal(31000m, RegistroDeEjemplo().GananciaTotal());
        }

        [Fact]
        public void CantidadConEstado_CuentaPorEstado()
        {
            RegistroVentas r = RegistroDeEjemplo();
            Assert.Equal(1, r.CantidadConEstado(EstadoVenta.Pedido));
            Assert.Equal(1, r.CantidadConEstado(EstadoVenta.Pagado));
            Assert.Equal(1, r.CantidadConEstado(EstadoVenta.Entregado));
        }

        [Fact]
        public void Filtrar_PorTexto_IgnoraMayusculas()
        {
            FiltroVentas f = new FiltroVentas();
            f.Texto = "termo";
            List<Venta> res = RegistroDeEjemplo().Filtrar(f);
            Assert.Single(res);
            Assert.Equal(2, res[0].Numero);
        }

        [Fact]
        public void Filtrar_PorTextoEnLaNota()
        {
            RegistroVentas r = RegistroDeEjemplo();
            r.Buscar(1).Nota = "Regalo para Ana";
            FiltroVentas f = new FiltroVentas();
            f.Texto = "ana";
            Assert.Single(r.Filtrar(f));
        }

        [Fact]
        public void Filtrar_PorRangoDeFechas_IncluyeLosExtremos()
        {
            FiltroVentas f = new FiltroVentas();
            f.Desde = new DateTime(2026, 11, 3);
            f.Hasta = new DateTime(2026, 11, 10);
            List<Venta> res = RegistroDeEjemplo().Filtrar(f);
            Assert.Equal(2, res.Count);
        }

        [Fact]
        public void Filtrar_PorEstadoYMedio_CombinaLosCriterios()
        {
            FiltroVentas f = new FiltroVentas();
            f.Estado = EstadoVenta.Pagado;
            f.Medio = MedioPago.Efectivo;
            Assert.Single(RegistroDeEjemplo().Filtrar(f));

            f.Medio = MedioPago.Transferencia; // ningún pagado por transferencia
            Assert.Empty(RegistroDeEjemplo().Filtrar(f));
        }

        [Fact]
        public void Filtrar_SinCriterios_DevuelveTodo()
        {
            Assert.Equal(3, RegistroDeEjemplo().Filtrar(new FiltroVentas()).Count);
        }
    }
}
