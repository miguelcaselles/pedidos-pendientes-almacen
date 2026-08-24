using PedidosPendientes.Core.Entities;

namespace PedidosPendientes.Web.Models;

public class DashboardViewModel
{
    // Contadores principales
    public int TotalPendientes { get; set; }
    public int ParaReclamar { get; set; }
    public int SinRespuesta { get; set; }
    public int ParaLlamar { get; set; }
    public int RecibidosHoy { get; set; }

    // Riesgo de retraso
    /// <summary>% de pedidos pendientes que ya superan su plazo objetivo de entrega.</summary>
    public double PorcentajeFueraPlazo { get; set; }

    /// <summary>% histórico de pedidos recibidos que tardaron más del plazo objetivo (probabilidad de retraso).</summary>
    public double? ProbabilidadRetrasoHistorica { get; set; }

    public int MuestraHistorica { get; set; }

    // Estado de cargas
    public UploadLog? UltimaPedidos { get; set; }
    public UploadLog? UltimaMd04 { get; set; }
    public UploadLog? UltimaMd04Na { get; set; }
    public UploadLog? UltimaAbc { get; set; }
    public UploadLog? UltimaCecos { get; set; }
    public bool CargaPedidosObsoleta { get; set; }
    public int DiasAvisoCarga { get; set; }

    // Semáforo MD04
    public int Md04Rojos { get; set; }
    public int Md04RojosSinPedido { get; set; }
    public DateTimeOffset? Md04CargadoAt { get; set; }

    // Incidencias por mes (últimos 6 meses)
    public IReadOnlyList<IncidenciaMes> IncidenciasPorMes { get; set; } = [];

    // Pedidos con alerta más urgentes (para llamar / sin respuesta), top N
    public IReadOnlyList<OrderRow> AlertasTop { get; set; } = [];

    // Proveedores con acuerdo marco incumpliendo plazos
    public IReadOnlyList<ProveedorIncumplimiento> AcuerdoMarcoIncumplimientos { get; set; } = [];
}

public class IncidenciaMes
{
    public string Etiqueta { get; set; } = string.Empty;  // "feb 26"
    public int EnFalta { get; set; }
    public int RespuestasProblema { get; set; }
    public int Total => EnFalta + RespuestasProblema;
}

public class ProveedorIncumplimiento
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int PlazoDias { get; set; }
    public int PendientesFueraPlazo { get; set; }
    public int PendientesTotales { get; set; }
}
