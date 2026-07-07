namespace PedidosPendientes.Core.Entities;

/// <summary>
/// Parametrización de criticidad por ubicación: un almacén o un centro de coste
/// marcado aquí eleva la prioridad de sus pedidos (p. ej. UCI, quirófano, urgencias).
/// </summary>
public class CriticidadUbicacion
{
    public int Id { get; set; }

    /// <summary>Ámbito del código: "almacen" | "centroCoste".</summary>
    public string Tipo { get; set; } = "almacen";

    /// <summary>Código del almacén o del centro de coste tal y como viene en el Excel.</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Descripción legible (p. ej. "UCI", "Quirófano 2ª planta").</summary>
    public string? Descripcion { get; set; }

    /// <summary>Nivel: "critico" (máxima prioridad) | "alto".</summary>
    public string Nivel { get; set; } = "critico";

    public DateTimeOffset CreatedAt { get; set; }
}
