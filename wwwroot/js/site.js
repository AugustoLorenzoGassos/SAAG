// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

//Función para cargar los laboratorios en el combo
async function cargarLaboratorios() {
    const cmbLaboratorios = document.getElementById("cmbLaboratorios");
    try {
        const response = await fetch('/Seguimiento/ObtenerLaboratorios');
        if (!response.ok) {
            throw new Error(`Error HTTP: ${response.status}`);
        }
        const data = await response.json();
        cmbLaboratorios.innerHTML = '<option value="">Seleccione un laboratorio... </option>';
        data.forEach(item => {
            const option = document.createElement("option");
            option.value = item.idLaboratoio;
            option.textContent = item.nombreLaboratorio;
            cmbLaboratorios.appendChild(option);
        });

    }
    catch (error) {
        console.error("Error al cargar la lista de laboratorios:", error);
    }
}

//Catálogo de regiones
async function cargarRegiones()
{
    const cmbRegion = document.getElementById("cmbRegion");
    try
    {
        const response = await fetch('/Seguimiento/ObtenerRegiones');
        if (!response.ok)
        {
            throw new Error(`Error HTTP: ${response.status}`);
        }
        const data = await response.json();
        cmbRegion.innerHTML = '<option value="00">Seleccione una región... </option>';
        data.forEach(item =>
        {
            const option = document.createElement("option");
            option.value = item.idRegion;
            option.textContent = item.nombreRegion;
            cmbRegion.appendChild(option);
        });

    }
    catch (error)
    {
        console.error("Error al cargar la lista de laboratorios:", error);
    }
}

//Catálogo de municipios
async function cargarMunicipios(pMunicipio = null, pLocalidad = null)
{
    const cmbMunicipio = document.getElementById("cmbMunicipio");
    try
    {
        const response = await fetch('/Seguimiento/ObtenerMunicipios');
        if (!response.ok)
        {
            throw new Error(`Error HTTP: ${response.status}`);
        }
        const data = await response.json();
        cmbMunicipio.innerHTML = '<option value="000">Seleccione un municipio...</option>';
        data.forEach(item =>
        {
            const option = document.createElement("option");
            option.value = item.claveMunicipio;
            option.textContent = item.nombreMunicipio;
            cmbMunicipio.appendChild(option);
        });
        if (pMunicipio && pMunicipio !== '000')
        {
            cmbMunicipio.value = pMunicipio;
            cargarLocalidades(pMunicipio, pLocalidad);
        }
    }
    catch (error)
    {
        console.error("Error al cargar la lista de municipios:", error);
    }
}

document.getElementById('cmbMunicipio')?.addEventListener('change', function (e)
{
    e.preventDefault();
    const valor = e.target.value;
    cargarLocalidades(valor);
});

async function cargarLocalidades(pMunicipio, pLocalidad = null)
{
    const cmbLocalidad = document.getElementById("cmbLocalidad");
    try
    {
        const response = await fetch(`/Seguimiento/ObtenerLocalidades/${pMunicipio}`);
        if (!response.ok) {
            throw new Error(`Error HTTP: ${response.status}`);
        }
        const data = await response.json();
        cmbLocalidad.innerHTML = '<option value="0000" disabled selected>Seleccione una localidad... </option>';
        data.forEach(item => {
            const option = document.createElement("option");
            option.value = item.claveLocalidad;
            option.textContent = item.nombreLocalidad;
            cmbLocalidad.appendChild(option);
        });

        if (pLocalidad && pLocalidad !== '0000')
        {
            cmbLocalidad.value = pLocalidad;
        }

    }
    catch (error)
    {
        console.error("Error al cargar la lista de localidades:", error);
    }
}
