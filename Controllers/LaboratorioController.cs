using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Seguimiento.Models;
using Seguimiento.Models.DTOs;
using System.Linq;

namespace Seguimiento.Controllers
{
    public class LaboratorioController : Controller
    {
        private readonly SedarpaContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        //Variable global para obtener el nombre de los meses
        private static readonly List<string> Meses = ["Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"];
        // Inyección de dependencias del DbContext
        public LaboratorioController(SedarpaContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        //Regresa la pantalla de inicio para el módulo de laboratorios móviles
        public async Task<IActionResult> LaboratorioInicio()
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

        //Realiza la descarga de los informes mesuales de los laboratorios móviles
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

        //Abre el formulario para agregar un registro mensual de un laboratorio
        public IActionResult RegistosMensualAgregar()
        {
            return View();
        }
        //Guarda la información del regisgtro mensual enviada por el formulario
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
                return RedirectToAction("LaboratorioInicio", "Laboratorio", new
                {
                    id = modelo.IdReporteMensual
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { exito = false, mensaje = "Error al guardar el registro.", detalle = ex.Message });
            }
        }

        //Carga el formulario para editar el registro mensual seleccionado
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
                return RedirectToAction("LaboratorioInicio", "Laboratorio");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocurrió un error al guardar: " + ex.Message });
            }
        }

        //Carga el formulario para eliminar el registro mensual de un laboratorio
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
                return RedirectToAction("LaboratorioInicio", "Laboratorio");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocurrió un error al guardar: " + ex.Message });
            }
        }

        //Obtiene la lista de las visitas capturadas en el registro mensual
        public async Task<IActionResult> bitacoraElectronicaInicio(int id)
        {
            var registroMensual = await _context.TblRegistroMensuals
                .Include(rm => rm.IdLaboratorioNavigation)
                .Include(rm => rm.IdUsuarioNavigation)
                .Include(rm => rm.TblBitacoraElectronicas)
                    .ThenInclude(rmd => rmd.IdProductorUppNavigation)
                        .ThenInclude(rmp => rmp.IdProductorNavigation)
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
        //Regresa el formulario para registrar una visita del laboratorio móvil
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
            _context.TblBitacoraElectronicas.Add(modelo);
            await _context.SaveChangesAsync();
            return RedirectToAction("bitacoraElectronicaInicio", "Laboratorio", new {id = modelo.IdReporteMensual });
        }

        //Regresa el formulario para editar una visita a un laboratorio
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

        //Guarda los cambios al registro de la visita de un laboratorio
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
                return RedirectToAction("bitacoraElectronicaInicio", "Laboratorio", new { id = modelo.IdReporteMensual });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocurrió un error al guardar: " + ex.Message });
            }

        }

        public IActionResult bitacoraElectronicaEliminar(int id)
        {
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
                return RedirectToAction("bitacoraElectronicaInicio", "Laboratorio", new { id = modelo.IdReporteMensual });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocurrió un error al guardar: " + ex.Message });
            }
        }

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

        public IActionResult RegistrosMensualActividadesAgregar(int id)
        {
            var laboratorio = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == id).Select(r => r.IdLaboratorio).FirstOrDefault();
            int mes = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == id).Select(r => (int)r.MesReporte).FirstOrDefault();
            ViewBag.IdReporteMensual = id;
            ViewBag.nombreLabotario = _context.CatalogoLaboratorios.Where(l => l.IdLaboratoio == laboratorio).Select(l => l.NombreLaboratorio).FirstOrDefault();
            ViewBag.mesReporte = Meses[mes - 1].ToString();
            ViewBag.anioReporte = _context.TblRegistroMensuals.Where(r => r.IdReporteMensual == id).Select(r => r.AñoReporte).FirstOrDefault();
            var accion = new TblRegistroMensualDetalle();
            return View(accion);
        }
        //Guarda el registro en la tabla de detalles del registro mensual y refirige la acción a la vista para el detalle del registro mensual
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrosMensualActividadesAgregar([FromForm] TblRegistroMensualDetalle modelo)
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
                return RedirectToAction("RegistrosMensualActividades", "Laboratorio", new { id = modelo.IdReporteMensual });
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Ocurrió un error al eliminar: " + ex.Message;
                return View("listaRegistrosMensualAgregarAccion", modelo);
                //return StatusCode(500, new { exito = false, mensaje = "Error al guardar el registro.", detalle = ex.Message });
            }
        }

        public IActionResult RegistrosMensualActividadesEditar(int id)
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
        public async Task<IActionResult> RegistrosMensualActividadesEditar([FromForm] TblRegistroMensualDetalle modelo)
        {
            try
            {
                modelo.FechaActualizacion = DateTime.Now;
                modelo.IdUsuario = HttpContext.Session.GetString("IdUsuario");
                _context.TblRegistroMensualDetalles.Update(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction("RegistrosMensualActividades", "Laboratorio", new { id = modelo.IdReporteMensual });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocurrió un error al guardar: " + ex.Message });
            }
        }

        public IActionResult RegistrosMensualActividadesEliminar(int id)
        {
            //Obtener la acción para eliminar
            var accion = _context.TblRegistroMensualDetalles
                .Include(mr => mr.IdReporteMensualNavigation)
                .Include(p => p.IdProductorNavigation) 
                .Include(l => l.IdReporteMensualNavigation)
                    .ThenInclude(lab => lab.IdLaboratorioNavigation)
                .FirstOrDefault(ra => ra.IdReporteMensualAccion == id);

            int mes = (int)accion.IdReporteMensualNavigation.MesReporte;
            ViewBag.mesReporte = Meses[mes - 1].ToString();
            return View(accion);
        }
        //Accion para eliminar una actividad del registro mensual
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrosMensualActividadesEliminar([FromForm] TblRegistroMensualDetalle modelo)
        {
            try
            {
                _context.TblRegistroMensualDetalles.Remove(modelo);
                await _context.SaveChangesAsync();
                return RedirectToAction("RegistrosMensualActividades", "Laboratorio", new { id = modelo.IdReporteMensual });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocurrió un error al guardar: " + ex.Message });
            }
        }

    }
}
