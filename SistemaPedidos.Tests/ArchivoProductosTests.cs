using System;
using System.Collections.Generic;
using System.IO;
using SistemaPedidos;
using Xunit;

namespace SistemaPedidos.Tests
{
    public class ArchivoProductosTests : IDisposable
    {
        private readonly string carpeta;

        public ArchivoProductosTests()
        {
            carpeta = Path.Combine(Path.GetTempPath(), "mateverso-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
        }

        public void Dispose()
        {
            Directory.Delete(carpeta, true);
        }

        [Fact]
        public void GuardarYCargar_ConservaIdNombreCostoYPrecio()
        {
            string ruta = Path.Combine(carpeta, "productos.csv");
            List<Producto> productos = new List<Producto>
            {
                new Producto(1, "Mate Imperial; edición \"verano\"", 12000.5m, 22000),
                new Producto(2, "Yerba Kurupi", 0, 6000)
            };

            ArchivoProductos.Guardar(productos, ruta);
            List<Producto> cargados = ArchivoProductos.Cargar(ruta);

            Assert.Equal(2, cargados.Count);
            Assert.Equal("Mate Imperial; edición \"verano\"", cargados[0].Nombre);
            Assert.Equal(12000.5m, cargados[0].Costo);
            Assert.Equal(6000m, cargados[1].Precio);
            Assert.Equal(2, cargados[1].Id);
        }

        [Fact]
        public void Cargar_ArchivoInexistente_DevuelveListaVacia()
        {
            Assert.Empty(ArchivoProductos.Cargar(Path.Combine(carpeta, "no-existe.csv")));
        }
    }
}
