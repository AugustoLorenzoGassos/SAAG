using System;
using System.Collections.Generic;

namespace Seguimiento.Models;

public partial class CatalogoTipoApoyo
{
    public short IdTipoApoyo { get; set; }

    public string NombreTipoApoyo { get; set; } = null!;

    public virtual ICollection<PadronBeneficiario> PadronBeneficiarios { get; set; } = new List<PadronBeneficiario>();
}
