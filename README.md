# Sistema de pedidos Mateverso

Aplicación de consola en **C#** para registrar y seguir los pedidos de un emprendimiento de mates: carga de clientes, productos con stock, estados del pedido y resumen de ventas. Los productos y precios son de ejemplo..

## Qué hace

- Muestra el catálogo de productos con su stock.
- Crea pedidos con varios productos. Valida el id y el stock disponible, y descuenta el stock al agregar.
- Guarda el cliente de cada pedido (nombre y usuario de Instagram o WhatsApp).
- Cambia el estado del pedido: Pendiente, Pagado, Enviado, Entregado. Solo se puede avanzar, no volver atrás.
- Calcula lo cobrado y lo pendiente de cobro.
- Valida que lo que se escribe en el menú sea un número.

## Conceptos de programación orientada a objetos que practiqué

- Clases con **encapsulamiento** (propiedades con `private set`)
- **Composición**: un `Pedido` tiene un `Cliente` y una lista de `LineaPedido`
- Colecciones con `List<T>` y recorridos con `foreach`
- `enum` para los estados del pedido
- Propiedades calculadas (`Total`, `Subtotal`)
- Sobrescritura de `ToString()`
- Separación de responsabilidades: cada clase tiene una sola tarea

## Estructura

```
sistema-pedidos-mateverso/
├── Program.cs        Menú y entrada por consola
├── Tienda.cs         Maneja los productos y los pedidos
├── Pedido.cs         Pedido: cliente, líneas, total y estado
├── LineaPedido.cs    Un producto y su cantidad dentro de un pedido
├── Producto.cs       Producto con precio y stock
├── Cliente.cs        Nombre y contacto del cliente
└── EstadoPedido.cs   Estados posibles del pedido
```

## Cómo ejecutarlo

1. Clonar el repo y abrir la solución en **Visual Studio** (.NET).
2. Presionar **F5**.

## Estado del proyecto

Prototipo funcional. Próximos pasos:
- Guardar productos y pedidos en un archivo JSON para que no se pierdan al cerrar el programa
- Buscar pedidos por cliente
- Agregar un reporte de productos más vendidos

## Autor

Janez · Estudiante de la Tecnicatura Universitaria en Programación (UTN Haedo)