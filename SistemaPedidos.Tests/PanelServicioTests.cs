using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SistemaPedidos;
using Xunit;

namespace SistemaPedidos.Tests
{
    public class PanelServicioTests : IDisposable
    {
        private readonly string carpeta;
        private readonly string ruta;
        private readonly PanelServicio servicio;
        private static readonly DateTime HOY = new DateTime(2026, 11, 27);

        public PanelServicioTests()
        {
            carpeta = Path.Combine(Path.GetTempPath(), "mateverso-panel-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ruta = Path.Combine(carpeta, "mateverso.db");
            servicio = new PanelServicio(ruta, () => HOY);

            RepositorioSqlite.GuardarProductos(new List<Producto>
            {
                new Producto(1, "Mate Imperial", 10000, 25000),
                new Producto(2, "Bombilla Acero", 3000, 8000)
            }, ruta);
            RepositorioSqlite.GuardarVentas(new List<Venta>
            {
                new Venta(1, new DateTime(2026, 10, 3), "Mate Imperial", 11000, 25000, MedioPago.MercadoPago, EstadoVenta.Entregado),
                new Venta(2, new DateTime(2026, 11, 10), "Bombilla Acero", 3500, 8000, MedioPago.Efectivo, EstadoVenta.Pedido),
                new Venta(3, new DateTime(2026, 11, 20), "Mate Imperial + Bombilla Acero", 14000, 33000, MedioPago.Transferencia, EstadoVenta.Pagado)
            }, ruta);
        }

        public void Dispose()
        {
            Directory.Delete(carpeta, true);
        }

        // Ejecuta la acción y devuelve el ErrorPanel que tiene que lanzar (falla si no lanza ninguno)
        private static ErrorPanel Falla(Action accion)
        {
            try
            {
                accion();
            }
            catch (ErrorPanel error)
            {
                return error;
            }
            throw new Xunit.Sdk.XunitException("Tenía que dar un error y no lo dio.");
        }

        private static VentaNuevaDto NuevaConItems(int productoId, int cantidad)
        {
            return new VentaNuevaDto
            {
                Items = new List<ItemNuevoDto> { new ItemNuevoDto { ProductoId = productoId, Cantidad = cantidad } },
                Medio = 1,
                Estado = 2
            };
        }

        // ---------- Lectura ----------

        [Fact]
        public void Resumen_SumaTodoYSeparaLoPendiente()
        {
            ResumenDto r = servicio.Resumen();
            Assert.Equal(3, r.Ventas);
            Assert.Equal(66000m, r.Facturado);
            Assert.Equal(28500m, r.Costos);
            Assert.Equal(37500m, r.Ganancia);
            Assert.Equal(58000m, r.Cobrado);
            Assert.Equal(8000m, r.PorCobrar);
            Assert.Equal(1, r.CantidadPorCobrar);
            Assert.Equal(1, r.CantidadPorEntregar);
            Assert.Equal(22000m, r.TicketPromedio);
        }

        [Fact]
        public void Resumen_BaseVacia_NoDividePorCero()
        {
            PanelServicio vacio = new PanelServicio(Path.Combine(carpeta, "otra.db"), () => HOY);
            ResumenDto r = vacio.Resumen();
            Assert.Equal(0, r.Ventas);
            Assert.Equal(0m, r.Margen);
            Assert.Equal(0m, r.TicketPromedio);
        }

        [Fact]
        public void ListarVentas_SinFiltro_VienenLasMasNuevasPrimero()
        {
            List<VentaDto> lista = servicio.ListarVentas("", "", "", 0, 0);
            Assert.Equal(3, lista.Count);
            Assert.Equal(3, lista[0].Numero);
            Assert.Equal(1, lista[2].Numero);
        }

        [Fact]
        public void ListarVentas_FiltraPorTextoEstadoMedioYFechas()
        {
            Assert.Equal(2, servicio.ListarVentas("bombilla", "", "", 0, 0).Count);
            Assert.Equal(1, servicio.ListarVentas("", "", "", 1, 0).Count);
            Assert.Equal(1, servicio.ListarVentas("", "", "", 0, 3).Count);
            Assert.Equal(2, servicio.ListarVentas("", "2026-11-01", "2026-11-30", 0, 0).Count);
            Assert.Equal(1, servicio.ListarVentas("", "", "2026-10-31", 0, 0).Count);
        }

        [Fact]
        public void ListarVentas_FechaMalEscrita_DaError()
        {
            ErrorPanel error = Falla(() => servicio.ListarVentas("", "27/11/2026", "", 0, 0));
            Assert.Contains("aaaa-mm-dd", error.Message);
        }

        [Fact]
        public void ObtenerVenta_Existente_TraeGananciaYDias()
        {
            VentaDto v = servicio.ObtenerVenta(2);
            Assert.Equal(4500m, v.Ganancia);
            Assert.Equal(17, v.DiasDeEspera);
            Assert.Equal("Efectivo", v.MedioNombre);
            Assert.Equal("Pedido", v.EstadoNombre);
        }

        [Fact]
        public void ObtenerVenta_Inexistente_Da404()
        {
            ErrorPanel error = Falla(() => servicio.ObtenerVenta(99));
            Assert.Equal(404, error.Codigo);
        }

        [Fact]
        public void Pendientes_LaMasVieja_Primero_YEntregadoNoSePermite()
        {
            List<VentaDto> porCobrar = servicio.Pendientes(1);
            Assert.Equal(1, porCobrar.Count);
            Assert.Equal(2, porCobrar[0].Numero);
            Assert.Equal(1, servicio.Pendientes(2).Count);
            Falla(() => servicio.Pendientes(3));
        }

        [Fact]
        public void Reportes_PorMes_AgrupaYCalculaVariacion()
        {
            List<FilaMesDto> meses = servicio.ReportePorMes();
            Assert.Equal(2, meses.Count);
            Assert.Equal("2026-10", meses[0].Mes);
            Assert.Equal(14000m, meses[0].Ganancia);
            Assert.Equal(23500m, meses[1].Ganancia);
        }

        // ---------- Crear ventas ----------

        [Fact]
        public void CrearVenta_ConCatalogo_CalculaCostoPrecioYTexto()
        {
            VentaNuevaDto datos = NuevaConItems(1, 2);
            datos.Items.Add(new ItemNuevoDto { ProductoId = 2, Cantidad = 1 });
            datos.Bolsa = 500;
            datos.Sticker = 100;
            datos.Fecha = "2026-11-26";
            VentaDto v = servicio.CrearVenta(datos);

            Assert.Equal(4, v.Numero);
            Assert.Equal("2 x Mate Imperial + Bombilla Acero", v.Productos);
            Assert.Equal(23600m, v.CostoTotal);   // 20000 + 3000 + bolsa 500 + sticker 100
            Assert.Equal(58000m, v.PrecioVenta);  // 50000 + 8000
            Assert.Equal(2, v.Items.Count);
            Assert.Equal(4, servicio.ListarVentas("", "", "", 0, 0).Count);
        }

        [Fact]
        public void CrearVenta_PrecioEditado_GanaElPrecioDelUsuario()
        {
            VentaNuevaDto datos = NuevaConItems(1, 1);
            datos.Precio = 20000;
            Assert.Equal(20000m, servicio.CrearVenta(datos).PrecioVenta);
        }

        [Fact]
        public void CrearVenta_SinFecha_UsaHoy()
        {
            Assert.Equal("2026-11-27", servicio.CrearVenta(NuevaConItems(2, 1)).Fecha);
        }

        [Fact]
        public void CrearVenta_ValoresDelCatalogoQuedanCongelados()
        {
            servicio.CrearVenta(NuevaConItems(1, 1));
            servicio.EditarProducto(1, new ProductoNuevoDto { Nombre = "Mate Imperial", Costo = 99999, Precio = 99999 });
            VentaDto v = servicio.ObtenerVenta(4);
            Assert.Equal(25000m, v.PrecioVenta);
            Assert.Equal(10000m, v.Items[0].CostoUnitario);
        }

        [Fact]
        public void CrearVenta_ManualSinCatalogo_Funciona()
        {
            VentaDto v = servicio.CrearVenta(new VentaNuevaDto
            {
                Productos = "Yerba especial", CostoProductos = 3000, Precio = 6000, Medio = 2, Estado = 1, Nota = "  retira\nel sábado "
            });
            Assert.Equal(3000m, v.Ganancia);
            Assert.Equal("retira el sábado", v.Nota);
            Assert.Empty(v.Items);
        }

        [Fact]
        public void CrearVenta_DatosInvalidos_DanErrorYNoGuardanNada()
        {
            VentaNuevaDto sinNada = new VentaNuevaDto { Medio = 1, Estado = 1 };
            Falla(() => servicio.CrearVenta(sinNada));
            Falla(() => servicio.CrearVenta(NuevaConItems(77, 1)));
            Falla(() => servicio.CrearVenta(NuevaConItems(1, 0)));

            VentaNuevaDto medioMalo = NuevaConItems(1, 1);
            medioMalo.Medio = 9;
            Falla(() => servicio.CrearVenta(medioMalo));

            VentaNuevaDto futura = NuevaConItems(1, 1);
            futura.Fecha = "2026-12-25";
            Falla(() => servicio.CrearVenta(futura));

            VentaNuevaDto precioCero = NuevaConItems(1, 1);
            precioCero.Precio = 0;
            Falla(() => servicio.CrearVenta(precioCero));

            VentaNuevaDto negativa = NuevaConItems(1, 1);
            negativa.Bolsa = -5;
            Falla(() => servicio.CrearVenta(negativa));

            Assert.Equal(3, servicio.ListarVentas("", "", "", 0, 0).Count);
        }

        // ---------- Estado y borrado ----------

        [Fact]
        public void CambiarEstado_Avanza_YNoDejaRetroceder()
        {
            Assert.Equal("Pagado", servicio.CambiarEstado(2, 2).EstadoNombre);
            Assert.Equal("Entregado", servicio.CambiarEstado(2, 3).EstadoNombre);
            ErrorPanel error = Falla(() => servicio.CambiarEstado(2, 1));
            Assert.Equal(409, error.Codigo);
            Assert.Equal("Entregado", servicio.ObtenerVenta(2).EstadoNombre);
        }

        [Fact]
        public void CambiarEstado_VentaInexistente_Da404()
        {
            Assert.Equal(404, Falla(() => servicio.CambiarEstado(99, 2)).Codigo);
        }

        [Fact]
        public void EliminarVenta_LaSaca_YUnSegundoIntentoDa404()
        {
            servicio.EliminarVenta(2);
            List<VentaDto> lista = servicio.ListarVentas("", "", "", 0, 0);
            Assert.Equal(2, lista.Count);
            Assert.Equal(404, Falla(() => servicio.EliminarVenta(2)).Codigo);
        }

        // ---------- Catálogo ----------

        [Fact]
        public void Productos_CrearEditarYEliminar()
        {
            ProductoDto nuevo = servicio.CrearProducto(new ProductoNuevoDto { Nombre = "  Yerba 1kg ", Costo = 4000, Precio = 7000 });
            Assert.Equal(3, nuevo.Id);
            Assert.Equal("Yerba 1kg", nuevo.Nombre);
            Assert.Equal(3000m, nuevo.Ganancia);

            ProductoDto editado = servicio.EditarProducto(3, new ProductoNuevoDto { Nombre = "Yerba 1kg", Costo = 4500, Precio = 8000 });
            Assert.Equal(3500m, editado.Ganancia);

            servicio.EliminarProducto(3);
            Assert.Equal(2, servicio.ListarProductos().Count);
        }

        [Fact]
        public void Productos_NombreRepetidoOInvalido_DaError()
        {
            Assert.Equal(409, Falla(() =>
                servicio.CrearProducto(new ProductoNuevoDto { Nombre = "mate imperial", Costo = 1, Precio = 2 })).Codigo);
            Falla(() => servicio.CrearProducto(new ProductoNuevoDto { Nombre = "  ", Costo = 1, Precio = 2 }));
            Falla(() => servicio.CrearProducto(new ProductoNuevoDto { Nombre = "X", Costo = -1, Precio = 2 }));
            Assert.Equal(409, Falla(() =>
                servicio.EditarProducto(2, new ProductoNuevoDto { Nombre = "Mate Imperial", Costo = 1, Precio = 2 })).Codigo);
            Assert.Equal(404, Falla(() => servicio.EliminarProducto(50)).Codigo);
        }

        [Fact]
        public void Productos_BorrarUnoNoTocaLasVentasViejas()
        {
            servicio.CrearVenta(NuevaConItems(1, 1));
            servicio.EliminarProducto(1);
            Assert.Equal("Mate Imperial", servicio.ObtenerVenta(4).Items[0].Nombre);
        }

        // ---------- Copias ----------

        [Fact]
        public void Guardar_HaceLaCopiaDelDia()
        {
            servicio.CambiarEstado(2, 2);
            Assert.Equal(1, Directory.GetFiles(Path.Combine(carpeta, "copias")).Length);
        }
    }
}
