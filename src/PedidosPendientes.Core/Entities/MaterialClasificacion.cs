namespace PedidosPendientes.Core.Entities;

/// <summary>
/// Clasificación A/B/C de un material (por código NEXUS) que parametriza la
/// urgencia de reclamación: la clase A se reclama antes que la B, y esta antes
/// que la C. Se carga desde el listado que facilita Suministros.
/// </summary>
public class MaterialClasificacion
{
    public int Id { get; set; }

    /// <summary>Código de material NEXUS/SAP. Único.</summary>
    public string Material { get; set; } = string.Empty;

    /// <summary>Clase de urgencia: "A" | "B" | "C".</summary>
    public string Clase { get; set; } = "C";

    public DateTimeOffset UpdatedAt { get; set; }
}
