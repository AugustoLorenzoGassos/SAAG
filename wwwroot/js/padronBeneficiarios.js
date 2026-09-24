//Apicultura
async function cargarTablaApiCultura() {
    //Varianles para el filtro
    const anioReporte = document.getElementById('txtAño').value;
    const nombre = document.getElementById('txtNombre').value;
    const region = document.getElementById('cmbRegion').value;
    const municipio = document.getElementById('cmbMunicipio').value;
    const localidad = document.getElementById('cmbLocalidad').value;
    //Construcción de la tabla
    const tbody = document.getElementById('tbodyRegistros');
    const thead = document.getElementById('theadRegistros')
    const paginas = document.getElementById('ulPaginacion');
    const info = document.getElementById('infoPaginacion');
    tbody.innerHTML = '';
    thead.innerHTML = '';
    paginas.innerHTML = '';
    info.innerHTML = '';
    thead.innerHTML = `
            <tr>
                <th class="text-center">Región</th>
                <th class="text-center">Nombre</th>
                <th class="text-center">C.U.R.P.</th>
                <th class="text-center">Municipio</th>
                <th class="text-center">Localidad</th>
                <th class="text-center">Dictamen</th>
                <th class="text-center">Folio</th>
                <th class="text-center">Acta</th>
                <th class="text-center" style="width: 150px;">Acciones</th>
            </tr>
        `;
    tbody.innerHTML = '<tr><td colspan="9" class="text-center">Cargando datos...</td></tr>';
    // Construcción de parámetros de consulta
    const params = new URLSearchParams();
    if (anioReporte) params.append('anioReporte', anioReporte);
    if (nombre) params.append('nombre', nombre);
    if (region != "00") params.append('region', region);
    if (municipio != "000") params.append('municipio', municipio);
    if (localidad != "0000") params.append('localidad', localidad);
    //Envía los parámetros a la vista. Valida la repuesta
    if (params.toString().length > 0) {
        try {
            const response = await fetch(`/Seguimiento/padronesBeneficiariosApicultura?${params.toString()}`);
            if (!response.ok) {
                throw new Error(`Error en la petición: ${response.statusText}`);
            }
            const data = await response.json();
            if (!data || data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="9" class="text-center">No se encontraron registros.</td></tr>';
                return;
            }
            else {
                //Limpia la tabla y la llena con la información regresada por el controlador
                tbody.innerHTML = '';
                data.forEach(item => {
                    const tr = document.createElement('tr');
                    tr.innerHTML = `
                            <td>${item.nombreRegion ?? '-'}</td>
                            <td>${item.nombreBeneficiarios ?? '-'}</td>
                            <td>${item.curp ?? '-' ?? '-'}</td>
                            <td>${item.nombreMunicipio ?? '-'}</td>
                            <td>${item.nombreLocalidad ?? '-'}</td>
                            <td>${item.dictamen ?? '-'}</td>
                            <td>${item.folio ?? '-'}</td>
                            <td>${item.folioActa ?? '-'}</td>
                            <td class="text-center">
                                <div class="btn-group btn-group-sm" role="group" aria-label="Acciones">
                                    <!-- Acciones de seguimiento -->
                                            <button type="button" class="btn btn-detalles" data-id=${item.idBeneficiario} data-seccion="sec-apicultura">
                                        <i class="fa fa-address-card" aria-hidden="true"></i>
                                    </button>
                                </div>
                            </td>
                        `;
                    tbody.appendChild(tr);
                });
                //Coloca los controles para la paginación
                inicializarPaginacion();
            }
        }
        catch (error) {
            console.error(error);
            tbody.innerHTML = '<tr><td colspan="9" class="text-center text-danger">Error al cargar los datos.</td></tr>';
        }
    }
    else {
        tbody.innerHTML = '<tr><td colspan="9" class="text-center">Seleccione filtros válidos.</td></tr>';
    }
}

//Función para cargar el padrón de Proyecto Desarrollo de la Acuacultura Ssustentable Veracruzana
async function cargarTablaAcuacultura() {
    //Varianles para el filreo
    const anioReporte = document.getElementById('txtAño').value;
    const nombre = document.getElementById('txtNombre').value;
    const region = document.getElementById('cmbRegion').value;
    const municipio = document.getElementById('cmbMunicipio').value;
    const localidad = document.getElementById('cmbLocalidad').value;
    //Construcción de la tabla
    const tbody = document.getElementById('tbodyRegistros');
    const thead = document.getElementById('theadRegistros')
    const paginas = document.getElementById('ulPaginacion');
    const info = document.getElementById('infoPaginacion');
    tbody.innerHTML = '';
    thead.innerHTML = '';
    paginas.innerHTML = '';
    info.innerHTML = '';
    thead.innerHTML = `
                <tr>
                    <th class="text-center">Región</th>
                    <th class="text-center">Nombre</th>
                    <th class="text-center">C.U.R.P.</th>
                    <th class="text-center">Municipio</th>
                    <th class="text-center">Localidad</th>
                    <th class="text-center" style="width: 150px;">Acciones</th>
                </tr>
            `;
    tbody.innerHTML = '<tr><td colspan="6" class="text-center">Cargando datos...</td></tr>';
    // Construcción de parámetros de consulta
    const params = new URLSearchParams();
    if (anioReporte) params.append('anioReporte', anioReporte);
    if (nombre) params.append('nombre', nombre);
    if (region != "00") params.append('region', region);
    if (municipio != "000") params.append('municipio', municipio);
    if (localidad != "0000") params.append('localidad', localidad);
    //Envía los parámetros a la vista. Valida la respuesta
    if (params.toString().length > 0) {
        try {
            const response = await fetch(`/Seguimiento/padronesBeneficiariosAcuacultura?${params.toString()}`);
            if (!response.ok) {
                throw new Error(`Error en la petición: ${response.statusText}`);
            }
            const data = await response.json();
            if (!data || data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="9" class="text-center">No se encontraron registros.</td></tr>';
                return;
            }
            else {
                //Limpia la tabla y la llena con la información regresada por el controlador
                tbody.innerHTML = '';
                data.forEach(item => {
                    const tr = document.createElement('tr');
                    tr.innerHTML = `
                                <td>${item.nombreRegion ?? '-'}</td>
                                <td>${item.nombreBeneficiarios ?? '-'}</td>
                                <td></td>
                                <td>${item.nombreMunicipio ?? '-'}</td>
                                <td>${item.nombreLocalidad ?? '-'}</td>
                                <td class="text-center">
                                    <div class="btn-group btn-group-sm" role="group" aria-label="Acciones">
                                        <!-- Acciones de seguimiento -->
                                        <button type="button" class="btn btn-detalles" data-id=${item.idBeneficiario} data-seccion="sec-Acuacultura">
                                            <i class="fa fa-address-card" aria-hidden="true"></i>
                                        </button>
                                        </div>
                                </td>
                            `;
                    tbody.appendChild(tr);
                });
                //Coloca los controles para la paginación
                inicializarPaginacion();
            }
        }
        catch (error) {
            console.error(error);
            tbody.innerHTML = '<tr><td colspan="6" class="text-center text-danger">Error al cargar los datos.</td></tr>';
        }
    }
    else {
        tbody.innerHTML = '<tr><td colspan="6" class="text-center">Seleccione filtros válidos.</td></tr>';
    }
}

//Función para cargar el padrón de Impulso y Fomento a la Producción de Aves de Postura de Traspatio en la Entidad Veracruzana
async function cargarTablaAves() {
    //Varianles para el filreo
    const anioReporte = document.getElementById('txtAño').value;
    const nombre = document.getElementById('txtNombre').value;
    const region = document.getElementById('cmbRegion').value;
    const municipio = document.getElementById('cmbMunicipio').value;
    const localidad = document.getElementById('cmbLocalidad').value;
    //Construcción de la tabla
    const tbody = document.getElementById('tbodyRegistros');
    const thead = document.getElementById('theadRegistros')
    const paginas = document.getElementById('ulPaginacion');
    const info = document.getElementById('infoPaginacion');
    tbody.innerHTML = '';
    thead.innerHTML = '';
    paginas.innerHTML = '';
    info.innerHTML = '';
    thead.innerHTML = `
                <tr>
                    <th class="text-center">Región</th>
                    <th class="text-center">Folio</th>
                    <th class="text-center">Nombre</th>
                    <th class="text-center">Tipo de apoyo</th>
                    <th class="text-center">Municipio</th>
                    <th class="text-center">Localidad</th>
                </tr>
            `;
    tbody.innerHTML = '<tr><td colspan="6" class="text-center">Cargando datos...</td></tr>';
    // Construcción de parámetros de consulta
    const params = new URLSearchParams();
    if (anioReporte) params.append('anioReporte', anioReporte);
    if (nombre) params.append('nombre', nombre);
    if (region != "00") params.append('region', region);
    if (municipio != "000") params.append('municipio', municipio);
    if (localidad != "0000") params.append('localidad', localidad);
    //Envía los parámetros. Valída la respuesta
    if (params.toString().length > 0) {
        try {
            const response = await fetch(`/Seguimiento/padronesBeneficiariosAves?${params.toString()}`);
            if (!response.ok) {
                throw new Error(`Error en la petición: ${response.statusText}`);
            }
            const data = await response.json();
            if (!data || data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="6" class="text-center">No se encontraron registros.</td></tr>';
                return;
            }
            else {
                //Limpia la tabla y la llena con la información regresada por el controlador
                tbody.innerHTML = '';
                data.forEach(item => {
                    const tr = document.createElement('tr');
                    tr.innerHTML = `
                                <td>${item.nombreRegion ?? '-'}</td>
                                <td>${item.folio ?? '-'}</td>
                                <td>${item.nombreBeneficiarios ?? '-'}</td>
                                <td>${item.nombreTipoApoyo}</trd>
                                <td>${item.nombreMunicipio ?? '-'}</td>
                                <td>${item.nombreLocalidad ?? '-'}</td>
                            `;
                    tbody.appendChild(tr);
                });
                //Coloca los controles para la paginación
                inicializarPaginacion();
            }
        }
        catch (error) {
            console.error(error);
            tbody.innerHTML = '<tr><td colspan="6" class="text-center text-danger">Error al cargar los datos.</td></tr>';
        }
    }
    else {
        tbody.innerHTML = '<tr><td colspan="6" class="text-center">Seleccione filtros válidos.</td></tr>';
    }
}

//Función para cargar el padrón de Impulso y Fomento a la Inseminación Artificial en la Entidad Veracruzana
async function cargarTablaInseminacion() {
    //Varianles para el filreo
    const anioReporte = document.getElementById('txtAño').value;
    const nombre = document.getElementById('txtNombre').value;
    const region = document.getElementById('cmbRegion').value;
    const municipio = document.getElementById('cmbMunicipio').value;
    const localidad = document.getElementById('cmbLocalidad').value;
    //Construcción de la tabla
    const tbody = document.getElementById('tbodyRegistros');
    const thead = document.getElementById('theadRegistros')
    const paginas = document.getElementById('ulPaginacion');
    const info = document.getElementById('infoPaginacion');
    tbody.innerHTML = '';
    thead.innerHTML = '';
    paginas.innerHTML = '';
    info.innerHTML = '';
    thead.innerHTML = `
                <tr>
                    <th class="text-center">Región</th>
                    <th class="text-center">Nombre</th>
                    <th class="text-center">Municipio</th>
                    <th class="text-center">Localidad</th>
                    <th class="text-center" style="width: 150px;">Acciones</th>
               </tr>
            `;
    tbody.innerHTML = '<tr><td colspan="6" class="text-center">Cargando datos...</td></tr>';
    // Construcción de parámetros de consulta
    const params = new URLSearchParams();
    if (anioReporte) params.append('anioReporte', anioReporte);
    if (nombre) params.append('nombre', nombre);
    if (region != "00") params.append('region', region);
    if (municipio != "000") params.append('municipio', municipio);
    if (localidad != "0000") params.append('localidad', localidad);
    //Envía los parámetros. Valída la respuesta
    if (params.toString().length > 0)
    {
        try
        {
            const response = await fetch(`/Seguimiento/padronesBeneficiariosIATF?${params.toString()}`);
            if (!response.ok)
            {
                throw new Error(`Error en la petición: ${response.statusText}`);
            }
            const data = await response.json();
            if (!data || data.length === 0)
            {
                tbody.innerHTML = '<tr><td colspan="6" class="text-center">No se encontraron registros.</td></tr>';
                return;
            }
            else
            {
            //Limpia la tabla y la llena con la información regresada por el controlador
            tbody.innerHTML = '';
            data.forEach(item =>
            {
                const tr = document.createElement('tr');
                tr.innerHTML = `
                    <td>${item.nombreRegion ?? '-'}</td>
                    <td>${item.nombreBeneficiarios ?? '-'}</td>
                    <td>${item.nombreMunicipio ?? '-'}</td>
                    <td>${item.nombreLocalidad ?? '-'}</td>
                    <td class="text-center">
                        <div class="btn-group btn-group-sm" role="group" aria-label="Acciones">
                            <!-- Acciones de seguimiento -->
                            <button type="button" class="btn btn-detalles" data-id=${item.idBeneficiario} data-seccion="sec-inseminacion">
                                <i class="fa fa-address-card" aria-hidden="true"></i>
                            </button>
                            </div>
                    </td>
                `;
                tbody.appendChild(tr);
                });
            //Coloca los controles para la paginación
            inicializarPaginacion();
            }
        }
        catch (error)
        {
            console.error(error);
            tbody.innerHTML = '<tr><td colspan="6" class="text-center text-danger">Error al cargar los datos.</td></tr>';
        }   
    }
    else
    {
        tbody.innerHTML = '<tr><td colspan="6" class="text-center">Seleccione filtros válidos.</td></tr>';
    }
}