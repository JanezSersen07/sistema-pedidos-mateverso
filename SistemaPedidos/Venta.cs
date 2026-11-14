//////// Venta.cs ////////
using System;
 
namespace SistemaPedidos
{
    // Una venta: equivale a una fila de la planilla.
    public class Venta
    {
        public int Numero { get; private set; }
        public DateTime Fecha { get; private set; }
        public string Productos { get; private set; }       // texto, por ejemplo "Imperial (Negro) + Bombilla"
        public decimal CostoTotal { get; private set; }     // incluye bolsa, sticker y tarjeta
        public decimal PrecioVenta { get; private set; }    // lo que se cobró en total
        public MedioPago Medio { get; private set; }
        public EstadoVenta Estado { get; private set; }
 
        // Datos opcionales
        public DateTime? FechaSegundoPago { get; set; }     // cuando se paga parte y parte
        public decimal Bolsa { get; set; }
        public decimal Sticker { get; set; }
        public decimal Tarjeta { get; set; }
        public bool EsRegalo { get; set; }
        public string Nota { get; set; }
 
        public Venta(int numero, DateTime fecha, string productos, decimal costoTotal,
                     decimal precioVenta, MedioPago medio, EstadoVenta estado)
        {
            Numero = numero;
            Fecha = fecha;
            Productos = productos;
            CostoTotal = costoTotal;
            PrecioVenta = precioVenta;
            Medio = medio;
            Estado = estado;
            Nota = "";
        }
 
        // Propiedades calculadas
        public decimal Ganancia
        {
            get { return PrecioVenta - CostoTotal; }
        }
 
        public decimal MargenPorcentaje
        {
            get
            {
                if (PrecioVenta == 0)
                {
                    return 0;
                }
                return Ganancia / PrecioVenta * 100;
            }
        }
 
        // Solo se puede avanzar de estado: Pedido -> Pagado -> Entregado
        public bool CambiarEstado(EstadoVenta nuevoEstado)
        {
            if ((int)nuevoEstado <= (int)Estado)
            {
                return false;
            }
            Estado = nuevoEstado;
            return true;
        }
 
        // Corrige los datos principales de la venta (por ejemplo, si se cargó mal un monto).
        // A diferencia de CambiarEstado, acá el estado también puede volver atrás.
        public void Corregir(DateTime fecha, string productos, decimal costoTotal,
                             decimal precioVenta, MedioPago medio, EstadoVenta estado)
        {
            Fecha = fecha;
            Productos = productos;
            CostoTotal = costoTotal;
            PrecioVenta = precioVenta;
            Medio = medio;
            Estado = estado;
        }
 
        // Una línea resumida para listar
        public string ALinea()
        {
            string productos = Productos;
            if (productos.Length > 45)
            {
                productos = productos.Substring(0, 42) + "...";
            }
            return string.Format("#{0,-3} {1:dd/MM/yyyy} {2,-9} {3,9} gan. {4,9}  {5}",
                Numero, Fecha, Estado, Formato.Pesos(PrecioVenta), Formato.Pesos(Ganancia), productos);
        }
 
        public void Mostrar()
        {
            Console.WriteLine("Venta #" + Numero + (EsRegalo ? "  (REGALO)" : ""));
            Console.WriteLine("  Fecha:    " + Fecha.ToString("dd/MM/yyyy")
                + (FechaSegundoPago.HasValue ? "  y  " + FechaSegundoPago.Value.ToString("dd/MM/yyyy") : ""));
            Console.WriteLine("  Productos: " + Productos);
            Console.WriteLine("  Costo total: " + Formato.Pesos(CostoTotal)
                + "   (bolsa " + Formato.Pesos(Bolsa) + ", sticker " + Formato.Pesos(Sticker)
                + ", tarjeta " + Formato.Pesos(Tarjeta) + ")");
            Console.WriteLine("  Precio de venta: " + Formato.Pesos(PrecioVenta));
            Console.WriteLine("  Ganancia: " + Formato.Pesos(Ganancia)
                + "  (" + MargenPorcentaje.ToString("0") + "%)");
            Console.WriteLine("  Pago: " + Medio + "   Estado: " + Estado);
            if (Nota != "")
            {
                Console.WriteLine("  Nota: " + Nota);
            }
        }
    }
}