using MedicalHub.Api.Middleware;
using MedicalHub.Application;
using MedicalHub.Infrastructure;
using MedicalHub.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, servicios, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(servicios)
    .Enrich.FromLogContext());

builder.Services.AddAplicacion();
builder.Services.AddInfraestructura(builder.Configuration);

var origenesPermitidos = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    if (origenesPermitidos.Length > 0)
        p.WithOrigins(origenesPermitidos).AllowAnyHeader().AllowAnyMethod()
         .WithExposedHeaders("X-Correlation-Id");
}));

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<MedicalHubDbContext>("base-de-datos", tags: ["ready"]);

var app = builder.Build();

app.UseMiddleware<IdCorrelacionMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();
app.UseMiddleware<CabecerasSeguridadMiddleware>();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();
else
    app.UseHsts();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });

app.Run();

public partial class Program;
