using System;
using System.Collections.Generic;
using System.IO;
using SistemaPedidos;
using Xunit;

namespace SistemaPedidos.Tests
{
    public class ReportesTests : IDisposable
    {
        private readonly string carpeta;

        public ReportesTests()
        {
            carpeta = Path.Combine(Path.GetTempPath(), "mateverso-reportes-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
        }

        public void Dispose()
        {
            Directory.Delete(carpeta, true);
        }

        private static Venta V(int numero, int anio, int mes, int dia, decimal costo, decimal precio,
                               EstadoVenta estado = EstadoVenta.Entregado)
        {
            return new Venta(numero, new DateTime(anio, mes, dia), "Venta " + numero, costo, precio,
                MedioPago.MercadoPago, estado);
        }

        private static Venta ConItems(int numero, int mes, params ItemVenta[] items)
        {
            Venta v = V(numero, 2026, mes, 1, 0, 0);
            foreach (ItemVenta item in items)
            {
                v.Items.Add(item);
            }
            return v;
        }

        // ---------- PorMes ----------

        [Fact]
        public void PorMes_SinVentas_DevuelveListaVacia()
        {
            Assert.Empty(Reportes.PorMes(new List<Venta>()));
        }

        [Fact]
        public void PorMes_AgrupaYSumaPorMes()
        {
            List<Venta> ventas = new List<Venta>
            {
                V(1, 2026, 10, 5, 100, 200),
                V(2, 2026, 10, 20, 50, 150),
                V(3, 2026, 11, 2, 300, 400)
            };

            List<FilaMes> meses = Reportes.PorMes(ventas);

            Assert.Equal(2, meses.Count);
            Assert.Equal("2026-10", meses[0].Mes);
            Assert.Equal(2, meses[0].Ventas);
            Assert.Equal(350m, meses[0].Facturado);
            Assert.Equal(150m, meses[0].Costos);
            Assert.Equal(200m, meses[0].Ganancia);
            Assert.Equal("2026-11", meses[1].Mes);
            Assert.Equal(1, meses[1].Ventas);
        }

        [Fact]
        public void PorMes_OrdenaCronologicamenteAunqueLasVentasEstenDesordenadas()
        {
            List<Venta> ventas = new List<Venta>
            {
                V(1, 2026, 11, 2, 0, 10),
                V(2, 2025, 12, 2, 0, 10),
                V(3, 2026, 1, 2, 0, 10)
            };

            List<FilaMes> meses = Reportes.PorMes(ventas);

            Assert.Equal("2025-12", meses[0].Mes);
            Assert.Equal("2026-01", meses[1].Mes);
            Assert.Equal("2026-11", meses[2].Mes);
        }

        [Fact]
        public void PorMes_CalculaElMargen()
        {
            FilaMes mes = Reportes.PorMes(new List<Venta> { V(1, 2026, 11, 1, 60, 100) })[0];
            Assert.Equal(40m, mes.Margen);
        }

        [Fact]
        public void PorMes_ConFacturadoCero_MargenEsCeroSinFallar()
        {
            FilaMes mes = Reportes.PorMes(new List<Venta> { V(1, 2026, 11, 1, 50, 0) })[0];
            Assert.Equal(0m, mes.Margen);
        }

        [Fact]
        public void PorMes_PrimerMes_NoTieneVariacion()
        {
            FilaMes mes = Reportes.PorMes(new List<Venta> { V(1, 2026, 11, 1, 0, 100) })[0];
            Assert.Null(mes.VariacionGanancia);
        }

        [Fact]
        public void PorMes_VariacionCuandoLaGananciaSube()
        {
            List<Venta> ventas = new List<Venta>
            {
                V(1, 2026, 10, 1, 0, 100),  // ganancia 100
                V(2, 2026, 11, 1, 0, 150)   // ganancia 150
            };
            Assert.Equal(50m, Reportes.PorMes(ventas)[1].VariacionGanancia);
        }

        [Fact]
        public void PorMes_VariacionCuandoLaGananciaBaja()
        {
            List<Venta> ventas = new List<Venta>
            {
                V(1, 2026, 10, 1, 0, 200),
                V(2, 2026, 11, 1, 0, 100)
            };
            Assert.Equal(-50m, Reportes.PorMes(ventas)[1].VariacionGanancia);
        }

        [Fact]
        public void PorMes_VariacionFuncionaAlCambiarDeAnio()
        {
            List<Venta> ventas = new List<Venta>
            {
                V(1, 2025, 12, 1, 0, 100),
                V(2, 2026, 1, 1, 0, 120)
            };
            Assert.Equal(20m, Reportes.PorMes(ventas)[1].VariacionGanancia);
        }

        [Fact]
        public void PorMes_SinVariacionSiElMesAnteriorNoEsElDeAntesEnElCalendario()
        {
            List<Venta> ventas = new List<Venta>
            {
                V(1, 2026, 8, 1, 0, 100),
                V(2, 2026, 11, 1, 0, 300)   // septiembre y octubre sin ventas
            };
            Assert.Null(Reportes.PorMes(ventas)[1].VariacionGanancia);
        }

        [Fact]
        public void PorMes_SinVariacionSiElMesAnteriorNoTuvoGanancia()
        {
            List<Venta> ventas = new List<Venta>
            {
                V(1, 2026, 10, 1, 100, 100),  // ganancia 0
                V(2, 2026, 11, 1, 0, 100)
            };
            Assert.Null(Reportes.PorMes(ventas)[1].VariacionGanancia);
        }

        // ---------- PorProducto ----------

        [Fact]
        public void PorProducto_SinDetalle_DevuelveListaVacia()
        {
            List<Venta> ventas = new List<Venta> { V(1, 2026, 11, 1, 10, 20) };
            Assert.Empty(Reportes.PorProducto(ventas, OrdenProductos.Unidades));
        }

        [Fact]
        public void PorProducto_SumaLoDeVariasVentas()
        {
            List<Venta> ventas = new List<Venta>
            {
                ConItems(1, 10, new ItemVenta("Imperial", 1, 100, 200)),
                ConItems(2, 11, new ItemVenta("Imperial", 2, 100, 200), new ItemVenta("Bombilla", 1, 10, 30))
            };

            List<FilaProducto> filas = Reportes.PorProducto(ventas, OrdenProductos.Unidades);

            Assert.Equal(2, filas.Count);
            Assert.Equal("Imperial", filas[0].Nombre);
            Assert.Equal(3, filas[0].Unidades);
            Assert.Equal(600m, filas[0].Facturado);
            Assert.Equal(300m, filas[0].Costos);
            Assert.Equal(300m, filas[0].Ganancia);
            Assert.Equal(50m, filas[0].Margen);
        }

        [Fact]
        public void PorProducto_OrdenaPorUnidades()
        {
            List<Venta> ventas = new List<Venta>
            {
                ConItems(1, 11, new ItemVenta("Caro", 1, 100, 1000), new ItemVenta("Barato", 5, 1, 2))
            };
            List<FilaProducto> filas = Reportes.PorProducto(ventas, OrdenProductos.Unidades);
            Assert.Equal("Barato", filas[0].Nombre);
        }

        [Fact]
        public void PorProducto_OrdenaPorGanancia()
        {
            List<Venta> ventas = new List<Venta>
            {
                ConItems(1, 11, new ItemVenta("Caro", 1, 100, 1000), new ItemVenta("Barato", 5, 1, 2))
            };
            List<FilaProducto> filas = Reportes.PorProducto(ventas, OrdenProductos.Ganancia);
            Assert.Equal("Caro", filas[0].Nombre);
        }

        [Fact]
        public void PorProducto_OrdenaPorMargen()
        {
            List<Venta> ventas = new List<Venta>
            {
                // Grande: gana mucho pero con margen 20%; Chico: gana poco pero con margen 80%
                ConItems(1, 11, new ItemVenta("Grande", 1, 800, 1000), new ItemVenta("Chico", 1, 20, 100))
            };
            List<FilaProducto> filas = Reportes.PorProducto(ventas, OrdenProductos.Margen);
            Assert.Equal("Chico", filas[0].Nombre);
            Assert.Equal(80m, filas[0].Margen);
        }

        [Fact]
        public void PorProducto_ConEmpateDesempataPorNombre()
        {
            List<Venta> ventas = new List<Venta>
            {
                ConItems(1, 11, new ItemVenta("B", 1, 10, 20), new ItemVenta("A", 1, 10, 20))
            };
            List<FilaProducto> filas = Reportes.PorProducto(ventas, OrdenProductos.Unidades);
            Assert.Equal("A", filas[0].Nombre);
            Assert.Equal("B", filas[1].Nombre);
        }

        // ---------- Pendientes ----------

        [Fact]
        public void Pendientes_FiltraPorEstadoYOrdenaDeMasViejaAMasNueva()
        {
            List<Venta> ventas = new List<Venta>
            {
                V(1, 2026, 11, 20, 0, 100, EstadoVenta.Pedido),
                V(2, 2026, 11, 5, 0, 100, EstadoVenta.Pedido),
                V(3, 2026, 11, 1, 0, 100, EstadoVenta.Entregado),
                V(4, 2026, 11, 10, 0, 100, EstadoVenta.Pagado)
            };

            List<Venta> porCobrar = Reportes.Pendientes(ventas, EstadoVenta.Pedido);

            Assert.Equal(2, porCobrar.Count);
            Assert.Equal(2, porCobrar[0].Numero);
            Assert.Equal(1, porCobrar[1].Numero);
            Assert.Equal(1, Reportes.Pendientes(ventas, EstadoVenta.Pagado).Count);
        }

        [Fact]
        public void Pendientes_SinNingunoEnEseEstado_DevuelveListaVacia()
        {
            List<Venta> ventas = new List<Venta> { V(1, 2026, 11, 1, 0, 100, EstadoVenta.Entregado) };
            Assert.Empty(Reportes.Pendientes(ventas, EstadoVenta.Pedido));
        }

        [Fact]
        public void DiasDeEspera_CuentaLosDiasDesdeLaVenta()
        {
            Venta v = V(1, 2026, 11, 1, 0, 100);
            Assert.Equal(25, Reportes.DiasDeEspera(v, new DateTime(2026, 11, 26)));
        }

        [Fact]
        public void DiasDeEspera_IgnoraLaHora()
        {
            Venta v = V(1, 2026, 11, 1, 0, 100);
            Assert.Equal(1, Reportes.DiasDeEspera(v, new DateTime(2026, 11, 2, 23, 59, 0)));
        }

        [Fact]
        public void DiasDeEspera_ConFechaFutura_EsCero()
        {
            Venta v = V(1, 2026, 12, 1, 0, 100);
            Assert.Equal(0, Reportes.DiasDeEspera(v, new DateTime(2026, 11, 26)));
        }

        // ---------- Exportar ----------

        [Fact]
        public void ExportarMensual_EscribeEncabezadoYFilasConComaDecimal()
        {
            List<Venta> ventas = new List<Venta>
            {
                V(1, 2026, 10, 1, 0, 100),
                V(2, 2026, 11, 1, 50, 150)   // ganancia 100 -> variación 0; margen 66,67
            };
            string ruta = Path.Combine(carpeta, "mensual.csv");

            Reportes.ExportarMensual(Reportes.PorMes(ventas), ruta);

            string[] lineas = File.ReadAllLines(ruta);
            Assert.Equal("Mes;Ventas;Facturado;Costos;Ganancia;Margen %;Variacion ganancia %", lineas[0]);
            Assert.Equal("2026-10;1;100;0;100;100;", lineas[1]);
            Assert.Equal("2026-11;1;150;50;100;66,67;0", lineas[2]);
        }

        [Fact]
        public void ExportarMensual_CreaLaCarpetaSiNoExiste()
        {
            string ruta = Path.Combine(carpeta, "sub", "carpeta", "mensual.csv");
            Reportes.ExportarMensual(Reportes.PorMes(new List<Venta> { V(1, 2026, 11, 1, 0, 100) }), ruta);
            Assert.True(File.Exists(ruta));
        }

        [Fact]
        public void ExportarMensual_GuardaConBomParaQueExcelLeaLasTildes()
        {
            string ruta = Path.Combine(carpeta, "mensual.csv");
            Reportes.ExportarMensual(new List<FilaMes>(), ruta);
            byte[] bytes = File.ReadAllBytes(ruta);
            Assert.Equal(0xEF, (int)bytes[0]);
            Assert.Equal(0xBB, (int)bytes[1]);
            Assert.Equal(0xBF, (int)bytes[2]);
        }

        [Fact]
        public void ExportarProductos_EscribeEncabezadoYFilas()
        {
            List<Venta> ventas = new List<Venta>
            {
                ConItems(1, 11, new ItemVenta("Mate Imperial", 2, 100, 250))
            };
            string ruta = Path.Combine(carpeta, "productos.csv");

            Reportes.ExportarProductos(Reportes.PorProducto(ventas, OrdenProductos.Ganancia), ruta);

            string[] lineas = File.ReadAllLines(ruta);
            Assert.Equal("Producto;Unidades;Facturado;Costos;Ganancia;Margen %", lineas[0]);
            Assert.Equal("Mate Imperial;2;500;200;300;60", lineas[1]);
        }

        [Fact]
        public void ExportarProductos_EscapaNombresConPuntoYComa()
        {
            List<Venta> ventas = new List<Venta>
            {
                ConItems(1, 11, new ItemVenta("Termo; negro", 1, 10, 20))
            };
            string ruta = Path.Combine(carpeta, "productos.csv");

            Reportes.ExportarProductos(Reportes.PorProducto(ventas, OrdenProductos.Unidades), ruta);

            Assert.Equal("\"Termo; negro\";1;20;10;10;50", File.ReadAllLines(ruta)[1]);
        }
    }
}
