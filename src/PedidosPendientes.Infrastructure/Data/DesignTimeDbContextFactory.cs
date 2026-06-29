using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PedidosPendientes.Infrastructure.Data;

/// <summary>
/// Permite a las herramientas de EF Core (dotnet ef migrations / database update)
/// crear el DbContext en tiempo de diseño sin arrancar la app web.
/// La cadena de conexión real se inyecta en producción desde la configuración.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("PEDIDOS_DB")
            ?? "Server=localhost;Database=PedidosPendientesAlmacen;Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(conn, sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }
}
