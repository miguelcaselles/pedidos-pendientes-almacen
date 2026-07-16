using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace PedidosPendientes.Web.Services;

/// <summary>
/// Verja de acceso para el ENTORNO DEMO: una única contraseña compartida protege
/// la aplicación de demostración pública. No sustituye a la autenticación
/// corporativa (AD/SSO) prevista para producción; solo se activa cuando
/// <c>Demo:Enabled = true</c>.
/// </summary>
public static class DemoAccess
{
    public const string CookieName = "demo_access";
    private const string Purpose = "PedidosPendientes.DemoAccess.v1";
    private const string Payload = "granted";

    private static IDataProtector Protector(IDataProtectionProvider provider) =>
        provider.CreateProtector(Purpose);

    /// <summary>Genera el token firmado que se guarda en la cookie tras validar la contraseña.</summary>
    public static string IssueToken(IDataProtectionProvider provider) =>
        Protector(provider).Protect(Payload);

    /// <summary>Comprueba que la cookie contiene un token válido emitido por esta app.</summary>
    public static bool ValidateToken(IDataProtectionProvider provider, string? token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        try { return Protector(provider).Unprotect(token) == Payload; }
        catch { return false; }
    }

    /// <summary>Compara la contraseña en tiempo constante para no filtrarla por temporización.</summary>
    public static bool PasswordMatches(string? provided, string? expected)
    {
        if (string.IsNullOrEmpty(provided) || string.IsNullOrEmpty(expected)) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(expected));
    }
}
