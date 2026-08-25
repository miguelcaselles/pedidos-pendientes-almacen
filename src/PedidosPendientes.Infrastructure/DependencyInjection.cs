using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PedidosPendientes.Core.Abstractions;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Infrastructure.Email;
using PedidosPendientes.Infrastructure.Excel;
using PedidosPendientes.Infrastructure.Services;

namespace PedidosPendientes.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra la infraestructura: base de datos, parser de Excel y servicios de aplicación.
    /// Con cadena de conexión "Default" usa SQL Server (corporativo) o PostgreSQL si la
    /// cadena tiene formato "postgresql://..." (demo persistente en la nube); sin cadena,
    /// usa una base en memoria (útil en desarrollo local sin SQL Server).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var conn = config.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(conn))
        {
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("PedidosPendientesDev"));
        }
        else if (IsPostgresConnectionString(conn))
        {
            // Las fechas de los listados SAP llegan sin zona horaria (Kind=Unspecified);
            // el mapeo clásico ("timestamp without time zone") evita el rechazo de Npgsql.
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            services.AddDbContext<AppDbContext>(o => o.UseNpgsql(ToNpgsqlConnectionString(conn)));
        }
        else
        {
            services.AddDbContext<AppDbContext>(o => o.UseSqlServer(conn,
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));
        }

        services.AddScoped<IExcelOrderParser, ExcelOrderParser>();
        services.AddScoped<IOrderImportService, OrderImportService>();
        services.AddScoped<IMd04ImportService, Md04ImportService>();
        services.AddScoped<IClasificacionImportService, ClasificacionImportService>();
        services.AddScoped<ICecosImportService, CecosImportService>();
        services.AddScoped<ISettingsService, SettingsService>();

        services.Configure<SmtpOptions>(config.GetSection(SmtpOptions.Section));
        services.AddScoped<IReclamacionEmailService, SmtpReclamacionEmailService>();

        return services;
    }

    /// <summary>True si no hay cadena de conexión configurada (modo memoria/desarrollo).</summary>
    public static bool UsesInMemoryDatabase(IConfiguration config)
        => string.IsNullOrWhiteSpace(config.GetConnectionString("Default"));

    private static bool IsPostgresConnectionString(string conn)
        => conn.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        || conn.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
        || conn.Contains("Host=", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Npgsql espera pares clave-valor; los proveedores en la nube (Neon, Render, etc.)
    /// entregan URIs "postgresql://usuario:clave@host/bd", que aquí se convierten.
    /// </summary>
    private static string ToNpgsqlConnectionString(string conn)
    {
        if (!conn.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
            return conn;

        var uri = new Uri(conn);
        var userInfo = uri.UserInfo.Split(':', 2);
        var b = new System.Text.StringBuilder()
            .Append("Host=").Append(uri.Host)
            .Append(";Port=").Append(uri.Port > 0 ? uri.Port : 5432)
            .Append(";Database=").Append(uri.AbsolutePath.TrimStart('/'))
            .Append(";Username=").Append(Uri.UnescapeDataString(userInfo[0]));
        if (userInfo.Length > 1)
            b.Append(";Password=").Append(Uri.UnescapeDataString(userInfo[1]));

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        if (string.Equals(query["sslmode"], "require", StringComparison.OrdinalIgnoreCase))
            b.Append(";SSL Mode=Require");
        if (string.Equals(query["channel_binding"], "require", StringComparison.OrdinalIgnoreCase))
            b.Append(";Channel Binding=Require");
        return b.ToString();
    }
}
