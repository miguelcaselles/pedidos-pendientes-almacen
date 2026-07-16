using PedidosPendientes.Core.Entities;

namespace PedidosPendientes.Web.Models;

/// <summary>Vista global de auditoría: KPIs agregados + una fila por proveedor.</summary>
public class AuditoriaGlobalViewModel
{
    public string? Search { get; set; }
    public bool SoloAcuerdoMarco { get; set; }
    public string Orden { get; set; } = "cumplimiento";
    public DateTime GeneradoEl { get; set; }

    // --- KPIs agregados ---
    public int TotalProveedores { get; set; }
    public int ProveedoresAcuerdoMarco { get; set; }
    public int ProveedoresIncumplen { get; set; }

    public int TotalLineas { get; set; }
    public int LineasRecibidas { get; set; }
    public int LineasPendientes { get; set; }
    public int LineasAnuladas { get; set; }
    public int LineasEnFalta { get; set; }
    public int PendientesFueraPlazo { get; set; }

    public int EntregadasATiempo { get; set; }
    public int EntregadasTarde { get; set; }
    public double? PorcentajeEntregaATiempo { get; set; }
    public double? TiempoMedioEntregaGlobal { get; set; }

    public int LineasReclamadas { get; set; }
    public int TotalReclamaciones { get; set; }
    public int RespuestasProveedor { get; set; }
    public double? TasaRespuestaGlobal { get; set; }

    public IReadOnlyList<AuditoriaProveedorRow> Filas { get; set; } = [];

    /// <summary>Reparto de proveedores por calificación (para la barra de distribución).</summary>
    public IReadOnlyList<(string Calificacion, int Count)> DistribucionCalificacion { get; set; } = [];
}

/// <summary>Métricas de cumplimiento de un proveedor (fila del ranking global y base del detalle).</summary>
public class AuditoriaProveedorRow
{
    public required Proveedor Proveedor { get; init; }

    /// <summary>Plazo de entrega aplicable en días hábiles (propio o por defecto).</summary>
    public int PlazoAplicable { get; init; }

    public int TotalLineas { get; init; }
    public int Pendientes { get; init; }
    public int Recibidas { get; init; }
    public int Anuladas { get; init; }
    public int EnFalta { get; init; }
    public int PendientesFueraPlazo { get; init; }

    public int EntregadasATiempo { get; init; }
    public int EntregadasTarde { get; init; }
    public double? PorcentajeATiempo { get; init; }
    public double? TiempoMedioEntrega { get; init; }
    public double? TiempoMedianoEntrega { get; init; }
    public int? TiempoMaxEntrega { get; init; }

    public int LineasReclamadas { get; init; }
    public int TotalReclamaciones { get; init; }
    public int Rereclamadas { get; init; }
    public double? TiempoMedioHastaReclamar { get; init; }
    public int EmailsEnviados { get; init; }

    public int RespuestasRecibidas { get; init; }
    public double? TasaRespuesta { get; init; }

    /// <summary>Puntuación de cumplimiento 0-100 (mayor = mejor).</summary>
    public double Puntuacion { get; init; }
    public string Calificacion { get; init; } = "Sin datos";

    public bool Incumple => Proveedor.AcuerdoMarco && (PendientesFueraPlazo > 0 || EntregadasTarde > 0);

    // Acumuladores internos para el agregado global (no se muestran directamente).
    public double SumaDiasEntrega { get; init; }
}

/// <summary>Detalle riguroso de auditoría de un proveedor.</summary>
public class AuditoriaProveedorDetalle
{
    public required AuditoriaProveedorRow Resumen { get; init; }
    public DateTime GeneradoEl { get; init; }

    public IReadOnlyList<MesAuditoria> Evolucion { get; init; } = [];
    public IReadOnlyList<ClaseAuditoria> PorClase { get; init; } = [];
    public IReadOnlyList<UbicacionAuditoria> PorCentro { get; init; } = [];
    public IReadOnlyList<(string Estado, int Count)> RespuestasPorEstado { get; init; } = [];
    public IReadOnlyList<LineaAuditoria> Lineas { get; init; } = [];
    public IReadOnlyList<ProviderResponse> Respuestas { get; init; } = [];
}

public class MesAuditoria
{
    public required string Etiqueta { get; init; }
    public int Pedidos { get; init; }
    public int Recibidos { get; init; }
    public int Reclamaciones { get; init; }
    public int EnFalta { get; init; }
    public double? TiempoMedioEntrega { get; init; }
}

public class ClaseAuditoria
{
    public required string Clase { get; init; }
    public int Lineas { get; init; }
    public int Pendientes { get; init; }
    public int FueraPlazo { get; init; }
}

public class UbicacionAuditoria
{
    public required string Centro { get; init; }
    public int Lineas { get; init; }
    public int FueraPlazo { get; init; }
}

/// <summary>Una línea de pedido con los cálculos de auditoría (para tabla detalle y Excel).</summary>
public class LineaAuditoria
{
    public required Order Order { get; init; }
    public string? Clase { get; init; }
    public string Estado { get; init; } = "";        // Recibido | Pendiente | En falta | Anulado
    public int DiasHabiles { get; init; }            // desde fecha documento a hoy (o a recepción)
    public int? DiasEntrega { get; init; }           // días hábiles hasta la recepción (si recibido)
    public bool FueraPlazo { get; init; }
    public bool Reclamado { get; init; }
    public int ReclamadoCount { get; init; }
    public bool TieneRespuesta { get; init; }
}
