////////////////////////////////////////////////////////////
//// ARCHIVO: Program.cs
////////////////////////////////////////////////////////////
using System;
using System.Text;

namespace SistemaPedidos
{
    class Program
    {
        static Tienda tienda = new Tienda();

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            int opcion = -1;

            while (opcion != 0)
            {
                MostrarMenu();
                opcion = LeerEntero("Opción: ");
                Console.WriteLine();

                switch (opcion)
                {
                    case 1:
                        tienda.ListarProductos();
                        break;
                    case 2:
                        NuevoPedido();
                        break;
                    case 3:
                        tienda.ListarPedidos();
                        break;
                    case 4:
                        CambiarEstadoDePedido();
                        break;
                    case 5:
                        MostrarVentas();
                        break;
                    case 0:
                        Console.WriteLine("¡Hasta luego!");
                        break;
                    default:
                        Console.WriteLine("Opción inválida.");
                        break;
                }
                Console.WriteLine();
            }
        }

        static void MostrarMenu()
        {
            Console.WriteLine("===== PEDIDOS MATEVERSO =====");
            Console.WriteLine("1. Ver productos");
            Console.WriteLine("2. Nuevo pedido");
            Console.WriteLine("3. Ver pedidos");
            Console.WriteLine("4. Cambiar estado de un pedido");
            Console.WriteLine("5. Ver ventas");
            Console.WriteLine("0. Salir");
        }

        // Pide un número hasta que el usuario escriba uno válido
        static int LeerEntero(string mensaje)
        {
            int numero;
            Console.Write(mensaje);
            string texto = Console.ReadLine();

            while (!int.TryParse(texto, out numero))
            {
                // Si ya no hay nada para leer (se cerró la entrada), devuelve 0 para no quedar trabado
                if (texto == null)
                {
                    return 0;
                }
                Console.Write("Escribí un número válido: ");
                texto = Console.ReadLine();
            }
            return numero;
        }

        static void NuevoPedido()
        {
            Console.Write("Nombre del cliente: ");
            string nombre = Console.ReadLine();
            Console.Write("Contacto (Instagram o WhatsApp): ");
            string contacto = Console.ReadLine();

            Pedido pedido = tienda.CrearPedido(new Cliente(nombre, contacto));

            int idProducto = -1;
            while (idProducto != 0)
            {
                Console.WriteLine();
                tienda.ListarProductos();
                idProducto = LeerEntero("Id del producto (0 para terminar): ");

                if (idProducto != 0)
                {
                    int cantidad = LeerEntero("Cantidad: ");
                    if (tienda.AgregarAlPedido(pedido, idProducto, cantidad))
                    {
                        Console.WriteLine("Producto agregado.");
                    }
                    else
                    {
                        Console.WriteLine("No se pudo agregar: revisá el id y el stock disponible.");
                    }
                }
            }

            if (pedido.CantidadLineas == 0)
            {
                Console.WriteLine("El pedido no tiene productos, no se guardó.");
                return;
            }

            tienda.RegistrarPedido(pedido);
            Console.WriteLine();
            Console.WriteLine("Pedido guardado:");
            pedido.Mostrar();
        }

        static void CambiarEstadoDePedido()
        {
            int numero = LeerEntero("Número de pedido: ");
            Pedido pedido = tienda.BuscarPedido(numero);

            if (pedido == null)
            {
                Console.WriteLine("No existe ese pedido.");
                return;
            }

            Console.WriteLine($"Estado actual: {pedido.Estado}");
            Console.WriteLine("1. Pendiente  2. Pagado  3. Enviado  4. Entregado");
            int nuevo = LeerEntero("Nuevo estado: ");

            if (nuevo < 1 || nuevo > 4)
            {
                Console.WriteLine("Estado inválido.");
            }
            else if (pedido.CambiarEstado((EstadoPedido)nuevo))
            {
                Console.WriteLine($"El pedido #{pedido.Numero} ahora está {pedido.Estado}.");
            }
            else
            {
                Console.WriteLine("Solo se puede avanzar de estado, no volver para atrás.");
            }
        }

        static void MostrarVentas()
        {
            Console.WriteLine($"Cobrado:   ${tienda.TotalCobrado():N0}");
            Console.WriteLine($"Pendiente: ${tienda.TotalPendiente():N0}");
        }
    }
}