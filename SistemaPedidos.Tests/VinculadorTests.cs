using System;
using System.Collections.Generic;
using SistemaPedidos;
using Xunit;

namespace SistemaPedidos.Tests
{
    public class VinculadorTests
    {
        private static CatalogoProductos CatalogoDeEjemplo()
        {
            return new CatalogoProductos(new List<Producto>
            {
                new Producto(1, "Mate Imperial Negro", 12000, 22000),
                new Producto(2, "Mate Imperial Rojo", 12000, 26000),
                new Producto(3, "Bombilla Pico de Loro", 1000, 4000),
                new Producto(4, "Yerba Kurupi", 3000, 6000),
                new Producto(5, "Termo Stanley Verde", 40000, 80000)
            });
        }

        private static Venta VentaVieja(string productos, decimal costo, decimal precio)
        {
            return new Venta(1, new DateTime(2026, 10, 1), productos, costo, precio,
                MedioPago.MercadoPago, EstadoVenta.Entregado);
        }

        // ---------- Partes y cantidades ----------

        [Fact]
        public void Partes_SeparaPorMasYLimpiaEspacios()
        {
            List<string> partes = Vinculador.Partes(" Imperial Negro +Bombilla  + Yerba ");
            Assert.Equal(3, partes.Count);
            Assert.Equal("Imperial Negro", partes[0]);
            Assert.Equal("Bombilla", partes[1]);
            Assert.Equal("Yerba", partes[2]);
        }

        [Fact]
        public void Partes_IgnoraPartesVacias()
        {
            Assert.Equal(1, Vinculador.Partes("Imperial + + ").Count);
            Assert.Empty(Vinculador.Partes("   "));
            Assert.Empty(Vinculador.Partes(null));
        }

        [Fact]
        public void QuitarCantidad_SinCantidad_EsUna()
        {
            int cantidad;
            Assert.Equal("Bombilla", Vinculador.QuitarCantidad("Bombilla", out cantidad));
            Assert.Equal(1, cantidad);
        }

        [Fact]
        public void QuitarCantidad_AlPrincipio()
        {
            int cantidad;
            Assert.Equal("Bombilla", Vinculador.QuitarCantidad("2 x Bombilla", out cantidad));
            Assert.Equal(2, cantidad);
            Assert.Equal("Bombilla", Vinculador.QuitarCantidad("3x Bombilla", out cantidad));
            Assert.Equal(3, cantidad);
        }

        [Fact]
        public void QuitarCantidad_AlFinal()
        {
            int cantidad;
            Assert.Equal("Bombilla", Vinculador.QuitarCantidad("Bombilla x2", out cantidad));
            Assert.Equal(2, cantidad);
        }

        [Fact]
        public void QuitarCantidad_NoConfundeUnNombreQueEmpiezaConX()
        {
            int cantidad;
            Assert.Equal("Xtreme", Vinculador.QuitarCantidad("Xtreme", out cantidad));
            Assert.Equal(1, cantidad);
        }

        // ---------- Normalizar ----------

        [Fact]
        public void Normalizar_SacaTildesMayusculasYSignos()
        {
            Assert.Equal("cana de azucar", Vinculador.Normalizar("  Caña  de-Azúcar!! "));
        }

        // ---------- Interpretar ----------

        [Fact]
        public void Interpretar_NombreIgual_EsExacta()
        {
            PropuestaParte p = Vinculador.InterpretarParte("mate imperial negro", CatalogoDeEjemplo());
            Assert.Equal(Coincidencia.Exacta, p.Tipo);
            Assert.Equal(1, p.Producto.Id);
        }

        [Fact]
        public void Interpretar_IgnoraTildesYMayusculas()
        {
            CatalogoProductos catalogo = new CatalogoProductos(new List<Producto>
            {
                new Producto(1, "Mate Algarrobo Cañita", 1, 2)
            });
            PropuestaParte p = Vinculador.InterpretarParte("MATE ALGARROBO CANITA", catalogo);
            Assert.Equal(Coincidencia.Exacta, p.Tipo);
        }

        [Fact]
        public void Interpretar_LeeLaCantidad()
        {
            PropuestaParte p = Vinculador.InterpretarParte("2 x Yerba Kurupi", CatalogoDeEjemplo());
            Assert.Equal(Coincidencia.Exacta, p.Tipo);
            Assert.Equal(2, p.Cantidad);
            Assert.Equal(4, p.Producto.Id);
        }

        [Fact]
        public void Interpretar_NombreParecidoYClaro_EsParecida()
        {
            // Falta la palabra "Mate": se parece a "Mate Imperial Negro" y no a "Mate Imperial Rojo"
            PropuestaParte p = Vinculador.InterpretarParte("Imperial Negro", CatalogoDeEjemplo());
            Assert.Equal(Coincidencia.Parecida, p.Tipo);
            Assert.Equal(1, p.Producto.Id);
        }

        [Fact]
        public void Interpretar_NombreAmbiguo_NoProponeNingunoPeroDaAlternativas()
        {
            // "Imperial" podría ser el negro o el rojo
            PropuestaParte p = Vinculador.InterpretarParte("Imperial", CatalogoDeEjemplo());
            Assert.Equal(Coincidencia.Ninguna, p.Tipo);
            Assert.Null(p.Producto);
            Assert.True(p.Alternativas.Count >= 2);
        }

        [Fact]
        public void Interpretar_ProductoQueNoExiste_EsNinguna()
        {
            PropuestaParte p = Vinculador.InterpretarParte("Cuchillo gaucho", CatalogoDeEjemplo());
            Assert.Equal(Coincidencia.Ninguna, p.Tipo);
            Assert.Empty(p.Alternativas);
        }

        [Fact]
        public void Interpretar_DosProductosConElMismoNombre_NoEligeSolo()
        {
            CatalogoProductos catalogo = new CatalogoProductos(new List<Producto>
            {
                new Producto(1, "Bombilla", 1000, 4000),
                new Producto(2, "bombilla", 1500, 5000)
            });
            PropuestaParte p = Vinculador.InterpretarParte("Bombilla", catalogo);
            Assert.Equal(Coincidencia.Ninguna, p.Tipo);
            Assert.Equal(2, p.Alternativas.Count);
        }

        [Fact]
        public void Interpretar_CatalogoVacio_EsNinguna()
        {
            PropuestaParte p = Vinculador.InterpretarParte("Imperial", new CatalogoProductos(new List<Producto>()));
            Assert.Equal(Coincidencia.Ninguna, p.Tipo);
        }

        [Fact]
        public void Interpretar_TextoCompleto_DevuelveUnaPropuestaPorParte()
        {
            List<PropuestaParte> props = Vinculador.Interpretar(
                "Mate Imperial Negro + Bombilla Pico de Loro + Yerba Kurupi", CatalogoDeEjemplo());
            Assert.Equal(3, props.Count);
            Assert.Equal(Coincidencia.Exacta, props[0].Tipo);
            Assert.Equal(Coincidencia.Exacta, props[1].Tipo);
            Assert.Equal(Coincidencia.Exacta, props[2].Tipo);
        }

        // ---------- CrearItems ----------

        [Fact]
        public void CrearItems_UnSoloProducto_UsaLosValoresRealesDeLaVenta()
        {
            // En la venta se cobró 20.000 y costó 11.000 (más 500 de bolsa): lo que dice la venta manda
            Venta v = VentaVieja("Mate Imperial Negro", 11500, 20000);
            v.Bolsa = 500;

            List<ItemVenta> items = Vinculador.CrearItems(v,
                new List<ParteElegida> { new ParteElegida(CatalogoDeEjemplo().Buscar(1), 1) });

            Assert.Equal(1, items.Count);
            Assert.Equal("Mate Imperial Negro", items[0].Nombre);
            Assert.Equal(11000m, items[0].CostoTotal);
            Assert.Equal(20000m, items[0].PrecioTotal);
        }

        [Fact]
        public void CrearItems_VariosProductos_RepartenProporcionalAlCatalogo()
        {
            CatalogoProductos catalogo = CatalogoDeEjemplo();
            // Catálogo: Imperial Negro 22000 + Bombilla 4000 = 26000; en la venta se cobró 13000 (mitad)
            Venta v = VentaVieja("Imperial Negro + Bombilla", 6500, 13000);

            List<ItemVenta> items = Vinculador.CrearItems(v, new List<ParteElegida>
            {
                new ParteElegida(catalogo.Buscar(1), 1),
                new ParteElegida(catalogo.Buscar(3), 1)
            });

            Assert.Equal(11000m, items[0].PrecioTotal);
            Assert.Equal(2000m, items[1].PrecioTotal);
            Assert.Equal(6000m, items[0].CostoTotal);
            Assert.Equal(500m, items[1].CostoTotal);
        }

        [Fact]
        public void CrearItems_ConCantidad_GuardaElValorDeUnaUnidad()
        {
            CatalogoProductos catalogo = CatalogoDeEjemplo();
            Venta v = VentaVieja("2 x Bombilla", 2000, 8000);

            List<ItemVenta> items = Vinculador.CrearItems(v,
                new List<ParteElegida> { new ParteElegida(catalogo.Buscar(3), 2) });

            Assert.Equal(2, items[0].Cantidad);
            Assert.Equal(1000m, items[0].CostoUnitario);
            Assert.Equal(4000m, items[0].PrecioUnitario);
            Assert.Equal(8000m, items[0].PrecioTotal);
        }

        [Fact]
        public void CrearItems_ConCostosEnCeroEnElCatalogo_RepartePorPrecio()
        {
            CatalogoProductos catalogo = new CatalogoProductos(new List<Producto>
            {
                new Producto(1, "A", 0, 3000),
                new Producto(2, "B", 0, 1000)
            });
            Venta v = VentaVieja("A + B", 2000, 4000);

            List<ItemVenta> items = Vinculador.CrearItems(v, new List<ParteElegida>
            {
                new ParteElegida(catalogo.Buscar(1), 1),
                new ParteElegida(catalogo.Buscar(2), 1)
            });

            Assert.Equal(1500m, items[0].CostoTotal);
            Assert.Equal(500m, items[1].CostoTotal);
        }

        [Fact]
        public void CrearItems_ConTodoEnCero_RepartePorCantidadSinFallar()
        {
            CatalogoProductos catalogo = new CatalogoProductos(new List<Producto>
            {
                new Producto(1, "A", 0, 0),
                new Producto(2, "B", 0, 0)
            });
            Venta v = VentaVieja("A + B", 1000, 3000);

            List<ItemVenta> items = Vinculador.CrearItems(v, new List<ParteElegida>
            {
                new ParteElegida(catalogo.Buscar(1), 1),
                new ParteElegida(catalogo.Buscar(2), 1)
            });

            Assert.Equal(500m, items[0].CostoTotal);
            Assert.Equal(1500m, items[0].PrecioTotal);
        }

        [Fact]
        public void CrearItems_VentaRegaladaConPrecioCero_DejaLosPreciosEnCero()
        {
            Venta v = VentaVieja("Yerba Kurupi", 3000, 0);
            List<ItemVenta> items = Vinculador.CrearItems(v,
                new List<ParteElegida> { new ParteElegida(CatalogoDeEjemplo().Buscar(4), 1) });
            Assert.Equal(0m, items[0].PrecioTotal);
            Assert.Equal(3000m, items[0].CostoTotal);
        }

        [Fact]
        public void CrearItems_SiLosExtrasSuperanElCosto_NoDejaCostosNegativos()
        {
            Venta v = VentaVieja("Yerba Kurupi", 100, 6000);
            v.Bolsa = 500;
            List<ItemVenta> items = Vinculador.CrearItems(v,
                new List<ParteElegida> { new ParteElegida(CatalogoDeEjemplo().Buscar(4), 1) });
            Assert.Equal(0m, items[0].CostoTotal);
        }

        [Fact]
        public void CrearItems_SinPartes_DevuelveListaVacia()
        {
            Assert.Empty(Vinculador.CrearItems(VentaVieja("x", 1, 2), new List<ParteElegida>()));
        }

        [Fact]
        public void CrearItems_NoModificaLosTotalesDeLaVenta()
        {
            Venta v = VentaVieja("Mate Imperial Negro", 11500, 20000);
            v.Bolsa = 500;
            decimal gananciaAntes = v.Ganancia;

            v.Items.AddRange(Vinculador.CrearItems(v,
                new List<ParteElegida> { new ParteElegida(CatalogoDeEjemplo().Buscar(1), 1) }));

            Assert.Equal(11500m, v.CostoTotal);
            Assert.Equal(20000m, v.PrecioVenta);
            Assert.Equal(gananciaAntes, v.Ganancia);
        }

        [Fact]
        public void CrearItems_Reportes_CuentanLaVentaVinculada()
        {
            Venta v = VentaVieja("Mate Imperial Negro", 11500, 20000);
            v.Items.AddRange(Vinculador.CrearItems(v,
                new List<ParteElegida> { new ParteElegida(CatalogoDeEjemplo().Buscar(1), 1) }));

            List<FilaProducto> filas = Reportes.PorProducto(new List<Venta> { v }, OrdenProductos.Unidades);

            Assert.Equal(1, filas.Count);
            Assert.Equal("Mate Imperial Negro", filas[0].Nombre);
            Assert.Equal(20000m, filas[0].Facturado);
        }
    }
}
