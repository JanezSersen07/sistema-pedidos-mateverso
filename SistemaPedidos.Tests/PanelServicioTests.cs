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

        // ---------- Editar ventas ----------

        private static VentaEdicionDto Edicion(VentaDto v)
        {
            return new VentaEdicionDto
            {
                Fecha = v.Fecha, Productos = v.Productos, CostoTotal = v.CostoTotal, PrecioVenta = v.PrecioVenta,
                Medio = v.Medio, Estado = v.Estado, EsRegalo = v.EsRegalo, Nota = v.Nota
            };
        }

        [Fact]
        public void EditarVenta_CambiaLosDatosYSeGuardan()
        {
            VentaEdicionDto datos = Edicion(servicio.ObtenerVenta(2));
            datos.PrecioVenta = 9000;
            datos.Nota = "  retira\nel sábado ";
            datos.Medio = 3;
            datos.Fecha = "2026-11-11";
            datos.EsRegalo = true;
            VentaDto v = servicio.EditarVenta(2, datos);

            Assert.Equal(5500m, v.Ganancia);
            Assert.Equal("retira el sábado", v.Nota);
            VentaDto releida = servicio.ObtenerVenta(2);
            Assert.Equal(9000m, releida.PrecioVenta);
            Assert.Equal("Transferencia", releida.MedioNombre);
            Assert.Equal("2026-11-11", releida.Fecha);
            Assert.True(releida.EsRegalo);
        }

        [Fact]
        public void EditarVenta_PermiteCorregirElEstadoEnAmbosSentidos()
        {
            VentaEdicionDto datos = Edicion(servicio.ObtenerVenta(1));
            datos.Estado = 1;
            Assert.Equal("Pedido", servicio.EditarVenta(1, datos).EstadoNombre);
        }

        [Fact]
        public void EditarVenta_NoTocaElDetalleDelCatalogo()
        {
            servicio.CrearVenta(NuevaConItems(1, 1));
            VentaEdicionDto datos = Edicion(servicio.ObtenerVenta(4));
            datos.Productos = "Mate Imperial (corregido)";
            VentaDto v = servicio.EditarVenta(4, datos);
            Assert.Equal(1, v.Items.Count);
            Assert.Equal("Mate Imperial", v.Items[0].Nombre);
        }

        [Fact]
        public void EditarVenta_DatosInvalidos_DanErrorYNoCambianNada()
        {
            VentaEdicionDto base0 = Edicion(servicio.ObtenerVenta(2));

            VentaEdicionDto precio = Edicion(servicio.ObtenerVenta(2));
            precio.PrecioVenta = 0;
            Falla(() => servicio.EditarVenta(2, precio));

            VentaEdicionDto costo = Edicion(servicio.ObtenerVenta(2));
            costo.CostoTotal = -1;
            Falla(() => servicio.EditarVenta(2, costo));

            VentaEdicionDto sinTexto = Edicion(servicio.ObtenerVenta(2));
            sinTexto.Productos = "  ";
            Falla(() => servicio.EditarVenta(2, sinTexto));

            VentaEdicionDto fecha = Edicion(servicio.ObtenerVenta(2));
            fecha.Fecha = "2027-01-01";
            Falla(() => servicio.EditarVenta(2, fecha));
            fecha.Fecha = "";
            Falla(() => servicio.EditarVenta(2, fecha));

            VentaEdicionDto medio = Edicion(servicio.ObtenerVenta(2));
            medio.Medio = 7;
            Falla(() => servicio.EditarVenta(2, medio));

            Assert.Equal(404, Falla(() => servicio.EditarVenta(99, base0)).Codigo);
            Assert.Equal(8000m, servicio.ObtenerVenta(2).PrecioVenta);
        }

        // ---------- Copias de seguridad ----------

        [Fact]
        public void Copias_CrearManualYListar()
        {
            Assert.Equal(0, servicio.ListarCopias().Count);
            CopiaDto copia = servicio.CrearCopia();
            Assert.Contains("-manual-", copia.Nombre);
            Assert.True(copia.Kilobytes >= 1);
            Assert.Equal(1, servicio.ListarCopias().Count);
        }

        [Fact]
        public void Copias_RestaurarDevuelveLosDatosYResguardaLoActual()
        {
            CopiaDto copia = servicio.CrearCopia();
            servicio.EliminarVenta(1);
            servicio.EliminarVenta(2);
            Assert.Equal(1, servicio.ListarVentas("", "", "", 0, 0).Count);

            servicio.RestaurarCopia(new RestauracionDto { Nombre = copia.Nombre, Confirmacion = "restaurar" });

            Assert.Equal(3, servicio.ListarVentas("", "", "", 0, 0).Count);
            Assert.True(servicio.ListarCopias().Any(c => c.Nombre.Contains("antes-de-restaurar")));
        }

        [Fact]
        public void Copias_RestaurarSinConfirmarONombreRaro_NoHaceNada()
        {
            CopiaDto copia = servicio.CrearCopia();
            servicio.EliminarVenta(1);

            Falla(() => servicio.RestaurarCopia(new RestauracionDto { Nombre = copia.Nombre, Confirmacion = "si" }));
            Falla(() => servicio.RestaurarCopia(new RestauracionDto { Nombre = copia.Nombre, Confirmacion = "" }));
            Assert.Equal(404, Falla(() => servicio.RestaurarCopia(new RestauracionDto { Nombre = "..\\mateverso.db", Confirmacion = "RESTAURAR" })).Codigo);
            Assert.Equal(404, Falla(() => servicio.RestaurarCopia(new RestauracionDto { Nombre = "no-existe.db", Confirmacion = "RESTAURAR" })).Codigo);
            Assert.Equal(2, servicio.ListarVentas("", "", "", 0, 0).Count);
        }

        // ---------- Exportar ----------

        [Fact]
        public void Exportar_GeneraCsvConEncabezadoYFilas()
        {
            string mensual = System.Text.Encoding.UTF8.GetString(servicio.CsvMensual());
            Assert.Contains("Mes;Ventas;Facturado", mensual);
            Assert.Contains("2026-10", mensual);

            string productos = System.Text.Encoding.UTF8.GetString(servicio.CsvProductos(2));
            Assert.Contains("Producto;Unidades", productos);

            string ventas = System.Text.Encoding.UTF8.GetString(servicio.CsvVentas());
            Assert.Contains("Bombilla Acero", ventas);
        }

        [Fact]
        public void Exportar_NoDejaArchivosTemporalesNiTocaLosDatos()
        {
            string[] antes = Directory.GetFiles(Path.GetTempPath(), "mateverso-????????????????????????????????.csv");
            servicio.CsvMensual();
            servicio.CsvVentas();
            string[] despues = Directory.GetFiles(Path.GetTempPath(), "mateverso-????????????????????????????????.csv");
            Assert.Equal(antes.Length, despues.Length);
            Assert.Equal(3, servicio.ListarVentas("", "", "", 0, 0).Count);
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
