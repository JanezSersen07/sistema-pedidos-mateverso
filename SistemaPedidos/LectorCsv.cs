//////// LectorCsv.cs ////////
using System.Collections.Generic;
using System.Text;

namespace SistemaPedidos
{
    // Lee y escribe archivos CSV. Respeta las comillas: un campo entre comillas
    // puede tener el separador o saltos de línea adentro.
    public static class LectorCsv
    {
        public static List<List<string>> Leer(string texto, char separador)
        {
            List<List<string>> filas = new List<List<string>>();
            List<string> fila = new List<string>();
            StringBuilder campo = new StringBuilder();
            bool entreComillas = false;

            for (int i = 0; i < texto.Length; i++)
            {
                char c = texto[i];

                if (entreComillas)
                {
                    if (c == '"')
                    {
                        // Dos comillas seguidas son una comilla dentro del texto
                        if (i + 1 < texto.Length && texto[i + 1] == '"')
                        {
                            campo.Append('"');
                            i++;
                        }
                        else
                        {
                            entreComillas = false;
                        }
                    }
                    else
                    {
                        campo.Append(c);
                    }
                }
                else if (c == '"')
                {
                    entreComillas = true;
                }
                else if (c == separador)
                {
                    fila.Add(campo.ToString());
                    campo.Clear();
                }
                else if (c == '\n')
                {
                    fila.Add(campo.ToString());
                    campo.Clear();
                    filas.Add(fila);
                    fila = new List<string>();
                }
                else if (c != '\r')
                {
                    campo.Append(c);
                }
            }

            // Última fila, si el archivo no termina con un salto de línea
            if (campo.Length > 0 || fila.Count > 0)
            {
                fila.Add(campo.ToString());
                filas.Add(fila);
            }
            return filas;
        }

        // Google Sheets separa con comas y el Excel en español con punto y coma.
        // Cuenta cuál aparece más en el principio del archivo.
        public static char DetectarSeparador(string texto)
        {
            int largo = texto.Length < 3000 ? texto.Length : 3000;
            int comas = 0;
            int puntoYComa = 0;
            for (int i = 0; i < largo; i++)
            {
                if (texto[i] == ',') comas++;
                if (texto[i] == ';') puntoYComa++;
            }
            return puntoYComa > comas ? ';' : ',';
        }

        // Prepara un texto para escribirlo en un CSV
        public static string Escapar(string valor, char separador)
        {
            if (valor == null)
            {
                return "";
            }
            bool necesitaComillas = valor.IndexOf(separador) >= 0 || valor.IndexOf('"') >= 0
                                    || valor.IndexOf('\n') >= 0 || valor.IndexOf('\r') >= 0;
            if (!necesitaComillas)
            {
                return valor;
            }
            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        }
    }
}