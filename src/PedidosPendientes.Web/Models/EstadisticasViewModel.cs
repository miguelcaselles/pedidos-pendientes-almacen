namespace PedidosPendientes.Web.Models;

public class EstadisticasViewModel
{
    public IReadOnlyList<EstadisticaMaterial> Rows { get; set; } = [];

    public string? Search { get; set; }

    /// <summary>Orden activo: "frecuencia" | "reciente" | "ciclo" | "pedidos".</summary>
    public string Orden { get; set; } = "pedidos";

    /// <summary>Filtro activo: "" (todos) | "vencidos" (solo «toca pedir»).</summary>
    public string Filtro { get; set; } = "";

    public int TotalMateriales { get; set; }
    public int ConPeriodicidad { get; set; }
    public int TocaPedir { get; set; }
    public int ConPendientes { get; set; }

    /// <summary>Documentos de compra distintos por mes (últimos 12 meses con datos).</summary>
    public IReadOnlyList<(DateOnly Mes, int Pedidos)> PedidosPorMes { get; set; } = [];

    /// <summary>Materiales por tramo de intervalo medio entre pedidos.</summary>
    public IReadOnlyList<(string Etiqueta, int Materiales)> DistribucionIntervalo { get; set; } = [];

    public bool Truncado { get; set; }
}

/// <summary>Periodicidad de pedido de un material, calculada sobre el histórico de líneas.</summary>
public class EstadisticaMaterial
{
    public string Material { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Clase { get; set; }

    /// <summary>Nº de pedidos (documentos de compra distintos) en el histórico.</summary>
    public int NumPedidos { get; set; }

    public DateOnly PrimerPedido { get; set; }
    public DateOnly UltimoPedido { get; set; }

    /// <summary>Días naturales medios entre pedidos (null si solo hay uno).</summary>
    public double? IntervaloMedioDias { get; set; }

    /// <summary>Días desde el último pedido.</summary>
    public int DiasDesdeUltimo { get; set; }

    /// <summary>Fecha estimada del próximo pedido (último + intervalo medio).</summary>
    public DateOnly? ProximoEstimado { get; set; }

    /// <summary>Cantidad media pedida por línea.</summary>
    public decimal? CantidadMedia { get; set; }

    /// <summary>Proveedor más habitual del material.</summary>
    public string? ProveedorHabitual { get; set; }

    /// <summary>Líneas actualmente pendientes de este material.</summary>
    public int PendientesActuales { get; set; }

    /// <summary>El próximo pedido estimado ya está vencido (y no hay líneas pendientes).</summary>
    public bool ProximoVencido => ProximoEstimado is not null
        && ProximoEstimado < DateOnly.FromDateTime(DateTime.Today)
        && PendientesActuales == 0;

    /// <summary>
    /// Porcentaje del ciclo de pedido consumido: días desde el último pedido sobre
    /// el intervalo medio (100 = tocaría pedir hoy; &gt;100 = vencido). Null sin intervalo.
    /// </summary>
    public double? CicloPct => IntervaloMedioDias is double d && d > 0
        ? Math.Round(DiasDesdeUltimo * 100.0 / d)
        : null;
}
