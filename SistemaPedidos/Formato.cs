//////// Formato.cs ////////
using System;
using System.Globalization;

namespace SistemaPedidos
{
    // Funciones de ayuda para mostrar los montos como en Argentina: $1.234
    public static class Formato
    {
        private static readonly CultureInfo cultura = ObtenerCultura();

        private static CultureInfo ObtenerCultura()
        {
            try
            {
                return new CultureInfo("es-AR");
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.InvariantCulture;
            }
        }

        public static string Pesos(decimal monto)
        {
            string signo = monto < 0 ? "-$" : "$";
            return signo + Math.Abs(monto).ToString("N0", cultura);
        }
    }
}