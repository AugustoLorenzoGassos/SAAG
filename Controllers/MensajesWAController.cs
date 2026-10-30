using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Seguimiento.Controllers
{
    public class MensajesWAController : Controller
    {

        [HttpPost]
        public async Task<IActionResult> NotificarAltaTramite()
        {
            // Instanciamos el servicio que armamos en el paso anterior
            var whatsappService = new WhatsAppService();

            // Ejecutamos el envío del mensaje
            await whatsappService.EnviarMensajePrueba();

            // Devolvemos una respuesta exitosa en formato JSON al frontend
            return Json(new { success = true, message = "Notificación de WhatsApp enviada con éxito al productor." });
        }

    }
}
