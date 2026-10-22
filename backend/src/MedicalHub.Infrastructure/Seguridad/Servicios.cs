using System.Security.Cryptography;
using System.Text;
using MedicalHub.Application.Comun;
using MedicalHub.Domain.Identidad;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MedicalHub.Infrastructure.Seguridad;

public sealed class HasherContrasena : IHasherContrasena
{
    private readonly PasswordHasher<string> _hasher = new();

    public string Hash(string contrasena) => _hasher.HashPassword("", contrasena);

    public bool Verificar(string hash, string contrasena)
        => _hasher.VerifyHashedPassword("", hash, contrasena) != PasswordVerificationResult.Failed;
}

public sealed class EmisorTokens(OpcionesSeguridad opciones) : IEmisorTokens
{
    private readonly SigningCredentials _firma = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ObtenerClave(opciones))), SecurityAlgorithms.HmacSha256);

    public static string ObtenerClave(OpcionesSeguridad o)
        => o.ClaveJwt is { Length: >= 32 } clave
            ? clave
            : throw new InvalidOperationException("Seguridad:ClaveJwt debe definirse (≥ 32 caracteres) mediante variable de entorno.");

    public TokenAcceso CrearTokenAcceso(Usuario usuario, bool mfaPendiente, DateTimeOffset ahora)
    {
        var expira = ahora.AddMinutes(opciones.MinutosTokenAcceso);
        var claims = new Dictionary<string, object>
        {
            ["sub"] = usuario.Id.ToString(),
            ["email"] = usuario.Email,
            ["role"] = usuario.ListaRoles.Select(r => r.ToString()).ToArray()
        };
        if (usuario.OrganizacionId is { } org) claims["org"] = org.ToString();
        if (mfaPendiente) claims["mfa_pendiente"] = "true";

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = opciones.Emisor,
            Audience = opciones.Audiencia,
            Claims = claims,
            IssuedAt = ahora.UtcDateTime,
            NotBefore = ahora.UtcDateTime,
            Expires = expira.UtcDateTime,
            SigningCredentials = _firma
        };
        return new TokenAcceso(new JsonWebTokenHandler().CreateToken(descriptor), expira);
    }

    public (string Token, string Hash) GenerarTokenRefresco()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        return (token, HashearTokenRefresco(token));
    }

    public string HashearTokenRefresco(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

/// <summary>TOTP (RFC 6238): HMAC-SHA1, 6 dígitos, paso de 30 s. Compatible con las apps autenticadoras habituales.</summary>
public sealed class ServicioTotp : IServicioTotp
{
    private const int SegundosPaso = 30;
    private const int Digitos = 6;
    private const string AlfabetoBase32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public string GenerarSecreto() => CodificarBase32(RandomNumberGenerator.GetBytes(20));

    public string ConstruirUri(string emisor, string cuenta, string secreto)
        => $"otpauth://totp/{Uri.EscapeDataString(emisor)}:{Uri.EscapeDataString(cuenta)}" +
           $"?secret={secreto}&issuer={Uri.EscapeDataString(emisor)}&algorithm=SHA1&digits={Digitos}&period={SegundosPaso}";

    public static long PasoDe(DateTimeOffset instante) => instante.ToUnixTimeSeconds() / SegundosPaso;

    /// <summary>Calcula el código de un paso (público para que las pruebas simulen la app autenticadora).</summary>
    public static string Generar(string secreto, long paso)
    {
        var contador = BitConverter.GetBytes(paso);
        if (BitConverter.IsLittleEndian) Array.Reverse(contador);
        var hash = HMACSHA1.HashData(DecodificarBase32(secreto), contador);
        var desplazamiento = hash[^1] & 0x0F;
        var binario = ((hash[desplazamiento] & 0x7F) << 24) | (hash[desplazamiento + 1] << 16)
                      | (hash[desplazamiento + 2] << 8) | hash[desplazamiento + 3];
        return (binario % (int)Math.Pow(10, Digitos)).ToString().PadLeft(Digitos, '0');
    }

    public bool Validar(string secreto, string codigo, DateTimeOffset ahora, long ultimoPasoUsado, out long paso)
    {
        paso = 0;
        codigo = (codigo ?? "").Replace(" ", "");
        if (codigo.Length != Digitos) return false;

        var actual = PasoDe(ahora);
        for (var p = actual - 1; p <= actual + 1; p++)
        {
            if (p <= ultimoPasoUsado) continue;
            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Generar(secreto, p)), Encoding.ASCII.GetBytes(codigo)))
            {
                paso = p;
                return true;
            }
        }
        return false;
    }

    private static string CodificarBase32(byte[] datos)
    {
        var sb = new StringBuilder();
        int buffer = 0, bits = 0;
        foreach (var b in datos)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5) { sb.Append(AlfabetoBase32[(buffer >> (bits - 5)) & 31]); bits -= 5; }
        }
        if (bits > 0) sb.Append(AlfabetoBase32[(buffer << (5 - bits)) & 31]);
        return sb.ToString();
    }

    private static byte[] DecodificarBase32(string texto)
    {
        var salida = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var c in texto.TrimEnd('=').ToUpperInvariant())
        {
            var valor = AlfabetoBase32.IndexOf(c);
            if (valor < 0) throw new FormatException("Secreto Base32 no válido.");
            buffer = (buffer << 5) | valor;
            bits += 5;
            if (bits >= 8) { salida.Add((byte)((buffer >> (bits - 8)) & 0xFF)); bits -= 8; }
        }
        return [.. salida];
    }
}

public sealed class ProtectorSecretos(IDataProtectionProvider proveedor) : IProtectorSecretos
{
    private readonly IDataProtector _protector = proveedor.CreateProtector("MedicalHub.Mfa.v1");

    public string Proteger(string texto) => _protector.Protect(texto);
    public string Desproteger(string protegido) => _protector.Unprotect(protegido);
}
