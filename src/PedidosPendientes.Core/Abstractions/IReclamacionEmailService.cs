using PedidosPendientes.Core.Entities;

namespace PedidosPendientes.Core.Abstractions;

/// <summary>Resultado del intento de envío de una reclamación por correo.</summary>
public class ReclamacionEmailResultado
{
    /// <summary>true si el correo salió (o salió al buzón de pruebas).</summary>
    public bool Enviado { get; set; }

    /// <summary>Motivo cuando no se envía: "deshabilitado" | "sin-email" | "error".</summary>
    public string? Motivo { get; set; }

    public string Detalle { get; set; } = string.Empty;
}

/// <summary>
/// Envío de la reclamación de un pedido al proveedor por correo electrónico.
/// La implementación real usa el SMTP institucional (sección "Smtp" de la
/// configuración); si está deshabilitado, el pedido se marca reclamado igualmente
/// y el envío queda pendiente de hacerse por otro medio.
/// </summary>
public interface IReclamacionEmailService
{
    /// <summary>El envío por SMTP está configurado y activado.</summary>
    bool Habilitado { get; }

    /// <summary>
    /// Envía la reclamación de la línea indicada (el correo incluye todas las
    /// líneas pendientes del mismo documento) y registra el intento en EmailLogs.
    /// </summary>
    Task<ReclamacionEmailResultado> EnviarAsync(Order linea, string tipo, CancellationToken ct = default);
}
