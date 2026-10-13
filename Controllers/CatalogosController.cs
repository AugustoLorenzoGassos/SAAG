using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Linq;

using Seguimiento.Models;
using Seguimiento.Models.DTOs;

namespace Seguimiento.Controllers
{
    public class CatalogosController : Controller
    {

        private readonly SedarpaContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        // Inyección de dependencias del DbContext
        public CatalogosController(SedarpaContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

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

    }
}

