//////// Vinculador.cs ////////
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SistemaPedidos
{
    // Qué tan seguro es el parecido entre lo que dice una venta vieja y un producto del catálogo
    public enum Coincidencia
    {
        Exacta,     // mismo nombre (sin importar mayúsculas, tildes ni signos)
        Parecida,   // hay un producto claramente más parecido que los demás: conviene confirmarlo
        Ninguna     // no hay un candidato claro: hay que elegirlo a mano
    }

    // Una parte del texto de una venta ("2 x Bombilla") y el producto que se le propone
    public class PropuestaParte
    {
        public string Texto { get; set; }
        public int Cantidad { get; set; }
        public Coincidencia Tipo { get; set; }
        public Producto Producto { get; set; }                 // null si Tipo es Ninguna
        public List<Producto> Alternativas { get; set; }       // los más parecidos, de más a menos

        public PropuestaParte()
        {
            Alternativas = new List<Producto>();
        }
    }

    // Un producto elegido para una venta, con su cantidad
    public class ParteElegida
    {
        public Producto Producto { get; private set; }
        public int Cantidad { get; private set; }

        public ParteElegida(Producto producto, int cantidad)
        {
            Producto = producto;
            Cantidad = cantidad < 1 ? 1 : cantidad;
        }
    }

    // Une las ventas viejas (que solo tienen un texto como "Imperial Negro + Bombilla")
    // con los productos del catálogo, para que cuenten en los reportes de productos.
    public static class Vinculador
    {
        private const decimal PARECIDO_MINIMO = 0.6m;   // qué tan parecido tiene que ser para proponerlo
        private const decimal VENTAJA_MINIMA = 0.15m;   // cuánto más parecido que el segundo mejor

        // "Imperial Negro + 2 x Bombilla" -> ["Imperial Negro", "2 x Bombilla"]
        public static List<string> Partes(string productos)
        {
            List<string> partes = new List<string>();
            if (string.IsNullOrWhiteSpace(productos))
            {
                return partes;
            }
            foreach (string parte in productos.Split('+'))
            {
                string limpia = parte.Trim();
                if (limpia != "")
                {
                    partes.Add(limpia);
                }
            }
            return partes;
        }

        // Minúsculas, sin tildes ni signos y con un solo espacio entre palabras
        public static string Normalizar(string texto)
        {
            if (texto == null)
            {
                return "";
            }
            const string conTilde = "áàäâéèëêíìïîóòöôúùüûñ";
            const string sinTilde = "aaaaeeeeiiiioooouuuun";

            StringBuilder sb = new StringBuilder();
            foreach (char c in texto.ToLowerInvariant())
            {
                int posicion = conTilde.IndexOf(c);
                if (posicion >= 0)
                {
                    sb.Append(sinTilde[posicion]);
                }
                else if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(' ');
                }
            }
            return string.Join(" ", sb.ToString().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        // Separa la cantidad del nombre: "2 x Bombilla", "2x Bombilla" o "Bombilla x2"
        public static string QuitarCantidad(string parte, out int cantidad)
        {
            cantidad = 1;
            string texto = parte.Trim();

            Match inicio = Regex.Match(texto, @"^(\d+)\s*[xX]\s*(.+)$");
            if (inicio.Success)
            {
                cantidad = int.Parse(inicio.Groups[1].Value);
                return inicio.Groups[2].Value.Trim();
            }
            Match final = Regex.Match(texto, @"^(.+?)\s+[xX]\s*(\d+)$");
            if (final.Success)
            {
                cantidad = int.Parse(final.Groups[2].Value);
                return final.Groups[1].Value.Trim();
            }
            return texto;
        }

        // Interpreta todo el texto de una venta, parte por parte
        public static List<PropuestaParte> Interpretar(string productos, CatalogoProductos catalogo)
        {
            List<PropuestaParte> propuestas = new List<PropuestaParte>();
            foreach (string parte in Partes(productos))
            {
                propuestas.Add(InterpretarParte(parte, catalogo));
            }
            return propuestas;
        }

        public static PropuestaParte InterpretarParte(string parte, CatalogoProductos catalogo)
        {
            int cantidad;
            string nombre = QuitarCantidad(parte, out cantidad);
            string buscado = Normalizar(nombre);

            PropuestaParte propuesta = new PropuestaParte();
            propuesta.Texto = parte;
            propuesta.Cantidad = cantidad < 1 ? 1 : cantidad;
            propuesta.Tipo = Coincidencia.Ninguna;

            // 1) Nombre igual. Si hay dos productos con el mismo nombre no se elige solo.
            List<Producto> iguales = new List<Producto>();
            foreach (Producto p in catalogo.Productos)
            {
                if (Normalizar(p.Nombre) == buscado)
                {
                    iguales.Add(p);
                }
            }
            if (iguales.Count == 1)
            {
                propuesta.Tipo = Coincidencia.Exacta;
                propuesta.Producto = iguales[0];
                propuesta.Alternativas.Add(iguales[0]);
                return propuesta;
            }

            // 2) Los más parecidos, por palabras en común
            List<KeyValuePair<Producto, decimal>> puntajes = new List<KeyValuePair<Producto, decimal>>();
            foreach (Producto p in catalogo.Productos)
            {
                decimal puntaje = Parecido(buscado, Normalizar(p.Nombre));
                if (puntaje > 0)
                {
                    puntajes.Add(new KeyValuePair<Producto, decimal>(p, puntaje));
                }
            }
            puntajes.Sort((a, b) =>
            {
                int comparacion = b.Value.CompareTo(a.Value);
                return comparacion != 0 ? comparacion : a.Key.Id.CompareTo(b.Key.Id);
            });

            for (int i = 0; i < puntajes.Count && i < 5; i++)
            {
                propuesta.Alternativas.Add(puntajes[i].Key);
            }

            if (iguales.Count == 0 && puntajes.Count > 0 && puntajes[0].Value >= PARECIDO_MINIMO)
            {
                bool claro = puntajes.Count == 1 || puntajes[0].Value - puntajes[1].Value >= VENTAJA_MINIMA;
                if (claro)
                {
                    propuesta.Tipo = Coincidencia.Parecida;
                    propuesta.Producto = puntajes[0].Key;
                }
            }
            return propuesta;
        }

        // Palabras en común dividido palabras en total (de 0 a 1)
        private static decimal Parecido(string a, string b)
        {
            HashSet<string> palabrasA = Palabras(a);
            HashSet<string> palabrasB = Palabras(b);
            if (palabrasA.Count == 0 || palabrasB.Count == 0)
            {
                return 0;
            }
            int comunes = 0;
            foreach (string palabra in palabrasA)
            {
                if (palabrasB.Contains(palabra))
                {
                    comunes++;
                }
            }
            int total = palabrasA.Count + palabrasB.Count - comunes;
            return (decimal)comunes / total;
        }

        private static HashSet<string> Palabras(string normalizado)
        {
            HashSet<string> palabras = new HashSet<string>();
            foreach (string palabra in normalizado.Split(' '))
            {
                if (palabra.Length > 1)
                {
                    palabras.Add(palabra);
                }
            }
            return palabras;
        }

        // Arma el detalle de productos de una venta vieja. Los valores del catálogo de hoy
        // se ajustan para que coincidan con lo que realmente se registró en la venta:
        // los costos de los productos suman (CostoTotal - bolsa - sticker - tarjeta)
        // y los precios suman el precio de venta. Así los totales de la venta no cambian.
        public static List<ItemVenta> CrearItems(Venta venta, List<ParteElegida> partes)
        {
            List<ItemVenta> items = new List<ItemVenta>();
            if (partes.Count == 0)
            {
                return items;
            }

            decimal costoProductos = venta.CostoTotal - venta.Bolsa - venta.Sticker - venta.Tarjeta;
            if (costoProductos < 0)
            {
                costoProductos = 0;
            }

            decimal[] pesosCosto = new decimal[partes.Count];
            decimal[] pesosPrecio = new decimal[partes.Count];
            decimal[] cantidades = new decimal[partes.Count];
            for (int i = 0; i < partes.Count; i++)
            {
                cantidades[i] = partes[i].Cantidad;
                pesosCosto[i] = partes[i].Producto.Costo * partes[i].Cantidad;
                pesosPrecio[i] = partes[i].Producto.Precio * partes[i].Cantidad;
            }

            // Si no hay datos en el catálogo para repartir, se usa el otro valor, y si no, la cantidad
            decimal[] costos = Repartir(costoProductos, partes, pesosCosto, pesosPrecio, cantidades);
            decimal[] precios = Repartir(venta.PrecioVenta, partes, pesosPrecio, pesosCosto, cantidades);

            for (int i = 0; i < partes.Count; i++)
            {
                items.Add(new ItemVenta(partes[i].Producto.Nombre, partes[i].Cantidad, costos[i], precios[i]));
            }
            return items;
        }

        // Reparte "objetivo" entre las partes según los pesos (el primero que sume algo)
        // y devuelve el valor de UNA unidad de cada parte, con dos decimales.
        private static decimal[] Repartir(decimal objetivo, List<ParteElegida> partes,
                                          decimal[] pesos1, decimal[] pesos2, decimal[] pesos3)
        {
            decimal[] pesos = pesos1;
            if (Suma(pesos) <= 0)
            {
                pesos = pesos2;
            }
            if (Suma(pesos) <= 0)
            {
                pesos = pesos3;
            }
            decimal total = Suma(pesos);

            decimal[] unitarios = new decimal[partes.Count];
            for (int i = 0; i < partes.Count; i++)
            {
                decimal lineaTotal = objetivo * pesos[i] / total;
                unitarios[i] = Math.Round(lineaTotal / partes[i].Cantidad, 2);
            }
            return unitarios;
        }

        private static decimal Suma(decimal[] valores)
        {
            decimal suma = 0;
            foreach (decimal v in valores)
            {
                suma += v;
            }
            return suma;
        }
    }
}
