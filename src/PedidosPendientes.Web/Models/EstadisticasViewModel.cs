namespace PedidosPendientes.Web.Models;

public class EstadisticasViewModel
{
    public IReadOnlyList<EstadisticaMaterial> Rows { get; set; } = [];

    public string? Search { get; set; }

    /// <summary>Orden activo: "frecuencia" | "reciente" | "pedidos".</summary>
    public string Orden { get; set; } = "pedidos";

    public int TotalMateriales { get; set; }
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
}
