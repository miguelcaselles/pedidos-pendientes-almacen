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

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
