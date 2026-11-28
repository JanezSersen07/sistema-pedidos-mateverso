using SistemaPedidos;

// Panel web de Mateverso: una API chica sobre la misma base de datos que usa el sistema de consola,
// más la página del panel (carpeta wwwroot). Solo atiende en esta computadora (localhost).

string documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
string rutaBase = Path.Combine(documentos, "Mateverso", "mateverso.db");
int puerto = 5080;

for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--db") rutaBase = args[i + 1];
    if (args[i] == "--puerto" && int.TryParse(args[i + 1], out int p)) puerto = p;
}

if (!File.Exists(rutaBase))
{
    Console.WriteLine("No encuentro la base de datos en: " + rutaBase);
    Console.WriteLine("Abrí primero el sistema de consola una vez (crea y migra la base) y volvé a probar.");
    return 1;
}

PanelServicio servicio = new PanelServicio(rutaBase);

WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args });
builder.WebHost.UseUrls("http://127.0.0.1:" + puerto);
builder.Logging.SetMinimumLevel(LogLevel.Warning);
WebApplication app = builder.Build();

// Protección: solo se acepta el panel abierto desde localhost, y los cambios solo si vienen
// de la propia página (evita que otra web abierta en tu navegador toque tus ventas).
app.Use(async (contexto, siguiente) =>
{
    string host = contexto.Request.Host.Host;
    if (host != "localhost" && host != "127.0.0.1")
    {
        contexto.Response.StatusCode = 403;
        return;
    }
    if (contexto.Request.Method != "GET" && contexto.Request.Method != "HEAD")
    {
        string origen = contexto.Request.Headers.Origin.ToString();
        if (origen != "" && !origen.EndsWith("//" + contexto.Request.Host.Value))
        {
            contexto.Response.StatusCode = 403;
            return;
        }
    }
    await siguiente();
});

// Los errores de datos (ErrorPanel) vuelven como { "error": "..." } con el código que corresponda
app.Use(async (contexto, siguiente) =>
{
    try
    {
        await siguiente();
    }
    catch (ErrorPanel error)
    {
        contexto.Response.StatusCode = error.Codigo;
        await contexto.Response.WriteAsJsonAsync(new { error = error.Message });
    }
    catch (Exception error) when (error is System.Text.Json.JsonException || error is BadHttpRequestException)
    {
        contexto.Response.StatusCode = 400;
        await contexto.Response.WriteAsJsonAsync(new { error = "Los datos enviados no son válidos." });
    }
    catch (Exception error)
    {
        Console.WriteLine("Error inesperado: " + error);
        contexto.Response.StatusCode = 500;
        await contexto.Response.WriteAsJsonAsync(new { error = "Algo salió mal en el servidor. Mirá la ventana donde corre el panel." });
    }
});

app.UseDefaultFiles();
app.UseStaticFiles();

// ---------- Resumen y reportes ----------
app.MapGet("/api/resumen", () => servicio.Resumen());
app.MapGet("/api/reportes/mensual", () => servicio.ReportePorMes());
app.MapGet("/api/reportes/productos", (int? orden) => servicio.ReportePorProducto(orden ?? 1));
app.MapGet("/api/pendientes/{estado:int}", (int estado) => servicio.Pendientes(estado));

// ---------- Ventas ----------
app.MapGet("/api/ventas", (string texto, string desde, string hasta, int? estado, int? medio) =>
    servicio.ListarVentas(texto, desde, hasta, estado ?? 0, medio ?? 0));
app.MapGet("/api/ventas/{numero:int}", (int numero) => servicio.ObtenerVenta(numero));
app.MapPost("/api/ventas", (VentaNuevaDto datos) =>
    Results.Json(servicio.CrearVenta(datos), statusCode: 201));
app.MapPut("/api/ventas/{numero:int}/estado", (int numero, CambioEstado cambio) =>
    servicio.CambiarEstado(numero, cambio.Estado));
app.MapDelete("/api/ventas/{numero:int}", (int numero) =>
{
    servicio.EliminarVenta(numero);
    return Results.NoContent();
});

// ---------- Catálogo ----------
app.MapGet("/api/productos", () => servicio.ListarProductos());
app.MapPost("/api/productos", (ProductoNuevoDto datos) =>
    Results.Json(servicio.CrearProducto(datos), statusCode: 201));
app.MapPut("/api/productos/{id:int}", (int id, ProductoNuevoDto datos) => servicio.EditarProducto(id, datos));
app.MapDelete("/api/productos/{id:int}", (int id) =>
{
    servicio.EliminarProducto(id);
    return Results.NoContent();
});

app.MapFallback("/api/{**ruta}", () => Results.Json(new { error = "Esa dirección de la API no existe." }, statusCode: 404));

Console.WriteLine("Panel de Mateverso listo.");
Console.WriteLine("Datos:  " + rutaBase);
Console.WriteLine("Abrilo: http://localhost:" + puerto);
Console.WriteLine("Para cerrarlo: Ctrl+C. No lo uses a la vez con el sistema de consola.");
app.Run();
return 0;

public record CambioEstado(int Estado);
