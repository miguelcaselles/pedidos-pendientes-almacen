namespace PedidosPendientes.Core.Entities;

/// <summary>
/// Registro de cada intento de envío de correo de reclamación (trazabilidad).
/// </summary>
public class EmailLog
{
    public int Id { get; set; }

    public int? ProveedorId { get; set; }

    public string DocumentoCompras { get; set; } = string.Empty;

    public string ProveedorEmail { get; set; } = string.Empty;

    /// <summary>"enviado" | "error".</summary>
    public string Estado { get; set; } = "enviado";

    public string? ErrorDetalle { get; set; }

    /// <summary>"automatico" | "manual".</summary>
    public string Tipo { get; set; } = "automatico";

    public DateTimeOffset SentAt { get; set; }
}
