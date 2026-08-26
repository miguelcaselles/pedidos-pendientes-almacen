using System.Text;
using PedidosPendientes.Core.Entities;

namespace PedidosPendientes.Web.Services;

/// <summary>
/// Construye el enlace mailto: de una reclamación para abrir el cliente de correo
/// del puesto (Outlook) con el borrador precargado. Se usa cuando el SMTP
/// institucional no está configurado o falla: la persona revisa y envía a mano.
/// </summary>
public static class ReclamacionMailto
{
    // Los clientes de correo truncan mailto: largos (Outlook clásico ~2KB de URL);
    // se listan como mucho estas líneas y se resume el resto.
    private const int MaxLineas = 12;

    public static string Build(Order linea, IReadOnlyList<Order> lineas, string? email)
    {
        var esReenvio = linea.ReclamadoCount > 0;
        var asunto = $"{(esReenvio ? "2ª reclamación" : "Reclamación")} pedido {linea.DocumentoCompras} · Hospital Universitario de Fuenlabrada";

        var sb = new StringBuilder();
        sb.AppendLine($"Estimado proveedor {linea.ProveedorNombre ?? linea.ProveedorCodigo ?? ""}:");
        sb.AppendLine();
        sb.AppendLine(esReenvio
            ? $"Les reiteramos la reclamación del pedido {linea.DocumentoCompras}, del que seguimos sin recibir el material indicado:"
            : $"Les reclamamos la entrega pendiente del pedido {linea.DocumentoCompras}, con fecha {linea.FechaDocumento:dd/MM/yyyy}:");
        sb.AppendLine();
        foreach (var o in lineas.Take(MaxLineas))
        {
            sb.Append($"- Material {o.Material}");
            if (o.TextoBreve is not null) sb.Append($" · {o.TextoBreve}");
            if (o.ReferenciaProveedor is not null) sb.Append($" · Ref. {o.ReferenciaProveedor}");
            if (o.PorEntregarCantidad is not null) sb.Append($" · Pendiente: {o.PorEntregarCantidad:0.##} uds.");
            sb.AppendLine();
        }
        if (lineas.Count > MaxLineas)
            sb.AppendLine($"… y {lineas.Count - MaxLineas} líneas más del mismo pedido.");
        sb.AppendLine();
        sb.AppendLine("Les rogamos nos confirmen la fecha prevista de entrega respondiendo a este correo.");
        var contratos = lineas.Where(o => o.ContratoMarco != null).Select(o => o.ContratoMarco!).Distinct().ToList();
        if (contratos.Count > 0)
            sb.AppendLine($"El pedido está sujeto a contrato marco ({string.Join(", ", contratos)}); los plazos contractuales de entrega son de aplicación.");
        sb.AppendLine();
        sb.AppendLine("Atentamente,");
        sb.AppendLine("Servicio de Suministros (Almacén)");
        sb.AppendLine("Hospital Universitario de Fuenlabrada · SERMAS");

        return $"mailto:{Uri.EscapeDataString(email ?? string.Empty)}" +
               $"?subject={Uri.EscapeDataString(asunto)}" +
               $"&body={Uri.EscapeDataString(sb.ToString())}";
    }
}
