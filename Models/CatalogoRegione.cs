using System;
using System.Collections.Generic;

namespace Seguimiento.Models;

public partial class CatalogoRegione
{
    public short IdRegion { get; set; }

    public string? NombreRegion { get; set; }

    public virtual ICollection<CatalogoMunicipio> CatalogoMunicipios { get; set; } = new List<CatalogoMunicipio>();
}
