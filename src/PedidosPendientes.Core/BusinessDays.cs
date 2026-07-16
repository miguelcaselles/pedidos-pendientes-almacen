namespace PedidosPendientes.Core;

/// <summary>Utilidades de días hábiles (lunes a viernes), sin festivos.</summary>
public static class BusinessDays
{
    /// <summary>Fecha resultante de restar <paramref name="dias"/> días hábiles a hoy.</summary>
    public static DateOnly SubtractBusinessDays(DateOnly from, int dias)
    {
        var d = from;
        while (dias > 0)
        {
            d = d.AddDays(-1);
            if (d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                dias--;
        }
        return d;
    }

    /// <summary>Fecha resultante de sumar <paramref name="dias"/> días hábiles a <paramref name="from"/>.</summary>
    public static DateOnly AddBusinessDays(DateOnly from, int dias)
    {
        var d = from;
        while (dias > 0)
        {
            d = d.AddDays(1);
            if (d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                dias--;
        }
        return d;
    }

    /// <summary>Cuenta los días hábiles transcurridos entre dos fechas (inclusivo del inicio, exclusivo del fin).</summary>
    public static int CountBusinessDays(DateOnly from, DateOnly to)
    {
        if (to <= from) return 0;
        int count = 0;
        for (var d = from; d < to; d = d.AddDays(1))
            if (d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                count++;
        return count;
    }
}
