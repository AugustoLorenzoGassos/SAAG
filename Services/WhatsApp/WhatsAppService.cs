using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class WhatsAppService
{
    public async Task EnviarMensajePrueba()
    {
        // 1. Tus credenciales de Meta
        string phoneNumberId = "1367842583083658"; // Tu Identificador de número de teléfono
        string accessToken = "EAAWbHIMZCqXsBSja0SNxijx5iDJ9p7QnCwKBZAs8EUFNU6jobjZC4SRI9L1NE4pGi5t74sIsDb8p4G8bVWfLBFhWIFX155HvaO99ybOweu9ojCEgLCQLZB691qCADW4utCNUO7EXIbv9zYLS1I9CtjffkrWZBZAYALNf3GVZBdxTlRsIT7zTeTc9qLrGLX842Po63wdeZCKEpEEur5UtLyoX2dQCJPsKOJriGbmjjvZCOzHdZCtrmhiXtLo8DZAwSX9t2QGaUkRcna6X5lXS2y9aDwZD";

        // La URL de la API de Graph (versión v20.0 o superior)
        string url = $"https://graph.facebook.com/v20.0/{phoneNumberId}/messages";

        // 2. El cuerpo del mensaje en formato JSON
        // Nota: El número de destino debe incluir el código de país (ej. 52 para México) sin el signo +
        /*string jsonPayload = @"{
            ""messaging_product"": ""whatsapp"",
            ""to"": ""525511282636"", // Reemplaza con tu número verificado
            ""type"": ""text"",
            ""text"": {
                ""body"": ""¡Hola! Este es un mensaje de prueba automático desde el sistema SAAG en ASP.NET.""
            }
        }";*/

        var payloadData = new
        {
            messaging_product = "whatsapp",
            to = "525511281636", // Reemplaza o pasa como variable el número verificado
            type = "text",
            text = new
            {
                body = "¡Hola! Este es un mensaje de prueba automático desde el sistema SAAG en ASP.NET."
            }
        };
        string jsonPayload = JsonSerializer.Serialize(payloadData);
        //var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // 3. Configuración y envío de la petición HTTP
        using (var client = new HttpClient())
        {
            // Se agrega el token de seguridad en la cabecera
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("¡Mensaje enviado con éxito!");
                    Console.WriteLine(responseString);
                }
                else
                {
                    Console.WriteLine($"Error al enviar: {response.StatusCode}");
                    Console.WriteLine(responseString);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrió un error: {ex.Message}");
            }
        }
    }
}
