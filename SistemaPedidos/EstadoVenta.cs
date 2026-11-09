//////// EstadoVenta.cs ////////
namespace SistemaPedidos
{
    // Los tres estados que ya usabas en la planilla (rojo, naranja y verde).
    public enum EstadoVenta
    {
        Pedido = 1,     // sin pagar y sin entregar
        Pagado = 2,     // pagado, falta entregar
        Entregado = 3   // pagado y entregado
    }
}

//////// MedioPago.cs ////////
namespace SistemaPedidos
{
    public enum MedioPago
    {
        MercadoPago = 1,
        Efectivo = 2,
        Transferencia = 3,
        TarjetaWeb = 4
    }
}