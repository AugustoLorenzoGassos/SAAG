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
    public class PadronesController : Controller
    {

        private readonly SedarpaContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public PadronesController(SedarpaContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

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
        //Regresa la información de un beneficiario en formato Json
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
        public async Task<IActionResult> padronesBeneficiariosAcuaculturaDescarga([FromQuery] FiltrosDescargaDto filtros)
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
        //Regresa la información de un beneficiario en formato Json
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

        //**********API's para el módulo de estadisticas
        //Regresa los datos de la gráfica en formato JSON para que puedan ser consumidos por el frontend
        [HttpGet]
        public async Task<IActionResult> PadronesBeneficiariosEstadisticas([FromQuery] string[] proyectos, int municipio)
        {
            try
            {

                // Construir la URL con parámetros de consulta (?proyectos=A&proyectos=B&municipios=X)
                var queryParams = new List<string>();
                var client = _httpClientFactory.CreateClient("PythonApi");

                if (proyectos != null)
                    queryParams.AddRange(proyectos.Select(p => $"proyectos={Uri.EscapeDataString(p)}"));
                if (municipio > 0)
                    queryParams.Add($"municipio={municipio}");

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
