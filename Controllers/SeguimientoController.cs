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

    }
}
