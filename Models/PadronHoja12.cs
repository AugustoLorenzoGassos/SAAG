using System;
using System.Collections.Generic;

namespace Seguimiento.Models;

public partial class PadronHoja12
{
    public short No { get; set; }

    public string Nombre { get; set; } = null!;

    public string Proyecto { get; set; } = null!;

    public string Folio { get; set; } = null!;

    public string TipoDeApoyo { get; set; } = null!;

    public string Municipio { get; set; } = null!;

    public string Localidad { get; set; } = null!;

    public string? Teléfono { get; set; }
}
