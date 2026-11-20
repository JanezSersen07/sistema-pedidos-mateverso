//////// ItemVenta.cs ////////
using System;
using System.Globalization;

namespace SistemaPedidos
{
    // Un producto dentro de una venta, con la cantidad y los valores del momento.
    // Se guardan el nombre, el costo y el precio de ese día: si después cambia el
    // catálogo (o se borra el producto), las ventas viejas no se modifican.
    public class ItemVenta
    {
        public string Nombre { get; private set; }
        public int Cantidad { get; private set; }
        public decimal CostoUnitario { get; private set; }
        public decimal PrecioUnitario { get; private set; }

        public ItemVenta(string nombre, int cantidad, decimal costoUnitario, decimal precioUnitario)
        {
            // Estos dos símbolos se usan para guardar el item en el archivo
            Nombre = nombre.Replace("|", "/").Replace("~", "-").Trim();
            Cantidad = cantidad;
            CostoUnitario = costoUnitario;
            PrecioUnitario = precioUnitario;
        }

        public decimal CostoTotal
        {
            get { return CostoUnitario * Cantidad; }
        }

        public decimal PrecioTotal
        {
            get { return PrecioUnitario * Cantidad; }
        }

        public decimal Ganancia
        {
            get { return PrecioTotal - CostoTotal; }
        }

        // "Mate Imperial" o "2 x Bombilla"
        public string ATexto()
        {
            return Cantidad > 1 ? Cantidad + " x " + Nombre : Nombre;
        }

        // Formato para el archivo: nombre~cantidad~costo~precio
        public string Serializar()
        {
            return Nombre + "~" + Cantidad
                + "~" + CostoUnitario.ToString("0.##", CultureInfo.InvariantCulture)
                + "~" + PrecioUnitario.ToString("0.##", CultureInfo.InvariantCulture);
        }

        public static ItemVenta Leer(string texto)
        {
            string[] partes = texto.Split('~');
            if (partes.Length != 4)
            {
                throw new FormatException("Detalle de producto inválido: " + texto);
            }
            return new ItemVenta(
                partes[0],
                int.Parse(partes[1], CultureInfo.InvariantCulture),
                decimal.Parse(partes[2], CultureInfo.InvariantCulture),
                decimal.Parse(partes[3], CultureInfo.InvariantCulture));
        }
    }
}
