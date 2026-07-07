using PedidosPendientes.Core;
using PedidosPendientes.Core.Entities;

namespace PedidosPendientes.Web.Models;

/// <summary>
/// Línea de pedido enriquecida para la vista: clase A/B/C, criticidad de la
/// ubicación, alerta de reclamación y riesgo de retraso.
/// </summary>
public class OrderRow
{
    public required Order Order { get; init; }

    /// <summary>Clase A/B/C del material (null = sin clasificar, se trata como C).</summary>
    public string? Clase { get; init; }

    /// <summary>Nivel de criticidad de la ubicación: "critico" | "alto" | null.</summary>
    public string? CriticidadNivel { get; init; }

    /// <summary>Descripción de la ubicación crítica (p. ej. "UCI").</summary>
    public string? CriticidadDescripcion { get; init; }

    public AlertaReclamacion Alerta { get; init; }

    /// <summary>Días hábiles transcurridos / plazo objetivo. &gt;= 1 = fuera de plazo.</summary>
    public double Riesgo { get; init; }

    /// <summary>Días hábiles desde la fecha del documento.</summary>
    public int DiasHabiles { get; init; }

    /// <summary>El proveedor tiene acuerdo marco.</summary>
    public bool AcuerdoMarco { get; init; }

    public bool FueraPlazo => Riesgo >= 1;
}
