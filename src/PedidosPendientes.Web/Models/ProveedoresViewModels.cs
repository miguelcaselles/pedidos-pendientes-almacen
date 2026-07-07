using PedidosPendientes.Core.Entities;

namespace PedidosPendientes.Web.Models;

public class ProveedoresViewModel
{
    public IReadOnlyList<ProveedorRow> Rows { get; set; } = [];
    public string? Search { get; set; }

    /// <summary>true =&gt; solo proveedores con acuerdo marco.</summary>
    public bool SoloAcuerdoMarco { get; set; }
}

public class ProveedorRow
{
    public required Proveedor Proveedor { get; init; }

    public int PendientesTotales { get; init; }
    public int PendientesFueraPlazo { get; init; }

    /// <summary>Plazo aplicable en días hábiles (propio o por defecto).</summary>
    public int PlazoAplicable { get; init; }

    /// <summary>Incumple si tiene acuerdo marco y pedidos pendientes fuera de plazo.</summary>
    public bool Incumple => Proveedor.AcuerdoMarco && PendientesFueraPlazo > 0;
}

public class ProveedorEditViewModel
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public bool AcuerdoMarco { get; set; }
    public int? PlazoEntregaDias { get; set; }
    public string? NotasPenalizacion { get; set; }
    public bool Activo { get; set; } = true;
}
