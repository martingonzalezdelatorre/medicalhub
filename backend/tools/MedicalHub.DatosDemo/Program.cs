using MedicalHub.Application;
using MedicalHub.Application.Ausencias;
using MedicalHub.Application.Comun;
using MedicalHub.Application.Identidad;
using MedicalHub.Application.Planificacion;
using MedicalHub.Application.Reglas;
using MedicalHub.Domain.Reglas;
using MedicalHub.Domain.Ausencias;
using MedicalHub.Domain.Catalogos;
using MedicalHub.Domain.Estructura;
using MedicalHub.Domain.Identidad;
using MedicalHub.Infrastructure;
using MedicalHub.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Datos de demostración IDENTIFICABLES: una sociedad «[DEMO] …» con usuarios @demo.medicalhub.local.
//
//   ConnectionStrings__MedicalHub  cadena de conexión (obligatoria)
//   DEMO_CONTRASENA                contraseña de TODOS los usuarios demo (obligatoria, ≥ 12 caracteres)
//   --confirmar                    necesario para ejecutar
//   --eliminar                     borra la sociedad demo y todos sus datos
//   --permitir-produccion          imprescindible si ASPNETCORE_ENVIRONMENT=Production
//
// No hay contraseña por defecto. Es idempotente: si la sociedad demo existe, no hace nada.

const string NombreOrganizacion = "[DEMO] Sociedad de Anestesiología Madrid";
const string Dominio = "demo.medicalhub.local";

var config = new ConfigurationBuilder().AddEnvironmentVariables().AddCommandLine(args).Build();
var banderas = args.Where(a => a.StartsWith("--")).ToHashSet();

if (!banderas.Contains("--confirmar"))
    return Salir("Añada --confirmar para ejecutar. Lea la cabecera de Program.cs para ver los parámetros.");
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production" && !banderas.Contains("--permitir-produccion"))
    return Salir("Entorno de producción: los datos de demostración no se cargan sin --permitir-produccion.");

var contrasena = Environment.GetEnvironmentVariable("DEMO_CONTRASENA");
if (!banderas.Contains("--eliminar") && (contrasena is null || contrasena.Length < 12))
    return Salir("Defina la variable DEMO_CONTRASENA (≥ 12 caracteres). No existe contraseña por defecto.");

var servicios = new ServiceCollection();
servicios.AddInfraestructura(config, esDesarrollo: true);
servicios.AddAplicacion();
var contexto = new ContextoDemo();
servicios.AddSingleton<IContextoActual>(contexto);

await using var proveedor = servicios.BuildServiceProvider();
await using var ambito = proveedor.CreateAsyncScope();
var sp = ambito.ServiceProvider;
var db = sp.GetRequiredService<MedicalHubDbContext>();
await db.Database.MigrateAsync();

var existente = await db.Organizaciones.FirstOrDefaultAsync(o => o.Nombre == NombreOrganizacion);

if (banderas.Contains("--eliminar"))
{
    if (existente is null) return Salir("No existe la sociedad demo.", 0);
    await EliminarAsync(db, existente.Id);
    return Salir("Sociedad demo eliminada.", 0);
}

if (existente is not null) return Salir("La sociedad demo ya existe; no se hace nada.", 0);

// 1. Sociedad y administrador (MFA desactivado en la demo para facilitar las pruebas)
var email = (string nombre) => $"{nombre}@{Dominio}";
var org = await sp.GetRequiredService<OrganizacionesServicio>().CrearAsync(new SolicitudCrearOrganizacion(
    NombreOrganizacion, "Europe/Madrid", MfaObligatorio: false, email("admin"), "Admin Demo", contrasena!), default);

var admin = await db.Usuarios.IgnoreQueryFilters().SingleAsync(u => u.Email == email("admin"));
contexto.Entrar(org.Id, admin.Id, Rol.AdministradorSociedad);

var ct = CancellationToken.None;
var catalogos = sp.GetRequiredService<CatalogosServicio>();
async Task<Guid> Cat<T>(string n, string? d = null) where T : EntidadCatalogo, ICreableCatalogo<T>
    => (await catalogos.CrearAsync<T>(new SolicitudCatalogo(n, d), ct)).Id;

// 2. Catálogos
var espAnest = await Cat<Especialidad>("Anestesiología y Reanimación");
var espDolor = await Cat<Especialidad>("Medicina del Dolor");
var catJefe = await Cat<CategoriaProfesional>("Jefe de Servicio");
var catAdjunto = await Cat<CategoriaProfesional>("Facultativo Adjunto");
var catResidente = await Cat<CategoriaProfesional>("Residente");
var compPediatria = await Cat<Competencia>("Anestesia pediátrica");
var compCardiaca = await Cat<Competencia>("Anestesia cardíaca");
var compEco = await Cat<Competencia>("Ecografía regional");
var compViaAerea = await Cat<Competencia>("Vía aérea difícil");

// 3. Usuarios
var usuarios = sp.GetRequiredService<UsuariosServicio>();
async Task<Guid> Usuario(string alias, string nombre, params Rol[] roles)
    => (await usuarios.CrearAsync(new SolicitudCrearUsuario(email(alias), nombre, contrasena!, roles), ct)).Id;

var uCoord = await Usuario("coordinadora", "Marta Coordinadora Demo", Rol.CoordinadorMedico);
var uResp = await Usuario("responsable", "Luis Responsable Demo", Rol.ResponsableCentro);

// 4. Centros y servicios
var centros = sp.GetRequiredService<CentrosServicio>();
var norte = (await centros.CrearAsync(new SolicitudCentro("Hospital Demo Norte", TipoCentro.Hospital, "Calle Demo 1, Madrid", "Europe/Madrid"), ct)).Id;
var sur = (await centros.CrearAsync(new SolicitudCentro("Clínica Demo Sur", TipoCentro.Clinica, "Avenida Demo 2, Madrid", "Europe/Madrid"), ct)).Id;
var islas = (await centros.CrearAsync(new SolicitudCentro("Hospital Demo Canarias", TipoCentro.Hospital, "Calle Demo 3, Las Palmas", "Atlantic/Canary"), ct)).Id;
await centros.EstablecerResponsablesAsync(norte, new SolicitudResponsables([uResp]), ct);

var serv = sp.GetRequiredService<ServiciosServicio>();
async Task<Guid> Servicio(Guid centro, string nombre, string descripcion, Guid[] categorias, Guid[] competencias, string? reglas = null)
    => (await serv.CrearAsync(new SolicitudServicio(centro, nombre, descripcion, uCoord, reglas, categorias, competencias), ct)).Id;
async Task<Guid> Tipo(Guid servicio, string nombre, int ini, int fin, bool guardia)
    => (await serv.CrearTipoTurnoAsync(servicio, new SolicitudTipoTurno(nombre, new TimeOnly(ini, 0), new TimeOnly(fin, 0), guardia), ct)).Id;

var quirofano = await Servicio(norte, "Quirófano general", "Bloque quirúrgico programado", [catJefe, catAdjunto, catResidente], [], """{"maxHorasSemana":48}""");
var reanimacion = await Servicio(norte, "Reanimación", "Unidad de recuperación postanestésica", [catJefe, catAdjunto], [compViaAerea]);
var obstetricia = await Servicio(sur, "Anestesia obstétrica", "Paritorio y cesáreas", [catJefe, catAdjunto], [compPediatria]);
var urgencias = await Servicio(islas, "Urgencias quirúrgicas", "Atención urgente 24 h", [catAdjunto], []);

var qManana = await Tipo(quirofano, "Mañana", 8, 15, false);
var qTarde = await Tipo(quirofano, "Tarde", 15, 22, false);
var rNoche = await Tipo(reanimacion, "Noche", 22, 8, false);
var oGuardia = await Tipo(obstetricia, "Guardia 24 h", 8, 8, true);
var uGuardia = await Tipo(urgencias, "Guardia 24 h", 8, 8, true);

await serv.ReemplazarRequisitosAsync(quirofano, new SolicitudRequisitos([
    new(qManana, 4, null), new(qTarde, 2, null)]), ct);
await serv.ReemplazarRequisitosAsync(reanimacion, new SolicitudRequisitos([new(rNoche, 1, catAdjunto)]), ct);
await serv.ReemplazarRequisitosAsync(obstetricia, new SolicitudRequisitos([new(oGuardia, 2, catAdjunto)]), ct);
await serv.ReemplazarRequisitosAsync(urgencias, new SolicitudRequisitos([new(uGuardia, 1, catAdjunto)]), ct);

// 5. Profesionales (algunos con usuario vinculado; uno con una competencia caducada)
var profesionales = sp.GetRequiredService<ProfesionalesServicio>();
var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
async Task<Guid> Prof(string nombre, string apellidos, Guid categoria, Guid[] esp, (Guid, DateOnly?)[] comp, Guid[] serviciosAut, string? colegiado, string? alias = null)
{
    Guid? usuarioId = alias is null ? null : await Usuario(alias, $"{nombre} {apellidos}", Rol.ProfesionalSanitario);
    return (await profesionales.CrearAsync(new SolicitudProfesional(usuarioId, nombre, apellidos, $"{nombre}.{apellidos}".ToLowerInvariant().Replace(" ", "") + "@" + Dominio,
        null, colegiado, categoria, esp, comp.Select(c => new CompetenciaProfesionalDto(c.Item1, c.Item2)).ToList(), serviciosAut), ct)).Id;
}

var pCarmen = await Prof("Carmen", "Ruiz Demo", catJefe, [espAnest], [(compPediatria, null), (compViaAerea, null), (compEco, null)], [quirofano, reanimacion, obstetricia], "DEMO-001", "carmen");
var pJavier = await Prof("Javier", "Moreno Demo", catAdjunto, [espAnest], [(compViaAerea, null), (compEco, null)], [quirofano, reanimacion], "DEMO-002", "javier");
var pElena = await Prof("Elena", "Santos Demo", catAdjunto, [espAnest], [(compPediatria, hoy.AddYears(1)), (compViaAerea, null)], [quirofano, reanimacion, obstetricia], "DEMO-003", "elena");
var pPablo = await Prof("Pablo", "Iglesias Demo", catAdjunto, [espAnest, espDolor], [(compViaAerea, null)], [quirofano, reanimacion, urgencias], "DEMO-004", "pablo");
await Prof("Lucía", "Navarro Demo", catAdjunto, [espAnest], [(compPediatria, hoy.AddDays(-30)), (compViaAerea, null)], [obstetricia, quirofano], "DEMO-005"); // pediatría CADUCADA
var pSergio = await Prof("Sergio", "Vega Demo", catAdjunto, [espAnest], [(compCardiaca, null), (compViaAerea, null)], [quirofano, reanimacion, urgencias], "DEMO-006");
await Prof("Marina", "Ortega Demo", catAdjunto, [espAnest], [(compPediatria, null), (compViaAerea, null)], [obstetricia, urgencias], "DEMO-007");
await Prof("Andrés", "Cano Demo", catAdjunto, [espDolor], [(compEco, null)], [quirofano], "DEMO-008");
await Prof("Irene", "Prieto Demo", catResidente, [espAnest], [], [quirofano], "DEMO-009");
await Prof("Hugo", "Lara Demo", catResidente, [espAnest], [], [quirofano], "DEMO-010");
var pNuria = await Prof("Nuria", "Gil Demo", catAdjunto, [espAnest], [(compViaAerea, null)], [quirofano, reanimacion], "DEMO-011");
await Prof("Raúl", "Fuentes Demo", catAdjunto, [espAnest], [(compViaAerea, null)], [urgencias, quirofano], "DEMO-012");

// 6. Planificación de la semana próxima (lunes a domingo)
var diasHastaLunes = ((int)DayOfWeek.Monday - (int)hoy.DayOfWeek + 7) % 7;
var lunes = hoy.AddDays(diasHastaLunes == 0 ? 7 : diasHastaLunes); // el próximo lunes
var cuadrantes = sp.GetRequiredService<CuadrantesServicio>();
var calendario = sp.GetRequiredService<CalendarioServicio>();
var asignaciones = sp.GetRequiredService<AsignacionesServicio>();

async Task<Guid> Necesidad(DateOnly dia, Guid servicioId, Guid tipo, int indice = 0)
{
    var turnos = await calendario.ConsultarAsync(new FiltroCalendario(dia, dia, null, servicioId, null, null, null, false), ct);
    return turnos.Single(t => t.TipoTurnoId == tipo).Necesidades[indice].Id;
}

var cuadQuirofano = await cuadrantes.CrearAsync(new SolicitudCuadrante(quirofano, null, lunes, lunes.AddDays(6), true), ct);
var cuadReanimacion = await cuadrantes.CrearAsync(new SolicitudCuadrante(reanimacion, null, lunes, lunes.AddDays(6), true), ct);

for (var d = 0; d < 7; d++)
{
    var dia = lunes.AddDays(d);
    var laborable = d < 5;
    var manana = await Necesidad(dia, quirofano, qManana);
    var tarde = await Necesidad(dia, quirofano, qTarde);
    // Quirófano: mañana con 3 de 4 plazas (queda una vacante) y tarde completa; fines de semana sin cubrir
    if (laborable)
    {
        foreach (var p in new[] { pCarmen, pJavier, pElena }) await asignaciones.AsignarAsync(manana, p, null, ct);
        foreach (var p in new[] { pPablo, pSergio }) await asignaciones.AsignarAsync(tarde, p, null, ct);
        // Reanimación: noches de lunes a viernes cubiertas; fin de semana sin cubrir
        await asignaciones.AsignarAsync(await Necesidad(dia, reanimacion, rNoche), pNuria, null, ct);
    }
}
// 6b. Ausencias y disponibilidad de ejemplo
var ausenciasServicio = sp.GetRequiredService<AusenciasServicio>();
var disponibilidadServicio = sp.GetRequiredService<DisponibilidadServicio>();
// Elena: baja aprobada el jueves → su plaza de Quirófano queda como vacante abierta
await ausenciasServicio.RegistrarAsync(new SolicitudAusenciaDeProfesional(pElena, TipoAusencia.BajaTemporal, lunes.AddDays(3), lunes.AddDays(3), null, true), ct);
// Carmen: vacaciones solicitadas para la semana siguiente (pendientes de decisión)
await ausenciasServicio.RegistrarAsync(new SolicitudAusenciaDeProfesional(pCarmen, TipoAusencia.Vacaciones, lunes.AddDays(7), lunes.AddDays(11), null, false), ct);
// Pablo declara un bloqueo (pendiente de aprobación); Javier prefiere evitar el viernes (aprobado)
await disponibilidadServicio.RegistrarAsync(new SolicitudDisponibilidadDeProfesional(pPablo, TipoDisponibilidad.Bloqueo, lunes.AddDays(7), lunes.AddDays(8), null, null, "Curso externo", false), ct);
await disponibilidadServicio.RegistrarAsync(new SolicitudDisponibilidadDeProfesional(pJavier, TipoDisponibilidad.Evita, lunes.AddDays(4), lunes.AddDays(4), null, null, null, true), ct);

// 7. Reglas de EJEMPLO (los valores son ilustrativos: la sociedad debe sustituirlos por los de su convenio y revisarlos)
var reglasServicio = sp.GetRequiredService<ReglasServicio>();
const string aviso = "Ejemplo de la demostración. No es una referencia legal: sustituir por lo que establezca el convenio o acuerdo aplicable.";
await reglasServicio.CrearAsync(new SolicitudRegla(TipoRegla.DescansoMinimo, "Descanso mínimo (ejemplo demo)", null, null, SeveridadRegla.Bloqueante, true, true,
    System.Text.Json.JsonDocument.Parse("""{"horas":12}""").RootElement.Clone(), aviso), ct);
await reglasServicio.CrearAsync(new SolicitudRegla(TipoRegla.MaxHorasPeriodo, "Máximo de horas en 7 días (ejemplo demo)", null, null, SeveridadRegla.Advertencia, false, false,
    System.Text.Json.JsonDocument.Parse("""{"ventanaDias":7,"maxHoras":40}""").RootElement.Clone(), aviso), ct);
await reglasServicio.CrearAsync(new SolicitudRegla(TipoRegla.CoberturaMinima, "Cobertura mínima del 75 % (ejemplo demo)", null, quirofano, SeveridadRegla.Advertencia, false, false,
    System.Text.Json.JsonDocument.Parse("""{"porcentajeMinimo":75}""").RootElement.Clone(), aviso), ct);

// La planificación de Reanimación se publica; la de Quirófano queda en borrador para poder editarla
await cuadrantes.PublicarAsync(cuadReanimacion.Id, ct);

Console.WriteLine($"""
    Sociedad demo creada: {NombreOrganizacion}
      Planificación:  semana del {lunes:dd/MM/yyyy} (Reanimación publicada; Quirófano en borrador con vacantes)
      Administrador:  {email("admin")}
      Coordinadora:   {email("coordinadora")}
      Responsable:    {email("responsable")}   (centro Hospital Demo Norte)
      Profesionales:  carmen@, javier@, elena@, pablo@{Dominio}
      Contraseña:     la indicada en DEMO_CONTRASENA (MFA desactivado en esta sociedad)
      Para eliminarla:  --confirmar --eliminar
    """);
return 0;

static int Salir(string mensaje, int codigo = 1)
{
    Console.Error.WriteLine(mensaje);
    return codigo;
}

static async Task EliminarAsync(MedicalHubDbContext db, Guid orgId)
{
    // Orden inverso a las dependencias. Solo afecta a la organización indicada.
    string[] borrados =
    [
        "DELETE FROM Vacantes WHERE OrganizacionId=@o",
        "DELETE FROM DecisionesAusencia WHERE OrganizacionId=@o",
        "DELETE FROM Ausencias WHERE OrganizacionId=@o",
        "DELETE FROM Disponibilidades WHERE OrganizacionId=@o",
        "DELETE FROM ExcepcionesRegla WHERE OrganizacionId=@o",
        "DELETE FROM ReglasPlanificacion WHERE OrganizacionId=@o",
        "DELETE FROM Asignaciones WHERE OrganizacionId=@o",
        "DELETE FROM NecesidadesCobertura WHERE OrganizacionId=@o",
        "DELETE FROM VersionesCuadrante WHERE OrganizacionId=@o",
        "DELETE FROM Turnos WHERE OrganizacionId=@o",
        "DELETE FROM Cuadrantes WHERE OrganizacionId=@o",
        "DELETE FROM PlantillaLineas WHERE PlantillaId IN (SELECT Id FROM PlantillasCuadrante WHERE OrganizacionId=@o)",
        "DELETE FROM PlantillasCuadrante WHERE OrganizacionId=@o",
        "DELETE FROM ProfesionalServicios WHERE ProfesionalId IN (SELECT Id FROM Profesionales WHERE OrganizacionId=@o)",
        "DELETE FROM ProfesionalCompetencias WHERE ProfesionalId IN (SELECT Id FROM Profesionales WHERE OrganizacionId=@o)",
        "DELETE FROM ProfesionalEspecialidades WHERE ProfesionalId IN (SELECT Id FROM Profesionales WHERE OrganizacionId=@o)",
        "DELETE FROM Profesionales WHERE OrganizacionId=@o",
        "DELETE FROM RequisitosCobertura WHERE OrganizacionId=@o",
        "DELETE FROM TiposTurno WHERE OrganizacionId=@o",
        "DELETE FROM ServicioCompetencias WHERE ServicioId IN (SELECT Id FROM Servicios WHERE OrganizacionId=@o)",
        "DELETE FROM ServicioCategorias WHERE ServicioId IN (SELECT Id FROM Servicios WHERE OrganizacionId=@o)",
        "DELETE FROM Servicios WHERE OrganizacionId=@o",
        "DELETE FROM CentroResponsables WHERE CentroId IN (SELECT Id FROM Centros WHERE OrganizacionId=@o)",
        "DELETE FROM Centros WHERE OrganizacionId=@o",
        "DELETE FROM Especialidades WHERE OrganizacionId=@o",
        "DELETE FROM CategoriasProfesionales WHERE OrganizacionId=@o",
        "DELETE FROM Competencias WHERE OrganizacionId=@o",
        "DELETE FROM TokensRefresco WHERE UsuarioId IN (SELECT Id FROM Usuarios WHERE OrganizacionId=@o)",
        "DELETE FROM UsuarioRoles WHERE UsuarioId IN (SELECT Id FROM Usuarios WHERE OrganizacionId=@o)",
        "DELETE FROM RegistrosAuditoria WHERE OrganizacionId=@o",
        "DELETE FROM EventosSeguridad WHERE OrganizacionId=@o",
        "DELETE FROM Usuarios WHERE OrganizacionId=@o",
        "DELETE FROM Organizaciones WHERE Id=@o",
    ];
    await using var tx = await db.Database.BeginTransactionAsync();
    foreach (var sql in borrados)
        await db.Database.ExecuteSqlRawAsync(sql, new Microsoft.Data.SqlClient.SqlParameter("@o", orgId));
    await tx.CommitAsync();
}

/// <summary>Contexto de ejecución de la herramienta: actúa como el administrador de la sociedad demo.</summary>
sealed class ContextoDemo : IContextoActual
{
    public bool EstaAutenticado => OrganizacionId is not null;
    public Guid? UsuarioId { get; private set; }
    public Guid? OrganizacionId { get; private set; }
    public IReadOnlySet<Rol> Roles { get; private set; } = new HashSet<Rol>();
    public bool MfaPendiente => false;
    public string? Ip => "herramienta-demo";
    public string? IdCorrelacion => "datos-demo";

    public void Entrar(Guid organizacionId, Guid usuarioId, Rol rol)
    {
        OrganizacionId = organizacionId;
        UsuarioId = usuarioId;
        Roles = new HashSet<Rol> { rol };
    }
}
