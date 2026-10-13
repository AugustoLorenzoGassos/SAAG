using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

using Seguimiento.Models;

namespace Seguimiento.Controllers
{
    public class MovilizacionController : Controller
    {
        private readonly SedarpaContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        // Inyección de dependencias del DbContext
        public MovilizacionController(SedarpaContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

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
                                       r.Destino,
                                       totalDocumentos = _context.SolicitudInternacionDocumentos.Count(d => d.IdSolicitudInternacion == r.IdSolicitudInternacion)
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
                        d.IdSolicitudInternacionDocumento,
                        d.IdDocumento,
                        d.NombreDocumento,
                        d.Observaciones,
                        tipoDocumento = d.IdDocumentoNavigation.DescDocumento,
                        d.FechaCarga,
                        d.TipoMime,
                        d.StatusDocumento
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

        [HttpPost]
        public async Task<IActionResult> ActualizarEstatusDocumento(int idDocumento)
        {
            var documento = await _context.SolicitudInternacionDocumentos.FindAsync(idDocumento);
            if (documento == null)
            {
                return NotFound("El documento no existe.");
            }
            if (documento.StatusDocumento == true) {
                documento.StatusDocumento = false;
            } else {
                documento.StatusDocumento = true;
            }
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Estatus actualizado correctamente." });
        }

        //**********Fin de api's para el´módulo de movilización de ganbado

    }
}
