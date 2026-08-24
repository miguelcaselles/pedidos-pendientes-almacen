namespace PedidosPendientes.Core.Entities;

/// <summary>
/// Relación entre almacenes de planta y centros de coste (listado "Relación
/// CECOs - Almacenes" de Suministros). Sirve para poner denominación legible a
/// los códigos y para parametrizar la criticidad de ubicaciones (UCI, quirófano...).
/// Cada carga sustituye por completo la anterior.
/// </summary>
public class AlmacenCeco
{
    public int Id { get; set; }

    /// <summary>Centro SAP (p. ej. "L025").</summary>
    public string Centro { get; set; } = string.Empty;

    /// <summary>Código de almacén (p. ej. "AL2A").</summary>
    public string Almacen { get; set; } = string.Empty;

    /// <summary>Denominación del almacén (p. ej. "Almacen H2A").</summary>
    public string? DenominacionAlmacen { get; set; }

    /// <summary>Código del centro de coste asociado (p. ej. "27UE2AXH88").</summary>
    public string? CentroCoste { get; set; }

    /// <summary>Denominación corta del centro de coste (p. ej. "U DE HOSPITALIZ 2A").</summary>
    public string? DenominacionCentroCoste { get; set; }

    /// <summary>Descripción larga (p. ej. "UNIDAD DE HOSPITALIZACIÓN 2A").</summary>
    public string? Descripcion { get; set; }

    /// <summary>Momento de la carga a la que pertenece esta fila.</summary>
    public DateTimeOffset CargadoAt { get; set; }
}
