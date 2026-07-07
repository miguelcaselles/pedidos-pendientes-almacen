namespace PedidosPendientes.Core.Entities;

/// <summary>
/// Proveedor / centro suministrador. Cargado desde el listado de proveedores (L025)
/// o creado automáticamente al importar pedidos. El email es imprescindible para reclamar.
/// </summary>
public class Proveedor
{
    public int Id { get; set; }

    /// <summary>Código de proveedor SAP. Único.</summary>
    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Fax { get; set; }

    /// <summary>NIF / CIF (Nº ident. fiscal).</summary>
    public string? NifCif { get; set; }

    public string? Direccion { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Poblacion { get; set; }
    public string? Centro { get; set; }

    public bool Activo { get; set; } = true;

    // --- Acuerdo marco / cumplimiento ---

    /// <summary>Proveedor sujeto a acuerdo marco (con plazo de entrega contractual).</summary>
    public bool AcuerdoMarco { get; set; }

    /// <summary>Plazo de entrega contractual en días hábiles (null = plazo por defecto de configuración).</summary>
    public int? PlazoEntregaDias { get; set; }

    /// <summary>Anotaciones sobre incumplimientos / penalizaciones aplicadas.</summary>
    public string? NotasPenalizacion { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
