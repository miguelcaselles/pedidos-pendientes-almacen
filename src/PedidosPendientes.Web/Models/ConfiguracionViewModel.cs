using PedidosPendientes.Core;
using PedidosPendientes.Core.Entities;

namespace PedidosPendientes.Web.Models;

public class ConfiguracionViewModel
{
    public ReclamacionParametros Parametros { get; set; } = new();

    public IReadOnlyList<CriticidadUbicacion> Criticidades { get; set; } = [];

    /// <summary>Relación CECOs - Almacenes cargada (para dar nombre y marcar criticidad).</summary>
    public IReadOnlyList<AlmacenCeco> AlmacenCecos { get; set; } = [];

    public int MaterialesClasificados { get; set; }
    public int ClaseA { get; set; }
    public int ClaseB { get; set; }
    public int ClaseC { get; set; }

    // Estado del correo institucional (solo lectura; se configura en el servidor).
    public bool SmtpHabilitado { get; set; }
    public bool SmtpModoPruebas { get; set; }
    public string? SmtpHost { get; set; }
    public string? SmtpFrom { get; set; }
    public int EmailsEnviados30d { get; set; }
    public int EmailsError30d { get; set; }
}
