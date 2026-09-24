using System;
using System.Collections.Generic;

namespace Seguimiento.Models;

public partial class ConjuntoDeDatosIter00csv20
{
    public byte Entidad { get; set; }

    public string NomEnt { get; set; } = null!;

    public byte Mun { get; set; }

    public string NomMun { get; set; } = null!;

    public short Loc { get; set; }

    public string NomLoc { get; set; } = null!;

    public string Longitud { get; set; } = null!;

    public string Latitud { get; set; } = null!;

    public string? Column9 { get; set; }

    public string? Column10 { get; set; }

    public string? Column11 { get; set; }

    public string? Column12 { get; set; }
}
