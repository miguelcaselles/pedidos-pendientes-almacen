using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using PedidosPendientes.Infrastructure;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDataProtection();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<PedidosOverviewService>();
builder.Services.AddScoped<AuditoriaService>();

var app = builder.Build();

// Render (y otros reverse proxies) termina TLS antes de llegar a Kestrel.
// La confianza en cabeceras reenviadas se activa explícitamente por entorno.
if (builder.Configuration.GetValue<bool>("ReverseProxy:TrustForwardedHeaders"))
{
    var forwarded = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    };
    forwarded.KnownNetworks.Clear();
    forwarded.KnownProxies.Clear();
    app.UseForwardedHeaders(forwarded);
}

// Inicialización de datos. El escenario DEMO sólo se admite con base no relacional:
// nunca se insertan datos ficticios en el SQL Server corporativo.
var demoMode = false;
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
        demoMode = builder.Configuration.GetValue<bool>("Demo:Enabled");
        if (demoMode)
            await DemoDataSeeder.SeedAsync(db);
    }
}

// Cabeceras de seguridad institucionales (equivalentes a las de la app original).
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Frame-Options"] = "DENY";
    h["Content-Security-Policy"] = "frame-ancestors 'none'";
    h["X-Content-Type-Options"] = "nosniff";
    h["Referrer-Policy"] = "no-referrer";
    h["X-Robots-Tag"] = "noindex, nofollow, noarchive, nosnippet";
    h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    if (demoMode)
    {
        h["Cache-Control"] = "no-store, max-age=0";
        ctx.Items["DemoMode"] = true;
    }
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

// Verja de acceso del entorno DEMO: exige la contraseña compartida antes de
// mostrar la aplicación. Solo se activa en modo demo; en producción el acceso
// lo gobernará la autenticación corporativa (AD/SSO).
if (demoMode)
{
    var dpProvider = app.Services.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>();
    app.Use(async (ctx, next) =>
    {
        var path = ctx.Request.Path.Value ?? "/";
        bool allowed =
            path.Equals("/acceso", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/img/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase);

        if (!allowed && !DemoAccess.ValidateToken(dpProvider, ctx.Request.Cookies[DemoAccess.CookieName]))
        {
            var returnUrl = Uri.EscapeDataString(ctx.Request.Path + ctx.Request.QueryString);
            ctx.Response.Redirect("/acceso?returnUrl=" + returnUrl);
            return;
        }

        await next();
    });
}

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
