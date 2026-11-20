//////// Producto.cs ////////
namespace SistemaPedidos
{
    // Un producto del catálogo: nombre, lo que cuesta y a cuánto se vende.
    public class Producto
    {
        public int Id { get; private set; }
        public string Nombre { get; private set; }
        public decimal Costo { get; private set; }
        public decimal Precio { get; private set; }

        public Producto(int id, string nombre, decimal costo, decimal precio)
        {
            Id = id;
            Nombre = nombre;
            Costo = costo;
            Precio = precio;
        }

        public decimal Ganancia
        {
            get { return Precio - Costo; }
        }

        public void Corregir(string nombre, decimal costo, decimal precio)
        {
            Nombre = nombre;
            Costo = costo;
            Precio = precio;
        }

        public string ALinea()
        {
            return string.Format("#{0,-3} {1,-40} costo {2,9}  precio {3,9}  gan. {4,9}",
                Id, Nombre, Formato.Pesos(Costo), Formato.Pesos(Precio), Formato.Pesos(Ganancia));
        }
    }
}
