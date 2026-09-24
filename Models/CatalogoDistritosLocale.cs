using System;
using System.Collections.Generic;

namespace Seguimiento.Models;

public partial class CatalogoDistritosLocale
{
    public short IdDistritoElectoral { get; set; }

    public string? NombreDistritoElectoral { get; set; }

    public virtual ICollection<CatalogoMunicipio> CatalogoMunicipios { get; set; } = new List<CatalogoMunicipio>();
}
