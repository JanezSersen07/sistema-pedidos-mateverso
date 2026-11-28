// Panel de Mateverso. Sin librerías: todo se dibuja acá.
// Los textos que vienen de la base (nombres, notas) siempre se insertan con textContent,
// nunca como HTML, así que un nombre raro no puede romper ni inyectar nada en la página.
"use strict";

const pesos = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS", maximumFractionDigits: 0 });
const dinero = (n) => pesos.format(Math.round(n));
const porcentaje = (n) => n.toLocaleString("es-AR", { maximumFractionDigits: 1 }) + "%";
const NOMBRE_ESTADO = { 1: "Pedido", 2: "Pagado", 3: "Entregado" };
const MEDIOS = { 1: "Mercado Pago", 2: "Efectivo", 3: "Transferencia", 4: "Tarjeta web" };
const MESES = ["ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sep", "oct", "nov", "dic"];

// ---------- Utilidades ----------

function h(etiqueta, atributos, ...hijos) {
  const el = document.createElement(etiqueta);
  for (const [clave, valor] of Object.entries(atributos || {})) {
    if (valor === false || valor == null) continue;
    if (clave === "onclick" || clave === "oninput" || clave === "onchange" || clave === "onsubmit") {
      el.addEventListener(clave.slice(2), valor);
    } else if (clave === "clase") {
      el.className = valor;
    } else if (valor === true) {
      el.setAttribute(clave, "");
    } else {
      el.setAttribute(clave, valor);
    }
  }
  for (const hijo of hijos.flat()) {
    if (hijo == null || hijo === false) continue;
    el.append(hijo.nodeType ? hijo : document.createTextNode(String(hijo)));
  }
  return el;
}

function svg(etiqueta, atributos, ...hijos) {
  const el = document.createElementNS("http://www.w3.org/2000/svg", etiqueta);
  for (const [clave, valor] of Object.entries(atributos || {})) el.setAttribute(clave, valor);
  for (const hijo of hijos.flat()) {
    if (hijo != null) el.append(hijo.nodeType ? hijo : document.createTextNode(String(hijo)));
  }
  return el;
}

async function api(ruta, opciones = {}) {
  const pedido = { method: opciones.metodo || "GET", headers: {} };
  if (opciones.cuerpo !== undefined) {
    pedido.headers["Content-Type"] = "application/json";
    pedido.body = JSON.stringify(opciones.cuerpo);
  }
  let respuesta;
  try {
    respuesta = await fetch(ruta, pedido);
  } catch {
    throw new Error("No se pudo conectar con el panel. ¿Sigue abierta la ventana donde lo iniciaste?");
  }
  if (respuesta.status === 204) return null;
  let datos = null;
  try { datos = await respuesta.json(); } catch { /* sin cuerpo */ }
  if (!respuesta.ok) throw new Error((datos && datos.error) || "Algo salió mal (código " + respuesta.status + ").");
  return datos;
}

let temporizadorAviso;
function avisar(texto, malo = false) {
  const caja = document.getElementById("aviso");
  caja.textContent = texto;
  caja.className = "visible" + (malo ? " mal" : "");
  clearTimeout(temporizadorAviso);
  temporizadorAviso = setTimeout(() => { caja.className = ""; }, malo ? 6000 : 3000);
}

function fechaCorta(iso) {
  const [a, m, d] = iso.split("-");
  return d + "/" + m + "/" + a.slice(2);
}

function hoyIso() {
  const f = new Date();
  return f.getFullYear() + "-" + String(f.getMonth() + 1).padStart(2, "0") + "-" + String(f.getDate()).padStart(2, "0");
}

function etiquetaMes(clave) {
  const [a, m] = clave.split("-");
  return MESES[Number(m) - 1] + " " + a.slice(2);
}

const chipEstado = (estado) => h("span", { clase: "estado e" + estado }, NOMBRE_ESTADO[estado]);

function tabla(columnas, filas, alClickFila) {
  const cuerpo = h("tbody");
  for (const fila of filas) {
    const tr = h("tr", { clase: alClickFila ? "fila" : null, tabindex: alClickFila ? "0" : null });
    columnas.forEach((col) => {
      const td = h("td", { clase: [col.num ? "num" : "", col.clase || ""].join(" ").trim() || null });
      const contenido = col.celda(fila);
      td.append(contenido && contenido.nodeType ? contenido : document.createTextNode(contenido ?? ""));
      tr.append(td);
    });
    if (alClickFila) {
      tr.addEventListener("click", (e) => { if (!e.target.closest("button")) alClickFila(fila); });
      tr.addEventListener("keydown", (e) => { if (e.key === "Enter") alClickFila(fila); });
    }
    cuerpo.append(tr);
  }
  return h("div", { clase: "tabla-caja" },
    h("table", {},
      h("thead", {}, h("tr", {}, columnas.map((c) => h("th", { clase: c.num ? "num" : null }, c.titulo)))),
      cuerpo));
}

// ---------- Ventana ----------

const dialogo = document.getElementById("dialogo");

function abrirDialogo(titulo, cuerpo, pie) {
  dialogo.replaceChildren(
    h("div", { clase: "dialogo-cab" },
      h("h2", {}, titulo),
      h("button", { clase: "cerrar", "aria-label": "Cerrar", onclick: () => dialogo.close() }, "×")),
    h("div", { clase: "dialogo-cuerpo" }, cuerpo),
    pie ? h("div", { clase: "dialogo-pie" }, pie) : null);
  if (!dialogo.open) dialogo.showModal();
}

dialogo.addEventListener("click", (e) => { if (e.target === dialogo) dialogo.close(); });

// ---------- Rutas ----------

const rutas = { resumen: vistaResumen, ventas: vistaVentas, catalogo: vistaCatalogo, reportes: vistaReportes };
let rutaActual = "resumen";

async function irA() {
  const nombre = (location.hash.replace("#/", "") || "resumen").split("?")[0];
  rutaActual = rutas[nombre] ? nombre : "resumen";
  document.querySelectorAll(".menu a").forEach((a) => {
    a.classList.toggle("activa", a.dataset.ruta === rutaActual);
  });
  const contenido = document.getElementById("contenido");
  try {
    await rutas[rutaActual](contenido);
  } catch (error) {
    contenido.replaceChildren(
      h("h1", {}, "No se pudo cargar"),
      h("p", { clase: "error-form" }, error.message),
      h("button", { clase: "boton", onclick: irA }, "Reintentar"));
  }
}

function refrescar() { return irA(); }

window.addEventListener("hashchange", irA);

// ---------- Resumen ----------

async function vistaResumen(contenedor) {
  const [resumen, meses, porCobrar, porEntregar] = await Promise.all([
    api("/api/resumen"), api("/api/reportes/mensual"), api("/api/pendientes/1"), api("/api/pendientes/2")]);

  contenedor.replaceChildren(
    h("div", { clase: "cabecera" },
      h("div", {}, h("h1", {}, "Resumen"), h("p", {}, resumen.ventas + " ventas registradas")),
      h("button", { clase: "boton", onclick: () => nuevaVenta() }, "Nueva venta")),

    h("div", { clase: "cifras" },
      cifra("Ganancia", dinero(resumen.ganancia), "Margen " + porcentaje(resumen.margen)),
      cifra("Facturado", dinero(resumen.facturado), "Costos " + dinero(resumen.costos)),
      cifra("Ticket promedio", dinero(resumen.ticketPromedio), "por venta"),
      cifra("Por cobrar", dinero(resumen.porCobrar), resumen.cantidadPorCobrar + (resumen.cantidadPorCobrar === 1 ? " pedido sin pagar" : " pedidos sin pagar"))),

    h("section", { clase: "bloque" },
      h("h2", {}, "Mes a mes"),
      graficoMensual(meses)),

    h("section", { clase: "bloque" },
      h("h2", {}, "Para atender"),
      h("div", { clase: "dos-columnas" },
        listaPendientes("Por cobrar", porCobrar, 2, "Marcar pagado"),
        listaPendientes("Por entregar", porEntregar, 3, "Marcar entregado"))));
}

function cifra(nombre, valor, detalle) {
  return h("div", { clase: "cifra" },
    h("div", { clase: "nombre" }, nombre),
    h("div", { clase: "valor" }, valor),
    h("div", { clase: "detalle" }, detalle));
}

function listaPendientes(titulo, ventas, estadoSiguiente, textoBoton) {
  const caja = h("div", { clase: "tarjeta-lista" }, h("h3", {}, titulo + " (" + ventas.length + ")"));
  if (ventas.length === 0) {
    caja.append(h("p", { clase: "vacio" }, "Nada pendiente. Todo al día."));
    return caja;
  }
  for (const v of ventas) {
    caja.append(h("div", { clase: "pendiente" },
      h("div", {},
        h("a", { href: "#/ventas", onclick: (e) => { e.preventDefault(); detalleVenta(v.numero); } }, "#" + v.numero + " · " + dinero(v.precioVenta)),
        h("div", { clase: "sub" }, v.productos)),
      h("div", {},
        h("div", { clase: "sub" }, v.diasDeEspera + (v.diasDeEspera === 1 ? " día" : " días")),
        h("button", { clase: "boton chico suave", onclick: () => avanzar(v.numero, estadoSiguiente) }, textoBoton))));
  }
  return caja;
}

async function avanzar(numero, estado) {
  try {
    await api("/api/ventas/" + numero + "/estado", { metodo: "PUT", cuerpo: { estado } });
    avisar("Venta #" + numero + ": " + NOMBRE_ESTADO[estado].toLowerCase() + ".");
    if (dialogo.open) dialogo.close();
    await refrescar();
  } catch (error) {
    avisar(error.message, true);
  }
}

// Barras = facturado, línea = ganancia. Dibujado a mano en SVG.
function graficoMensual(meses) {
  if (meses.length === 0) return h("div", { clase: "grafico" }, h("p", { clase: "vacio" }, "Todavía no hay ventas para graficar."));
  const ancho = 900, alto = 280, izq = 64, der = 16, arriba = 16, abajo = 34;
  const maximo = Math.max(...meses.map((m) => m.facturado)) * 1.1 || 1;
  const x0 = izq, areaAncho = ancho - izq - der, areaAlto = alto - arriba - abajo;
  const paso = areaAncho / meses.length;
  const anchoBarra = Math.min(44, paso * 0.6);
  const y = (valor) => arriba + areaAlto - (valor / maximo) * areaAlto;

  const g = svg("svg", { viewBox: "0 0 " + ancho + " " + alto, role: "img", "aria-label": "Facturado y ganancia por mes" });
  for (let i = 0; i <= 4; i++) {
    const valor = (maximo / 4) * i, yy = y(valor);
    g.append(
      svg("line", { x1: x0, x2: ancho - der, y1: yy, y2: yy, stroke: "#243516", "stroke-opacity": i === 0 ? 0.4 : 0.1 }),
      svg("text", { x: x0 - 8, y: yy + 4, "text-anchor": "end", "font-size": 11, fill: "#5d6556" }, abreviar(valor)));
  }
  const puntos = [];
  meses.forEach((m, i) => {
    const cx = x0 + paso * i + paso / 2;
    const barra = svg("rect", { x: cx - anchoBarra / 2, y: y(m.facturado), width: anchoBarra, height: Math.max(0, y(0) - y(m.facturado)), rx: 3, fill: "#7a9a45" },
      svg("title", {}, etiquetaMes(m.mes) + "\nFacturado: " + dinero(m.facturado) + "\nGanancia: " + dinero(m.ganancia) + "\nVentas: " + m.ventas));
    g.append(barra, svg("text", { x: cx, y: alto - 12, "text-anchor": "middle", "font-size": 11, fill: "#5d6556" }, etiquetaMes(m.mes)));
    puntos.push([cx, y(m.ganancia), m]);
  });
  g.append(svg("polyline", { points: puntos.map((p) => p[0] + "," + p[1]).join(" "), fill: "none", stroke: "#a9782b", "stroke-width": 2.5, "stroke-linejoin": "round" }));
  for (const [cx, cy, m] of puntos) {
    g.append(svg("circle", { cx, cy, r: 4, fill: "#a9782b" }, svg("title", {}, etiquetaMes(m.mes) + ": ganancia " + dinero(m.ganancia))));
  }
  return h("div", { clase: "grafico" },
    h("div", { clase: "leyenda" },
      h("span", {}, h("i", { style: "background:#7a9a45" }), "Facturado"),
      h("span", {}, h("i", { style: "background:#a9782b" }), "Ganancia")),
    g);
}

function abreviar(valor) {
  if (valor >= 1000000) return "$" + (valor / 1000000).toLocaleString("es-AR", { maximumFractionDigits: 1 }) + " M";
  if (valor >= 1000) return "$" + Math.round(valor / 1000) + " mil";
  return "$" + Math.round(valor);
}

// ---------- Ventas ----------

const filtrosVentas = { texto: "", estado: "0", medio: "0", desde: "", hasta: "" };

async function vistaVentas(contenedor) {
  const parametros = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtrosVentas)) {
    if (valor && valor !== "0") parametros.set(clave, valor);
  }
  const ventas = await api("/api/ventas?" + parametros);
  const hayFiltros = Object.values(filtrosVentas).some((v) => v && v !== "0");
  const total = ventas.reduce((s, v) => s + v.precioVenta, 0);
  const ganancia = ventas.reduce((s, v) => s + v.ganancia, 0);

  const buscar = h("input", { type: "search", placeholder: "Producto o nota", value: filtrosVentas.texto, "aria-label": "Buscar" });
  let espera;
  buscar.addEventListener("input", () => {
    clearTimeout(espera);
    espera = setTimeout(() => {
      filtrosVentas.texto = buscar.value.trim();
      refrescarManteniendoFoco();
    }, 300);
  });

  const selector = (clave, opciones) => {
    const s = h("select", { onchange: () => { filtrosVentas[clave] = s.value; refrescar(); } },
      h("option", { value: "0" }, "Todos"),
      Object.entries(opciones).map(([valor, nombre]) => h("option", { value: valor, selected: filtrosVentas[clave] === valor }, nombre)));
    return s;
  };
  const fecha = (clave) => h("input", { type: "date", value: filtrosVentas[clave], onchange: (e) => { filtrosVentas[clave] = e.target.value; refrescar(); } });

  contenedor.replaceChildren(
    h("div", { clase: "cabecera" },
      h("div", {},
        h("h1", {}, "Ventas"),
        h("p", {}, ventas.length + (ventas.length === 1 ? " venta" : " ventas") + " · " + dinero(total) + " facturado · " + dinero(ganancia) + " de ganancia")),
      h("button", { clase: "boton", onclick: () => nuevaVenta() }, "Nueva venta")),

    h("div", { clase: "filtros" },
      h("label", {}, "Buscar", buscar),
      h("label", {}, "Estado", selector("estado", NOMBRE_ESTADO)),
      h("label", {}, "Medio de pago", selector("medio", MEDIOS)),
      h("label", {}, "Desde", fecha("desde")),
      h("label", {}, "Hasta", fecha("hasta")),
      hayFiltros ? h("button", { clase: "boton suave", onclick: () => { Object.assign(filtrosVentas, { texto: "", estado: "0", medio: "0", desde: "", hasta: "" }); refrescar(); } }, "Limpiar filtros") : null),

    ventas.length === 0
      ? h("p", { clase: "vacio" }, hayFiltros ? "Ninguna venta coincide con esos filtros." : "Todavía no cargaste ventas. Usá “Nueva venta”.")
      : tabla([
        { titulo: "N°", celda: (v) => "#" + v.numero },
        { titulo: "Fecha", celda: (v) => fechaCorta(v.fecha) },
        { titulo: "Productos", clase: "productos", celda: (v) => v.productos },
        { titulo: "Estado", celda: (v) => chipEstado(v.estado) },
        { titulo: "Cobrado", num: true, celda: (v) => dinero(v.precioVenta) },
        { titulo: "Ganancia", num: true, celda: (v) => dinero(v.ganancia) },
        { titulo: "Margen", num: true, celda: (v) => porcentaje(v.margen) }],
        ventas, (v) => detalleVenta(v.numero)));
}

async function refrescarManteniendoFoco() {
  await refrescar();
  const campo = document.querySelector(".filtros input[type=search]");
  if (campo) { campo.focus(); campo.setSelectionRange(campo.value.length, campo.value.length); }
}

async function detalleVenta(numero) {
  let v;
  try { v = await api("/api/ventas/" + numero); } catch (error) { avisar(error.message, true); return; }

  const filasItems = v.items.length
    ? h("ul", { clase: "detalle-lista" }, v.items.map((i) =>
      h("li", {}, h("span", {}, (i.cantidad > 1 ? i.cantidad + " × " : "") + i.nombre), h("span", {}, dinero(i.precioUnitario * i.cantidad)))))
    : h("p", { clase: "vacio" }, v.productos + " (sin detalle del catálogo)");

  const extras = [["Bolsa", v.bolsa], ["Sticker", v.sticker], ["Tarjeta", v.tarjeta]].filter((e) => e[1] > 0);

  const pie = [
    h("button", {
      clase: "boton peligro", onclick: async () => {
        if (!confirm("¿Eliminar la venta #" + v.numero + "? Se guarda una copia del día, pero esto no se puede deshacer desde el panel.")) return;
        try {
          await api("/api/ventas/" + v.numero, { metodo: "DELETE" });
          dialogo.close();
          avisar("Venta #" + v.numero + " eliminada.");
          await refrescar();
        } catch (error) { avisar(error.message, true); }
      }
    }, "Eliminar"),
    h("button", { clase: "boton suave", onclick: () => dialogo.close() }, "Cerrar")];
  if (v.estado < 3) {
    const siguiente = v.estado + 1;
    pie.push(h("button", { clase: "boton", onclick: () => avanzar(v.numero, siguiente) }, siguiente === 2 ? "Marcar pagado" : "Marcar entregado"));
  }

  abrirDialogo("Venta #" + v.numero + (v.esRegalo ? " · regalo" : ""),
    [
      h("p", {}, chipEstado(v.estado), " ", fechaCorta(v.fecha) + " · " + v.medioNombre),
      filasItems,
      h("ul", { clase: "detalle-lista" },
        extras.map((e) => h("li", {}, h("span", {}, e[0]), h("span", {}, dinero(e[1])))),
        h("li", {}, h("span", {}, "Costo total"), h("span", {}, dinero(v.costoTotal))),
        h("li", {}, h("b", {}, "Cobrado"), h("b", {}, dinero(v.precioVenta))),
        h("li", {}, h("b", {}, "Ganancia"), h("b", {}, dinero(v.ganancia) + " (" + porcentaje(v.margen) + ")"))),
      v.nota ? h("p", {}, h("b", {}, "Nota: "), v.nota) : null,
    ], pie);
}

// ---------- Nueva venta ----------

async function nuevaVenta() {
  let productos;
  try { productos = await api("/api/productos"); } catch (error) { avisar(error.message, true); return; }

  const lineas = []; // { producto, cantidad }
  const listaLineas = h("div", { clase: "lineas" });
  const sugerencias = h("div", { clase: "sugerencias", hidden: true });
  const buscador = h("input", { type: "search", placeholder: "Buscar producto del catálogo…", "aria-label": "Buscar producto", autocomplete: "off" });
  const vista = h("div", { clase: "vista-previa" });
  const error = h("div", { clase: "error-form", hidden: true });

  const bolsa = numero("Bolsa"), sticker = numero("Sticker"), tarjeta = numero("Tarjeta");
  const precio = h("input", { type: "number", min: "0", step: "1", inputmode: "numeric" });
  const fecha = h("input", { type: "date", value: hoyIso(), max: hoyIso() });
  const medio = h("select", {}, Object.entries(MEDIOS).map(([v, n]) => h("option", { value: v }, n)));
  const estado = h("select", {}, Object.entries(NOMBRE_ESTADO).map(([v, n]) => h("option", { value: v }, n)));
  const nota = h("textarea", { rows: "2", maxlength: "300" });
  const regalo = h("input", { type: "checkbox" });

  function numero(nombre) {
    return h("input", { type: "number", min: "0", step: "1", inputmode: "numeric", placeholder: "0", "aria-label": nombre });
  }
  const valor = (campo) => Number(campo.value) || 0;

  function totales() {
    const costoProductos = lineas.reduce((s, l) => s + l.producto.costo * l.cantidad, 0);
    const sugerido = lineas.reduce((s, l) => s + l.producto.precio * l.cantidad, 0);
    const costo = costoProductos + valor(bolsa) + valor(sticker) + valor(tarjeta);
    const cobrado = precio.value === "" ? sugerido : Number(precio.value);
    return { costo, sugerido, cobrado, ganancia: cobrado - costo };
  }

  function actualizar() {
    const t = totales();
    precio.placeholder = lineas.length ? String(Math.round(t.sugerido)) : "";
    vista.replaceChildren(
      h("div", {}, h("b", {}, dinero(t.costo)), h("span", {}, "Costo total")),
      h("div", {}, h("b", {}, dinero(t.cobrado)), h("span", {}, "Se cobra")),
      h("div", {}, h("b", {}, dinero(t.ganancia)), h("span", {}, t.cobrado > 0 ? "Ganancia (" + porcentaje(t.ganancia / t.cobrado * 100) + ")" : "Ganancia")));
    listaLineas.replaceChildren(...(lineas.length ? lineas.map((l, i) =>
      h("div", { clase: "linea" },
        h("span", {}, l.producto.nombre),
        h("input", { type: "number", min: "1", step: "1", value: l.cantidad, "aria-label": "Cantidad de " + l.producto.nombre, oninput: (e) => { l.cantidad = Math.max(1, Math.floor(Number(e.target.value)) || 1); actualizarSoloVista(); } }),
        h("span", {}, dinero(l.producto.precio)),
        h("button", { clase: "boton chico peligro", type: "button", "aria-label": "Quitar " + l.producto.nombre, onclick: () => { lineas.splice(i, 1); actualizar(); } }, "Quitar")))
      : [h("p", { clase: "vacio", style: "padding:10px 12px;margin:0" }, "Todavía no agregaste productos.")]));
  }

  function actualizarSoloVista() {
    const t = totales();
    vista.children[0].firstChild.textContent = dinero(t.costo);
    vista.children[1].firstChild.textContent = dinero(t.cobrado);
    vista.children[2].firstChild.textContent = dinero(t.ganancia);
    precio.placeholder = String(Math.round(t.sugerido));
  }

  function mostrarSugerencias() {
    const texto = buscador.value.trim().toLowerCase();
    const coincidencias = productos.filter((p) => p.nombre.toLowerCase().includes(texto)).slice(0, 30);
    sugerencias.replaceChildren(...(coincidencias.length ? coincidencias.map((p) =>
      h("button", {
        type: "button", onclick: () => {
          const existente = lineas.find((l) => l.producto.id === p.id);
          if (existente) existente.cantidad += 1; else lineas.push({ producto: p, cantidad: 1 });
          buscador.value = "";
          sugerencias.hidden = true;
          actualizar();
          buscador.focus();
        }
      }, h("span", {}, p.nombre), h("span", {}, dinero(p.precio))))
      : [h("p", { clase: "vacio", style: "padding:8px 12px;margin:0" }, "Ese producto no está en el catálogo.")]));
    sugerencias.hidden = false;
  }

  buscador.addEventListener("input", mostrarSugerencias);
  buscador.addEventListener("focus", mostrarSugerencias);
  buscador.addEventListener("keydown", (e) => { if (e.key === "Escape" && !sugerencias.hidden) { e.stopPropagation(); e.preventDefault(); sugerencias.hidden = true; } });
  const cerrarSugerencias = (e) => { if (!e.target.closest(".buscador")) sugerencias.hidden = true; };
  dialogo.addEventListener("click", cerrarSugerencias);
  dialogo.addEventListener("close", () => dialogo.removeEventListener("click", cerrarSugerencias), { once: true });
  [bolsa, sticker, tarjeta, precio].forEach((c) => c.addEventListener("input", actualizarSoloVista));

  const campo = (titulo, control, clase) => h("label", { clase: "campo " + (clase || "") }, titulo, control);

  const guardar = h("button", { clase: "boton", type: "button" }, "Guardar venta");
  guardar.addEventListener("click", async () => {
    error.hidden = true;
    if (lineas.length === 0) { error.textContent = "Agregá al menos un producto."; error.hidden = false; return; }
    guardar.disabled = true;
    try {
      const creada = await api("/api/ventas", {
        metodo: "POST",
        cuerpo: {
          fecha: fecha.value,
          items: lineas.map((l) => ({ productoId: l.producto.id, cantidad: l.cantidad })),
          bolsa: valor(bolsa), sticker: valor(sticker), tarjeta: valor(tarjeta),
          precio: precio.value === "" ? null : Number(precio.value),
          medio: Number(medio.value), estado: Number(estado.value),
          esRegalo: regalo.checked, nota: nota.value,
        },
      });
      dialogo.close();
      avisar("Venta #" + creada.numero + " guardada.");
      if (location.hash === "#/ventas" || location.hash === "#/resumen" || location.hash === "") await refrescar();
      else location.hash = "#/ventas";
    } catch (e) {
      error.textContent = e.message;
      error.hidden = false;
      guardar.disabled = false;
    }
  });

  abrirDialogo("Nueva venta",
    [
      error,
      h("div", { clase: "buscador" }, buscador, sugerencias),
      listaLineas,
      h("div", { clase: "fila-campos" },
        campo("Bolsa", bolsa), campo("Sticker", sticker), campo("Tarjeta de curado", tarjeta)),
      campo("Precio de venta (vacío = suma de precios del catálogo)", precio),
      vista,
      h("div", { clase: "fila-campos" },
        campo("Fecha", fecha), campo("Medio de pago", medio), campo("Estado", estado)),
      campo("Nota (opcional)", nota),
      h("label", { clase: "campo casilla" }, regalo, "Es un regalo"),
    ],
    [h("button", { clase: "boton suave", type: "button", onclick: () => dialogo.close() }, "Cancelar"), guardar]);
  actualizar();
  buscador.focus();
}

// ---------- Catálogo ----------

let textoCatalogo = "";

async function vistaCatalogo(contenedor) {
  const productos = await api("/api/productos");
  const buscar = h("input", { type: "search", placeholder: "Buscar producto", value: textoCatalogo, "aria-label": "Buscar producto" });
  const zonaTabla = h("div");

  function dibujar() {
    const texto = textoCatalogo.toLowerCase();
    const visibles = productos.filter((p) => p.nombre.toLowerCase().includes(texto));
    zonaTabla.replaceChildren(visibles.length === 0
      ? h("p", { clase: "vacio" }, "Ningún producto coincide.")
      : tabla([
        { titulo: "Producto", celda: (p) => p.nombre },
        { titulo: "Costo", num: true, celda: (p) => dinero(p.costo) },
        { titulo: "Precio", num: true, celda: (p) => dinero(p.precio) },
        { titulo: "Ganancia", num: true, celda: (p) => dinero(p.ganancia) }],
        visibles, (p) => editarProducto(p)));
  }
  buscar.addEventListener("input", () => { textoCatalogo = buscar.value; dibujar(); });

  contenedor.replaceChildren(
    h("div", { clase: "cabecera" },
      h("div", {}, h("h1", {}, "Catálogo"), h("p", {}, productos.length + " productos. Cambiar un precio no modifica las ventas ya cargadas.")),
      h("button", { clase: "boton", onclick: () => editarProducto(null) }, "Nuevo producto")),
    h("div", { clase: "filtros" }, h("label", {}, "Buscar", buscar)),
    zonaTabla);
  dibujar();
}

function editarProducto(producto) {
  const nombre = h("input", { type: "text", value: producto ? producto.nombre : "", maxlength: "120", required: true });
  const costo = h("input", { type: "number", min: "0", step: "1", value: producto ? Math.round(producto.costo) : "", inputmode: "numeric" });
  const precio = h("input", { type: "number", min: "0", step: "1", value: producto ? Math.round(producto.precio) : "", inputmode: "numeric" });
  const error = h("div", { clase: "error-form", hidden: true });

  const guardar = h("button", { clase: "boton", type: "button" }, producto ? "Guardar cambios" : "Crear producto");
  guardar.addEventListener("click", async () => {
    error.hidden = true;
    guardar.disabled = true;
    try {
      await api(producto ? "/api/productos/" + producto.id : "/api/productos", {
        metodo: producto ? "PUT" : "POST",
        cuerpo: { nombre: nombre.value, costo: Number(costo.value) || 0, precio: Number(precio.value) || 0 },
      });
      dialogo.close();
      avisar(producto ? "Producto actualizado." : "Producto creado.");
      await refrescar();
    } catch (e) {
      error.textContent = e.message;
      error.hidden = false;
      guardar.disabled = false;
    }
  });

  const pie = [];
  if (producto) {
    pie.push(h("button", {
      clase: "boton peligro", type: "button", onclick: async () => {
        if (!confirm("¿Eliminar “" + producto.nombre + "” del catálogo? Las ventas que ya lo tienen no se tocan.")) return;
        try {
          await api("/api/productos/" + producto.id, { metodo: "DELETE" });
          dialogo.close();
          avisar("Producto eliminado.");
          await refrescar();
        } catch (e) { error.textContent = e.message; error.hidden = false; }
      }
    }, "Eliminar"));
  }
  pie.push(h("button", { clase: "boton suave", type: "button", onclick: () => dialogo.close() }, "Cancelar"), guardar);

  abrirDialogo(producto ? "Editar producto" : "Nuevo producto",
    [error,
      h("label", { clase: "campo" }, "Nombre", nombre),
      h("div", { clase: "fila-campos" },
        h("label", { clase: "campo" }, "Costo", costo),
        h("label", { clase: "campo" }, "Precio de venta", precio))],
    pie);
  nombre.focus();
}

// ---------- Reportes ----------

let ordenProductos = 1;

async function vistaReportes(contenedor) {
  const [meses, productos] = await Promise.all([api("/api/reportes/mensual"), api("/api/reportes/productos?orden=" + ordenProductos)]);

  const orden = (valor, texto) => h("button", {
    "aria-pressed": String(ordenProductos === valor),
    onclick: () => { ordenProductos = valor; refrescar(); },
  }, texto);

  contenedor.replaceChildren(
    h("div", { clase: "cabecera" }, h("div", {}, h("h1", {}, "Reportes"), h("p", {}, "Calculados sobre todas las ventas cargadas."))),

    h("section", { clase: "bloque" },
      h("h2", {}, "Por mes"),
      meses.length === 0 ? h("p", { clase: "vacio" }, "Sin datos todavía.") :
        tabla([
          { titulo: "Mes", celda: (m) => etiquetaMes(m.mes) },
          { titulo: "Ventas", num: true, celda: (m) => m.ventas },
          { titulo: "Facturado", num: true, celda: (m) => dinero(m.facturado) },
          { titulo: "Costos", num: true, celda: (m) => dinero(m.costos) },
          { titulo: "Ganancia", num: true, celda: (m) => dinero(m.ganancia) },
          { titulo: "Margen", num: true, celda: (m) => porcentaje(m.margen) },
          {
            titulo: "vs. mes anterior", num: true, celda: (m) => m.variacionGanancia == null ? "–"
              : h("span", { clase: m.variacionGanancia >= 0 ? "sube" : "baja" }, (m.variacionGanancia >= 0 ? "+" : "") + porcentaje(m.variacionGanancia))
          }],
          [...meses].reverse())),

    h("section", { clase: "bloque" },
      h("h2", {}, "Por producto"),
      h("div", { clase: "pestanas" }, orden(1, "Más vendidos"), orden(2, "Más ganancia"), orden(3, "Mejor margen")),
      productos.length === 0 ? h("p", { clase: "vacio" }, "Ninguna venta tiene detalle de productos todavía.") :
        tabla([
          { titulo: "Producto", celda: (p) => p.nombre },
          { titulo: "Unidades", num: true, celda: (p) => p.unidades },
          { titulo: "Facturado", num: true, celda: (p) => dinero(p.facturado) },
          { titulo: "Ganancia", num: true, celda: (p) => dinero(p.ganancia) },
          { titulo: "Margen", num: true, celda: (p) => porcentaje(p.margen) }],
          productos)));
}

irA();
