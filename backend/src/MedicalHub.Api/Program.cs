using System.Threading.RateLimiting;
using MedicalHub.Api.Middleware;
using MedicalHub.Api.Seguridad;
using MedicalHub.Application;
using MedicalHub.Application.Comun;
using MedicalHub.Application.Identidad;
using MedicalHub.Infrastructure;
using MedicalHub.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, servicios, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(servicios)
    .Enrich.FromLogContext());

builder.Services.AddAplicacion();
builder.Services.AddInfraestructura(builder.Configuration, builder.Environment.IsDevelopment());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IContextoActual, ContextoActual>();

// Los secretos MFA se cifran con Data Protection. En producción hay que persistir el anillo de claves
// (DataProtection:RutaClaves) en un almacenamiento compartido y protegido.
var dp = builder.Services.AddDataProtection().SetApplicationName("MedicalHub");
if (builder.Configuration["DataProtection:RutaClaves"] is { Length: > 0 } rutaClaves)
    dp.PersistKeysToFileSystem(new DirectoryInfo(rutaClaves));

// ── Autenticación JWT ──
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<OpcionesSeguridad>((o, seg) =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = seg.Emisor,
            ValidateAudience = true, ValidAudience = seg.Audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(seg.ClaveJwt!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

// Por defecto TODO exige sesión completa (sin MFA pendiente). Lo público se marca con [AllowAnonymous].
builder.Services.AddAuthorization(o =>
{
    var completa = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireAssertion(c => !c.User.HasClaim("mfa_pendiente", "true"))
        .Build();
    o.DefaultPolicy = completa;
    o.FallbackPolicy = completa;
    o.AddPolicy(Politicas.SesionIncluidaMfaPendiente, p => p.RequireAuthenticatedUser());
    // Se usan políticas con nombre (no [Authorize(Roles=...)]) para garantizar que se exige sesión completa.
    o.AddPolicy(Politicas.AdministradorPlataforma, p => p.Combine(completa).RequireRole("AdministradorPlataforma"));
    o.AddPolicy(Politicas.AdministradorSociedad, p => p.Combine(completa).RequireRole("AdministradorSociedad"));
    o.AddPolicy(Politicas.LecturaSociedad, p => p.Combine(completa)
        .RequireRole("AdministradorSociedad", "CoordinadorMedico", "ResponsableCentro"));
    o.AddPolicy(Politicas.GestionPlanificacion, p => p.Combine(completa).RequireRole("AdministradorSociedad", "CoordinadorMedico"));
    o.AddPolicy(Politicas.UsuarioSociedad, p => p.Combine(completa).RequireClaim("org"));
});

// ── Límite de intentos de autenticación (por IP) ──
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(Politicas.LimiteLogin, ctx =>
    {
        var limite = ctx.RequestServices.GetRequiredService<OpcionesSeguridad>().LimiteLoginPorMinuto;
        return RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = limite, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
    });
});

var origenesPermitidos = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    if (origenesPermitidos.Length > 0)
        p.WithOrigins(origenesPermitidos).AllowAnyHeader().AllowAnyMethod()
         .WithExposedHeaders("X-Correlation-Id");
}));

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<MedicalHubDbContext>("base-de-datos", tags: ["ready"]);

var app = builder.Build();

// ── Migración opcional y alta del primer administrador de plataforma (sin credenciales por defecto) ──
using (var ambito = app.Services.CreateScope())
{
    if (app.Configuration.GetValue<bool>("Base:MigrarAlIniciar"))
        ambito.ServiceProvider.GetRequiredService<MedicalHubDbContext>().Database.Migrate();

    var boot = app.Configuration.GetSection("Bootstrap:AdministradorPlataforma");
    var creado = await ambito.ServiceProvider.GetRequiredService<InicializadorPlataforma>()
        .AsegurarAdministradorAsync(boot["Email"], boot["Nombre"], boot["Contrasena"], CancellationToken.None);
    if (creado) app.Logger.LogWarning("Se ha creado el administrador de plataforma inicial. Retire Bootstrap__* de la configuración.");
}

app.UseMiddleware<IdCorrelacionMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();
app.UseMiddleware<CabecerasSeguridadMiddleware>();

if (app.Environment.IsDevelopment())
    app.MapOpenApi().AllowAnonymous();
else
    app.UseHsts();

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") }).AllowAnonymous();

app.Run();

public partial class Program;
