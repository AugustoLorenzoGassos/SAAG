using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Seguimiento.Models;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net;
using System.Net.Mail;
using System.Net.Mail;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using QRCoder;

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

        //API para gebnerar elQR
        private byte[] GenerarOObtenerBytesQr(int id) {

            //string textoQr = $"https://localhost:7095/solicitudInternacion/solicitudInternacionSeguimiento/{id}";
            String textoQr = $"https://www.gob.mx/sep";

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                // Creamos los datos del código QR
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(textoQr, QRCodeGenerator.ECCLevel.Q);

                // Usamos el generador PNG para obtener la imagen en bytes
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    // El método GetGraphic genera los bytes de la imagen PNG (con tamaño de píxeles por módulo, ej. 20)
                    byte[] qrBytes = qrCode.GetGraphic(20);
                    return qrBytes;
                }
            }

        }

        //**********API's para el´módulo de movilización de ganbado
        public IActionResult movilizacionGanadoInicio()
        {
            return View();
        }

        //Página con la lista de los permisos recibidos
        public async Task<IActionResult> movilizacionGanadoPermisos()
        {

            var lista = await _context.SolicitudInternacions
                .Include(t => t.MotivoTrasladoNavigation)
                .Include(g => g.TipoGanadoNavigation)
                .Include(d => d.SolicitudInternacionDocumentos)
                .Include(s=>s.StatusSolicitudNavigation)
                .ToListAsync();

            return View(lista);


        }

        public async Task<IActionResult> movilizacionGanadoPermisosIniciarAtencion(int id) {
            var registo = _context.SolicitudInternacions.Find(id);
            if (registo != null) {
                registo.IdUsuario = HttpContext.Session.GetString("IdUsuario");
                registo.StatusSolicitud = 2;
                registo.FechaInicioAtencion = DateTime.Now;
                _context.SolicitudInternacions.Update(registo);
                _context.SaveChanges();
            }

            return RedirectToAction("SolicitudesInternacionDetalle", "Movilizacion", new { id = registo.IdSolicitudInternacion });
        }

        public async Task<IActionResult> movilizacionGanadoPermisosAceptar(int id, int id1)
        {
            var registo = _context.SolicitudInternacions.Find(id);
            if (registo != null)
            {
                registo.IdUsuario = HttpContext.Session.GetString("IdUsuario");
                registo.StatusSolicitud = id1;
                registo.FechaInicioAtencion = DateTime.Now;
                _context.SolicitudInternacions.Update(registo);
                _context.SaveChanges();
            }

            return RedirectToAction("movilizacionGanadoPermisosMensaje", "Movilizacion", new { id = registo.IdSolicitudInternacion });

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> movilizacionGanadoPermisosAceptarNotificar([FromForm] SolicitudInternacion modelo)
        {

            //Actualización con el motivo de rechazo y las observaciones
            if (modelo.StatusSolicitud == 4)
            {
                modelo.MotivoRechazo = null;
            }
            _context.SolicitudInternacions.Update(modelo);
            _context.SaveChanges();

            //Recuperación de las relaciones asociadas al contexto para el registro
            _context.Attach(modelo);
            await _context.Entry(modelo)
                              .Reference(u => u.IdUsuarioNavigation)
                              .LoadAsync();
            await _context.Entry(modelo)
                              .Reference(m => m.MotivoRechazoNavigation)
                              .LoadAsync();

            //Obneter datos del origen
            var datosOrigen = _context.CatalogoProductoresUpps
                                .Include(p => p.IdProductorNavigation)
                                .Include(l => l.CatalogoLocalidade)
                                    .ThenInclude(m => m.ClaveMunicipioNavigation)
                                .FirstOrDefault(upp => upp.ClaveUpp == modelo.Origen);
            //Obneter datos del origen
            var datosDestino = _context.CatalogoProductoresUpps
                                .Include(p => p.IdProductorNavigation)
                                .Include(l => l.CatalogoLocalidade)
                                    .ThenInclude(m => m.ClaveMunicipioNavigation)
                                .FirstOrDefault(upp => upp.ClaveUpp == modelo.Destino);

            try
            {
                string remitente = "augusto.lorenzo.acero@gmail.com";
                string passwordApp = "lipl brce pobo nknr"; // Tu contraseña de aplicación

                MailMessage mensaje = new MailMessage();
                mensaje.From = new MailAddress(remitente, "Sistema SAAG - SEDARPA");
                mensaje.To.Add("rosa.reyes.ayala@gmail.com");
                mensaje.To.Add("augusto.lorenzo.acero@gmail.com");
                mensaje.Subject = $"Notificación de Solicitud de Internación #{modelo.IdSolicitudInternacion}";

                //Carga de imagenes
                string rutaImagen = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logo-sedarpa.png");
                LinkedResource inlineLogo = new LinkedResource(rutaImagen, MediaTypeNames.Image.Png);
                inlineLogo.ContentId = "logo_sedarpa";

                //Generación del QR
                byte[] bytesQr = GenerarOObtenerBytesQr(modelo.IdSolicitudInternacion); // Tu lógica para obtener el QR
                MemoryStream streamQr = new MemoryStream(bytesQr);
                streamQr.Position = 0;
                LinkedResource inlineQr = new LinkedResource(streamQr, MediaTypeNames.Image.Png);
                inlineQr.ContentId = "codigo_qr";

                string htmlBody = $@"
                                    <div style='font-family: Arial, sans-serif; width:900px'>
                                        <img src='cid:logo_sedarpa' style='width: 30%; height: auto;' />
                                        <p>La solicitud con ID <strong>{modelo.IdSolicitudInternacion}</strong> ha sido <strong>{(modelo.StatusSolicitud == 4 ? "ACEPTADA" : "RECHAZADA")}</strong>.</p>
                                        <p>Motivo de rechazo: {modelo.MotivoRechazoNavigation?.DescMotivoRechazo ?? ""}</p>
                                        <p>Observaciones o recomendaciones: {modelo.Observaciones}</p>
                                        <p>Usuario que dió atención al trámite: {modelo.IdUsuarioNavigation.NombreUsuario}</p>
                                        <table>
                                            <thead>
                                                <tr>
                                                    <th style='width:50%;'>Origen</th>
                                                    <th style='width:50%;'>Destino</th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                <tr>
                                                    <td>
                                                        <p><strong>UPP/PSG:</strong> {modelo.Origen}</p>
                                                        <p><strong>Productor:</strong> {datosOrigen.IdProductorNavigation.NombreProductor} {datosOrigen.IdProductorNavigation.ApellidoPaternoProductor} {datosOrigen.IdProductorNavigation.ApellidoMaternoProductor}</p>
                                                        <p><strong>Ubicación:</strong> {datosOrigen.CatalogoLocalidade.NombreLocalidad} en el municipio {datosOrigen.CatalogoLocalidade.ClaveMunicipioNavigation.NombreMunicipio}</td>           
                                                    </td>
                                                    <td>
                                                        <p><strong>UPP/PSG:</strong> {modelo.Destino}</p>
                                                        <p><strong>Productor:</strong> {datosDestino.IdProductorNavigation.NombreProductor} {datosDestino.IdProductorNavigation.ApellidoPaternoProductor} {datosDestino.IdProductorNavigation.ApellidoMaternoProductor}</p>
                                                        <p><strong>Ubicación:</strong> {datosDestino.CatalogoLocalidade.NombreLocalidad} en el municipio {datosDestino.CatalogoLocalidade.ClaveMunicipioNavigation.NombreMunicipio}</p>           
                                                    </td>
                                                </tr>
                                            </tbody>
                                        </table>
                                        <p>Presente el siguiente código QR para su validación:</p>
                                        <div style='margin-top: 20px;'>
                                            <img src='cid:codigo_qr' style='width: 150px; height: 150px;' />
                                        </div>
                                    </div>";

                AlternateView avHtml = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);
                avHtml.LinkedResources.Add(inlineLogo);
                avHtml.LinkedResources.Add(inlineQr);
                mensaje.AlternateViews.Add(avHtml);

                mensaje.IsBodyHtml = true;

                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential(remitente, passwordApp);
                    smtp.EnableSsl = true; // Habilita la seguridad TLS/SSL al igual que en Python
                    await smtp.SendMailAsync(mensaje);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                // Si ocurre un error con el correo, puedes capturarlo aquí para que no rompa la ejecución
                
            }

            return RedirectToAction("movilizacionGanadoPermisos", "Movilizacion");

        }
        public async Task<IActionResult> movilizacionGanadoPermisosMensaje(int id) {

            var registro = await _context.SolicitudInternacions
                           .Include(u => u.IdUsuarioNavigation)
                           .FirstOrDefaultAsync(s=>s.IdSolicitudInternacion == id);

            return View(registro);

        }

        //Obtener la lista de las solicitudes de intenación
        public async Task<IActionResult> SolicitudesInternacion(int? id, string? nombre, DateOnly? fecha, string? certificado, int? status)
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
            if (!string.IsNullOrEmpty(certificado)) {
                query = query.Where(r => r.CertificadoMovilizacion.Contains(certificado));
            }
            if (status.HasValue)
            {
                query = query.Where(r => r.StatusSolicitud == status.Value);
            }

            var resultado = await (from r in query
                                   select new
                                   {
                                       r.IdSolicitudInternacion,
                                       r.FechaSolicitud,
                                       r.NombreSolicitante,
                                       r.TipoGanadoNavigation.DescTipoGanado,
                                       r.MotivoTrasladoNavigation.DescMotivoTraslado,
                                       r.StatusSolicitudNavigation.DescStatusSolicitud,
                                       r.Origen,
                                       r.Destino,
                                       r.CertificadoMovilizacion,
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
                .Include(d=> d.SolicitudInternacionDocumentos)
                .Include(upp => upp.ClaveUppNavigation)
                    .ThenInclude (p => p.IdProductorNavigation)
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
            if (documento.StatusDocumento == true)
            {
                documento.StatusDocumento = false;
            }
            else
            {
                documento.StatusDocumento = true;
            }
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Estatus actualizado correctamente." });
        }

        [HttpGet]
        public async Task<IActionResult> buscarUPP(string id)
        {

            try
            {

                var Upp = await _context.CatalogoProductoresUpps
                    .Select(upp => new
                    {
                        upp.ClaveUpp,
                        nombreProductor = upp.IdProductorNavigation.NombreProductor + " " + upp.IdProductorNavigation.ApellidoPaternoProductor + " " + upp.IdProductorNavigation.ApellidoMaternoProductor,
                        nombrePredio = upp.NombreUpp.ToUpper(),
                        ubicacion = upp.Ubicacion != null ? upp.Ubicacion.ToUpper() : "Sin dato",
                        nombreMunicipio = upp.CatalogoLocalidade.ClaveMunicipioNavigation.NombreMunicipio.ToUpper(),
                        nombreLocalidad = upp.CatalogoLocalidade.NombreLocalidad,
                        nombreEstado = _context.CatalogoEntidadesFederativas
                            .Where(e => e.ClaveEstado == upp.ClaveUpp.Substring(0, 2))
                            .Select(e => e.NombreEstado.ToUpper())
                            .FirstOrDefault(),
                        upp.CatalogoLocalidade.ClaveMunicipioNavigation.TipoZona,
                        tipoEstatus = "SIN DATO"
                    })
                    .Where(c => c.ClaveUpp == id)
                    .FirstOrDefaultAsync();
                if (Upp == null)
                {
                    return NotFound(new { mensaje = "No se encontró la UPP/PSG (" + id + ")" });
                }
                return Json(Upp);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener laboratorios", detalle = ex.Message });
            }

        }

        [HttpGet]
        public async Task<IActionResult> VisorMultiple(string ids)
        {
            if (string.IsNullOrEmpty(ids))
            {
                return BadRequest("No se especificaron documentos para abrir.");
            }

            // 1. Separar la cadena por comas y convertirla a una lista de enteros (o strings según tu BD)
            var listaIds = ids.Split(',')
                              .Select(int.Parse)
                              .ToList();

            // 2. Ir a la base de datos a consultar los documentos que coincidan con esos IDs
            // (Asegúrate de ajustar esto según los nombres de tus tablas y DbSet)
            var documentos = await _context.SolicitudInternacionDocumentos
                .Where(d => listaIds.Contains(d.IdSolicitudInternacionDocumento))
                .ToListAsync();

            if (documentos == null)
            {
                return NotFound("No se encontraron los documentos solicitados.");
            }

            // 3. Enviar esta lista de documentos a la nueva vista Razor
            return View(documentos);
        }

        //**********Fin de api's para el´módulo de movilización de ganbado

    }
}
