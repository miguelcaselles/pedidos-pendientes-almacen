using ClosedXML.Excel;
using PedidosPendientes.Web.Models;

namespace PedidosPendientes.Web.Services;

/// <summary>Generación de los libros Excel (.xlsx) de la auditoría de proveedores.</summary>
public static class AuditoriaExcelBuilder
{
    private static readonly XLColor Azul = XLColor.FromHtml("#0877bd");
    private static readonly XLColor AzulClaro = XLColor.FromHtml("#eef8fe");

    private static void Titulo(IXLWorksheet ws, string titulo, DateTime generado, int cols)
    {
        ws.Cell(1, 1).Value = titulo;
        ws.Range(1, 1, 1, cols).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Font.FontColor = Azul;

        ws.Cell(2, 1).Value = $"Hospital Universitario de Fuenlabrada · Servicio de Suministros · Generado el {generado:dd/MM/yyyy HH:mm} · Datos de demostración";
        ws.Range(2, 1, 2, cols).Merge();
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
        ws.Cell(2, 1).Style.Font.FontSize = 9;
    }

    private static void CabeceraTabla(IXLWorksheet ws, int fila, string[] cols)
    {
        for (int i = 0; i < cols.Length; i++)
        {
            var c = ws.Cell(fila, i + 1);
            c.Value = cols[i];
            c.Style.Font.Bold = true;
            c.Style.Font.FontColor = XLColor.White;
            c.Style.Fill.BackgroundColor = Azul;
            c.Style.Alignment.WrapText = true;
            c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        ws.SheetView.FreezeRows(fila);
    }

    private static string Pct(double? v) => v.HasValue ? $"{v.Value:0}%" : "—";
    private static string Num(double? v, int dec = 1) => v.HasValue ? v.Value.ToString("0." + new string('0', dec)) : "—";

    public static byte[] Global(AuditoriaGlobalViewModel m)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Auditoría proveedores");

        var cols = new[]
        {
            "Código", "Proveedor", "Acuerdo marco", "Plazo (d háb.)", "Total líneas", "Pendientes",
            "Recibidas", "Anuladas", "En falta", "Pend. fuera plazo", "Entregadas a tiempo",
            "Entregadas tarde", "% a tiempo", "T. medio entrega (d)", "T. máx (d)", "Líneas reclamadas",
            "Reclamaciones", "Re-reclamadas", "T. medio hasta reclamar (d)", "Emails", "Respuestas",
            "Tasa respuesta", "Puntuación", "Calificación",
        };

        Titulo(ws, "Auditoría de proveedores — Resumen global", m.GeneradoEl, cols.Length);

        // Bloque de KPIs
        int r = 4;
        void Kpi(string k, string v) { ws.Cell(r, 1).Value = k; ws.Cell(r, 1).Style.Font.Bold = true; ws.Cell(r, 2).Value = v; r++; }
        Kpi("Proveedores auditados", m.TotalProveedores.ToString());
        Kpi("Con acuerdo marco", m.ProveedoresAcuerdoMarco.ToString());
        Kpi("Proveedores que incumplen", m.ProveedoresIncumplen.ToString());
        Kpi("Líneas totales / pendientes / recibidas", $"{m.TotalLineas} / {m.LineasPendientes} / {m.LineasRecibidas}");
        Kpi("Entrega a tiempo (global)", Pct(m.PorcentajeEntregaATiempo));
        Kpi("Tiempo medio de entrega (global)", Num(m.TiempoMedioEntregaGlobal) + " d háb.");
        Kpi("Reclamaciones / respuestas", $"{m.TotalReclamaciones} / {m.RespuestasProveedor}");
        Kpi("Tasa de respuesta (global)", Pct(m.TasaRespuestaGlobal));

        int head = r + 1;
        CabeceraTabla(ws, head, cols);

        int fila = head + 1;
        foreach (var f in m.Filas)
        {
            int c = 1;
            ws.Cell(fila, c++).Value = f.Proveedor.Codigo;
            ws.Cell(fila, c++).Value = f.Proveedor.Nombre;
            ws.Cell(fila, c++).Value = f.Proveedor.AcuerdoMarco ? "Sí" : "No";
            ws.Cell(fila, c++).Value = f.PlazoAplicable;
            ws.Cell(fila, c++).Value = f.TotalLineas;
            ws.Cell(fila, c++).Value = f.Pendientes;
            ws.Cell(fila, c++).Value = f.Recibidas;
            ws.Cell(fila, c++).Value = f.Anuladas;
            ws.Cell(fila, c++).Value = f.EnFalta;
            ws.Cell(fila, c++).Value = f.PendientesFueraPlazo;
            ws.Cell(fila, c++).Value = f.EntregadasATiempo;
            ws.Cell(fila, c++).Value = f.EntregadasTarde;
            ws.Cell(fila, c++).Value = Pct(f.PorcentajeATiempo);
            ws.Cell(fila, c++).Value = Num(f.TiempoMedioEntrega);
            ws.Cell(fila, c++).Value = f.TiempoMaxEntrega.HasValue ? f.TiempoMaxEntrega.Value.ToString() : "—";
            ws.Cell(fila, c++).Value = f.LineasReclamadas;
            ws.Cell(fila, c++).Value = f.TotalReclamaciones;
            ws.Cell(fila, c++).Value = f.Rereclamadas;
            ws.Cell(fila, c++).Value = Num(f.TiempoMedioHastaReclamar);
            ws.Cell(fila, c++).Value = f.EmailsEnviados;
            ws.Cell(fila, c++).Value = f.RespuestasRecibidas;
            ws.Cell(fila, c++).Value = Pct(f.TasaRespuesta);
            ws.Cell(fila, c++).Value = Math.Round(f.Puntuacion);
            ws.Cell(fila, c++).Value = f.Calificacion;

            if (f.Incumple)
                ws.Range(fila, 1, fila, cols.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#fdecec");
            fila++;
        }

        ws.Range(head, 1, fila - 1, cols.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Columns().AdjustToContents();
        ws.Column(2).Width = 42;

        return ToBytes(wb);
    }

    public static byte[] Proveedor(AuditoriaProveedorDetalle d)
    {
        var pr = d.Resumen.Proveedor;
        using var wb = new XLWorkbook();

        // --- Hoja Resumen ---
        var ws = wb.AddWorksheet("Resumen");
        Titulo(ws, $"Auditoría — {pr.Nombre} ({pr.Codigo})", d.GeneradoEl, 2);
        int r = 4;
        void Kpi(string k, string v) { ws.Cell(r, 1).Value = k; ws.Cell(r, 1).Style.Font.Bold = true; ws.Cell(r, 2).Value = v; r++; }
        var m = d.Resumen;
        Kpi("NIF/CIF", pr.NifCif ?? "—");
        Kpi("Email", pr.Email ?? "—");
        Kpi("Teléfono", pr.Telefono ?? "—");
        Kpi("Acuerdo marco", pr.AcuerdoMarco ? "Sí" : "No");
        Kpi("Plazo de entrega aplicable", $"{m.PlazoAplicable} días hábiles");
        Kpi("Calificación", $"{m.Calificacion} ({Math.Round(m.Puntuacion)}/100)");
        r++;
        Kpi("Líneas totales", m.TotalLineas.ToString());
        Kpi("Pendientes", m.Pendientes.ToString());
        Kpi("Pendientes fuera de plazo", m.PendientesFueraPlazo.ToString());
        Kpi("En falta", m.EnFalta.ToString());
        Kpi("Recibidas", m.Recibidas.ToString());
        Kpi("Anuladas", m.Anuladas.ToString());
        r++;
        Kpi("Entregadas a tiempo / tarde", $"{m.EntregadasATiempo} / {m.EntregadasTarde}");
        Kpi("% entregas a tiempo", Pct(m.PorcentajeATiempo));
        Kpi("Tiempo medio de entrega", Num(m.TiempoMedioEntrega) + " d háb.");
        Kpi("Tiempo mediano de entrega", Num(m.TiempoMedianoEntrega) + " d háb.");
        Kpi("Tiempo máximo de entrega", m.TiempoMaxEntrega.HasValue ? $"{m.TiempoMaxEntrega} d háb." : "—");
        r++;
        Kpi("Líneas reclamadas", m.LineasReclamadas.ToString());
        Kpi("Reclamaciones totales", m.TotalReclamaciones.ToString());
        Kpi("Re-reclamadas (2+)", m.Rereclamadas.ToString());
        Kpi("Tiempo medio hasta reclamar", Num(m.TiempoMedioHastaReclamar) + " d háb.");
        Kpi("Correos enviados", m.EmailsEnviados.ToString());
        Kpi("Respuestas recibidas", m.RespuestasRecibidas.ToString());
        Kpi("Tasa de respuesta", Pct(m.TasaRespuesta));
        ws.Column(1).Width = 34; ws.Column(2).Width = 40;

        // --- Hoja Evolución mensual ---
        var we = wb.AddWorksheet("Evolución mensual");
        CabeceraTabla(we, 1, ["Mes", "Pedidos", "Recibidos", "Reclamaciones", "En falta", "T. medio entrega (d)"]);
        int fe = 2;
        foreach (var x in d.Evolucion)
        {
            we.Cell(fe, 1).Value = x.Etiqueta;
            we.Cell(fe, 2).Value = x.Pedidos;
            we.Cell(fe, 3).Value = x.Recibidos;
            we.Cell(fe, 4).Value = x.Reclamaciones;
            we.Cell(fe, 5).Value = x.EnFalta;
            we.Cell(fe, 6).Value = Num(x.TiempoMedioEntrega);
            fe++;
        }
        we.Columns().AdjustToContents();

        // --- Hoja Líneas ---
        var wl = wb.AddWorksheet("Líneas");
        CabeceraTabla(wl, 1, ["Documento", "Material", "Descripción", "Clase", "Fecha pedido", "Estado",
            "Días transc.", "Días entrega", "Fuera plazo", "Reclamado", "Nº reclam.", "Respuesta"]);
        int fl = 2;
        foreach (var l in d.Lineas)
        {
            int c = 1;
            wl.Cell(fl, c++).Value = l.Order.DocumentoCompras;
            wl.Cell(fl, c++).Value = l.Order.Material;
            wl.Cell(fl, c++).Value = l.Order.TextoBreve ?? "";
            wl.Cell(fl, c++).Value = l.Clase ?? "—";
            wl.Cell(fl, c++).Value = l.Order.FechaDocumento.ToDateTime(TimeOnly.MinValue);
            wl.Cell(fl, c).Style.DateFormat.Format = "dd/MM/yyyy"; c++;
            wl.Cell(fl, c++).Value = l.Estado;
            wl.Cell(fl, c++).Value = l.DiasHabiles;
            wl.Cell(fl, c++).Value = l.DiasEntrega.HasValue ? l.DiasEntrega.Value.ToString() : "—";
            wl.Cell(fl, c++).Value = l.FueraPlazo ? "Sí" : "No";
            wl.Cell(fl, c++).Value = l.Reclamado ? "Sí" : "No";
            wl.Cell(fl, c++).Value = l.ReclamadoCount;
            wl.Cell(fl, c++).Value = l.TieneRespuesta ? "Sí" : "No";
            if (l.FueraPlazo)
                wl.Range(fl, 1, fl, 12).Style.Fill.BackgroundColor = XLColor.FromHtml("#fdecec");
            fl++;
        }
        wl.Columns().AdjustToContents();
        wl.Column(3).Width = 40;

        // --- Hoja Respuestas del proveedor ---
        var wr = wb.AddWorksheet("Respuestas");
        CabeceraTabla(wr, 1, ["Fecha", "Documento", "Material", "Estado", "Fecha estimada", "Revisión", "Comentario"]);
        int fr = 2;
        foreach (var resp in d.Respuestas)
        {
            wr.Cell(fr, 1).Value = resp.CreatedAt.LocalDateTime;
            wr.Cell(fr, 1).Style.DateFormat.Format = "dd/MM/yyyy";
            wr.Cell(fr, 2).Value = resp.DocumentoCompras;
            wr.Cell(fr, 3).Value = resp.Material ?? "(global)";
            wr.Cell(fr, 4).Value = resp.Estado;
            wr.Cell(fr, 5).Value = resp.FechaEstimada.HasValue ? resp.FechaEstimada.Value.ToString("dd/MM/yyyy") : "—";
            wr.Cell(fr, 6).Value = resp.RevisionEstado;
            wr.Cell(fr, 7).Value = resp.Comentario ?? "";
            fr++;
        }
        wr.Columns().AdjustToContents();
        wr.Column(7).Width = 50;

        return ToBytes(wb);
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
