using PedidosPendientes.Core.Entities;

namespace PedidosPendientes.Web.Models;

public class CargaViewModel
{
    public UploadLog? UltimaPedidos { get; set; }
    public UploadLog? UltimaMd04 { get; set; }
    public UploadLog? UltimaMd04Na { get; set; }
    public UploadLog? UltimaAbc { get; set; }
    public UploadLog? UltimaCecos { get; set; }

    public IReadOnlyList<UploadLog> Historial { get; set; } = [];
}
