using PedidosPendientes.Core.Entities;

namespace PedidosPendientes.Core;

/// <summary>Nivel de alerta de seguimiento de una reclamación.</summary>
public enum AlertaReclamacion
{
    /// <summary>Sin alerta.</summary>
    Ninguna,

    /// <summary>Pendiente y ya vencido el plazo de reclamación de su clase: hay que reclamar.</summary>
    Reclamar,

    /// <summary>Reclamado y sin respuesta del proveedor dentro del plazo: reenviar/insistir.</summary>
    SinRespuesta,

    /// <summary>Reclamado, sin respuesta y agotado también el plazo extra: llamar por teléfono.</summary>
    Llamar,
}

/// <summary>
/// Reglas de negocio de urgencia y seguimiento de reclamaciones.
/// Combinan la clase A/B/C del material, la criticidad de la ubicación y los
/// plazos parametrizados en Configuración.
/// </summary>
public static class ReclamacionRules
{
    /// <summary>
    /// Calcula la alerta de seguimiento de una línea pendiente.
    /// <paramref name="tieneRespuesta"/> indica si el proveedor ya contestó
    /// (formulario firmado) después de la última reclamación.
    /// </summary>
    public static AlertaReclamacion Alerta(Order o, string? clase, bool tieneRespuesta,
        ReclamacionParametros p, DateOnly hoy)
    {
        if (o.Recibido || o.Anulado || o.PausaReclamacion) return AlertaReclamacion.Ninguna;

        if (o.Reclamado && o.ReclamadoAt is not null)
        {
            if (tieneRespuesta) return AlertaReclamacion.Ninguna;

            var desdeReclamo = BusinessDays.CountBusinessDays(
                DateOnly.FromDateTime(o.ReclamadoAt.Value.LocalDateTime), hoy);

            if (desdeReclamo >= p.PlazoRespuestaDias + p.PlazoLlamadaDias)
                return AlertaReclamacion.Llamar;
            if (desdeReclamo >= p.PlazoRespuestaDias)
                return AlertaReclamacion.SinRespuesta;
            return AlertaReclamacion.Ninguna;
        }

        var antiguedad = BusinessDays.CountBusinessDays(o.FechaDocumento, hoy);
        return antiguedad >= p.PlazoReclamo(clase) ? AlertaReclamacion.Reclamar : AlertaReclamacion.Ninguna;
    }

    /// <summary>
    /// Riesgo de que el pedido tarde más de lo debido: días hábiles transcurridos
    /// divididos por el plazo objetivo de entrega (del proveedor si tiene acuerdo
    /// marco con plazo propio; si no, el global). &gt;= 1 significa fuera de plazo.
    /// </summary>
    public static double RiesgoRetraso(Order o, int? plazoProveedor, ReclamacionParametros p, DateOnly hoy)
    {
        var plazo = plazoProveedor ?? p.PlazoObjetivoEntregaDias;
        if (plazo <= 0) plazo = 1;
        return (double)BusinessDays.CountBusinessDays(o.FechaDocumento, hoy) / plazo;
    }
}
