namespace Seguimiento.Models.DTOs
{
    public class EstadisticasProgramaDto
    {
        public List<string> Labels { get; set; } = new();
        public List<int> Values { get; set; } = new();
    }
}
