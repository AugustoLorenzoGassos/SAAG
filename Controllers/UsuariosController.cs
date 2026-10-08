using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Seguimiento.Models;
using Seguimiento.Models.DTOs;

namespace Seguimiento.Controllers
{
    public class UsuariosController : Controller
    {

        private readonly SedarpaContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public UsuariosController(SedarpaContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult usuariosInicio()
        {
            return View();
        }

    }
}
