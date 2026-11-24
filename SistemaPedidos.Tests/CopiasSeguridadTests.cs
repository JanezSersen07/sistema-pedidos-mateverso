using System;
using System.IO;
using System.Linq;
using SistemaPedidos;
using Xunit;

namespace SistemaPedidos.Tests
{
    public class CopiasSeguridadTests : IDisposable
    {
        private readonly string carpeta;
        private readonly string ruta;

        public CopiasSeguridadTests()
        {
            carpeta = Path.Combine(Path.GetTempPath(), "mateverso-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ruta = Path.Combine(carpeta, "ventas.csv");
        }

        public void Dispose()
        {
            Directory.Delete(carpeta, true);
        }

        private string CopiaDeHoy()
        {
            return Path.Combine(carpeta, "copias", "ventas-" + DateTime.Today.ToString("yyyy-MM-dd") + ".csv");
        }

        [Fact]
        public void CopiaDelDia_SinArchivoOriginal_NoHaceNada()
        {
            CopiasSeguridad.CopiaDelDia(ruta);
            Assert.False(Directory.Exists(Path.Combine(carpeta, "copias")));
        }

        [Fact]
        public void CopiaDelDia_CreaLaCopiaConLaFechaDeHoy()
        {
            File.WriteAllText(ruta, "version 1");
            CopiasSeguridad.CopiaDelDia(ruta);
            Assert.True(File.Exists(CopiaDeHoy()));
            Assert.Equal("version 1", File.ReadAllText(CopiaDeHoy()));
        }

        [Fact]
        public void CopiaDelDia_UnaSolaVezPorDia_ConservaLaPrimera()
        {
            File.WriteAllText(ruta, "version 1");
            CopiasSeguridad.CopiaDelDia(ruta);
            File.WriteAllText(ruta, "version 2");
            CopiasSeguridad.CopiaDelDia(ruta);
            Assert.Equal("version 1", File.ReadAllText(CopiaDeHoy()));
        }

        [Fact]
        public void CopiaDelDia_ConservaSoloLas30MasRecientes()
        {
            string carpetaCopias = Path.Combine(carpeta, "copias");
            Directory.CreateDirectory(carpetaCopias);
            for (int i = 1; i <= 35; i++)
            {
                string fecha = new DateTime(2026, 1, 1).AddDays(i).ToString("yyyy-MM-dd");
                File.WriteAllText(Path.Combine(carpetaCopias, "ventas-" + fecha + ".csv"), "x");
            }
            File.WriteAllText(ruta, "hoy");

            CopiasSeguridad.CopiaDelDia(ruta);

            string[] quedan = Directory.GetFiles(carpetaCopias, "ventas-????-??-??.csv");
            Assert.Equal(30, quedan.Length);
            Assert.True(File.Exists(CopiaDeHoy()));
            // la más vieja de las 35 simuladas se borró
            Assert.False(File.Exists(Path.Combine(carpetaCopias, "ventas-2026-01-02.csv")));
        }

        [Fact]
        public void Listar_SinCopias_DevuelveListaVacia()
        {
            Assert.Empty(CopiasSeguridad.Listar(ruta));
        }

        [Fact]
        public void Listar_OrdenaDeLaMasRecienteALaMasVieja()
        {
            string carpetaCopias = Path.Combine(carpeta, "copias");
            Directory.CreateDirectory(carpetaCopias);
            File.WriteAllText(Path.Combine(carpetaCopias, "ventas-2026-10-01.csv"), "a");
            File.WriteAllText(Path.Combine(carpetaCopias, "ventas-2026-10-15.csv"), "b");

            var lista = CopiasSeguridad.Listar(ruta);

            Assert.Equal(2, lista.Count);
            Assert.Equal("ventas-2026-10-15.csv", Path.GetFileName(lista[0]));
        }

        [Fact]
        public void Listar_NoMezclaLasCopiasDeOtroArchivo()
        {
            string carpetaCopias = Path.Combine(carpeta, "copias");
            Directory.CreateDirectory(carpetaCopias);
            File.WriteAllText(Path.Combine(carpetaCopias, "productos-2026-10-01.csv"), "a");
            Assert.Empty(CopiasSeguridad.Listar(ruta));
        }

        [Fact]
        public void Restaurar_ReemplazaElArchivoYResguardaElActual()
        {
            string carpetaCopias = Path.Combine(carpeta, "copias");
            Directory.CreateDirectory(carpetaCopias);
            string copia = Path.Combine(carpetaCopias, "ventas-2026-10-01.csv");
            File.WriteAllText(copia, "contenido viejo");
            File.WriteAllText(ruta, "contenido actual");

            CopiasSeguridad.Restaurar(copia, ruta);

            Assert.Equal("contenido viejo", File.ReadAllText(ruta));
            string[] previas = Directory.GetFiles(carpetaCopias, "ventas-antes-de-restaurar-*");
            Assert.Single(previas);
            Assert.Equal("contenido actual", File.ReadAllText(previas[0]));
        }
    }
}
