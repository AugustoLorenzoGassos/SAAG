using System;
using System.Collections.Generic;

namespace Seguimiento.Models;

public partial class CatalogoProyecto
{
    public string IdProyecto { get; set; } = null!;

    public string NombreProyecto { get; set; } = null!;

    public string? NombreProyectoCorto { get; set; }

    public virtual ICollection<PadronBeneficiario> PadronBeneficiarios { get; set; } = new List<PadronBeneficiario>();
}
