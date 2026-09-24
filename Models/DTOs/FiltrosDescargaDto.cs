namespace Seguimiento.Models.DTOs
{
    public class FiltrosDescargaDto
    {
        public short? AnioReporte { get; set; }
        public string? Nombre { get; set; }
        public short? Region { get; set; }
        public string? Municipio { get; set; }
        public string? Localidad { get; set; }
    }
}
