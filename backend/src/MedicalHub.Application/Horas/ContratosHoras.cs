using MedicalHub.Domain.Planificacion;

namespace MedicalHub.Application.Horas;

public sealed record AjusteHorasDto(int Minutos, string Motivo, string Usuario, DateTimeOffset Fecha);

/// <summary>Una asignación con sus horas planificadas y, si existe, registradas. Los minutos evitan errores de redondeo.</summary>
public sealed record LineaHorasDto(Guid AsignacionId, Guid ProfesionalId, string Profesional, DateOnly Fecha, string Turno, string Servicio, string Centro,
    bool EsGuardia, DateTimeOffset InicioUtc, DateTimeOffset FinUtc, int PlanificadasMin, int NocturnasPlanMin, Guid? RegistroId, DateTimeOffset? InicioRealUtc,
    DateTimeOffset? FinRealUtc, int? RealesMin, int? NocturnasRealMin, int AjustesMin, int? EfectivasMin, IReadOnlyList<AjusteHorasDto> Ajustes);

public sealed record SolicitudRegistroHoras(DateTimeOffset InicioUtc, DateTimeOffset FinUtc);

public sealed record SolicitudAjusteHoras(int Minutos, string Motivo);

public sealed record FiltroHoras(DateOnly Desde, DateOnly Hasta, Guid? ProfesionalId = null, Guid? ServicioId = null, Guid? CentroId = null);

// ── Informes ──
public sealed record FilaInformeHorasDto(Guid ProfesionalId, string Profesional, int Turnos, int Guardias, int PlanificadasMin, int NocturnasPlanMin,
    int TurnosRegistrados, int RealesMin, int NocturnasRealMin, int AjustesMin, int EfectivasMin);

public sealed record InformeHorasDto(DateOnly Desde, DateOnly Hasta, IReadOnlyList<FilaInformeHorasDto> Filas, FilaInformeHorasDto Total);

public sealed record FilaInformeCoberturaDto(Guid ServicioId, string Servicio, string Centro, int Plazas, int Cubiertas, int VacantesAbiertas, int Excepciones)
{
    public int Libres => Plazas - Cubiertas;
    public double PorcentajeCobertura => Plazas == 0 ? 100 : Math.Round(100.0 * Cubiertas / Plazas, 1);
}

public sealed record InformeCoberturaDto(DateOnly Desde, DateOnly Hasta, IReadOnlyList<FilaInformeCoberturaDto> Filas, FilaInformeCoberturaDto Total);

// ── Dashboard ──
public sealed record TurnoSinCubrirDto(Guid TurnoId, DateOnly Fecha, string Turno, string Servicio, string Centro, int PlazasLibres);

public sealed record ProximoTurnoDto(Guid AsignacionId, DateOnly Fecha, string Turno, string Servicio, string Centro, DateTimeOffset InicioUtc, DateTimeOffset FinUtc,
    EstadoCuadrante CuadranteEstado);

/// <param name="Pendientes">Solo para administración y coordinación; null para el responsable de centro.</param>
/// <summary>Verde = justo las plazas previstas, amarillo = faltan profesionales, rojo = hay más de los permitidos.</summary>
public enum EstadoSemaforo { Verde = 1, Amarillo = 2, Rojo = 3 }

public sealed record SemaforoCoberturaDto(int Verde, int Amarillo, int Rojo);

/// <param name="Detalle">Por categoría, asignados/plazas (p. ej. «Anestesiología 5/4 · Enfermería 3/3»).</param>
public sealed record TurnoSemaforoDto(Guid TurnoId, DateOnly Fecha, string Turno, string Servicio, string Centro, EstadoSemaforo Estado, string Detalle);

public sealed record DashboardGestionDto(DateOnly Desde, DateOnly Hasta, int PlazasTotales, int PlazasCubiertas, int CuadrantesBorrador,
    IReadOnlyList<TurnoSinCubrirDto> ProximosSinCubrir, PendientesGestionDto? Pendientes,
    SemaforoCoberturaDto? Semaforo = null, IReadOnlyList<TurnoSemaforoDto>? Atencion = null);

public sealed record PendientesGestionDto(int VacantesAbiertas, int AusenciasSolicitadas, int OfertasPorAprobar, int DisponibilidadesPendientes, int ExcepcionesUltimos30Dias);

public sealed record DashboardProfesionalDto(IReadOnlyList<ProximoTurnoDto> ProximosTurnos, int HorasPlanificadasMesMin, int OfertasPendientes, int AusenciasSolicitadas);

/// <summary>Cada bloque aparece solo si el rol del usuario lo permite (y el de profesional, si tiene ficha vinculada).</summary>
public sealed record DashboardDto(DashboardGestionDto? Gestion, DashboardProfesionalDto? Profesional);
