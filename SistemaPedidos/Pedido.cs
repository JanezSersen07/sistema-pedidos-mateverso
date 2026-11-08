////////////////////////////////////////////////////////////
//// ARCHIVO: Pedido.cs
////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;

namespace SistemaPedidos
{
    public class Pedido
    {
        public int Numero { get; private set; }
        public Cliente Cliente { get; private set; }   // composición: un pedido tiene un cliente
        public DateTime Fecha { get; private set; }
        public EstadoPedido Estado { get; private set; }

        private List<LineaPedido> lineas;              // composición: un pedido tiene muchas líneas

        public Pedido(int numero, Cliente cliente)
        {
            Numero = numero;
            Cliente = cliente;
            Fecha = DateTime.Now;
            Estado = EstadoPedido.Pendiente;
            lineas = new List<LineaPedido>();
        }

        public int CantidadLineas
        {
            get { return lineas.Count; }
        }

        public decimal Total
        {
            get
            {
                decimal total = 0;
                foreach (LineaPedido linea in lineas)
                {
                    total = total + linea.Subtotal;
                }
                return total;
            }
        }

        // Si el producto ya está en el pedido, suma la cantidad; si no, agrega una línea nueva
        public void AgregarProducto(Producto producto, int cantidad)
        {
            foreach (LineaPedido linea in lineas)
            {
                if (linea.Producto.Id == producto.Id)
                {
                    linea.SumarCantidad(cantidad);
                    return;
                }
            }
            lineas.Add(new LineaPedido(producto, cantidad));
        }

        // Solo se puede avanzar de estado, nunca volver para atrás
        public bool CambiarEstado(EstadoPedido nuevoEstado)
        {
            if ((int)nuevoEstado <= (int)Estado)
            {
                return false;
            }
            Estado = nuevoEstado;
            return true;
        }

        public void Mostrar()
        {
            Console.WriteLine($"Pedido #{Numero} - {Fecha:dd/MM/yyyy HH:mm} - {Estado}");
            Console.WriteLine($"  Cliente: {Cliente}");
            foreach (LineaPedido linea in lineas)
            {
                Console.WriteLine($"  - {linea}");
            }
            Console.WriteLine($"  Total: ${Total:N0}");
        }
    }
}