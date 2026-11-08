////////////////////////////////////////////////////////////
//// ARCHIVO: EstadoPedido.cs
////////////////////////////////////////////////////////////
namespace SistemaPedidos
{
    // Los estados por los que pasa un pedido, en orden.
    public enum EstadoPedido
    {
        Pendiente = 1,
        Pagado = 2,
        Enviado = 3,
        Entregado = 4
    }
}