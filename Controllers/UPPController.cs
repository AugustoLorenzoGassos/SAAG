using ClosedXML;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelDataReader;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Validation;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SqlServer.Server;
using Microsoft.Win32;
using Seguimiento.Models;
using Seguimiento.Models.DTOs;
using System.ClientModel.Primitives;
using System.Collections;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace Seguimiento.Controllers
{
    public class UPPController : Controller
    {
        private readonly SedarpaContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        // Inyección de dependencias del DbContext
        public UPPController(SedarpaContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        //**********API's para el registro de UPP
        //Función para validar le existencoia de una UPP
        [HttpGet]
        public async Task<IActionResult> ValidarClaveUPP(string clave)
        {
            var existe = await _context.CatalogoProductoresUpps.AnyAsync(u => u.ClaveUpp == clave);
            return Ok(new { exists = existe });
        }
        //Función que regresa la ventana principal de UPP
        public IActionResult UPPInicio()
        {
            return View();
        }
        //Descarga a excel del padrón de producgtores y UPP
        public async Task<IActionResult> UPPDescargaCompleta([FromQuery] FiltrosDescargaUPP filtros)
        {

            var query = _context.VistaPadronProductoresUpps.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(filtros.CURP))
            {
                query = query.Where(x => x.CurpProductor.Contains(filtros.CURP));
            }
            if (!string.IsNullOrEmpty(filtros.Nombre))
            {
                query = query.Where(x => x.NombreProductor.Contains(filtros.Nombre));
            }
            if (!string.IsNullOrEmpty(filtros.Paterno))
            {
                query = query.Where(x => x.ApellidoPaternoProductor.Contains(filtros.Paterno));
            }
            if (!string.IsNullOrEmpty(filtros.Materno))
            {
                query = query.Where(x => x.ApellidoMaternoProductor.Contains(filtros.Materno));
            }
            var acciones = await query.ToListAsync();


            // 1. Consultar la vista de SQL Server mediante el DbContext
            //var acciones = await _context.VistaPadronProductoresUpps.AsNoTracking().ToListAsync();

            // 2. Generar el Excel con ClosedXML
            using (XLWorkbook wb = new XLWorkbook())
            {
                // Carga la lista directamente en la hoja de Excel
                var worksheet1 = wb.Worksheets.Add("Padrón productores - UPP");
                worksheet1.Cell(1, 1).InsertTable(acciones);
                worksheet1.Rows().Style.Alignment.SetWrapText(false);
                worksheet1.Columns().AdjustToContents(); // Ajusta el ancho de las columnas

                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    byte[] content = stream.ToArray();

                    string fileName = $"PadronProductoresUPP_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return File(content, contentType, fileName);
                }
            }
        }
        //Función re carga la vista para mostrar el mapa de las UPP y pasa la lista al HTML
        //UPPInicioMostarMapa
        public async Task<IActionResult> UPPInicioMostarMapa()
        {

            var listaUpps = await _context.CatalogoProductoresUpps
                        .Select(u => new
                        {
                            u.ClaveUpp,
                            u.NombreUpp,
                            u.CveMunicipio,
                            u.CveLocalidad,
                            u.Latitud,
                            u.CatalogoLocalidade.ClaveMunicipioNavigation.NombreMunicipio,
                            Longitud = u.Longitud > 0 ? u.Longitud * -1 : u.Longitud
                        })
                        .ToListAsync();

            return View(listaUpps);

        }


        //Función que regresa la información filtrada de productores
        public async Task<IActionResult> ObtenerListaProductores(string? CURP, string? nombre, string? paterno, string? materno, string? upp)
        {
            //Variable para guardar la conexión a la tabla
            var query = _context.CatalogoProductores
                .AsQueryable();
            //Sección para construir el where dependiendo de los parametros recibidos
            if (!string.IsNullOrEmpty(CURP)) { query = query.Where(r => r.CurpProductor.Contains(CURP)); }
            if (!string.IsNullOrEmpty(nombre)) { query = query.Where(r => r.NombreProductor.Contains(nombre)); }
            if (!string.IsNullOrEmpty(paterno)) { query = query.Where(r => r.ApellidoPaternoProductor.Contains(paterno)); }
            if (!string.IsNullOrEmpty(materno)) { query = query.Where(r => r.ApellidoMaternoProductor.Contains(materno)); }

            if (!string.IsNullOrEmpty(upp))
            {
                query = query.Where(r => r.CatalogoProductoresUpps != null &&
                                         r.CatalogoProductoresUpps.Any(u => u.ClaveUpp.Contains(upp)));
            }

            //Ejecución de la consulta
            var resultado = await (from r in query
                                   select new
                                   {
                                       r.IdProductor,
                                       r.CurpProductor,
                                       r.NombreProductor,
                                       //UsuarioNombre = r.IdUsuarioNavigation != null ? r.IdUsuarioNavigation.NombreUsuario : "Sin usuario",
                                       r.ApellidoPaternoProductor,
                                       r.ApellidoMaternoProductor,
                                       totalUPP = r.CatalogoProductoresUpps.Count()
                                   }).OrderBy(r => r.NombreProductor).ThenBy(r => r.ApellidoPaternoProductor).ThenBy(r => r.ApellidoMaternoProductor).ToListAsync();
            //Regresa información
            return Json(resultado);
            //Añade filtro por tipo de usuario y regresa la información a la vista en formato de Json
            //if (HttpContext.Session.GetString("RolUsuario") == "A")
            //{
            //    return Json(resultado);
            //}
            //else
            //{
            //    return Json(resultado.Where(rm => rm.IdUsuario == HttpContext.Session.GetString("IdUsuario")));
            //}
        }
        public IActionResult UPPProductorAgregar()
        {
            var productorNuevo = new CatalogoProductore();
            return View(productorNuevo);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UPPProductorAgregar([FromForm] CatalogoProductore modelo)
        {
            try
            {
                var idUsuarioSesion = HttpContext.Session.GetString("IdUsuario");
                modelo.FechaCaptura = DateTime.Now;
                modelo.FechaActualizacion = DateTime.Now;
                modelo.IdUsuario = idUsuarioSesion;
                modelo.CurpProductor = modelo.CurpProductor?.ToUpper();
                modelo.NombreProductor = modelo.NombreProductor?.ToUpper();
                modelo.ApellidoPaternoProductor = modelo.ApellidoPaternoProductor?.ToUpper();
                modelo.ApellidoMaternoProductor = modelo.ApellidoMaternoProductor?.ToUpper();
                modelo.DomicilioProductor = modelo.DomicilioProductor?.ToUpper();
                _context.CatalogoProductores.Add(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction("UPPInicio", "UPP");
            }
            catch (Exception ex)
            {
                return View(modelo);
            }
        }
        //Funcion para actualizar la información de un registro
        public IActionResult UPPProductorEditar(int id)
        {
            var registro = _context.CatalogoProductores.FirstOrDefault(p => p.IdProductor == id);
            return View(registro);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UPPProductorEditar([FromForm] CatalogoProductore modelo)
        {
            try
            {
                var idUsuarioSesion = HttpContext.Session.GetString("IdUsuario");
                modelo.FechaActualizacion = DateTime.Now;
                modelo.IdUsuario = idUsuarioSesion;
                modelo.CurpProductor = modelo.CurpProductor?.ToUpper();
                modelo.NombreProductor = modelo.NombreProductor?.ToUpper();
                modelo.ApellidoPaternoProductor = modelo.ApellidoPaternoProductor?.ToUpper();
                modelo.ApellidoMaternoProductor = modelo.ApellidoMaternoProductor?.ToUpper();
                modelo.DomicilioProductor = modelo.DomicilioProductor?.ToUpper();
                _context.CatalogoProductores.Update(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction("UPPInicio", "UPP");
            }
            catch (Exception ex)
            {
                return View(modelo);
            }
        }
        public IActionResult UPPProductorEliminar(int id)
        {
            var registro = _context.CatalogoProductores.FirstOrDefault(p => p.IdProductor == id);
            ViewBag.UPPRegistradas = _context.CatalogoProductoresUpps.Where(up => up.IdProductor == id).ToList().Count();
            return View(registro);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UPPProductorEliminar([FromForm] CatalogoProductore modelo)
        {
            try
            {
                _context.CatalogoProductores.Remove(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction("UPPInicio", "UPP");
            }
            catch (Exception ex)
            {
                return View(modelo);
            }
        }
        //Regresa a la vista la lista de las UPP asociadas al productgoe enviado en el parámetro
        public async Task<IActionResult> UPPProductorUPPAsociadas(int id)
        {
            //Obtiene el registro mensual junto con sus detalles y el usuario relacionado
            var productorUPP = await _context.CatalogoProductores
                .Include(cp => cp.CatalogoProductoresUpps)
                    .ThenInclude(cl => cl.CatalogoLocalidade)
                        .ThenInclude(cm => cm.ClaveMunicipioNavigation)
                .FirstOrDefaultAsync(cp => cp.IdProductor == id);
            //Valida que el registro mensual exista
            if (productorUPP == null)
            {
                return NotFound();
            }
            //Manda los datos a la vista y reenderiza la información del registro mensual y sus detalles
            return View(productorUPP);
        }
        //Envía el formulario para dar de alta una UPP
        public IActionResult UPPProductorUPPAsociadaAgregar(int id)
        {
            var registro = new CatalogoProductoresUpp();
            var idUsuarioSesion = HttpContext.Session.GetString("IdUsuario");
            ViewBag.nombreProductor = _context.CatalogoProductores.Where(p => p.IdProductor == id).Select(p => p.NombreProductor + " " + p.ApellidoPaternoProductor + " " + p.ApellidoMaternoProductor).FirstOrDefault();
            registro.FechaCaptura = DateTime.Now;
            registro.FechaActualizacion = DateTime.Now;
            registro.IdProductor = id;
            registro.IdUsuario = idUsuarioSesion;
            return View(registro);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UPPProductorUPPAsociadaAgregar([FromForm] CatalogoProductoresUpp modelo)
        {
            try
            {
                _context.CatalogoProductoresUpps.Add(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction("UPPProductorUPPAsociadas", "UPP", new { id = modelo.IdProductor });
            }
            catch (Exception ex)
            {
                return View(modelo);
            }
        }
        //Enviar infoemación de una UPP para que sea editada
        public IActionResult UPPProductorUPPAsociadaEditar(int id)
        {
            var registro = _context.CatalogoProductoresUpps.FirstOrDefault(up => up.IdProductorUpp == id);
            var productor = _context.CatalogoProductores.FirstOrDefault(p => p.IdProductor == registro.IdProductor);
            var idUsuarioSesion = HttpContext.Session.GetString("IdUsuario");
            ViewBag.nombreProductor = productor.NombreProductor + " " + productor.ApellidoPaternoProductor + " " + productor.ApellidoMaternoProductor;
            registro?.FechaActualizacion = DateTime.Now;
            return View(registro);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UPPProductorUPPAsociadaEditar([FromForm] CatalogoProductoresUpp modelo)
        {
            try
            {
                _context.CatalogoProductoresUpps.Update(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction("UPPProductorUPPAsociadas", "UPP", new { id = modelo.IdProductor });
            }
            catch (Exception ex)
            {
                return View(modelo);
            }
        }
        //Enviar infoemación de una UPP para que sea eliminada
        public IActionResult UPPProductorUPPAsociadaEliminar(int id)
        {
            var registro = _context.CatalogoProductoresUpps
                                .Include(cl => cl.CatalogoLocalidade)
                                    .ThenInclude(cm => cm.ClaveMunicipioNavigation)
                                .FirstOrDefault(up => up.IdProductorUpp == id);

            if (registro == null)
            {
                return NotFound();
            }

            var idUsuarioSesion = HttpContext.Session.GetString("IdUsuario");

            // Obtenemos solo el nombre del productor para el ViewBag
            var productor = _context.CatalogoProductores.FirstOrDefault(p => p.IdProductor == registro.IdProductor);
            if (productor != null)
            {
                ViewBag.nombreProductor = productor.NombreProductor + " " + productor.ApellidoPaternoProductor + " " + productor.ApellidoMaternoProductor;
            }

            return View(registro);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UPPProductorUPPAsociadaEliminar([FromForm] CatalogoProductoresUpp modelo)
        {
            try
            {
                // 1. Buscamos el registro real en la base de datos usando el ID que viajó en el formulario
                var registroAEliminar = await _context.CatalogoProductoresUpps
                    .FirstOrDefaultAsync(up => up.IdProductorUpp == modelo.IdProductorUpp);

                if (registroAEliminar != null)
                {
                    // 2. Removemos el objeto real que sí está conectado al contexto de la BD
                    _context.CatalogoProductoresUpps.Remove(registroAEliminar);
                    await _context.SaveChangesAsync();
                }

                // 3. Redireccionamos a la lista usando el IdProductor que trae tu modelo
                return RedirectToAction("UPPProductorUPPAsociadas", "UPP", new { id = modelo.IdProductor });
            }
            catch (Exception ex)
            {
                // Si ocurre algún error, recargamos la vista con el modelo original
                return View(modelo);
            }
        }
        //Regresa el registro de visitas a una UPP
        public async Task<IActionResult> UPPProductorUPPAsociadaVisitas(int id)
        {

            var registro = await _context.ProduccionUpps
                .Include(p => p.IdProductorUppNavigation)
                    .ThenInclude(up => up.IdProductorNavigation)
                .Include(p => p.IdProductorUppNavigation)
                    .ThenInclude(l => l.CatalogoLocalidade)
                        .ThenInclude(loc => loc.ClaveMunicipioNavigation)
                .Include(u => u.IdUsuarioNavigation)
                .Where(p => p.IdProductorUpp == id)
                .ToListAsync();
            var idUsuarioSesion = HttpContext.Session.GetString("IdUsuario");

            ViewBag.IdProductor = _context.CatalogoProductoresUpps
                .Where(up => up.IdProductorUpp == id)
                .Select(up => up.IdProductor)
                .FirstOrDefault();
            ViewBag.IdProductorUpp = id;
            
            return View(registro);

        }
        //Regresa la información de una visita en paericular a través del id
        public async Task<IActionResult> UPPProductorUPPAsociadaVisitasDetalle(int id)
        {
            var registro = _context.ProduccionUpps
                .Include(p => p.IdProductorUppNavigation)
                    .ThenInclude(up => up.IdProductorNavigation)
                .Include(p => p.IdProductorUppNavigation)
                    .ThenInclude(l => l.CatalogoLocalidade)
                        .ThenInclude(loc => loc.ClaveMunicipioNavigation)
                .Include(u => u.IdUsuarioNavigation)
                .Include(r => r.IdRazaNavigation)
                .Include(t => t.IdUsoRazaNavigation)
                .FirstOrDefault(x => x.IdRegistroProduccion == id);
            return View(registro);
        }
        //Genera la vista para agregar juna visita a la UPP
        public ActionResult UPPProductorUPPAsociadaVisitasAgregar(int id)
        {

            var upp = _context.CatalogoProductoresUpps
                .Include(x => x.IdProductorNavigation)    // Trae los datos del productor
                .Include(x => x.CatalogoLocalidade)       // Trae los datos de la localidad (revisa el nombre exacto de esta propiedad en tu modelo)
                    .ThenInclude(l => l.ClaveMunicipioNavigation) // Trae los datos del municipio
                .FirstOrDefault(x => x.IdProductorUpp == id); // O .Id == id, dependiendo de tu llave primaria

            ViewBag.claveUPP = upp.ClaveUpp != null ? upp.ClaveUpp : "Sin clave de UPP";
            ViewBag.Productor = upp.IdProductorNavigation != null ? upp.IdProductorNavigation.NombreProductor + " " + upp.IdProductorNavigation.ApellidoPaternoProductor + " " + upp.IdProductorNavigation.ApellidoMaternoProductor : "";
            ViewBag.Municipio = upp.CatalogoLocalidade != null && upp.CatalogoLocalidade.ClaveMunicipioNavigation != null ? upp.CatalogoLocalidade.ClaveMunicipioNavigation.NombreMunicipio : "";
            ViewBag.Localidad = upp.CatalogoLocalidade != null ? upp.CatalogoLocalidade.NombreLocalidad : "";
            ViewBag.Razas = _context.CatalogoRazas.ToList();
            ViewBag.TiposProduccion = _context.CatalogoUsos.ToList();

            ProduccionUpp datosVisita = new ProduccionUpp();
            datosVisita.FechaCaptura = DateTime.Now;
            datosVisita.IdUsuario = HttpContext.Session.GetString("IdUsuario");
            datosVisita.IdProductorUpp = id;
            datosVisita.IdEtapaRegistro = 1;

            return View(datosVisita);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UPPProductorUPPAsociadaVisitasAgregar([FromForm] ProduccionUpp modelo)
        {
            _context.ProduccionUpps.Add(modelo);
            await _context.SaveChangesAsync();
            return RedirectToAction("UPPProductorUPPAsociadaVisitas", "UPP", new { id = modelo.IdProductorUpp });
        }
        //Genera la vista para agregar visitas a la UPP desde un archivo Excel
        public ActionResult UPPProductorUPPAsociadaVisitasAgregarExcel()
        {
            return View();
        }
        //Recibe el archivo Excel y lo procesa para agregar las visitas a la UPP
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(104857600)]
        public async Task<IActionResult> UPPProductorUPPAsociadaVisitasAgregarExcel(IFormFile archivoExcel)
        {

            if (archivoExcel == null || archivoExcel.Length == 0)
            {
                ModelState.AddModelError("archivoExcel", "Por favor, seleccione un archivo Excel.");
                TempData["Mensaje"] = $"En espera de la lectura y carga del archivo a procesar.";
                return View();
            }

            // Registramos la codificación necesaria para ExcelDataReader (requiere System.Text.Encoding.CodePages)
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = archivoExcel.OpenReadStream())
            {
                // ExcelDataReader lee el archivo sin importarle las imágenes o elementos gráficos internos
                using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet(new ExcelDataReader.ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataReader.ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true // Esto hace que la primera fila sea tu cabecera
                        }
                    });

                    // Obtenemos la primera hoja del Excel como una tabla de datos normal
                    var dataTable = result.Tables[0];

                    // Aquí puedes recorrer tus filas de datos con total normalidad:
                    var registrosConError = 0;
                    var registrosLeidos = 0;
                    var registrosSinError = 0;
                    if (dataTable.Columns.Count == 24)
                    {
                        dataTable.Columns.Add("Estatus", typeof(string));
                        foreach (System.Data.DataRow row in dataTable.Rows)
                        {
                            // validación de los datos
                            var valorUPP = row["UPP"]?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(valorUPP))
                            {
                                //Validar que exista en el catálogo de UPP
                                bool existeCatalogo = await _context.CatalogoProductoresUpps.AnyAsync(x => x.ClaveUpp == valorUPP);
                                if (existeCatalogo)
                                {
                                    //Validar que no este duplicada en la tabla de visitas de la UPP
                                    int idUPP = await _context.CatalogoProductoresUpps.Where(x => x.ClaveUpp == valorUPP).Select(x => x.IdProductorUpp).FirstOrDefaultAsync();
                                    bool existeVisita = await _context.ProduccionUpps.AnyAsync(x => x.IdProductorUpp == idUPP);
                                    if (!existeVisita)
                                    {
                                        //Clave UPP no registrada en la tabla de visitas, se puede agregar
                                        registrosSinError++;
                                        var idUpp = await _context.CatalogoProductoresUpps.Where(x => x.ClaveUpp == valorUPP).Select(x => x.IdProductorUpp).FirstOrDefaultAsync();
                                        var idRaza = await _context.CatalogoRazas.Where(x => x.DescRaza == row["RAZA DOMINANTE"].ToString()).Select(x => x.IdRaza).FirstOrDefaultAsync();
                                        var idUso = await _context.CatalogoUsos.Where(x => x.DescUsoRaza == row["PRODUCCIÓN"].ToString()).Select(x => x.IdUsoRaza).FirstOrDefaultAsync();
                                        if (idRaza == 0) { idRaza = 6; } //Asignar raza por default
                                        if (idUso == 0) { idUso = 3; } //Asignar uso por default
                                        var idUsuarioSesion = HttpContext.Session.GetString("IdUsuario");
                                        ProduccionUpp datosVisita = new ProduccionUpp();
                                        datosVisita.IdEtapaRegistro = 1;
                                        datosVisita.FechaRegistro = Convert.ToDateTime(row["FECHA"]);
                                        datosVisita.IdUsuario = idUsuarioSesion;
                                        datosVisita.IdProductorUpp = idUpp;
                                        datosVisita.Vientres = (row["VIENTRES"] != null && int.TryParse(row["VIENTRES"].ToString(), out int v)) ? v : 0;
                                        datosVisita.Semental = (row["SEMENTAL"] != null && int.TryParse(row["SEMENTAL"].ToString(), out int sm)) ? sm : 0;
                                        datosVisita.Vaquillas1214 = (row["VAQUILLAS 12 A 24 MESES"] != null && int.TryParse(row["VAQUILLAS 12 A 24 MESES"].ToString(), out int vq)) ? vq : 0;
                                        datosVisita.NovillosToretes12 = (row["NOVILLOS-TORETES >12MESES"] != null && int.TryParse(row["NOVILLOS-TORETES >12MESES"].ToString(), out int nt)) ? nt : 0;
                                        datosVisita.CriasHembras812 = (row["CRIAS HEMBRAS (8A12 MESES)"] != null && int.TryParse(row["CRIAS HEMBRAS (8A12 MESES)"].ToString(), out int ch)) ? ch : 0;
                                        datosVisita.CriasMachos812 = (row["CRIAS MACHOS (8A12 MESES)"] != null && int.TryParse(row["CRIAS MACHOS (8A12 MESES)"].ToString(), out int cm)) ? cm : 0;
                                        datosVisita.BecerrasLactantes08 = (row["BECERRAS LACTANTES (0-8 MESES)"] != null && int.TryParse(row["BECERRAS LACTANTES (0-8 MESES)"].ToString(), out int bl)) ? bl : 0;
                                        datosVisita.BecerrosLactantes08 = (row["BECERROS LACTANTES (0-8 MESES)"] != null && int.TryParse(row["BECERROS LACTANTES (0-8 MESES)"].ToString(), out int bcl)) ? bcl : 0;
                                        datosVisita.CantidadCabezas = (row["NO. DE CABEZAS"] != null && int.TryParse(row["NO. DE CABEZAS"].ToString(), out int tc)) ? tc : 0;
                                        datosVisita.CantidadCabezasConArete = (row["CON ARETE"] != null && int.TryParse(row["CON ARETE"].ToString(), out int tca)) ? tca : 0;
                                        datosVisita.CantidadCabezasSinArete = (row["SIN ARETE"] != null && int.TryParse(row["SIN ARETE"].ToString(), out int tcs)) ? tcs : 0;
                                        datosVisita.CantidadCabezasAretesGratuitos = (row["ARETES GRATUITOS"] != null && int.TryParse(row["ARETES GRATUITOS"].ToString(), out int tcag)) ? tcag : 0;
                                        datosVisita.CantidadCabezasAretesProductor = (row["ARETES QUE PAGA PRODUCTOR"] != null && int.TryParse(row["ARETES QUE PAGA PRODUCTOR"].ToString(), out int tcap)) ? tcap : 0;
                                        datosVisita.IdRaza = 6;
                                        datosVisita.IdUsoRaza = 3;
                                        datosVisita.FechaCaptura = DateTime.Now;
                                        _context.ProduccionUpps.Add(datosVisita);
                                        _context.SaveChanges();
                                    }
                                    else
                                    {
                                        //Clave UPP registrada en la tabla de visitas
                                        registrosConError++;
                                        row["Estatus"] = "Clave UPP registrada en la tabla de visitas";
                                    }
                                }
                                else
                                {
                                    //Clave de UPP no encontrada en el catálogo

                                    registrosConError++;
                                    row["Estatus"] = "Clave UPP no encontrada en el catálogo";
                                }
                            }
                            registrosLeidos++;
                        }
                        TempData["Mensaje"] = $"Archivo de procesamiento: {archivoExcel.FileName}.<br />Se procesaron {registrosLeidos} registros.<br /><br />Registros agregados correctamente a la base de datos {registrosSinError}. Registros con error {registrosConError} <br /><br />La inflormación a detalle podrá ser consultada en el archivo de descarga del proceso.";
                        //Agregar registro con el lote de la carga del documento
                        if (registrosConError > 0)
                        {
                            var sb = new System.Text.StringBuilder();
                            // 1. Encabezados
                            string[] nombresColumnas = new string[dataTable.Columns.Count];
                            for (int i = 0; i < dataTable.Columns.Count; i++)
                            {
                                nombresColumnas[i] = dataTable.Columns[i].ColumnName;
                            }
                            sb.AppendLine(string.Join(",", nombresColumnas));
                            // 2. Filas
                            foreach (System.Data.DataRow row in dataTable.Rows)
                            {
                                string[] campos = new string[row.ItemArray.Length];
                                for (int i = 0; i < row.ItemArray.Length; i++)
                                {
                                    campos[i] = $"\"{row.ItemArray[i]?.ToString().Replace("\"", "\"\"")}\"";
                                }
                                sb.AppendLine(string.Join(",", campos));
                            }
                            // 3. Guardar en sesión
                            byte[] archivoBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
                            HttpContext.Session.Set("ArchivoErroresBytes", archivoBytes);
                            ViewBag.MostrarBotonDescarga = true;
                        }
                        //Agregar registro con el lote de la carga del documento
                        UsuariosProcesosCarga datosCarga = new UsuariosProcesosCarga();
                        datosCarga.IdUsuario = HttpContext.Session.GetString("IdUsuario");
                        datosCarga.FechaCarga = DateTime.Now;
                        datosCarga.NombreArchivo = archivoExcel.FileName;
                        datosCarga.RegistrosLeidos = registrosLeidos;
                        datosCarga.RegistrosError = registrosConError;
                        datosCarga.RegistrosCargados = registrosSinError;
                        _context.UsuariosProcesosCargas.Add(datosCarga);
                        _context.SaveChanges();
                        return View();
                    }
                    else
                    {
                        registrosConError++;
                        ViewBag.MostrarBotonDescarga = false;
                        return View();
                    }
                }
            }
            //return RedirectToAction("UPPInicio", "Seguimiento");
        }

        [HttpGet]
        public IActionResult DescargarExcelConErrores()
        {
            byte[] archivoBytes = HttpContext.Session.Get("ArchivoErroresBytes");

            if (archivoBytes == null)
            {
                return NotFound("No hay archivos disponibles.");
            }

            return File(archivoBytes, "text/csv", "Errores_Carga.csv");
        }

        public ActionResult UPPProductorUPPAsociadaVisitasEditar(int id)
        {
            var registro = _context.ProduccionUpps.FirstOrDefault(up => up.IdRegistroProduccion == id);

            var upp = _context.CatalogoProductoresUpps
                .Include(x => x.IdProductorNavigation)    // Trae los datos del productor
                .Include(x => x.CatalogoLocalidade)       // Trae los datos de la localidad (revisa el nombre exacto de esta propiedad en tu modelo)
                    .ThenInclude(l => l.ClaveMunicipioNavigation) // Trae los datos del municipio
                .FirstOrDefault(x => x.IdProductorUpp == registro.IdProductorUpp); // O .Id == id, dependiendo de tu llave primaria


            ViewBag.claveUPP = upp.ClaveUpp != null ? upp.ClaveUpp : "Sin clave de UPP";
            ViewBag.Productor = upp.IdProductorNavigation != null ? upp.IdProductorNavigation.NombreProductor + " " + upp.IdProductorNavigation.ApellidoPaternoProductor + " " + upp.IdProductorNavigation.ApellidoMaternoProductor : "";
            ViewBag.Municipio = upp.CatalogoLocalidade != null && upp.CatalogoLocalidade.ClaveMunicipioNavigation != null ? upp.CatalogoLocalidade.ClaveMunicipioNavigation.NombreMunicipio : "";
            ViewBag.Localidad = upp.CatalogoLocalidade != null ? upp.CatalogoLocalidade.NombreLocalidad : "";
            ViewBag.Razas = _context.CatalogoRazas.ToList();
            ViewBag.TiposProduccion = _context.CatalogoUsos.ToList();

            return View(registro);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UPPProductorUPPAsociadaVisitasEditar([FromForm] ProduccionUpp modelo)
        {
            try
            {
                _context.ProduccionUpps.Update(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction("UPPProductorUPPAsociadaVisitas", "UPP", new { id = modelo.IdProductorUpp });
            }
            catch (Exception ex)
            {
                return View(modelo);
            }
        }

        public ActionResult UPPProductorUPPAsociadaVisitasEliminar(int id)
        {
            var registro = _context.ProduccionUpps.FirstOrDefault(up => up.IdRegistroProduccion == id);

            var upp = _context.CatalogoProductoresUpps
                .Include(x => x.IdProductorNavigation)    // Trae los datos del productor
                .Include(x => x.CatalogoLocalidade)       // Trae los datos de la localidad (revisa el nombre exacto de esta propiedad en tu modelo)
                    .ThenInclude(l => l.ClaveMunicipioNavigation) // Trae los datos del municipio
                .FirstOrDefault(x => x.IdProductorUpp == registro.IdProductorUpp); // O .Id == id, dependiendo de tu llave primaria


            ViewBag.claveUPP = upp.ClaveUpp != null ? upp.ClaveUpp : "Sin clave de UPP";
            ViewBag.Productor = upp.IdProductorNavigation != null ? upp.IdProductorNavigation.NombreProductor + " " + upp.IdProductorNavigation.ApellidoPaternoProductor + " " + upp.IdProductorNavigation.ApellidoMaternoProductor : "";
            ViewBag.Municipio = upp.CatalogoLocalidade != null && upp.CatalogoLocalidade.ClaveMunicipioNavigation != null ? upp.CatalogoLocalidade.ClaveMunicipioNavigation.NombreMunicipio : "";
            ViewBag.Localidad = upp.CatalogoLocalidade != null ? upp.CatalogoLocalidade.NombreLocalidad : "";
            ViewBag.Razas = _context.CatalogoRazas.ToList();
            ViewBag.TiposProduccion = _context.CatalogoUsos.ToList();

            return View(registro);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UPPProductorUPPAsociadaVisitasEliminar([FromForm] ProduccionUpp modelo)
        {
            try
            {
                _context.ProduccionUpps.Remove(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction("UPPProductorUPPAsociadaVisitas", "UPP", new { id = modelo.IdProductorUpp });
            }
            catch (Exception ex)
            {
                return View(modelo);
            }
        }



    }
}
