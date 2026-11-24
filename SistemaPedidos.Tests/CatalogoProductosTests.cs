using System.Collections.Generic;
using SistemaPedidos;
using Xunit;

namespace SistemaPedidos.Tests
{
    public class CatalogoProductosTests
    {
        private static CatalogoProductos Catalogo()
        {
            return new CatalogoProductos(new List<Producto>
            {
                new Producto(1, "Mate Imperial", 12000, 22000),
                new Producto(5, "Bombilla Pico de Loro", 3000, 6000)
            });
        }

        [Fact]
        public void Agregar_AsignaElSiguienteId()
        {
            CatalogoProductos c = Catalogo();
            Producto nuevo = c.Agregar("Termo", 18000, 36000);
            Assert.Equal(6, nuevo.Id);
            Assert.Equal(3, c.Cantidad);
        }

        [Fact]
        public void Agregar_EnCatalogoVacio_EmpiezaEn1()
        {
            CatalogoProductos c = new CatalogoProductos(new List<Producto>());
            Assert.Equal(1, c.Agregar("Mate", 1, 2).Id);
        }

        [Fact]
        public void Ganancia_EsPrecioMenosCosto()
        {
            Assert.Equal(10000m, Catalogo().Buscar(1).Ganancia);
        }

        [Fact]
        public void BuscarPorTexto_IgnoraMayusculas()
        {
            List<Producto> res = Catalogo().BuscarPorTexto("BOMBILLA");
            Assert.Single(res);
            Assert.Equal(5, res[0].Id);
        }

        [Fact]
        public void ExisteNombre_IgnoraMayusculas()
        {
            Assert.True(Catalogo().ExisteNombre("mate imperial"));
            Assert.False(Catalogo().ExisteNombre("Mate Camionero"));
        }

        [Fact]
        public void Eliminar_BorraElProducto()
        {
            CatalogoProductos c = Catalogo();
            Assert.True(c.Eliminar(1));
            Assert.Null(c.Buscar(1));
            Assert.False(c.Eliminar(1));
        }

        [Fact]
        public void Corregir_CambiaLosDatos()
        {
            Producto p = Catalogo().Buscar(1);
            p.Corregir("Mate Imperial Premium", 13000, 26000);
            Assert.Equal("Mate Imperial Premium", p.Nombre);
            Assert.Equal(13000m, p.Ganancia);
        }
    }
}
