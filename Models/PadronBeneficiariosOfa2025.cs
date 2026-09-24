using System;
using System.Collections.Generic;

namespace Seguimiento.Models;

public partial class PadronBeneficiariosOfa2025
{
    public short? No { get; set; }

    public string? Folio { get; set; }

    public string? NombreCompleto { get; set; }

    public string? Municipio { get; set; }

    public string? Localidad { get; set; }

    public int? IdApoyo { get; set; }

    public string? DescripciónDelApoyoRecibido { get; set; }

    public string? UMedida { get; set; }

    public string? Cantidad { get; set; }

    public decimal? PUnitario { get; set; }

    public decimal? Importe { get; set; }

    public string? Dictamen { get; set; }

    public string? Region { get; set; }

    public string? FolioActa { get; set; }
}
