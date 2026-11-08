////////////////////////////////////////////////////////////
//// ARCHIVO: Tienda.cs
////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;

namespace SistemaPedidos
{
    // Maneja los productos y los pedidos. Es la clase que "organiza" todo.
    public class Tienda
    {
        private List<Producto> productos;
        private List<Pedido> pedidos;
        private int proximoNumero;

        public Tienda()
        {
            productos = new List<Producto>();
            pedidos = new List<Pedido>();
            proximoNumero = 1;
            CargarProductosDeEjemplo();
        }

        // Productos y precios de ejemplo
        private void CargarProductosDeEjemplo()
        {
            productos.Add(new Producto(1, "Mate Imperial de cuero", 38000, 5));
            productos.Add(new Producto(2, "Mate Camionero de calabaza", 22000, 8));
            productos.Add(new Producto(3, "Mate Torpedo de acero", 27500, 6));
            productos.Add(new Producto(4, "Bombilla pico de loro", 9500, 20));
            productos.Add(new Producto(5, "Termo de acero 1 litro", 45000, 3));
            productos.Add(new Producto(6, "Yerba mate suave 1 kg", 7800, 30));
        }

        public void ListarProductos()
        {
            foreach (Producto producto in productos)
            {
                Console.WriteLine(producto);
            }
        }

        public Producto BuscarProducto(int id)
        {
            foreach (Producto producto in productos)
            {
                if (producto.Id == id)
                {
                    return producto;
                }
            }
            return null;
        }

        public Pedido BuscarPedido(int numero)
        {
            foreach (Pedido pedido in pedidos)
            {
                if (pedido.Numero == numero)
                {
                    return pedido;
                }
            }
            return null;
        }

        // Crea un pedido nuevo (todavía vacío) con el próximo número disponible
        public Pedido CrearPedido(Cliente cliente)
        {
            Pedido pedido = new Pedido(proximoNumero, cliente);
            proximoNumero++;
            return pedido;
        }

        // Agrega una línea al pedido si hay stock, y descuenta el stock
        public bool AgregarAlPedido(Pedido pedido, int idProducto, int cantidad)
        {
            Producto producto = BuscarProducto(idProducto);
            if (producto == null || !producto.HayStock(cantidad))
            {
                return false;
            }
            pedido.AgregarProducto(producto, cantidad);
            producto.DescontarStock(cantidad);
            return true;
        }

        // Guarda el pedido en la lista de pedidos de la tienda
        public void RegistrarPedido(Pedido pedido)
        {
            pedidos.Add(pedido);
        }

        public void ListarPedidos()
        {
            if (pedidos.Count == 0)
            {
                Console.WriteLine("Todavía no hay pedidos.");
                return;
            }
            foreach (Pedido pedido in pedidos)
            {
                pedido.Mostrar();
                Console.WriteLine();
            }
        }

        // Suma lo que ya se cobró: todos los pedidos que no están pendientes
        public decimal TotalCobrado()
        {
            decimal total = 0;
            foreach (Pedido pedido in pedidos)
            {
                if (pedido.Estado != EstadoPedido.Pendiente)
                {
                    total = total + pedido.Total;
                }
            }
            return total;
        }

        // Lo que falta cobrar: pedidos pendientes
        public decimal TotalPendiente()
        {
            decimal total = 0;
            foreach (Pedido pedido in pedidos)
            {
                if (pedido.Estado == EstadoPedido.Pendiente)
                {
                    total = total + pedido.Total;
                }
            }
            return total;
        }
    }
}