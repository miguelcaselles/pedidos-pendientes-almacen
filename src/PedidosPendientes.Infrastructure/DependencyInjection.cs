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
    /// Usa SQL Server si hay cadena de conexión "Default"; si no, usa una base en memoria
    /// (útil en desarrollo local sin SQL Server).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var conn = config.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(conn))
        {
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("PedidosPendientesDev"));
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
}
