namespace PedidosPendientes.Core.Dtos;

/// <summary>Fila parseada del listado de clasificación A/B/C por código NEXUS.</summary>
public class ParsedClasificacion
{
    public string Material { get; set; } = string.Empty;

    /// <summary>"A" | "B" | "C".</summary>
    public string Clase { get; set; } = "C";
}
