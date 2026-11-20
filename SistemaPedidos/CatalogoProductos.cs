//////// CatalogoProductos.cs ////////
using System;
using System.Collections.Generic;

namespace SistemaPedidos
{
    // La lista de productos que vendés, con su costo y su precio.
    public class CatalogoProductos
    {
        private List<Producto> productos;

        public CatalogoProductos(List<Producto> productosIniciales)
        {
            productos = productosIniciales;
        }

        public List<Producto> Productos
        {
            get { return productos; }
        }

        public int Cantidad
        {
            get { return productos.Count; }
        }

        public int ProximoId()
        {
            int mayor = 0;
            foreach (Producto p in productos)
            {
                if (p.Id > mayor)
                {
                    mayor = p.Id;
                }
            }
            return mayor + 1;
        }

        public Producto Agregar(string nombre, decimal costo, decimal precio)
        {
            Producto producto = new Producto(ProximoId(), nombre, costo, precio);
            productos.Add(producto);
            return producto;
        }

        public Producto Buscar(int id)
        {
            foreach (Producto p in productos)
            {
                if (p.Id == id)
                {
                    return p;
                }
            }
            return null;
        }

        // Productos cuyo nombre contiene el texto (sin importar mayúsculas)
        public List<Producto> BuscarPorTexto(string texto)
        {
            List<Producto> resultado = new List<Producto>();
            foreach (Producto p in productos)
            {
                if (p.Nombre.IndexOf(texto, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    resultado.Add(p);
                }
            }
            return resultado;
        }

        public bool ExisteNombre(string nombre)
        {
            foreach (Producto p in productos)
            {
                if (string.Equals(p.Nombre, nombre, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Eliminar(int id)
        {
            Producto producto = Buscar(id);
            if (producto == null)
            {
                return false;
            }
            productos.Remove(producto);
            return true;
        }
    }
}
