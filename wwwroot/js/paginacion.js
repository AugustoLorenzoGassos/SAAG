// Configuración de la paginación. Aplica a la misma tabla
const REGISTROS_POR_PAGINA = 10;
let paginaActual = 1;
/**
 * Función principal para paginar las filas de la tabla #tablaRegistros
 * Llama a esta función después de insertar nuevos <tr> en #tbodyRegistros.
 */
function inicializarPaginacion() {
    const tbody = document.getElementById('tbodyRegistros');
    // Obtenemos solo las filas directas (excluyendo mensajes vacíos si aplica)
    const filas = Array.from(tbody.querySelectorAll("tr"));

    // Si no hay datos o hay un mensaje de "Sin datos / Filtre", limpiamos controles
    if (filas.length === 0 || filas[0].querySelector("td[colspan]")) {
        document.getElementById("ulPaginacion").innerHTML = "";
        document.getElementById("infoPaginacion").textContent = "";
        return;
    }

    paginaActual = 1;
    mostrarPagina(filas, paginaActual);
    generarBotones(filas);
}

/**
 * Muestra solo las filas correspondientes a la página seleccionada
 */
function mostrarPagina(filas, pagina) {
    const inicio = (pagina - 1) * REGISTROS_POR_PAGINA;
    const fin = inicio + REGISTROS_POR_PAGINA;

    filas.forEach((fila, index) => {
        if (index >= inicio && index < fin) {
            fila.style.display = ""; // Muestra la fila
        } else {
            fila.style.display = "none"; // Oculta la fila
        }
    });

    // Actualiza el texto de información (ej. Mostrando 1-5 de 12 registros)
    const info = document.getElementById("infoPaginacion");
    const total = filas.length;
    const mostrandoHasta = Math.min(fin, total);
    info.textContent = `Mostrando ${inicio + 1} a ${mostrandoHasta} de ${total} registros`;
}

/**
 * Genera los botones de la paginación
 */
function generarBotones(filas) {
    const ulPaginacion = document.getElementById("ulPaginacion");
    ulPaginacion.innerHTML = "";

    const totalPaginas = Math.ceil(filas.length / REGISTROS_POR_PAGINA);
    if (totalPaginas <= 1) return;

    // Helper para crear ítems <li>
    const crearItem = (texto, pagina, esActivo = false, esDeshabilitado = false) => {
        const li = document.createElement("li");
        li.className = `page-item ${esActivo ? 'active' : ''} ${esDeshabilitado ? 'disabled' : ''}`;

        const claseLink = esActivo ? 'bg-secondary text-white border border-secondary' : '';
        li.innerHTML = `<a class="page-link ${claseLink}" href="#">${texto}</a>`;

        if (!esDeshabilitado && pagina !== null) {
            li.addEventListener("click", (e) => {
                e.preventDefault();
                cambiarPagina(filas, pagina);
            });
        }
        return li;
    };

    // Botón Anterior
    ulPaginacion.appendChild(crearItem("«", paginaActual - 1, false, paginaActual === 1));

    // Rango dinámico (Máximo 5 páginas numéricas a la vista)
    const maxVisibles = 5;
    let inicio = Math.max(1, paginaActual - Math.floor(maxVisibles / 2));
    let fin = Math.min(totalPaginas, inicio + maxVisibles - 1);

    if (fin - inicio + 1 < maxVisibles) {
        inicio = Math.max(1, fin - maxVisibles + 1);
    }

    // Primera página y puntos suspensivos
    if (inicio > 1) {
        ulPaginacion.appendChild(crearItem("1", 1));
        if (inicio > 2) ulPaginacion.appendChild(crearItem("...", null, false, true));
    }

    // Botones numéricos visibles
    for (let i = inicio; i <= fin; i++) {
        ulPaginacion.appendChild(crearItem(i, i, i === paginaActual));
    }

    // Última página y puntos suspensivos
    if (fin < totalPaginas) {
        if (fin < totalPaginas - 1) ulPaginacion.appendChild(crearItem("...", null, false, true));
        ulPaginacion.appendChild(crearItem(totalPaginas, totalPaginas));
    }

    // Botón Siguiente
    ulPaginacion.appendChild(crearItem("»", paginaActual + 1, false, paginaActual === totalPaginas));
}

/**
 * Cambia la página activa y recalcula controles
 */
function cambiarPagina(filas, nuevaPagina) {
    paginaActual = nuevaPagina;
    mostrarPagina(filas, paginaActual);
    generarBotones(filas);
}
