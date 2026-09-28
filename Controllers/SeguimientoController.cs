using ClosedXML;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Spreadsheet;
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
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Seguimiento.Controllers
{
    //[ApiController]
    //[Route("api/[controller]")]
    public class SeguimientoController : Controller
    {
        //Variable privada para el contexto de la base de datos
        private readonly SedarpaContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        
        //Variable global para obtener el nombre de los meses
        private static readonly List<string> Meses = ["Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio","Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"];
        // Inyección de dependencias del DbContext
        public SeguimientoController(SedarpaContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }
        //Valida que el registro con el Id enviado exista
        private bool TblRegistroMensualExists(int id)
        {
            return _context.TblRegistroMensuals.Any(e => e.IdReporteMensual == id);
        }

        //**********API's para obtener información de catálogos
        //Regresa la lista de laboratorios en formato JSON para el dropdown
        [HttpGet]
        //Catálogo de laboratorios
        public async Task<IActionResult> ObtenerLaboratorios()
        {
            try
            {
                // Se seleccionan solo los campos necesarios para aligerar la respuesta JSON
                var laboratorios = await _context.CatalogoLaboratorios
                    .Select(l => new
                    {
                        l.IdLaboratoio,
                        l.NombreLaboratorio
                    })
                    .OrderBy(l => l.NombreLaboratorio)
                    .ToListAsync();
                
                return Json(laboratorios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener laboratorios", detalle = ex.Message });
            }
        }
        //Catálogo de productores
        [HttpGet]
        public async Task<IActionResult> ObtenerProductores()
        {
            try
            {
                // Se seleccionan solo los campos necesarios para aligerar la respuesta JSON
                var productores = await _context.CatalogoProductores
                    .Select(p => new
                    {
                        p.IdProductor,
                        p.NombreProductor,
                        p.ApellidoPaternoProductor,
                        p.ApellidoMaternoProductor
                    })
                    .OrderBy(p => p.NombreProductor)
                    .ToListAsync();

                return Json(productores);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener productores", detalle = ex.Message });
            }
        }
        //Catalogo de ñproductores con UPP asociadas
        [HttpGet]
        public async Task<IActionResult> ObtenerProductoresUPP()
        {
            try
            {
                // Se seleccionan solo los campos necesarios para aligerar la respuesta JSON
                var productores = await _context.CatalogoProductores
                    .Select(p => new
                    {
                        p.IdProductor,
                        p.NombreProductor,
                        p.ApellidoPaternoProductor,
                        p.ApellidoMaternoProductor,
                        ProductoresUPP = p.CatalogoProductoresUpps.Select(upp => new { upp.ClaveUpp, upp.IdProductorUpp })
                    })
                    .OrderBy(p => p.NombreProductor)
                    .ToListAsync();
                return Json(productores);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener productores", detalle = ex.Message });
            }
        }
        //Funcion que regresa el catálogo de regiones
        public async Task<IActionResult> ObtenerRegiones() 
        {
            try
            {
                var regiones = await _context.CatalogoRegiones
                    .Select(m => new
                    {
                        m.IdRegion,
                        m.NombreRegion
                    })
                    .OrderBy(m => m.NombreRegion)
                    .ToListAsync();
                return Json(regiones);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener regiones", detalle = ex.Message });
            }
        }
        public async Task<IActionResult> ObtenerMunicipios()
        {
            try
            {
                var municipios = await _context.CatalogoMunicipios
                    .Select(m => new
                    {
                        m.ClaveMunicipio,
                        m.NombreMunicipio
                    })
                    .OrderBy(m => m.NombreMunicipio)
                    .ToListAsync();
                return Json(municipios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener municipios", detalle = ex.Message });
            }
        }
        public async Task<IActionResult> ObtenerLocalidades(string id)
        {
            try
            {
                var localidades = await _context.CatalogoLocalidades
                    .Where(m => m.ClaveMunicipio == id)
                    .Select(m => new
                    {
                        m.ClaveMunicipio,
                        m.ClaveLocalidad,
                        m.NombreLocalidad
                    })
                    .OrderBy(m => m.NombreLocalidad)
                    .ToListAsync();
                return Json(localidades);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener localidades", detalle = ex.Message });
            }
        }
        //**********Fin de API's para la obtención de catálogos

        //**********API's para la ventana de inicio de sesión, validación de usuario y cierre de sesión
        //Vista para el incio de sesión
        [HttpGet]
        public IActionResult Inicio()
        {
            return View();
        }
        //Acción para la validción del usuario al enviar el formulario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidarUsuario([FromForm] string idUsuario, [FromForm] string contraseñaUsuario)
        {
            try
            {
                // 1. Validar parámetros requeridos
                if (string.IsNullOrWhiteSpace(idUsuario) || string.IsNullOrWhiteSpace(contraseñaUsuario))
                {
                    return Json(new { exito = false, mensaje = "Debe ingresar el usuario y la contraseña." });
                }
                // 2. Buscar usuario activo en BD
                var usuario = await _context.CatalogoUsuarios
                    .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario && u.StatusUsuario == "A");
                if (usuario == null)
                {
                    return Json(new { exito = false, mensaje = "Usuario o contraseña incorrectos." });
                }
                // 3. Validar contraseña
                if (usuario.ContraseñaUsuario != contraseñaUsuario)
                {
                    return Json(new { exito = false, mensaje = "contraseña incorrecta" });
                }
                //bool esContraseñaValida = VerificarHashContraseña(contraseñaUsuario, usuario.ContraseñaUsuario);
                //if (!esContraseñaValida)
                //{
                //    return Json(new { exito = false, mensaje = "Usuario o contraseña incorrectos." });
                //}
                // 4. Guardar datos en Sesión
                HttpContext.Session.SetString("IdUsuario", usuario.IdUsuario);
                HttpContext.Session.SetString("NombreUsuario", usuario.NombreUsuario ?? "");
                HttpContext.Session.SetString("RolUsuario", usuario.RolUsuario ?? "");
                // 5. Retornar respuesta exitosa con la URL de redirección
                return Json(new { exito = true, redirectUrl = Url.Action("Index", "Seguimiento") });
            }
            catch (System.Exception ex)
            {
                return Json(new { exito = false, mensaje = "Error interno del servidor: " + ex.Message });
            }
        }
        //Acción para cerrar la sesión
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CerrarSesion()
        {
            // Limpia todas las variables de la sesión actual
            HttpContext.Session.Clear();
            // Redirige a la vista de inicio/login
            return RedirectToAction("Inicio", "Seguimiento");
        }
        //**********Fin de API's para el inicio de sesión, validación de usuario y cierre se sesión

        //**********Carga la vista para la página principal una vez que se ha iniciado la sesión
        public IActionResult Index()
        {
            return View();
        }
        //**********Fin de carga de página principal
        //**********API's para la configuración del sistema
        public IActionResult configuracionInicio() 
        { 
            return View(); 
        }
        //**********Fin de API's para la configurión del sistema
        //**********API para la consulta y desacarga de los padrones de beneficiarios
        public IActionResult padronesBeneficiariosInicio() 
        { 
            return View();
        }

        //**********Regresa la información de los padrones de beneficiarios en formato JSON para el mapa
        [HttpGet]
        public async Task<IActionResult> padronesBeneficiariosPuntos(short? anioReporte, string? nombre, short? region, string? municipio, string? localidad, [FromQuery] List<string>? padron)
        {
            
            var query = _context.VistaPadronBeneficiariosUbicacions
                .AsQueryable();
            if (anioReporte.HasValue && anioReporte > 0)
            {
                query = query.Where(r => r.PeriodoPadron == anioReporte.Value);
            }
            if (!string.IsNullOrEmpty(nombre))
            {
                query = query.Where(r => r.NombreBeneficiarios.Contains(nombre));
            }
            if (region.HasValue && region > 0)
            {
                query = query.Where(r => r.IdRegion == region.Value);
            }
            if (!string.IsNullOrEmpty(municipio))
            {
                query = query.Where(r => r.CveMunicipio == municipio);
            }
            if (!string.IsNullOrEmpty(localidad))
            {
                query = query.Where(r => r.CveLocalidad == localidad);
            }
            if (padron != null && padron.Any())
            {
                query = query.Where(r => padron.Contains(r.IdProyecto));
            }
            
            //Aplica los criterios y regresa la información en JSon
            var resultado = await query.Select(p => new
            {
                p.Latitud,
                p.Longitud,
                p.IdProyecto,
                p.NombreProyecto,
                p.NombreBeneficiarios
            })
            .ToListAsync();
            return Json(resultado);
        }

        //**********Padrón de beneficirios para Apicultura
        public async Task<IActionResult> padronesBeneficiariosApicultura(short? anioReporte, string? nombre, short? region, string? municipio, string? localidad)
        {
            var query = _context.VistaPadronBeneficiariosApiculturas
                .AsQueryable();
            // Aplicación de filtros opcionales
            if (anioReporte.HasValue && anioReporte > 0)
            {
                query = query.Where(r => r.PeriodoPadron == anioReporte.Value);
            }
            if (!string.IsNullOrEmpty(nombre)) 
            {
                query = query.Where(r => r.NombreBeneficiarios.Contains(nombre));
            }
            if (region.HasValue && region > 0)
            {
                query = query.Where(r => r.IdRegion == region.Value);
            }
            if (!string.IsNullOrEmpty(municipio))
            {
                query = query.Where(r => r.CveMunicipio == municipio);
            }
            if (!string.IsNullOrEmpty(localidad))
            {
                query = query.Where(r => r.CveLocalidad == localidad);
            }
            var resultado = await (from r in query
                                   select new
                                   {
                                       r.PeriodoPadron,
                                       r.IdBeneficiario,
                                       r.NombreRegion,
                                       r.NombreMunicipio,
                                       r.NombreLocalidad,
                                       r.NombreBeneficiarios,
                                       r.Curp,
                                       r.Dictamen,
                                       r.Folio,
                                       r.FolioActa
                                   })
                                   .Distinct()
                                   .OrderBy(r => r.NombreBeneficiarios)
                                   .ToListAsync();
            return Json(resultado);
        }
        //Regresa la información de jun beneficiario en formato Json
        [HttpGet]
        public JsonResult padronesBeneficiariosApiculturaBeneficiario(int id)
        {
            // Consulta a tu base de datos (Ejemplo con Entity Framework / Dapper)
            var listaApoyos = _context.VistaPadronBeneficiariosApiculturas.Where(pb => pb.IdBeneficiario == id).ToList();

            if (listaApoyos == null || !listaApoyos.Any())
            {
                return Json(new { success = false, message = "Registro no encontrado" });
            }

            // Retorna los datos requeridos
            return Json(new
            {
                success = true,
                registros = listaApoyos
            });
        }
        //Descarga del padron de beneficiarios de Apicultura
        public async Task<IActionResult> padronesBeneficiariosApiculturaDescarga([FromQuery] FiltrosDescargaDto filtros)
        {

            var query = _context.VistaPadronBeneficiariosApiculturas.AsNoTracking().AsQueryable();
            if (filtros.AnioReporte.HasValue)
            {
                query = query.Where(x => x.PeriodoPadron == filtros.AnioReporte);
            }
            if (!string.IsNullOrEmpty(filtros.Nombre))
            {
                query = query.Where(x => x.NombreBeneficiarios.Contains(filtros.Nombre));
            }
            if (filtros.Region.HasValue && filtros.Region != 0)
            {
                query = query.Where(x => x.IdRegion == filtros.Region);
            }
            if (!string.IsNullOrEmpty(filtros.Municipio))
            {
                query = query.Where(x => x.CveMunicipio == filtros.Municipio);
            }
            if (!string.IsNullOrEmpty(filtros.Localidad))
            {
                query = query.Where(x => x.CveLocalidad == filtros.Localidad);
            }
            var acciones = await query.ToListAsync();

            // 1. Consultar la vista de SQL Server mediante el DbContext
            //var acciones = await _context.VistaPadronBeneficiariosApiculturas.AsNoTracking().ToListAsync();

            // 2. Generar el Excel con ClosedXML
            using (XLWorkbook wb = new XLWorkbook())
            {
                // Carga la lista directamente en la hoja de Excel
                var worksheet1 = wb.Worksheets.Add("Padrón - Apicultura");
                worksheet1.Cell(1, 1).InsertTable(acciones);
                worksheet1.Rows().Style.Alignment.SetWrapText(false);
                worksheet1.Columns().AdjustToContents(); // Ajusta el ancho de las columnas

                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    byte[] content = stream.ToArray();

                    string fileName = $"PadronApicultura_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return File(content, contentType, fileName);
                }
            }
        }
        //**********Padrón de beneficirios para Apicultura

        //**********Padrón de beneficirios para Acuacultura
        public async Task<IActionResult> padronesBeneficiariosAcuacultura(short? anioReporte, string? nombre, short? region, string? municipio, string? localidad)
        {
            var query = _context.VistaPadronBeneficiariosAcuaculturas.AsQueryable();
            // Aplicación de filtros opcionales
            if (anioReporte.HasValue && anioReporte > 0)
            {
                query = query.Where(r => r.PeriodoPadron == anioReporte.Value);
            }
            if (!string.IsNullOrEmpty(nombre))
            {
                query = query.Where(r => r.NombreBeneficiarios.Contains(nombre));
            }
            if (region.HasValue && region > 0)
            {
                query = query.Where(r => r.IdRegion == region.Value);
            }
            if (!string.IsNullOrEmpty(municipio))
            {
                query = query.Where(r => r.CveMunicipio == municipio);
            }
            if (!string.IsNullOrEmpty(localidad))
            {
                query = query.Where(r => r.CveLocalidad == localidad);
            }
            var resultado = await (from r in query
                                   select new
                                   {
                                       r.PeriodoPadron,
                                       r.NombreRegion,
                                       r.NombreMunicipio,
                                       r.NombreLocalidad,
                                       r.NombreBeneficiarios,
                                       r.IdBeneficiario
                                   })
                                   .Distinct()
                                   .OrderBy(r => r.NombreBeneficiarios)
                                   .ToListAsync();
            return Json(resultado);
        }
        [HttpGet]
        public JsonResult padronesBeneficiariosAcuaculturaBeneficiario(int id)
        {
            // Consulta a tu base de datos (Ejemplo con Entity Framework / Dapper)
            var listaApoyos = _context.VistaPadronBeneficiariosAcuaculturas.Where(pb => pb.IdBeneficiario == id).ToList();

            if (listaApoyos == null || !listaApoyos.Any())
            {
                return Json(new { success = false, message = "Registro no encontrado" });
            }

            // Retorna los datos requeridos
            return Json(new
            {
                success = true,
                registros = listaApoyos
            });
        }
        public async Task<IActionResult> padronesBeneficiariosAcuaculturaDescarga([FromQuery] FiltrosDescargaDto filtros )
        {

            var query = _context.VistaPadronBeneficiariosAcuaculturas.AsNoTracking().AsQueryable();
            if (filtros.AnioReporte.HasValue)
            {
                query = query.Where(x => x.PeriodoPadron == filtros.AnioReporte);
            }
            if (!string.IsNullOrEmpty(filtros.Nombre))
            {
                query = query.Where(x => x.NombreBeneficiarios.Contains(filtros.Nombre));
            }
            if (filtros.Region.HasValue && filtros.Region != 0)
            {
                query = query.Where(x => x.IdRegion == filtros.Region);
            }
            if (!string.IsNullOrEmpty(filtros.Municipio))
            {
                query = query.Where(x => x.CveMunicipio == filtros.Municipio);
            }
            if (!string.IsNullOrEmpty(filtros.Localidad))
            {
                query = query.Where(x => x.CveLocalidad == filtros.Localidad);
            }
            var acciones = await query.ToListAsync();

            // 1. Consultar la vista de SQL Server mediante el DbContext
            //var acciones = await _context.VistaPadronBeneficiariosAcuaculturas.AsNoTracking().ToListAsync();

            // 2. Generar el Excel con ClosedXML
            using (XLWorkbook wb = new XLWorkbook())
            {
                // Carga la lista directamente en la hoja de Excel
                var worksheet1 = wb.Worksheets.Add("Padrón - Acuacultura");
                worksheet1.Cell(1, 1).InsertTable(acciones);
                worksheet1.Rows().Style.Alignment.SetWrapText(false);
                worksheet1.Columns().AdjustToContents(); // Ajusta el ancho de las columnas

                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    byte[] content = stream.ToArray();

                    string fileName = $"PadronAcuacultura_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return File(content, contentType, fileName);
                }
            }
        }
        //**********Padrón de beneficirios para Acuacultura

        //**********Padrón de beneficirios para Aves
        public async Task<IActionResult> padronesBeneficiariosAves(short? anioReporte, string? nombre, short? region, string? municipio, string? localidad)
        {
            var query = _context.VistaPadronBeneficiariosIfpaptevs
                .AsQueryable();
            // Aplicación de filtros opcionales
            if (anioReporte.HasValue && anioReporte > 0)
            {
                query = query.Where(r => r.PeriodoPadron == anioReporte.Value);
            }
            if (!string.IsNullOrEmpty(nombre))
            {
                query = query.Where(r => r.NombreBeneficiarios.Contains(nombre));
            }
            if (region.HasValue && region > 0)
            {
                query = query.Where(r => r.IdRegion == region.Value);
            }
            if (!string.IsNullOrEmpty(municipio))
            {
                query = query.Where(r => r.CveMunicipio == municipio);
            }
            if (!string.IsNullOrEmpty(localidad))
            {
                query = query.Where(r => r.CveLocalidad == localidad);
            }
            var resultado = await (from r in query
                                   select new
                                   {
                                       r.PeriodoPadron,
                                       r.Folio,
                                       r.NombreTipoApoyo,
                                       r.NombreBeneficiarios,
                                       r.NombreMunicipio,
                                       r.NombreLocalidad,
                                       r.IdRegion,
                                       r.NombreRegion
                                   })
                                   .Distinct()
                                   .OrderBy(r => r.NombreBeneficiarios)
                                   .ToListAsync();
            return Json(resultado);
        }
        public async Task<IActionResult> padronesBeneficiariosAvesDescarga([FromQuery] FiltrosDescargaDto filtros)
        {

            var query = _context.VistaPadronBeneficiariosIfpaptevs.AsNoTracking().AsQueryable();
            if (filtros.AnioReporte.HasValue)
            {
                query = query.Where(x => x.PeriodoPadron == filtros.AnioReporte);
            }
            if (!string.IsNullOrEmpty(filtros.Nombre))
            {
                query = query.Where(x => x.NombreBeneficiarios.Contains(filtros.Nombre));
            }
            if (filtros.Region.HasValue && filtros.Region != 0)
            {
                query = query.Where(x => x.IdRegion == filtros.Region);
            }
            if (!string.IsNullOrEmpty(filtros.Municipio))
            {
                query = query.Where(x => x.CveMunicipio == filtros.Municipio);
            }
            if (!string.IsNullOrEmpty(filtros.Localidad))
            {
                query = query.Where(x => x.CveLocalidad == filtros.Localidad);
            }
            var acciones = await query.ToListAsync();

            // 1. Consultar la vista de SQL Server mediante el DbContext
            //var acciones = await _context.VistaPadronBeneficiariosIfpaptevs.AsNoTracking().ToListAsync();

            // 2. Generar el Excel con ClosedXML
            using (XLWorkbook wb = new XLWorkbook())
            {
                // Carga la lista directamente en la hoja de Excel
                var worksheet1 = wb.Worksheets.Add("Padrón - Aves");
                worksheet1.Cell(1, 1).InsertTable(acciones);
                worksheet1.Rows().Style.Alignment.SetWrapText(false);
                worksheet1.Columns().AdjustToContents(); // Ajusta el ancho de las columnas

                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    byte[] content = stream.ToArray();

                    string fileName = $"PadronAves_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return File(content, contentType, fileName);
                }
            }
        }
        //**********Padrón de beneficirios para Aves

        //**********Padrón de beneficirios para ITAF
        public async Task<IActionResult> padronesBeneficiariosIATF(short? anioReporte, string? nombre, short? region, string? municipio, string? localidad)
        {
            var query = _context.VistaPadronBeneficiariosIatfs
                .AsQueryable();
            // Aplicación de filtros opcionales
            if (anioReporte.HasValue && anioReporte > 0)
            {
                query = query.Where(r => r.PeriodoPadron == anioReporte.Value);
            }
            if (!string.IsNullOrEmpty(nombre))
            {
                query = query.Where(r => r.NombreBeneficiarios.Contains(nombre));
            }
            if (region.HasValue && region > 0)
            {
                query = query.Where(r => r.IdRegion == region.Value);
            }
            if (!string.IsNullOrEmpty(municipio))
            {
                query = query.Where(r => r.CveMunicipio == municipio);
            }
            if (!string.IsNullOrEmpty(localidad))
            {
                query = query.Where(r => r.CveLocalidad == localidad);
            }
            var resultado = await (from r in query
                                   select new
                                   {
                                       r.IdBeneficiario,
                                       r.PeriodoPadron,
                                       r.NombreBeneficiarios,
                                       r.NombreMunicipio,
                                       r.NombreLocalidad,
                                       r.IdRegion,
                                       r.NombreRegion
                                   })
                                   .Distinct()
                                   .OrderBy(r => r.NombreBeneficiarios)
                                   .ToListAsync();
            return Json(resultado);
        }
        //Regresa la información de jun beneficiario en formato Json
        public JsonResult padronesBeneficiariosIATFBeneficiario(int id)
        {
            // Consulta a tu base de datos (Ejemplo con Entity Framework / Dapper)
            var listaApoyos = _context.VistaPadronBeneficiariosIatfs.Where(pb => pb.IdBeneficiario == id).ToList();

            if (listaApoyos == null || !listaApoyos.Any())
            {
                return Json(new { success = false, message = "Registro no encontrado" });
            }

            // Retorna los datos requeridos
            return Json(new
            {
                success = true,
                registros = listaApoyos
            });
        }

        //Descarga del padrón de beneficiarios IATF
        public async Task<IActionResult> padronesBeneficiariosIATFDescargar([FromQuery] FiltrosDescargaDto filtros)
        {

            var query = _context.VistaPadronBeneficiariosIatfs.AsNoTracking().AsQueryable();
            if (filtros.AnioReporte.HasValue)
            {
                query = query.Where(x => x.PeriodoPadron == filtros.AnioReporte);
            }
            if (!string.IsNullOrEmpty(filtros.Nombre))
            {
                query = query.Where(x => x.NombreBeneficiarios.Contains(filtros.Nombre));
            }
            if (filtros.Region.HasValue && filtros.Region != 0)
            {
                query = query.Where(x => x.IdRegion == filtros.Region);
            }
            if (!string.IsNullOrEmpty(filtros.Municipio))
            {
                query = query.Where(x => x.CveMunicipio == filtros.Municipio);
            }
            if (!string.IsNullOrEmpty(filtros.Localidad))
            {
                query = query.Where(x => x.CveLocalidad == filtros.Localidad);
            }
            var acciones = await query.ToListAsync();

            // 1. Consultar la vista de SQL Server mediante el DbContext
            //var acciones = await _context.VistaPadronBeneficiariosIfpaptevs.AsNoTracking().ToListAsync();

            // 2. Generar el Excel con ClosedXML
            using (XLWorkbook wb = new XLWorkbook())
            {
                // Carga la lista directamente en la hoja de Excel
                var worksheet1 = wb.Worksheets.Add("Padrón - IATF");
                worksheet1.Cell(1, 1).InsertTable(acciones);
                worksheet1.Rows().Style.Alignment.SetWrapText(false);
                worksheet1.Columns().AdjustToContents(); // Ajusta el ancho de las columnas

                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    byte[] content = stream.ToArray();

                    string fileName = $"PadronIATF_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return File(content, contentType, fileName);
                }
            }
        }
        //**********Padrón de beneficirios para Aves

        //**********API's para el registro de UPP
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
        public async Task<IActionResult> ObtenerListaProductores(string? CURP, string? nombre, string? paterno, string? materno) 
        {
            //Variable para guardar la conexión a la tabla
            var query = _context.CatalogoProductores
                .AsQueryable();
            //Sección para construir el where dependiendo de los parametros recibidos
            if (!string.IsNullOrEmpty(CURP)) { query = query.Where(r => r.CurpProductor.Contains(CURP)); }
            if (!string.IsNullOrEmpty(nombre)) { query = query.Where(r => r.NombreProductor.Contains(nombre)); }
            if (!string.IsNullOrEmpty(paterno)) { query = query.Where(r => r.ApellidoPaternoProductor.Contains(paterno)); }
            if (!string.IsNullOrEmpty(materno)) { query = query.Where(r => r.ApellidoMaternoProductor.Contains(materno)); }
            //Ejecución de la consulta
            var resultado = await (from r in query
                                   select new
                                   {
                                       r.IdProductor,
                                       r.CurpProductor  ,
                                       r.NombreProductor,
                                       //UsuarioNombre = r.IdUsuarioNavigation != null ? r.IdUsuarioNavigation.NombreUsuario : "Sin usuario",
                                       r.ApellidoPaternoProductor,
                                       r.ApellidoMaternoProductor,
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
                return RedirectToAction("UPPInicio", "Seguimiento");
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
                return RedirectToAction("UPPInicio", "Seguimiento");
            }
            catch (Exception ex)
            {
                return View(modelo);
            }
        }
        public IActionResult UPPProductorEliminar(int id)
        {
            var registro = _context.CatalogoProductores.FirstOrDefault(p => p.IdProductor == id);
            ViewBag.UPPRegistradas  = _context.CatalogoProductoresUpps.Where(up => up.IdProductor == id).ToList().Count();
            return View(registro);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UPPProductorEliminar([FromForm] CatalogoProductore modelo)
        {
            try
            {
                //var idUsuarioSesion = HttpContext.Session.GetString("IdUsuario");
                //modelo.FechaActualizacion = DateTime.Now;
                //modelo.IdUsuario = idUsuarioSesion;
                //modelo.CurpProductor = modelo.CurpProductor?.ToUpper();
                //modelo.NombreProductor = modelo.NombreProductor?.ToUpper();
                //modelo.ApellidoPaternoProductor = modelo.ApellidoPaternoProductor?.ToUpper();
                //modelo.ApellidoMaternoProductor = modelo.ApellidoMaternoProductor?.ToUpper();
                //modelo.DomicilioProductor = modelo.DomicilioProductor?.ToUpper();
                _context.CatalogoProductores.Remove(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction("UPPInicio", "Seguimiento");
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
                    .ThenInclude(cl=>cl.CatalogoLocalidade)
                        .ThenInclude(cm=>cm.ClaveMunicipioNavigation)
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
            ViewBag.nombreProductor = _context.CatalogoProductores.FirstOrDefault(p => p.IdProductor == id);
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
                return RedirectToAction("UPPProductorUPPAsociadas", "Seguimiento", new { id = modelo.IdProductor });
            }
            catch (Exception ex)
            {
                return View(modelo);
            }
        }
        //Enviar infoemación de una UPP para que sea editada
        public IActionResult UPPProductorUPPAsociadaEditar(int id) 
        {
            var registro = _context.CatalogoProductoresUpps.FirstOrDefault(up=>up.IdProductorUpp == id);
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
                return RedirectToAction("UPPProductorUPPAsociadas", "Seguimiento", new { id = modelo.IdProductor });
            }
            catch (Exception ex)
            {
                return View(modelo);
            }
        }
        //Enviar infoemación de una UPP para que sea eliminada
        public IActionResult UPPProductorUPPAsociadaEliminar(int id) 
        {
            var registro = _context.CatalogoProductoresUpps.FirstOrDefault(up => up.IdProductorUpp == id);
            var idUsuarioSesion = HttpContext.Session.GetString("IdUsuario");
            ViewBag.nombreProductor = _context.CatalogoProductores.FirstOrDefault(p => p.IdProductor == registro.IdProductor);
            return View(registro);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UPPProductorUPPAsociadaEliminar([FromForm] CatalogoProductoresUpp modelo) 
        {
            try
            {
                _context.CatalogoProductoresUpps.Remove(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction("UPPProductorUPPAsociadas", "Seguimiento", new { id = modelo.IdProductorUpp });
            }
            catch (Exception ex)
            {
                return View(modelo);
            }
        }
        //**********Fin de API's para el registro de UPP
        //**********API's para mostra la lista de los registros mensuales y el filtro de información
        public async Task<IActionResult> RegistrosMensualInicio() 
        { 
            return View(); 
        }
        // Endpoint AJAX para obtener registros filtrados de la tabla de TblRegistroMensuals y retornar un JSON con los resultados
        [HttpGet]
        public async Task<IActionResult> ObtenerListaLaboratorioRegistroMensual(int? idLaboratorio, short? mesReporte, short? anioReporte)
        {
            var query = _context.TblRegistroMensuals
                .Include(r => r.IdUsuarioNavigation)
                .AsQueryable();
            // Aplicación de filtros opcionales
            if (idLaboratorio.HasValue && idLaboratorio > 0)
            {
                query = query.Where(r => r.IdLaboratorio == idLaboratorio.Value);
            }
            if (mesReporte.HasValue && mesReporte > 0)
            {
                query = query.Where(r => r.MesReporte == mesReporte.Value);
            }
            if (anioReporte.HasValue && anioReporte > 0)
            {
                query = query.Where(r => r.AñoReporte == anioReporte.Value);
            }
            var resultado = await (from r in query
                                    join lab in _context.CatalogoLaboratorios
                                        on r.IdLaboratorio equals lab.IdLaboratoio into labGroup
                                    from lab in labGroup.DefaultIfEmpty() // LEFT JOIN en caso de que IdLaboratorio sea NULL
                                    select new
                                    {
                                        r.IdReporteMensual,
                                        r.IdUsuario,
                                        UsuarioNombre = r.IdUsuarioNavigation != null ? r.IdUsuarioNavigation.NombreUsuario : "Sin usuario",
                                        r.IdLaboratorio,
                                        NombreLaboratorio = lab != null ? lab.NombreLaboratorio : "Sin laboratorio",
                                        r.MesReporte,
                                        r.AñoReporte,
                                        FechaCaptura = r.FechaCaptura.HasValue ? r.FechaCaptura.Value.ToString("dd/MM/yyyy") : ""
                                    }).OrderBy(r => r.IdLaboratorio).ThenBy(r => r.MesReporte).ThenBy(r => r.AñoReporte).ToListAsync();
            if (HttpContext.Session.GetString("RolUsuario") == "A") 
            {
                return Json(resultado);
            }
            else 
            {
                return Json(resultado.Where(rm => rm.IdUsuario == HttpContext.Session.GetString("IdUsuario")));
            }
        }
        //
        public async Task<IActionResult> RegistroMensualDescarga([FromQuery] FiltrosDescargaLab filtros)
        {

            var queryAcciones = _context.VistaAccionesSeguimientos.AsNoTracking().AsQueryable();
            var queryBitacora = _context.VistaBitacoraElectronicas.AsNoTracking().AsQueryable();

            if (filtros.laboratorio.HasValue && filtros.laboratorio > 0)
            {
                queryAcciones = queryAcciones.Where(x => x.IdLaboratorio == filtros.laboratorio);
                queryBitacora = queryBitacora.Where(x => x.IdLaboratorio == filtros.laboratorio);
            }
            if (filtros.mesReporte.HasValue && filtros.mesReporte > 0)
            {
                queryAcciones = queryAcciones.Where(x => x.MesReporte == filtros.mesReporte);
                queryBitacora = queryBitacora.Where(x => x.MesReporte == filtros.mesReporte);
            }
            if (filtros.añoReporte.HasValue && filtros.añoReporte > 0)
            {
                queryAcciones = queryAcciones.Where(x => x.AñoReporte == filtros.añoReporte);
                queryBitacora = queryBitacora.Where(x => x.AñoReporte == filtros.añoReporte);
            }

            var acciones = await queryAcciones.ToListAsync();
            var bitacora = await queryBitacora.ToListAsync();

            // 2. Generar el Excel con ClosedXML
            using (XLWorkbook wb = new XLWorkbook())
            {
                // Carga la lista directamente en la hoja de Excel
                var worksheet1 = wb.Worksheets.Add("Acciones de seguimiento");
                var worksheet2 = wb.Worksheets.Add("Bitacora electrónica");
                worksheet1.Cell(1, 1).InsertTable(acciones);
                worksheet1.Rows().Style.Alignment.SetWrapText(false);
                worksheet1.Columns().AdjustToContents(); // Ajusta el ancho de las columnas
                worksheet2.Cell(1, 1).InsertTable(bitacora);
                worksheet2.Rows().Style.Alignment.SetWrapText(false);
                worksheet2.Columns().AdjustToContents(); // Ajusta el ancho de las columnas

                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    byte[] content = stream.ToArray();

                    string fileName = $"Reporte_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return File(content, contentType, fileName);
                }
            }
        }
        //**********Fin de API's para mostra la lista de los registros mensuales y el filtro de información

        //**********API's para mostrar el formulario de captura de un nuevo registro mensual y guardado de la información
        //Carga la vista para la captura de un registo mensual
        public IActionResult RegistosMensualAgregar() 
        { 
            return View();
        }
        //Guarda in informackin enviada por el formulario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistosMensualAgregar([FromForm] TblRegistroMensual modelo) 
        {
            try
            {
                modelo.FechaCaptura = DateTime.Now;
                modelo.FechaActualizacion = DateTime.Now;
                var idUsuarioSesion = HttpContext.Session.GetString("IdUsuario");
                modelo.IdUsuario = idUsuarioSesion;
                //Agregar registro a la base de datos
                _context.TblRegistroMensuals.Add(modelo);
                await _context.SaveChangesAsync();
                //ViewBag.nombreLabotario = _context.CatalogoLaboratorios.FirstOrDefault(l => l.IdLaboratoio == id)?.NombreLaboratorio;
                var laboratorio = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == modelo.IdReporteMensual).Select(r => r.IdLaboratorio).FirstOrDefault();
                var mes = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == modelo.IdReporteMensual).Select(r => r.MesReporte).FirstOrDefault();
                var anio = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == modelo.IdReporteMensual).Select(r => r.AñoReporte).FirstOrDefault();
                //Redirigir a la página del registro mensual
                return RedirectToAction("RegistrosMensualActividades", "Seguimiento", new
                {
                    id = modelo.IdReporteMensual
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { exito = false, mensaje = "Error al guardar el registro.", detalle = ex.Message });
            }
        }
        //**********Fin de API' para mostrar el formulario de captura de un nuevo registro mensual y guardado de la información

        //**********API's para mostrar la información del registro mensual seleccionado, agregar acciones, editar información y borrar un registro mensual
        //Regresa la información de un registro mensual específico junto con sus actividades relacionadas en formato JSON
        public async Task<IActionResult> RegistrosMensualActividades(int id) 
        {
            //Obtiene el registro mensual junto con sus detalles y el usuario relacionado
            var registroMensual = await _context.TblRegistroMensuals
                .Include(rm => rm.IdLaboratorioNavigation)
                .Include(rm => rm.IdUsuarioNavigation)
                .Include(rm => rm.TblRegistroMensualDetalles)
                    .ThenInclude(rmd => rmd.IdProductorNavigation)
                .FirstOrDefaultAsync(rm => rm.IdReporteMensual == id);
            //Valida que el registro mensual exista
            if (registroMensual == null)
            {
                return NotFound();
            }
            //Manda los datos a la vista y reenderiza la información del registro mensual y sus detalles
            ViewBag.mesReporte = Meses[(int)registroMensual.MesReporte.Value - 1].ToString();
            return View(registroMensual);
        }
        //Regresa la información de un registro mensual para que pueda ser editado
        [HttpGet]
        public async Task<IActionResult> RegistrosMensualEditar(int id)
        {
            //ViewData["IdLaboratorio"] = new SelectList(await _context.CatalogoLaboratorios.ToListAsync(), "IdLaboratorio", "NombreLaboratorio");
            var model = await _context.TblRegistroMensuals.SingleOrDefaultAsync(m => m.IdReporteMensual == id);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }
        //Actualiza la información de un registro mensual con los datos que recibe de la vista: laboratorioMovilEditarRegistroMensual
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrosMensualEditar([FromForm] TblRegistroMensual modelo) 
        {
            try
            {
                modelo.FechaActualizacion = DateTime.Now;
                _context.TblRegistroMensuals.Update(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction("RegistrosMensualInicio", "Seguimiento");
            }
            catch (Exception ex) 
            {
                return Json(new { success = false, message = "Ocurrió un error al guardar: " + ex.Message });
            }
        }
        //Vista que presenta la información del registro mensual que se desea eliminar
        public async Task<IActionResult> RegistrosMensualEliminar(int id)
        {
            //Obtiene el registro mensual junto con sus detalles y el usuario relacionado
            var registroMensual = await _context.TblRegistroMensuals
                .Include(rm => rm.IdLaboratorioNavigation)
                .Include(rm => rm.IdUsuarioNavigation)
                .Include(rm => rm.TblRegistroMensualDetalles)
                    .ThenInclude(rmd => rmd.IdProductorNavigation)
                .FirstOrDefaultAsync(rm => rm.IdReporteMensual == id);
            //Valida que el registro mensual exista
            if (registroMensual == null)
            {
                return NotFound();
            }
            //Manda los datos a la vista y reenderiza la información del registro mensual y sus detalles
            ViewBag.mesReporte = Meses[(int)registroMensual.MesReporte.Value - 1].ToString();
            ViewBag.totalAcciones = registroMensual.TblRegistroMensualDetalles?.Count ?? 0;
            return View(registroMensual);
        }
        //Acción que elimina el registro mensual
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrosMensualEliminar([FromForm] TblRegistroMensual modelo) 
        {
            try
            {
                _context.TblRegistroMensuals.Remove(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction("RegistrosMensualInicio", "Seguimiento");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocurrió un error al guardar: " + ex.Message });
            }
        }
        //**********Fin de API's para mostrar la información del registro mensual seleccionado

        //**********API's para agregar, editar o eliminar una acción al registro mensual seleccionado
        //Llama a la vista para la captura de una acción y genera un registroi vacío pafra pasarlo a la vista
        public IActionResult listaRegistrosMensualAgregarAccion(int id)
        {
            var laboratorio = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == id).Select(r => r.IdLaboratorio).FirstOrDefault();
            int mes = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == id).Select(r => (int)r.MesReporte).FirstOrDefault();
            ViewBag.IdReporteMensual = id;
            ViewBag.nombreLabotario = _context.CatalogoLaboratorios.Where(l=>l.IdLaboratoio==laboratorio).Select(l=>l.NombreLaboratorio).FirstOrDefault();
            ViewBag.mesReporte = Meses[mes-1].ToString();
            ViewBag.anioReporte = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == id).Select(r => r.AñoReporte).FirstOrDefault();
            var accion = new TblRegistroMensualDetalle();
            return View(accion);
        }
        //Guarda el registro en la tabla de detalles del registro mensual y refirige la acción a la vista para el detalle del registro mensual
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarAccionRegistroMensual([FromForm] TblRegistroMensualDetalle modelo)
        {
            try
            {
                modelo.FechaCaptura = DateTime.Now;
                var idUsuarioSesion = HttpContext.Session.GetString("IdUsuario");
                modelo.IdUsuario = idUsuarioSesion;
                //Agregar registro a la base de datos
                _context.TblRegistroMensualDetalles.Add(modelo);
                await _context.SaveChangesAsync();
                //ViewBag.nombreLabotario = _context.CatalogoLaboratorios.FirstOrDefault(l => l.IdLaboratoio == id)?.NombreLaboratorio;
                var laboratorio = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == modelo.IdReporteMensual).Select(r => r.IdLaboratorio).FirstOrDefault();
                var mes = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == modelo.IdReporteMensual).Select(r => r.MesReporte).FirstOrDefault();
                var anio = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == modelo.IdReporteMensual).Select(r => r.AñoReporte).FirstOrDefault();
                //Redirigir a la página del registro mensual
                return RedirectToAction("RegistrosMensualActividades", "Seguimiento", new
                {
                    id = modelo.IdReporteMensual
                });
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Ocurrió un error al eliminar: " + ex.Message;
                return View("listaRegistrosMensualAgregarAccion", modelo);
                //return StatusCode(500, new { exito = false, mensaje = "Error al guardar el registro.", detalle = ex.Message });
            }
        }
        //Acción que regresa a la vista los datos del registro de acciones que se va a actualizar
        public IActionResult listaRegistrosMensualEditarAccion(int id)
        {
            //Obtener la acción para editar
            var accion = _context.TblRegistroMensualDetalles.FirstOrDefault(ra => ra.IdReporteMensualAccion == id);
            //Obtener el nombre del laboratorio, mes y año del registro mensual
            var laboratorio = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == accion.IdReporteMensual).Select(r => r.IdLaboratorio).FirstOrDefault();
            int mes = (int)_context.TblRegistroMensuals.Where(r => r.IdReporteMensual == accion.IdReporteMensual).Select(r => r.MesReporte).FirstOrDefault();
            ViewBag.nombreLabotario = _context.CatalogoLaboratorios.Where(l => l.IdLaboratoio == laboratorio).Select(l => l.NombreLaboratorio).FirstOrDefault();
            ViewBag.mesReporte = Meses[mes - 1].ToString();
            ViewBag.anioReporte = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == accion.IdReporteMensual).Select(r => r.AñoReporte).FirstOrDefault();
            //Envía el registro a la vista
            return View(accion);
        }
        //Accion que actualiza el registro de la actividad en el registro mensual
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarAccionRegistroMensual([FromForm] TblRegistroMensualDetalle modelo) 
        {
            try
            {
                modelo.FechaActualizacion = DateTime.Now;
                modelo.IdUsuario = HttpContext.Session.GetString("IdUsuario");
                _context.TblRegistroMensualDetalles.Update(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction("RegistrosMensualActividades", "Seguimiento", new {id=modelo.IdReporteMensual});
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocurrió un error al guardar: " + ex.Message });
            }
        }
        //Acción para eliminar la actividad seleccionada del registro mesual
        public IActionResult listaRegistrosMensualEliminarAccion(int id) 
        {
            //Obtener la acción para eliminar
            var accion = _context.TblRegistroMensualDetalles.FirstOrDefault(ra => ra.IdReporteMensualAccion == id);
            //Obtener el nombre del laboratorio, mes y año del registro mensual
            var laboratorio = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == accion.IdReporteMensual).Select(r => r.IdLaboratorio).FirstOrDefault();
            int mes = (int)_context.TblRegistroMensuals.Where(r => r.IdReporteMensual == accion.IdReporteMensual).Select(r => r.MesReporte).FirstOrDefault();
            ViewBag.nombreLabotario = _context.CatalogoLaboratorios.Where(l => l.IdLaboratoio == laboratorio).Select(l => l.NombreLaboratorio).FirstOrDefault();
            ViewBag.mesReporte = Meses[mes - 1].ToString();
            ViewBag.anioReporte = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == accion.IdReporteMensual).Select(r => r.AñoReporte).FirstOrDefault();
            ViewBag.nombreProductor = _context.CatalogoProductores.Where(r=> r.IdProductor == accion.IdProductor).Select(r =>r.NombreProductor).FirstOrDefault();
            //Envía el registro a la vista
            return View(accion);
        }
        //Accion para eliminar una actividad del registro mensual
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ElimimarAccionRegistroMensual([FromForm] TblRegistroMensualDetalle modelo)
        {
            try
            {
                _context.TblRegistroMensualDetalles.Remove(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction("RegistrosMensualActividades", "Seguimiento", new {id=modelo.IdReporteMensual});
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocurrió un error al guardar: " + ex.Message });
            }
        }
        //**********Fin de API's para agregar, editar o eliminar una acción al registro mensual seleccionado

        //*********Inicio de las API para el manejo de la información en la bitácora electrónica
        //Obtiene la lista de las visitas capturadas en el registro mensual
        public async Task<IActionResult> bitacoraElectronicaInicio(int id)
        {
            var registroMensual = await _context.TblRegistroMensuals
                .Include(rm => rm.IdLaboratorioNavigation)
                .Include(rm => rm.IdUsuarioNavigation)
                .Include(rm => rm.TblBitacoraElectronicas)
                    .ThenInclude(rmd => rmd.IdProductorUppNavigation)
                        .ThenInclude(rmp=>rmp.IdProductorNavigation)
                .FirstOrDefaultAsync(rm => rm.IdReporteMensual == id);
            //Valida que el registro mensual exista
            if (registroMensual == null)
            {
                return NotFound();
            }
            //Manda los datos a la vista y reenderiza la información del registro mensual y sus detalles
            ViewBag.mesReporte = Meses[(int)registroMensual.MesReporte.Value - 1].ToString();
            return View(registroMensual);
        }
        //
        public IActionResult bitacoraElectronicaAgregar(int id) 
        {
            var laboratorio = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == id).Select(r => r.IdLaboratorio).FirstOrDefault();
            int mes = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == id).Select(r => (int)r.MesReporte).FirstOrDefault();
            ViewBag.IdReporteMensual = id;
            ViewBag.nombreLabotario = _context.CatalogoLaboratorios.Where(l => l.IdLaboratoio == laboratorio).Select(l => l.NombreLaboratorio).FirstOrDefault();
            ViewBag.mesReporte = Meses[mes - 1].ToString();
            ViewBag.anioReporte = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == id).Select(r => r.AñoReporte).FirstOrDefault();
            var accion = new TblBitacoraElectronica();
            accion?.IdReporteMensual = id;
            return View(accion);
        }
        //Acción para guarar la información de visitias que recibe de la vista
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> bitacoraElectronicaAgregar([FromForm] TblBitacoraElectronica modelo)
        {
            modelo.IdUsuario = HttpContext.Session.GetString("IdUsuario");
            modelo.FechaCaptura = DateTime.Now;

            // 4. Agregar y guardar en la base de datos
            _context.TblBitacoraElectronicas.Add(modelo);
            await _context.SaveChangesAsync();
            //ViewBag.nombreLabotario = _context.CatalogoLaboratorios.FirstOrDefault(l => l.IdLaboratoio == id)?.NombreLaboratorio;
            //var laboratorio = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == modelo.IdReporteMensual).Select(r => r.IdLaboratorio).FirstOrDefault();
            //var mes = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == modelo.IdReporteMensual).Select(r => r.MesReporte).FirstOrDefault();
            //var anio = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == modelo.IdReporteMensual).Select(r => r.AñoReporte).FirstOrDefault();
            //Redirigir a la página del registro mensual
            return RedirectToAction("bitacoraElectronicaInicio", "Seguimiento", new
            {
                id = modelo.IdReporteMensual
            });
        }
        public IActionResult bitacoraElectronicaEditar(int id)
        {
            var accion = _context.TblBitacoraElectronicas.FirstOrDefault(rb => rb.IdBitacoraElectronica == id);
            accion.FechaActualizacion = DateTime.Now;
            var laboratorio = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == accion.IdReporteMensual).Select(r => r.IdLaboratorio).FirstOrDefault();
            int mes = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == accion.IdReporteMensual).Select(r => (int)r.MesReporte).FirstOrDefault();
            ViewBag.IdReporteMensual = accion.IdReporteMensual;
            ViewBag.nombreLabotario = _context.CatalogoLaboratorios.Where(l => l.IdLaboratoio == laboratorio).Select(l => l.NombreLaboratorio).FirstOrDefault();
            ViewBag.mesReporte = Meses[mes - 1].ToString();
            ViewBag.anioReporte = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == accion.IdReporteMensual).Select(r => r.AñoReporte).FirstOrDefault();
            return View(accion);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> bitacoraElectronicaEditar([FromForm] TblBitacoraElectronica modelo) 
        {
            try
            {
                modelo.FechaActualizacion = DateTime.Now;
                modelo.IdUsuario = HttpContext.Session.GetString("IdUsuario");
                _context.TblBitacoraElectronicas.Update(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction("bitacoraElectronicaInicio", "Seguimiento", new { id = modelo.IdReporteMensual });
            }
            catch (Exception ex) 
            {
                return Json(new { success = false, message = "Ocurrió un error al guardar: " + ex.Message });
            }

        }
        public IActionResult bitacoraElectronicaEliminar(int id) {
            var accion = _context.TblBitacoraElectronicas
                .Include(upp => upp.IdProductorUppNavigation)
                    .ThenInclude(p => p.IdProductorNavigation)
                .FirstOrDefault(rb => rb.IdBitacoraElectronica == id);

            //var laboratorio = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == accion.IdReporteMensual).Select(r => r.IdLaboratorio).FirstOrDefault();
            //int mes = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == accion.IdReporteMensual).Select(r => (int)r.MesReporte).FirstOrDefault();
            //ViewBag.IdReporteMensual = accion.IdReporteMensual;
            //ViewBag.nombreLabotario = _context.CatalogoLaboratorios.Where(l => l.IdLaboratoio == laboratorio).Select(l => l.NombreLaboratorio).FirstOrDefault();
            //ViewBag.mesReporte = Meses[mes - 1].ToString();
            //ViewBag.anioReporte = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == accion.IdReporteMensual).Select(r => r.AñoReporte).FirstOrDefault();
            //ViewBag.anioReporte = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == accion.IdReporteMensual).Select(r => r.AñoReporte).FirstOrDefault();
            ////ViewBag.nombreProductor = _context.CatalogoProductores.Where(p => p.IdProductor == accion.IdProductorUppNavigation.IdProductorNavigation.IdProductor).Select(p => p.NombreProductor).FirstOrDefault();

            var laboratorio = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == accion.IdReporteMensual).Select(r => r.IdLaboratorio).FirstOrDefault();
            int mes = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == accion.IdReporteMensual).Select(r => (int)r.MesReporte).FirstOrDefault();
            ViewBag.IdReporteMensual = accion.IdReporteMensual;
            ViewBag.nombreLabotario = _context.CatalogoLaboratorios.Where(l => l.IdLaboratoio == laboratorio).Select(l => l.NombreLaboratorio).FirstOrDefault();
            ViewBag.mesReporte = Meses[mes - 1].ToString();
            ViewBag.anioReporte = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == accion.IdReporteMensual).Select(r => r.AñoReporte).FirstOrDefault();

            return View(accion);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> bitacoraElectronicaEliminar([FromForm] TblBitacoraElectronica modelo)
        {
            try
            {
                _context.TblBitacoraElectronicas.Remove(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction("bitacoraElectronicaInicio", "Seguimiento", new { id = modelo.IdReporteMensual });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocurrió un error al guardar: " + ex.Message });
            }
        }
        //**********Fin de las API para el manejo de la información en la bitácora electrónica
        //**********API's para el´módulo de movilización de ganbado
        public IActionResult movilizacionGanadoInicio() 
        { 
            return View(); 
        }
        //Obtener la lista de las solicitudes de intenación
        public async Task<IActionResult> SolicitudesInternacion(int? id, string? nombre, DateOnly? fecha)
        {

            var query = _context.SolicitudInternacions.AsQueryable();
            // Aplicación de filtros opcionales
            if (id.HasValue)
            {
                query = query.Where(r => r.IdSolicitudInternacion == id.Value);
            }
            if (!string.IsNullOrEmpty(nombre))
            {
                query = query.Where(r => r.NombreSolicitante.Contains(nombre));
            }
            if (fecha.HasValue)
            {
                query = query.Where(r => r.FechaSolicitud == fecha.Value);
            }
            var resultado = await (from r in query
                                   select new
                                   {
                                       r.IdSolicitudInternacion,
                                       r.FechaSolicitud,
                                       r.NombreSolicitante,
                                       r.TipoGanadoNavigation.DescTipoGanado,
                                       r.MotivoTrasladoNavigation.DescMotivoTraslado,
                                       r.Origen,
                                       r.Destino
                                   })
                                   .Distinct()
                                   .OrderBy(r => r.FechaSolicitud).ThenBy(r => r.NombreSolicitante)    
                                   .ToListAsync();
            return Json(resultado);
        }

        //Regresa la información de una solicitud de internación para que pueda ser consultada
        public async Task<IActionResult> SolicitudesInternacionDetalle(int id)
        {
            var solicitud = await _context.SolicitudInternacions
                .Include(s => s.TipoGanadoNavigation)
                .Include(s => s.MotivoTrasladoNavigation)
                .FirstOrDefaultAsync(s => s.IdSolicitudInternacion == id);
            if (solicitud == null)
            {
                return NotFound();
            }
            return View(solicitud);
        }
        //Regresa la lista de documentos cargados para una solicitud de internación específica en formato JSON
        public async Task<IActionResult> SolicitudesInternacionDocumentos(int idSolicitud)
        {
            try
            {
                var documentosCargados = await _context.SolicitudInternacionDocumentos
                    .Where(d => d.IdSolicitudInternacion == idSolicitud)
                    .Select(d => new
                    {
                        d.IdDocumento,
                        d.NombreDocumento,
                        d.Observaciones,
                        tipoDocumento = d.IdDocumentoNavigation.DescDocumento,
                        d.FechaCarga
                    })
                    .ToListAsync();
                return Json(documentosCargados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener documentos cargados", detalle = ex.Message });
            }
        }
        //Obuiene el documento para mostrar en la vista de detalle de la solicitud de internación
        [HttpGet("obtener-documento/{nombreArchivo}")]
        public IActionResult ObtenerDocumento(string nombreArchivo)
        {
            // Ruta donde el otro proyecto guarda las imágenes
            string rutaBase = @"C:\SEDARPA\Proyectos\solicitudInternacion\wwwroot\uploads";
            string rutaCompleta = Path.Combine(rutaBase, nombreArchivo);

            if (!System.IO.File.Exists(rutaCompleta))
            {
                return NotFound("El archivo no existe.");
            }

            // Obtener el tipo de contenido (image/png, application/pdf, etc.)
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(rutaCompleta, out string contentType))
            {
                contentType = "application/octet-stream"; // Valor por defecto si no reconoce la extensión
            }

            byte[] bytesArchivo = System.IO.File.ReadAllBytes(rutaCompleta);
            
            return File(bytesArchivo, contentType); // O usa un proveedor de tipos MIME
        }

        //**********Fin de api's para el´módulo de movilización de ganbado

        //**********API's para el módulo de estadisticas
        //Regresa los datos de la gráfica en formato JSON para que puedan ser consumidos por el frontend
        [HttpGet]
        public async Task<IActionResult> PadronesBeneficiariosEstadisticas([FromQuery] string[] proyectos)
        {
            try
            {

                // Construir la URL con parámetros de consulta (?proyectos=A&proyectos=B&municipios=X)
                var queryParams = new List<string>();
                var client = _httpClientFactory.CreateClient("PythonApi");

                if (proyectos != null)
                    queryParams.AddRange(proyectos.Select(p => $"proyectos={Uri.EscapeDataString(p)}"));

                string queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                string endpoint = $"api/estadisticas/beneficiarios-por-programa-filtro{queryString}";

                var response = await client.GetFromJsonAsync<EstadisticasProgramaDto>(endpoint);
                return Json(response);

                //var client = _httpClientFactory.CreateClient("PythonApi");
                //var response = await client.GetFromJsonAsync<EstadisticasProgramaDto>("api/estadisticas/beneficiarios-por-programa");

                //return Json(response); // Devuelve los datos en formato JSON directo
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error al conectar con la API de Python",
                    detalle = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        public async Task<IActionResult> PadronesBeneficiariosApicultura5mas()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PythonApi");
                var response = await client.GetFromJsonAsync<EstadisticasProgramaDto>("api/estadisticas/beneficiarios-apicultura-5mas");

                return Json(response); // Devuelve los datos en formato JSON directo
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }

        }

        public async Task<IActionResult> PadronesBeneficiariosAves5mas()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PythonApi");
                var response = await client.GetFromJsonAsync<EstadisticasProgramaDto>("api/estadisticas/beneficiarios-aves-5mas");

                return Json(response); // Devuelve los datos en formato JSON directo
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }

        }

        public async Task<IActionResult> PadronesBeneficiariosAcuacultura5mas()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PythonApi");
                var response = await client.GetFromJsonAsync<EstadisticasProgramaDto>("api/estadisticas/beneficiarios-acuacultura-5mas");

                return Json(response); // Devuelve los datos en formato JSON directo
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }

        }
        
        public async Task<IActionResult> PadronesBeneficiariosInseminacion5mas()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PythonApi");
                var response = await client.GetFromJsonAsync<EstadisticasProgramaDto>("api/estadisticas/beneficiarios-inseminacion-5mas");

                return Json(response); // Devuelve los datos en formato JSON directo
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }

        }

    }
}
