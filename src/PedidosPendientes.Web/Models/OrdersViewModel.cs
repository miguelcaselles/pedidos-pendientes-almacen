namespace PedidosPendientes.Web.Models;

public class OrdersViewModel
{
    public IReadOnlyList<OrderRow> Rows { get; set; } = [];
    public string? Search { get; set; }

    /// <summary>Pestaña activa: pendientes | reclamar | alertas | reclamados | recibidos | anulados | enfalta.</summary>
    public string Estado { get; set; } = "pendientes";

    // Contadores de pestañas
    public int TotalPendientes { get; set; }
    public int ParaReclamar { get; set; }
    public int ConAlerta { get; set; }
    public int Reclamados { get; set; }
    public int EnFalta { get; set; }
    public int Recibidos { get; set; }
    public int Anulados { get; set; }

    public DateTimeOffset? UltimaCarga { get; set; }
    public bool CargaObsoleta { get; set; }
    public int DiasAvisoCarga { get; set; }

    public bool Truncado { get; set; }
}
