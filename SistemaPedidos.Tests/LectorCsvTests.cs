using System.Collections.Generic;
using SistemaPedidos;
using Xunit;

namespace SistemaPedidos.Tests
{
    public class LectorCsvTests
    {
        [Fact]
        public void Leer_SeparaPorPuntoYComa()
        {
            List<List<string>> filas = LectorCsv.Leer("a;b;c\n1;2;3\n", ';');
            Assert.Equal(2, filas.Count);
            Assert.Equal("b", filas[0][1]);
            Assert.Equal("3", filas[1][2]);
        }

        [Fact]
        public void Leer_RespetaElSeparadorDentroDeComillas()
        {
            List<List<string>> filas = LectorCsv.Leer("\"uno;dos\";tres\n", ';');
            Assert.Equal(2, filas[0].Count);
            Assert.Equal("uno;dos", filas[0][0]);
        }

        [Fact]
        public void Leer_InterpretaComillasDobles()
        {
            List<List<string>> filas = LectorCsv.Leer("\"dijo \"\"hola\"\"\";x\n", ';');
            Assert.Equal("dijo \"hola\"", filas[0][0]);
        }

        [Fact]
        public void DetectarSeparador_ElijeElMasFrecuente()
        {
            Assert.Equal(';', LectorCsv.DetectarSeparador("a;b;c\n1;2;3"));
            Assert.Equal(',', LectorCsv.DetectarSeparador("a,b,c\n1,2,3"));
        }

        [Fact]
        public void Escapar_NoToca_LoQueNoNecesitaComillas()
        {
            Assert.Equal("Mate", LectorCsv.Escapar("Mate", ';'));
        }

        [Fact]
        public void Escapar_PonePorComillasYDuplicaLasComillas()
        {
            Assert.Equal("\"a;b\"", LectorCsv.Escapar("a;b", ';'));
            Assert.Equal("\"dijo \"\"hola\"\"\"", LectorCsv.Escapar("dijo \"hola\"", ';'));
        }

        [Fact]
        public void Escapar_ConNull_DevuelveVacio()
        {
            Assert.Equal("", LectorCsv.Escapar(null, ';'));
        }

        [Fact]
        public void EscaparYLeer_SonInversos()
        {
            string original = "Entregar; \"urgente\"";
            string linea = LectorCsv.Escapar(original, ';') + ";fin";
            List<List<string>> filas = LectorCsv.Leer(linea, ';');
            Assert.Equal(original, filas[0][0]);
            Assert.Equal("fin", filas[0][1]);
        }
    }
}
