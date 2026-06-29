namespace PedidosPendientes.Core.Dtos;

/// <summary>Resultado resumido de una importación de Excel.</summary>
public class UploadResult
{
    public bool Success { get; set; }
    public int TotalParsed { get; set; }
    public int Insertados { get; set; }
    public int Actualizados { get; set; }
    public string Message { get; set; } = string.Empty;
}
