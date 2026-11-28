//////// PanelServicio.cs ////////
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SistemaPedidos
{
    // Error "de negocio" para mostrarle al usuario en el panel (dato inválido, no existe, etc.)
    public class ErrorPanel : Exception
    {
        public int Codigo { get; private set; }

        public ErrorPanel(string mensaje, int codigo = 400) : base(mensaje)
        {
            Codigo = codigo;
        }
    }

    // ---------- Datos que viajan entre el panel y el servidor ----------

    public class ItemDto
    {
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal PrecioUnitario { get; set; }
    }

    public class VentaDto
    {
        public int Numero { get; set; }
        public string Fecha { get; set; }
        public string Productos { get; set; }
        public decimal CostoTotal { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal Ganancia { get; set; }
        public decimal Margen { get; set; }
        public int Medio { get; set; }
        public string MedioNombre { get; set; }
        public int Estado { get; set; }
        public string EstadoNombre { get; set; }
        public decimal Bolsa { get; set; }
        public decimal Sticker { get; set; }
        public decimal Tarjeta { get; set; }
        public bool EsRegalo { get; set; }
        public string Nota { get; set; }
        public int DiasDeEspera { get; set; }
        public List<ItemDto> Items { get; set; }
    }

    public class ProductoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal Costo { get; set; }
        public decimal Precio { get; set; }
        public decimal Ganancia { get; set; }
    }

    public class ItemNuevoDto
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }

    // Lo que manda el panel para crear una venta
    public class VentaNuevaDto
    {
        public string Fecha { get; set; }                 // "2026-11-27"; vacío = hoy
        public List<ItemNuevoDto> Items { get; set; }     // productos del catálogo
        public string Productos { get; set; }             // texto libre, solo si no hay Items
        public decimal? CostoProductos { get; set; }      // solo si no hay Items
        public decimal Bolsa { get; set; }
        public decimal Sticker { get; set; }
        public decimal Tarjeta { get; set; }
        public decimal? Precio { get; set; }              // vacío = la suma de los precios del catálogo
        public int Medio { get; set; }
        public int Estado { get; set; }
        public bool EsRegalo { get; set; }
        public string Nota { get; set; }
    }

    public class ProductoNuevoDto
    {
        public string Nombre { get; set; }
        public decimal Costo { get; set; }
        public decimal Precio { get; set; }
    }

    public class ResumenDto
    {
        public int Ventas { get; set; }
        public decimal Facturado { get; set; }
        public decimal Costos { get; set; }
        public decimal Ganancia { get; set; }
        public decimal Margen { get; set; }
        public decimal Cobrado { get; set; }
        public decimal PorCobrar { get; set; }
        public int CantidadPorCobrar { get; set; }
        public int CantidadPorEntregar { get; set; }
        public decimal TicketPromedio { get; set; }
        public int VentasConDetalle { get; set; }
    }

    public class FilaMesDto
    {
        public string Mes { get; set; }
        public int Ventas { get; set; }
        public decimal Facturado { get; set; }
        public decimal Costos { get; set; }
        public decimal Ganancia { get; set; }
        public decimal Margen { get; set; }
        public decimal? VariacionGanancia { get; set; }
    }

    public class FilaProductoDto
    {
        public string Nombre { get; set; }
        public int Unidades { get; set; }
        public decimal Facturado { get; set; }
        public decimal Costos { get; set; }
        public decimal Ganancia { get; set; }
        public decimal Margen { get; set; }
    }

    // Todo lo que hace el panel con los datos. Cada operación abre la base, hace lo suyo y guarda,
    // así que lo que cambies en el sistema de consola se ve al recargar el panel (y al revés).
    // Las operaciones que modifican van de a una por vez (lock) y hacen la copia del día antes de guardar.
    public class PanelServicio
    {
        private readonly string rutaBase;
        private readonly object candado = new object();
        private readonly Func<DateTime> hoy;

        public PanelServicio(string rutaBase, Func<DateTime> hoy = null)
        {
            this.rutaBase = rutaBase;
            this.hoy = hoy ?? (() => DateTime.Today);
        }

        // ---------- Lectura ----------

        private List<Venta> Ventas()
        {
            return RepositorioSqlite.CargarVentas(rutaBase);
        }

        public ResumenDto Resumen()
        {
            lock (candado)
            {
                RegistroVentas registro = new RegistroVentas(Ventas());
                ResumenDto r = new ResumenDto();
                r.Ventas = registro.Cantidad;
                r.Facturado = registro.TotalVentas();
                r.Costos = registro.TotalCostos();
                r.Ganancia = registro.GananciaTotal();
                r.Margen = r.Facturado == 0 ? 0 : Math.Round(r.Ganancia / r.Facturado * 100, 1);
                r.Cobrado = registro.TotalCobrado();
                r.PorCobrar = registro.TotalPorCobrar();
                r.CantidadPorCobrar = registro.CantidadConEstado(EstadoVenta.Pedido);
                r.CantidadPorEntregar = registro.CantidadConEstado(EstadoVenta.Pagado);
                r.TicketPromedio = r.Ventas == 0 ? 0 : Math.Round(r.Facturado / r.Ventas, 0);
                r.VentasConDetalle = registro.CantidadConDetalle();
                return r;
            }
        }

        // Todos los parámetros son opcionales: vacío = no filtra
        public List<VentaDto> ListarVentas(string texto, string desde, string hasta, int estado, int medio)
        {
            FiltroVentas filtro = new FiltroVentas();
            filtro.Texto = (texto ?? "").Trim();
            filtro.Desde = LeerFechaOpcional(desde, "desde");
            filtro.Hasta = LeerFechaOpcional(hasta, "hasta");
            if (estado != 0)
            {
                filtro.Estado = LeerEstado(estado);
            }
            if (medio != 0)
            {
                filtro.Medio = LeerMedio(medio);
            }

            lock (candado)
            {
                RegistroVentas registro = new RegistroVentas(Ventas());
                DateTime hoyDia = hoy();
                return registro.Filtrar(filtro)
                    .OrderByDescending(v => v.Fecha).ThenByDescending(v => v.Numero)
                    .Select(v => Convertir(v, hoyDia)).ToList();
            }
        }

        public VentaDto ObtenerVenta(int numero)
        {
            lock (candado)
            {
                Venta venta = new RegistroVentas(Ventas()).Buscar(numero);
                if (venta == null)
                {
                    throw new ErrorPanel("No existe la venta #" + numero + ".", 404);
                }
                return Convertir(venta, hoy());
            }
        }

        // Pendientes: Pedido (por cobrar) o Pagado (por entregar), lo más viejo primero
        public List<VentaDto> Pendientes(int estado)
        {
            EstadoVenta e = LeerEstado(estado);
            if (e == EstadoVenta.Entregado)
            {
                throw new ErrorPanel("Los pendientes son Pedido (por cobrar) o Pagado (por entregar).");
            }
            lock (candado)
            {
                DateTime hoyDia = hoy();
                return Reportes.Pendientes(Ventas(), e).Select(v => Convertir(v, hoyDia)).ToList();
            }
        }

        public List<ProductoDto> ListarProductos()
        {
            lock (candado)
            {
                return RepositorioSqlite.CargarProductos(rutaBase).Select(Convertir).ToList();
            }
        }

        public List<FilaMesDto> ReportePorMes()
        {
            lock (candado)
            {
                return Reportes.PorMes(Ventas()).Select(f => new FilaMesDto
                {
                    Mes = f.Mes,
                    Ventas = f.Ventas,
                    Facturado = f.Facturado,
                    Costos = f.Costos,
                    Ganancia = f.Ganancia,
                    Margen = Math.Round(f.Margen, 1),
                    VariacionGanancia = f.VariacionGanancia.HasValue ? Math.Round(f.VariacionGanancia.Value, 1) : (decimal?)null
                }).ToList();
            }
        }

        // orden: 1 = unidades, 2 = ganancia, 3 = margen
        public List<FilaProductoDto> ReportePorProducto(int orden)
        {
            if (orden < 1 || orden > 3)
            {
                orden = 1;
            }
            lock (candado)
            {
                return Reportes.PorProducto(Ventas(), (OrdenProductos)orden).Select(f => new FilaProductoDto
                {
                    Nombre = f.Nombre,
                    Unidades = f.Unidades,
                    Facturado = f.Facturado,
                    Costos = f.Costos,
                    Ganancia = f.Ganancia,
                    Margen = Math.Round(f.Margen, 1)
                }).ToList();
            }
        }

        // ---------- Escritura: ventas ----------

        public VentaDto CrearVenta(VentaNuevaDto datos)
        {
            if (datos == null)
            {
                throw new ErrorPanel("Faltan los datos de la venta.");
            }
            if (datos.Bolsa < 0 || datos.Sticker < 0 || datos.Tarjeta < 0)
            {
                throw new ErrorPanel("Bolsa, sticker y tarjeta no pueden ser negativos.");
            }
            LeerMedio(datos.Medio);
            EstadoVenta estado = LeerEstado(datos.Estado);
            DateTime fecha = LeerFechaOpcional(datos.Fecha, "fecha") ?? hoy();
            if (fecha.Date > hoy().Date)
            {
                throw new ErrorPanel("La fecha no puede ser futura.");
            }

            lock (candado)
            {
                List<Producto> productos = RepositorioSqlite.CargarProductos(rutaBase);
                CatalogoProductos catalogo = new CatalogoProductos(productos);
                List<ItemVenta> items = new List<ItemVenta>();
                decimal costoProductos;
                decimal precioSugerido = 0;
                string textoProductos;

                if (datos.Items != null && datos.Items.Count > 0)
                {
                    foreach (ItemNuevoDto pedido in datos.Items)
                    {
                        if (pedido.Cantidad < 1)
                        {
                            throw new ErrorPanel("La cantidad de cada producto tiene que ser 1 o más.");
                        }
                        Producto p = catalogo.Buscar(pedido.ProductoId);
                        if (p == null)
                        {
                            throw new ErrorPanel("No existe el producto " + pedido.ProductoId + " en el catálogo.");
                        }
                        items.Add(new ItemVenta(p.Nombre, pedido.Cantidad, p.Costo, p.Precio));
                    }
                    costoProductos = items.Sum(i => i.CostoTotal);
                    precioSugerido = items.Sum(i => i.PrecioTotal);
                    textoProductos = string.Join(" + ", items.Select(i => i.ATexto()));
                }
                else
                {
                    textoProductos = (datos.Productos ?? "").Trim();
                    if (textoProductos == "")
                    {
                        throw new ErrorPanel("Elegí al menos un producto o escribí qué se vendió.");
                    }
                    if (!datos.CostoProductos.HasValue || datos.CostoProductos.Value < 0)
                    {
                        throw new ErrorPanel("Falta el costo de los productos.");
                    }
                    costoProductos = datos.CostoProductos.Value;
                }

                decimal precio = datos.Precio ?? precioSugerido;
                if (precio <= 0)
                {
                    throw new ErrorPanel("El precio de venta tiene que ser mayor a cero.");
                }

                List<Venta> ventas = Ventas();
                RegistroVentas registro = new RegistroVentas(ventas);
                Venta venta = new Venta(registro.ProximoNumero(), fecha, textoProductos,
                    costoProductos + datos.Bolsa + datos.Sticker + datos.Tarjeta, precio,
                    (MedioPago)datos.Medio, estado);
                venta.Bolsa = datos.Bolsa;
                venta.Sticker = datos.Sticker;
                venta.Tarjeta = datos.Tarjeta;
                venta.EsRegalo = datos.EsRegalo;
                venta.Nota = LimpiarTexto(datos.Nota);
                venta.Items.AddRange(items);

                registro.Agregar(venta);
                GuardarVentas(ventas);
                return Convertir(venta, hoy());
            }
        }

        // Igual que en la consola: solo se puede avanzar (Pedido -> Pagado -> Entregado)
        public VentaDto CambiarEstado(int numero, int nuevoEstado)
        {
            EstadoVenta nuevo = LeerEstado(nuevoEstado);
            lock (candado)
            {
                List<Venta> ventas = Ventas();
                Venta venta = new RegistroVentas(ventas).Buscar(numero);
                if (venta == null)
                {
                    throw new ErrorPanel("No existe la venta #" + numero + ".", 404);
                }
                if (!venta.CambiarEstado(nuevo))
                {
                    throw new ErrorPanel("Solo se puede avanzar de estado, no volver para atrás.", 409);
                }
                GuardarVentas(ventas);
                return Convertir(venta, hoy());
            }
        }

        public void EliminarVenta(int numero)
        {
            lock (candado)
            {
                List<Venta> ventas = Ventas();
                if (!new RegistroVentas(ventas).Eliminar(numero))
                {
                    throw new ErrorPanel("No existe la venta #" + numero + ".", 404);
                }
                GuardarVentas(ventas);
            }
        }

        // ---------- Escritura: catálogo ----------

        public ProductoDto CrearProducto(ProductoNuevoDto datos)
        {
            string nombre = ValidarProducto(datos);
            lock (candado)
            {
                List<Producto> productos = RepositorioSqlite.CargarProductos(rutaBase);
                CatalogoProductos catalogo = new CatalogoProductos(productos);
                if (catalogo.ExisteNombre(nombre))
                {
                    throw new ErrorPanel("Ya hay un producto con ese nombre.", 409);
                }
                Producto nuevo = catalogo.Agregar(nombre, datos.Costo, datos.Precio);
                GuardarProductos(productos);
                return Convertir(nuevo);
            }
        }

        // Cambiar un producto no toca las ventas viejas: ellas guardan el costo y precio de ese día.
        public ProductoDto EditarProducto(int id, ProductoNuevoDto datos)
        {
            string nombre = ValidarProducto(datos);
            lock (candado)
            {
                List<Producto> productos = RepositorioSqlite.CargarProductos(rutaBase);
                CatalogoProductos catalogo = new CatalogoProductos(productos);
                Producto producto = catalogo.Buscar(id);
                if (producto == null)
                {
                    throw new ErrorPanel("No existe el producto " + id + ".", 404);
                }
                foreach (Producto otro in productos)
                {
                    if (otro.Id != id && string.Equals(otro.Nombre, nombre, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ErrorPanel("Ya hay un producto con ese nombre.", 409);
                    }
                }
                producto.Corregir(nombre, datos.Costo, datos.Precio);
                GuardarProductos(productos);
                return Convertir(producto);
            }
        }

        public void EliminarProducto(int id)
        {
            lock (candado)
            {
                List<Producto> productos = RepositorioSqlite.CargarProductos(rutaBase);
                if (!new CatalogoProductos(productos).Eliminar(id))
                {
                    throw new ErrorPanel("No existe el producto " + id + ".", 404);
                }
                GuardarProductos(productos);
            }
        }

        // ---------- Internos ----------

        private void GuardarVentas(List<Venta> ventas)
        {
            CopiasSeguridad.CopiaDelDia(rutaBase);
            RepositorioSqlite.GuardarVentas(ventas, rutaBase);
        }

        private void GuardarProductos(List<Producto> productos)
        {
            CopiasSeguridad.CopiaDelDia(rutaBase);
            RepositorioSqlite.GuardarProductos(productos, rutaBase);
        }

        private static string ValidarProducto(ProductoNuevoDto datos)
        {
            if (datos == null || string.IsNullOrWhiteSpace(datos.Nombre))
            {
                throw new ErrorPanel("El producto necesita un nombre.");
            }
            if (datos.Costo < 0 || datos.Precio < 0)
            {
                throw new ErrorPanel("El costo y el precio no pueden ser negativos.");
            }
            return datos.Nombre.Trim();
        }

        private static string LimpiarTexto(string texto)
        {
            return (texto ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private static DateTime? LeerFechaOpcional(string texto, string campo)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return null;
            }
            DateTime fecha;
            if (!DateTime.TryParseExact(texto.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out fecha))
            {
                throw new ErrorPanel("La fecha (" + campo + ") tiene que ser aaaa-mm-dd.");
            }
            return fecha;
        }

        private static EstadoVenta LeerEstado(int valor)
        {
            if (valor < 1 || valor > 3)
            {
                throw new ErrorPanel("Estado inválido: 1 = Pedido, 2 = Pagado, 3 = Entregado.");
            }
            return (EstadoVenta)valor;
        }

        private static MedioPago LeerMedio(int valor)
        {
            if (valor < 1 || valor > 4)
            {
                throw new ErrorPanel("Medio de pago inválido: 1 a 4.");
            }
            return (MedioPago)valor;
        }

        private static string NombreMedio(MedioPago medio)
        {
            switch (medio)
            {
                case MedioPago.MercadoPago: return "Mercado Pago";
                case MedioPago.TarjetaWeb: return "Tarjeta web";
                default: return medio.ToString();
            }
        }

        private static ProductoDto Convertir(Producto p)
        {
            return new ProductoDto { Id = p.Id, Nombre = p.Nombre, Costo = p.Costo, Precio = p.Precio, Ganancia = p.Ganancia };
        }

        private static VentaDto Convertir(Venta v, DateTime hoyDia)
        {
            return new VentaDto
            {
                Numero = v.Numero,
                Fecha = v.Fecha.ToString("yyyy-MM-dd"),
                Productos = v.Productos,
                CostoTotal = v.CostoTotal,
                PrecioVenta = v.PrecioVenta,
                Ganancia = v.Ganancia,
                Margen = Math.Round(v.MargenPorcentaje, 1),
                Medio = (int)v.Medio,
                MedioNombre = NombreMedio(v.Medio),
                Estado = (int)v.Estado,
                EstadoNombre = v.Estado.ToString(),
                Bolsa = v.Bolsa,
                Sticker = v.Sticker,
                Tarjeta = v.Tarjeta,
                EsRegalo = v.EsRegalo,
                Nota = v.Nota ?? "",
                DiasDeEspera = Reportes.DiasDeEspera(v, hoyDia),
                Items = v.Items.Select(i => new ItemDto
                {
                    Nombre = i.Nombre,
                    Cantidad = i.Cantidad,
                    CostoUnitario = i.CostoUnitario,
                    PrecioUnitario = i.PrecioUnitario
                }).ToList()
            };
        }
    }
}
