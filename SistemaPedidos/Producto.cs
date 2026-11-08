////////////////////////////////////////////////////////////
//// ARCHIVO: Producto.cs
////////////////////////////////////////////////////////////
namespace SistemaPedidos
{
    public class Producto
    {
        // private set: el valor solo se puede cambiar desde adentro de la clase (encapsulamiento)
        public int Id { get; private set; }
        public string Nombre { get; private set; }
        public decimal Precio { get; private set; }
        public int Stock { get; private set; }

        public Producto(int id, string nombre, decimal precio, int stock)
        {
            Id = id;
            Nombre = nombre;
            Precio = precio;
            Stock = stock;
        }

        // Devuelve true si se puede vender esa cantidad
        public bool HayStock(int cantidad)
        {
            return cantidad > 0 && cantidad <= Stock;
        }

        public void DescontarStock(int cantidad)
        {
            Stock = Stock - cantidad;
        }

        public override string ToString()
        {
            return $"{Id}. {Nombre} - ${Precio:N0} (stock: {Stock})";
        }
    }
}