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

using Seguimiento.Models;
using Seguimiento.Models.DTOs;


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
        //Catálogo de productores
        //Catalogo de ñproductores con UPP asociadas
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

    }
}
