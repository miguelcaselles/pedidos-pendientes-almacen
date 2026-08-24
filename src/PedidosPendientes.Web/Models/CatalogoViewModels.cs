using PedidosPendientes.Core.Entities;

namespace PedidosPendientes.Web.Models;

public class CatalogoIndexViewModel
{
    public string? Search { get; set; }

    public IReadOnlyList<CatalogoResultado> Rows { get; set; } = [];

    public int FichasCreadas { get; set; }
    public bool Truncado { get; set; }
}

/// <summary>Resultado del buscador del catálogo (haya ficha creada o no).</summary>
public class CatalogoResultado
{
    public string Material { get; set; } = string.Empty;
    public string? Denominacion { get; set; }
    public string? Clase { get; set; }
    public bool TieneFicha { get; set; }
    public int NumAdjuntos { get; set; }
    public decimal? PresupuestoUnitario { get; set; }
    public string? ProveedorHabitual { get; set; }
}

public class CatalogoFichaViewModel
{
    public required ProductoFicha Ficha { get; set; }

    public bool EsNueva { get; set; }

    public string? Clase { get; set; }

    /// <summary>Descripción vista en pedidos/MD04 (sugerencia si la ficha no tiene denominación).</summary>
    public string? DenominacionSugerida { get; set; }

    public string? ProveedorHabitual { get; set; }

    public int PedidosHistoricos { get; set; }
    public int PendientesActuales { get; set; }
}
