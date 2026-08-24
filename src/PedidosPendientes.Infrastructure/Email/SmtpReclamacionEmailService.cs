using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using PedidosPendientes.Core.Abstractions;
using PedidosPendientes.Core.Entities;
using PedidosPendientes.Infrastructure.Data;

namespace PedidosPendientes.Infrastructure.Email;

/// <summary>
/// Envío de reclamaciones por el SMTP institucional (MailKit). El correo agrupa
/// todas las líneas pendientes del documento reclamado y queda registrado en
/// EmailLogs (enviado o error). En modo pruebas se desvía al buzón configurado.
/// </summary>
public class SmtpReclamacionEmailService : IReclamacionEmailService
{
    private readonly AppDbContext _db;
    private readonly SmtpOptions _opt;
    private readonly ILogger<SmtpReclamacionEmailService> _log;

    public SmtpReclamacionEmailService(AppDbContext db, IOptions<SmtpOptions> opt,
        ILogger<SmtpReclamacionEmailService> log)
    {
        _db = db;
        _opt = opt.Value;
        _log = log;
    }

    public bool Habilitado => _opt.Enabled
        && !string.IsNullOrWhiteSpace(_opt.Host)
        && !string.IsNullOrWhiteSpace(_opt.FromAddress);

    public async Task<ReclamacionEmailResultado> EnviarAsync(Order linea, string tipo, CancellationToken ct = default)
    {
        if (!Habilitado)
            return new ReclamacionEmailResultado
            {
                Enviado = false,
                Motivo = "deshabilitado",
                Detalle = "El envío por correo no está configurado (sección Smtp). La línea queda marcada como reclamada.",
            };

        var proveedor = linea.ProveedorCodigo is null ? null
            : await _db.Proveedores.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Codigo == linea.ProveedorCodigo, ct);

        var email = proveedor?.Email?.Trim();
        if (string.IsNullOrEmpty(email))
            return new ReclamacionEmailResultado
            {
                Enviado = false,
                Motivo = "sin-email",
                Detalle = $"El proveedor {linea.ProveedorNombre ?? linea.ProveedorCodigo ?? "desconocido"} no tiene email configurado. " +
                          "Complétalo en la pestaña Proveedores para reclamar por correo.",
            };

        // El correo incluye todas las líneas aún pendientes del mismo documento.
        var lineas = await _db.Orders.AsNoTracking()
            .Where(o => o.DocumentoCompras == linea.DocumentoCompras
                        && !o.Recibido && !o.Anulado)
            .OrderBy(o => o.Material)
            .ToListAsync(ct);
        if (lineas.Count == 0) lineas = [linea];

        var destinoReal = _opt.ModoPruebas && !string.IsNullOrWhiteSpace(_opt.DestinatarioPruebas)
            ? _opt.DestinatarioPruebas!
            : email;

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_opt.FromName, _opt.FromAddress));
        msg.To.Add(new MailboxAddress(proveedor!.Nombre, destinoReal));
        if (!string.IsNullOrWhiteSpace(_opt.ReplyTo))
            msg.ReplyTo.Add(MailboxAddress.Parse(_opt.ReplyTo));
        if (!string.IsNullOrWhiteSpace(_opt.Bcc))
            msg.Bcc.Add(MailboxAddress.Parse(_opt.Bcc));

        var esReenvio = linea.ReclamadoCount > 0;
        msg.Subject = $"{(_opt.ModoPruebas ? "[PRUEBAS] " : "")}{(esReenvio ? "2ª reclamación" : "Reclamación")} pedido {linea.DocumentoCompras} · Hospital Universitario de Fuenlabrada";

        msg.Body = new BodyBuilder
        {
            TextBody = CuerpoTexto(linea, lineas, proveedor, esReenvio),
            HtmlBody = CuerpoHtml(linea, lineas, proveedor, esReenvio),
        }.ToMessageBody();

        var logEntry = new EmailLog
        {
            ProveedorId = proveedor.Id,
            DocumentoCompras = linea.DocumentoCompras,
            ProveedorEmail = destinoReal,
            Tipo = tipo,
            SentAt = DateTimeOffset.UtcNow,
        };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_opt.Host, _opt.Port,
                _opt.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, ct);
            if (!string.IsNullOrWhiteSpace(_opt.User))
                await client.AuthenticateAsync(_opt.User, _opt.Password ?? string.Empty, ct);
            await client.SendAsync(msg, ct);
            await client.DisconnectAsync(true, ct);

            logEntry.Estado = "enviado";
            _db.EmailLogs.Add(logEntry);
            await _db.SaveChangesAsync(ct);

            return new ReclamacionEmailResultado
            {
                Enviado = true,
                Detalle = _opt.ModoPruebas
                    ? $"Reclamación enviada al buzón de PRUEBAS ({destinoReal})."
                    : $"Reclamación enviada por correo a {destinoReal}.",
            };
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error enviando la reclamación del pedido {Doc} a {Email}",
                linea.DocumentoCompras, destinoReal);

            logEntry.Estado = "error";
            logEntry.ErrorDetalle = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            _db.EmailLogs.Add(logEntry);
            await _db.SaveChangesAsync(ct);

            return new ReclamacionEmailResultado
            {
                Enviado = false,
                Motivo = "error",
                Detalle = "No se pudo enviar el correo (queda registrado el error). La línea está marcada como reclamada.",
            };
        }
    }

    private static string CuerpoTexto(Order linea, List<Order> lineas, Proveedor proveedor, bool esReenvio)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Estimado proveedor {proveedor.Nombre}:");
        sb.AppendLine();
        sb.AppendLine(esReenvio
            ? $"Les reiteramos la reclamación del pedido {linea.DocumentoCompras}, del que seguimos sin recibir el material indicado:"
            : $"Les reclamamos la entrega pendiente del pedido {linea.DocumentoCompras}, con fecha {linea.FechaDocumento:dd/MM/yyyy}:");
        sb.AppendLine();
        foreach (var o in lineas)
        {
            sb.Append($"  - Material {o.Material}");
            if (o.TextoBreve is not null) sb.Append($" · {o.TextoBreve}");
            if (o.ReferenciaProveedor is not null) sb.Append($" · Ref. {o.ReferenciaProveedor}");
            if (o.PorEntregarCantidad is not null) sb.Append($" · Pendiente: {o.PorEntregarCantidad:0.##} uds.");
            sb.AppendLine();
        }
        sb.AppendLine();
        sb.AppendLine("Les rogamos nos confirmen la fecha prevista de entrega respondiendo a este correo.");
        if (lineas.Any(o => o.ContratoMarco is not null))
            sb.AppendLine($"El pedido está sujeto a contrato marco ({string.Join(", ", lineas.Where(o => o.ContratoMarco != null).Select(o => o.ContratoMarco).Distinct())}); los plazos contractuales de entrega son de aplicación.");
        sb.AppendLine();
        sb.AppendLine("Atentamente,");
        sb.AppendLine("Servicio de Suministros (Almacén)");
        sb.AppendLine("Hospital Universitario de Fuenlabrada · SERMAS");
        return sb.ToString();
    }

    private static string CuerpoHtml(Order linea, List<Order> lineas, Proveedor proveedor, bool esReenvio)
    {
        static string H(string? s) => System.Net.WebUtility.HtmlEncode(s ?? string.Empty);

        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#1c2833\">");
        sb.Append($"<p>Estimado proveedor <strong>{H(proveedor.Nombre)}</strong>:</p>");
        sb.Append(esReenvio
            ? $"<p>Les <strong>reiteramos la reclamación</strong> del pedido <strong>{H(linea.DocumentoCompras)}</strong>, del que seguimos sin recibir el material indicado:</p>"
            : $"<p>Les reclamamos la entrega pendiente del pedido <strong>{H(linea.DocumentoCompras)}</strong>, con fecha {linea.FechaDocumento:dd/MM/yyyy}:</p>");
        sb.Append("<table style=\"border-collapse:collapse;margin:12px 0\" cellpadding=\"6\">");
        sb.Append("<tr style=\"background:#eef2f6\"><th align=\"left\">Material</th><th align=\"left\">Descripción</th><th align=\"left\">Ref. proveedor</th><th align=\"right\">Pendiente</th></tr>");
        foreach (var o in lineas)
        {
            sb.Append("<tr style=\"border-bottom:1px solid #dde3ea\">");
            sb.Append($"<td>{H(o.Material)}</td><td>{H(o.TextoBreve)}</td><td>{H(o.ReferenciaProveedor)}</td>");
            sb.Append($"<td align=\"right\">{(o.PorEntregarCantidad is decimal c ? c.ToString("0.##") : "—")}</td></tr>");
        }
        sb.Append("</table>");
        sb.Append("<p>Les rogamos nos <strong>confirmen la fecha prevista de entrega</strong> respondiendo a este correo.</p>");
        var contratos = lineas.Where(o => o.ContratoMarco != null).Select(o => o.ContratoMarco!).Distinct().ToList();
        if (contratos.Count > 0)
            sb.Append($"<p>El pedido está sujeto a contrato marco ({H(string.Join(", ", contratos))}); los plazos contractuales de entrega son de aplicación.</p>");
        sb.Append("<p>Atentamente,<br/>Servicio de Suministros (Almacén)<br/>Hospital Universitario de Fuenlabrada · SERMAS</p>");
        sb.Append("</div>");
        return sb.ToString();
    }
}
