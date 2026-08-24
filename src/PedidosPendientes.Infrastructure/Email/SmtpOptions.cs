namespace PedidosPendientes.Infrastructure.Email;

/// <summary>
/// Configuración del SMTP institucional (sección "Smtp" de appsettings /
/// variables de entorno). Las credenciales NUNCA se guardan en el repositorio:
/// en el hospital se configuran como variables de entorno o en el secret store
/// del servidor (p. ej. Smtp__Password).
/// </summary>
public class SmtpOptions
{
    public const string Section = "Smtp";

    /// <summary>Activa el envío real de correos. Con false, la app funciona igual pero no envía.</summary>
    public bool Enabled { get; set; }

    /// <summary>Servidor SMTP corporativo (p. ej. smtp.salud.madrid.org).</summary>
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    /// <summary>STARTTLS (recomendado con el puerto 587).</summary>
    public bool UseStartTls { get; set; } = true;

    /// <summary>Usuario del buzón institucional (vacío si el relay no requiere autenticación).</summary>
    public string? User { get; set; }

    public string? Password { get; set; }

    /// <summary>Remitente (buzón institucional de Suministros).</summary>
    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "Servicio de Suministros · Hospital Universitario de Fuenlabrada";

    /// <summary>Dirección de respuesta si difiere del remitente.</summary>
    public string? ReplyTo { get; set; }

    /// <summary>Copia oculta de control (buzón propio para trazabilidad).</summary>
    public string? Bcc { get; set; }

    /// <summary>
    /// Modo pruebas: todos los correos se desvían a DestinatarioPruebas en lugar
    /// del proveedor. Imprescindible en los entornos de pruebas del hospital.
    /// </summary>
    public bool ModoPruebas { get; set; } = true;

    public string? DestinatarioPruebas { get; set; }
}
