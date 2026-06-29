namespace PedidosPendientes.Core.Entities;

/// <summary>
/// Configuración clave/valor de la aplicación (SMTP, plantillas de email,
/// destinatarios de resúmenes, flags de automatización). Evita tocar archivos
/// para cambiar la configuración operativa.
/// </summary>
public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
