namespace PedidosPendientes.Core.Entities;

/// <summary>
/// Registro de cada carga de fichero Excel (pedidos, MD04, clasificación ABC...).
/// Permite mostrar en la app cuándo se subió el último archivo y si hubo errores.
/// </summary>
public class UploadLog
{
    public int Id { get; set; }

    /// <summary>Tipo de carga: "pedidos" | "md04" | "abc".</summary>
    public string Tipo { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public bool Success { get; set; }

    public int Filas { get; set; }
    public int Insertados { get; set; }
    public int Actualizados { get; set; }

    public string? Message { get; set; }

    public DateTimeOffset At { get; set; }
}
