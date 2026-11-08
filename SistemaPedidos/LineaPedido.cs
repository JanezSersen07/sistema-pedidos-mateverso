////////////////////////////////////////////////////////////
//// ARCHIVO: LineaPedido.cs
////////////////////////////////////////////////////////////
namespace SistemaPedidos
{
    // Una línea del pedido: un producto y cuántas unidades se piden.
    public class LineaPedido
    {
        public Producto Producto { get; private set; }
        public int Cantidad { get; private set; }

        public LineaPedido(Producto producto, int cantidad)
        {
            Producto = producto;
            Cantidad = cantidad;
        }

        // Propiedad calculada: no se guarda, se calcula cada vez
        public decimal Subtotal
        {
            get { return Producto.Precio * Cantidad; }
        }

        public void SumarCantidad(int cantidad)
        {
            Cantidad = Cantidad + cantidad;
        }

        public override string ToString()
        {
            return $"{Cantidad} x {Producto.Nombre} = ${Subtotal:N0}";
        }
    }
}