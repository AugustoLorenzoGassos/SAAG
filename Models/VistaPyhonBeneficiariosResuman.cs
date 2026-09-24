using System;
using System.Collections.Generic;

namespace Seguimiento.Models;

public partial class VistaPyhonBeneficiariosResuman
{
    public string IdProyecto { get; set; } = null!;

    public string? NombreProyectoCorto { get; set; }

    public int? TotalBeneficiarios { get; set; }
}
