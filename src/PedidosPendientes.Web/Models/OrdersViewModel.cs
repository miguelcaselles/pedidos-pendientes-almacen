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

    // --- Filtros adicionales (fechas del pedido, proveedor, almacén, clase) ---

    /// <summary>Fecha de pedido desde, en formato yyyy-MM-dd (input type=date).</summary>
    public string? Desde { get; set; }

    /// <summary>Fecha de pedido hasta, en formato yyyy-MM-dd.</summary>
    public string? Hasta { get; set; }

    public string? Proveedor { get; set; }
    public string? Almacen { get; set; }
    public string? Clase { get; set; }

    public IReadOnlyList<string> ProveedoresDisponibles { get; set; } = [];
    public IReadOnlyList<string> AlmacenesDisponibles { get; set; } = [];

    public bool HayFiltros => Desde is not null || Hasta is not null
        || Proveedor is not null || Almacen is not null || Clase is not null;

    /// <summary>Parámetros de ruta comunes para que pestañas y enlaces conserven los filtros.</summary>
    public Dictionary<string, string> RutaFiltros(string? estado = null)
    {
        var ruta = new Dictionary<string, string> { ["estado"] = estado ?? Estado };
        void Add(string clave, string? valor) { if (!string.IsNullOrEmpty(valor)) ruta[clave] = valor; }
        Add("search", Search);
        Add("desde", Desde);
        Add("hasta", Hasta);
        Add("proveedor", Proveedor);
        Add("almacen", Almacen);
        Add("clase", Clase);
        return ruta;
    }
}
