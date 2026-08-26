using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using PedidosPendientes.Infrastructure;
using PedidosPendientes.Infrastructure.Data;
using PedidosPendientes.Web.Services;

// Cultura fija es-ES: los formatos de número/fecha no dependen de la configuración
// regional del servidor (contenedor Linux sin LANG = cultura invariante; el Mac o
// el IIS del hospital, es-ES). Los parsers de Excel usan cultura explícita aparte.
var esES = new System.Globalization.CultureInfo("es-ES");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = esES;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = esES;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
// Las claves de DataProtection (cookie de la verja demo, antiforgery) se guardan
// en la base de datos: sobreviven a reinicios/hibernaciones del contenedor. Con
// base en memoria el efecto es el mismo que el almacén efímero por defecto.
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>()
    .SetApplicationName("PedidosPendientesAlmacen");
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<PedidosOverviewService>();
builder.Services.AddScoped<AuditoriaService>();

// --- Autenticación institucional (Active Directory) ---
// Auth:Mode = "Windows" activa Negotiate (Kerberos/NTLM): en IIS con
// autenticación de Windows o en Kestrel unido al dominio. Todo requiere
// usuario autenticado; Auth:AllowedGroups (lista de grupos AD) restringe
// además el acceso al personal de Suministros. Con "None" (por defecto)
// no hay autenticación: solo para desarrollo y demo.
var authMode = builder.Configuration.GetValue<string>("Auth:Mode") ?? "None";
var windowsAuth = string.Equals(authMode, "Windows", StringComparison.OrdinalIgnoreCase);
if (windowsAuth)
{
    builder.Services
        .AddAuthentication(Microsoft.AspNetCore.Authentication.Negotiate.NegotiateDefaults.AuthenticationScheme)
        .AddNegotiate();

    var allowedGroups = builder.Configuration.GetSection("Auth:AllowedGroups").Get<string[]>() ?? [];
    builder.Services.AddAuthorization(options =>
    {
        var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser();
        if (allowedGroups.Length > 0)
            policy.RequireRole(allowedGroups);
        options.FallbackPolicy = policy.Build();
    });
}

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

// Inicialización de datos. La verja de contraseña demo (Demo:Enabled) es independiente
// de la base de datos; los datos ficticios del seeder siguen limitados a la base en
// memoria: nunca se insertan en una base relacional (SQL Server corporativo o PostgreSQL).
var demoMode = builder.Configuration.GetValue<bool>("Demo:Enabled");
var demoPersistente = false; // demo con base relacional: los datos cargados se conservan
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    demoPersistente = demoMode && db.Database.IsRelational();
    if (!db.Database.IsRelational())
    {
        db.Database.EnsureCreated();
        if (demoMode)
            await DemoDataSeeder.SeedAsync(db);
    }
    else if (db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
    {
        // La demo persistente en PostgreSQL no usa migraciones (son de SQL Server):
        // el esquema se crea directamente desde el modelo.
        db.Database.EnsureCreated();
    }
    else
    {
        db.Database.Migrate();
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
        ctx.Items["DemoPersistente"] = demoPersistente;
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
            path.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase) ||
            // La página de error debe poder re-ejecutarse sin pasar por la verja.
            path.Equals("/Home/Error", StringComparison.OrdinalIgnoreCase);

        if (!allowed && !DemoAccess.ValidateToken(dpProvider, ctx.Request.Cookies[DemoAccess.CookieName]))
        {
            // Un POST interceptado (p. ej. subir un Excel con la sesión caducada) no
            // puede reanudarse con un GET a la misma ruta: se devuelve a la página
            // de origen para que repita la acción tras identificarse.
            var destino = HttpMethods.IsPost(ctx.Request.Method)
                ? (Uri.TryCreate(ctx.Request.Headers.Referer.ToString(), UriKind.Absolute, out var referer)
                    ? referer.PathAndQuery : "/")
                : ctx.Request.Path + ctx.Request.QueryString;
            ctx.Response.Redirect("/acceso?returnUrl=" + Uri.EscapeDataString(destino));
            return;
        }

        await next();
    });
}

app.UseRouting();
if (windowsAuth)
    app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
