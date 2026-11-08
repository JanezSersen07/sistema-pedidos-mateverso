////////////////////////////////////////////////////////////
//// ARCHIVO: Cliente.cs
////////////////////////////////////////////////////////////
namespace SistemaPedidos
{
    public class Cliente
    {
        public string Nombre { get; private set; }
        public string Contacto { get; private set; } // usuario de Instagram o WhatsApp

        public Cliente(string nombre, string contacto)
        {
            Nombre = nombre;
            Contacto = contacto;
        }

        public override string ToString()
        {
            return $"{Nombre} ({Contacto})";
        }
    }
}