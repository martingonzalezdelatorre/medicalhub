using MedicalHub.Application.Comun;
using MedicalHub.Domain.Ausencias;
using MedicalHub.Domain.Profesionales;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Ausencias;

/// <summary>
/// Disponibilidad declarada por el profesional (disponible, bloqueo o preferencias). Solo lo APROBADO por el coordinador
/// se tiene en cuenta en las reglas; una preferencia no es una prohibición salvo que la sociedad lo configure.
/// </summary>
public sealed class DisponibilidadServicio(IAppDbContext db, IContextoActual contexto, TimeProvider reloj, Planificacion.AutoAsignacionServicio autoAsignacion)
{
    public const int MaxBloques = 50;

    // ───────────── Del propio profesional ─────────────

    public async Task<IReadOnlyList<DisponibilidadDto>> MiDisponibilidadAsync(CancellationToken ct)
    {
        var p = await ProfesionalPropioAsync(ct);
        var lista = await db.Disponibilidades.Where(d => d.ProfesionalId == p.Id).OrderByDescending(d => d.Desde).Take(300).ToListAsync(ct);
        return await ComponerAsync(lista, ct);
    }

    public async Task<DisponibilidadDto> DeclararAsync(SolicitudDisponibilidad s, CancellationToken ct)
    {
        var p = await ProfesionalPropioAsync(ct);
        return (await CrearAsync(p, [s], aprobar: false, ct))[0];
    }

    /// <summary>Declara varios bloques (hospital · días · turno) a la vez.</summary>
    public async Task<IReadOnlyList<DisponibilidadDto>> DeclararLoteAsync(SolicitudDisponibilidadLote s, CancellationToken ct)
    {
        var p = await ProfesionalPropioAsync(ct);
        return await CrearAsync(p, s.Bloques, aprobar: false, ct);
    }

    /// <summary>El profesional retira una declaración suya que aún no está aprobada. Las aprobadas las revoca el coordinador.</summary>
    public async Task EliminarPropiaAsync(Guid id, CancellationToken ct)
    {
        var p = await ProfesionalPropioAsync(ct);
        var d = await db.Disponibilidades.FirstOrDefaultAsync(x => x.Id == id && x.ProfesionalId == p.Id, ct) ?? throw new ExcepcionNoEncontrado("Declaración no encontrada.");
        if (d.Estado == EstadoDisponibilidad.Aprobada)
            throw new ExcepcionConflicto("Esta declaración ya está aprobada: solicite a su coordinador que la revoque.");
        db.Disponibilidades.Remove(d);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Tipos de turno de los servicios donde el profesional está autorizado (para indicar preferencias por turno).</summary>
    public async Task<IReadOnlyList<TipoTurnoPropioDto>> MisTiposTurnoAsync(CancellationToken ct)
    {
        var p = await ProfesionalPropioAsync(ct);
        var servicios = p.ServiciosAutorizados.Select(s => s.ServicioId).ToList();
        var filas = await (from t in db.TiposTurno where servicios.Contains(t.ServicioId) && t.Activo
                           join s in db.Servicios on t.ServicioId equals s.Id
                           join c in db.Centros on s.CentroId equals c.Id
                           orderby c.Nombre, s.Nombre, t.HoraInicio
                           select new { t.Id, t.Nombre, t.HoraInicio, t.HoraFin, Servicio = s.Nombre, Centro = c.Nombre, CentroId = c.Id }).ToListAsync(ct);
        return filas.Select(f => new TipoTurnoPropioDto(f.Id, f.Nombre, $"{f.HoraInicio:HH\\:mm}–{f.HoraFin:HH\\:mm}", f.Servicio, f.Centro, f.CentroId)).ToList();
    }

    // ───────────── Gestión (administrador y coordinador) ─────────────

    public async Task<IReadOnlyList<DisponibilidadDto>> ListarAsync(EstadoDisponibilidad? estado, Guid? profesionalId, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var q = db.Disponibilidades.AsQueryable();
        if (estado is { } e) q = q.Where(d => d.Estado == e);
        if (profesionalId is { } p) q = q.Where(d => d.ProfesionalId == p);
        var lista = await q.OrderBy(d => d.Estado == EstadoDisponibilidad.Declarada ? 0 : 1).ThenByDescending(d => d.FechaDeclaracion).Take(300).ToListAsync(ct);
        return await ComponerAsync(lista, ct);
    }

    public async Task<DisponibilidadDto> RegistrarAsync(SolicitudDisponibilidadDeProfesional s, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var p = await db.Profesionales.FirstOrDefaultAsync(x => x.Id == s.ProfesionalId, ct) ?? throw new ExcepcionNoEncontrado("Profesional no encontrado.");
        return (await CrearAsync(p, [new SolicitudDisponibilidad(s.Tipo, s.Desde, s.Hasta, s.Dias, s.TipoTurnoId, s.Comentario, s.CentroId)], s.Aprobar, ct))[0];
    }

    public Task<DisponibilidadDto> AprobarAsync(Guid id, string? motivo, CancellationToken ct) => DecidirAsync(id, true, motivo, ct);

    public Task<DisponibilidadDto> RechazarAsync(Guid id, string? motivo, CancellationToken ct) => DecidirAsync(id, false, motivo, ct);

    /// <summary>Elimina una declaración en cualquier estado (queda en la auditoría).</summary>
    public async Task EliminarAsync(Guid id, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var d = await db.Disponibilidades.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new ExcepcionNoEncontrado("Declaración no encontrada.");
        db.Disponibilidades.Remove(d);
        await db.SaveChangesAsync(ct);
    }

    // ───────────── Internos ─────────────

    private async Task<DisponibilidadDto> DecidirAsync(Guid id, bool aprobar, string? motivo, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
        var d = await db.Disponibilidades.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new ExcepcionNoEncontrado("Declaración no encontrada.");
        if (d.Estado != EstadoDisponibilidad.Declarada) throw new ExcepcionConflicto("La declaración ya fue resuelta.");
        if (aprobar) d.Aprobar(uid, reloj.GetUtcNow(), motivo); else d.Rechazar(uid, reloj.GetUtcNow(), motivo);
        await db.SaveChangesAsync(ct);
        var asignados = aprobar ? await AsignarSiOfreceAsync(d, ct) : 0;
        return (await ComponerAsync([d], ct)).Single() with { TurnosAsignados = asignados };
    }

    /// <summary>Crea todos los bloques en una sola operación: si alguno no es válido no se guarda ninguno.</summary>
    private async Task<IReadOnlyList<DisponibilidadDto>> CrearAsync(Profesional p, IReadOnlyList<SolicitudDisponibilidad> bloques, bool aprobar, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
        if (bloques.Count == 0)
            throw new ExcepcionValidacion(new Dictionary<string, string[]> { ["bloques"] = ["Indique al menos un bloque de disponibilidad."] });
        if (bloques.Count > MaxBloques)
            throw new ExcepcionValidacion(new Dictionary<string, string[]> { ["bloques"] = [$"No se pueden declarar más de {MaxBloques} bloques a la vez."] });

        var autorizados = p.ServiciosAutorizados.Select(s => s.ServicioId).ToList();
        var ahora = reloj.GetUtcNow();
        var creadas = new List<Disponibilidad>();
        var horarios = new Dictionary<Guid, (TimeOnly Inicio, TimeOnly Fin)>();
        foreach (var b in bloques)
        {
            var centroId = b.CentroId;
            if (b.TipoTurnoId is { } tt)
            {
                var tipo = await db.TiposTurno.AsNoTracking().Where(t => t.Id == tt && autorizados.Contains(t.ServicioId))
                    .Join(db.Servicios, t => t.ServicioId, s => s.Id, (t, s) => new { s.CentroId, t.HoraInicio, t.HoraFin }).FirstOrDefaultAsync(ct)
                    ?? throw new ExcepcionValidacion(new Dictionary<string, string[]> { ["tipoTurnoId"] = ["El tipo de turno no existe o pertenece a un servicio donde no está autorizado."] });
                if (centroId is not null && centroId != tipo.CentroId)
                    throw new ExcepcionValidacion(new Dictionary<string, string[]> { ["centroId"] = ["El turno elegido no pertenece al hospital indicado."] });
                centroId = tipo.CentroId;
                horarios[tt] = (tipo.HoraInicio, tipo.HoraFin);
            }
            else if (centroId is { } c
                     && !await db.Servicios.AnyAsync(s => s.CentroId == c && autorizados.Contains(s.Id), ct))
                throw new ExcepcionValidacion(new Dictionary<string, string[]> { ["centroId"] = ["El hospital no existe o no está autorizado en ninguno de sus servicios."] });

            var d = new Disponibilidad(org, p.Id, b.Tipo, b.Desde, b.Hasta, b.Dias ?? DiasSemana.Todos, b.TipoTurnoId, b.Comentario, uid, ahora, centroId);
            if (aprobar) d.Aprobar(uid, ahora, null);
            creadas.Add(d);
        }

        await ExigirSinSolapesAsync(p.Id, creadas, horarios, ct);

        db.Disponibilidades.AddRange(creadas);
        await db.SaveChangesAsync(ct);
        if (aprobar) foreach (var d in creadas) await AsignarSiOfreceAsync(d, ct);
        return await ComponerAsync(creadas, ct);
    }

    /// <summary>
    /// Un mismo día, hospital y horario solo puede declararse una vez (se ignoran las rechazadas). «Cualquier hospital» o
    /// «todos los turnos» abarcan todos los demás. Los bloques de un mismo envío que solo difieren en el servicio del turno
    /// (igual horario y días) son la misma elección del usuario y no se consideran solape.
    /// </summary>
    private async Task ExigirSinSolapesAsync(Guid profesionalId, List<Disponibilidad> nuevas, Dictionary<Guid, (TimeOnly Inicio, TimeOnly Fin)> horarios, CancellationToken ct)
    {
        var desde = nuevas.Min(n => n.Desde);
        var hasta = nuevas.Max(n => n.Hasta);
        var existentes = await db.Disponibilidades.AsNoTracking()
            .Where(x => x.ProfesionalId == profesionalId && x.Estado != EstadoDisponibilidad.Rechazada && x.Desde <= hasta && x.Hasta >= desde)
            .ToListAsync(ct);

        var faltan = existentes.Where(x => x.TipoTurnoId is { } t && !horarios.ContainsKey(t)).Select(x => x.TipoTurnoId!.Value).Distinct().ToList();
        if (faltan.Count > 0)
            foreach (var t in await db.TiposTurno.AsNoTracking().Where(t => faltan.Contains(t.Id)).Select(t => new { t.Id, t.HoraInicio, t.HoraFin }).ToListAsync(ct))
                horarios[t.Id] = (t.HoraInicio, t.HoraFin);

        for (var i = 0; i < nuevas.Count; i++)
        {
            var n = nuevas[i];
            foreach (var o in existentes)
                if (Solapan(n, o, horarios, mismaEleccion: false))
                    throw Ocupado(n, o);
            for (var j = 0; j < i; j++)
                if (Solapan(n, nuevas[j], horarios, mismaEleccion: true))
                    throw Ocupado(n, nuevas[j]);
        }
    }

    private static ExcepcionValidacion Ocupado(Disponibilidad n, Disponibilidad o)
    {
        var ini = n.Desde > o.Desde ? n.Desde : o.Desde;
        return new ExcepcionValidacion(new Dictionary<string, string[]>
        {
            ["bloques"] = [$"Ya tiene ocupado ese turno: existe una declaración que coincide a partir del {ini:dd/MM/yyyy}. Retírela antes de volver a declararlo."],
        });
    }

    private static bool Solapan(Disponibilidad a, Disponibilidad b, Dictionary<Guid, (TimeOnly Inicio, TimeOnly Fin)> horarios, bool mismaEleccion)
    {
        if (a.CentroId is { } ca && b.CentroId is { } cb && ca != cb) return false;

        var ini = a.Desde > b.Desde ? a.Desde : b.Desde;
        var fin = a.Hasta < b.Hasta ? a.Hasta : b.Hasta;
        var hayDia = false;
        for (var f = ini; f <= fin; f = f.AddDays(1))
        {
            var bit = DiasSemana.Bit(f.DayOfWeek);
            if ((a.Dias & bit) != 0 && (b.Dias & bit) != 0) { hayDia = true; break; }
        }
        if (!hayDia) return false;

        if (a.TipoTurnoId is not { } ta || b.TipoTurnoId is not { } tb)
            return !(mismaEleccion && a.TipoTurnoId is null && b.TipoTurnoId is null && a.CentroId != b.CentroId);
        if (ta == tb) return true;
        var (ha, hb) = (horarios[ta], horarios[tb]);
        if (mismaEleccion && a.Dias == b.Dias && a.CentroId == b.CentroId && ha == hb) return false;
        return HorasSolapan(ha, hb);
    }

    private static bool HorasSolapan((TimeOnly Inicio, TimeOnly Fin) a, (TimeOnly Inicio, TimeOnly Fin) b)
    {
        static (int Ini, int Fin) Min((TimeOnly Inicio, TimeOnly Fin) h)
        {
            var i = h.Inicio.Hour * 60 + h.Inicio.Minute;
            var f = h.Fin.Hour * 60 + h.Fin.Minute;
            return (i, f <= i ? f + 1440 : f);
        }
        var (x, y) = (Min(a), Min(b));
        foreach (var desfase in new[] { -1440, 0, 1440 })
            if (x.Ini < y.Fin + desfase && y.Ini + desfase < x.Fin) return true;
        return false;
    }

    /// <summary>Una disponibilidad «Disponible» aprobada coloca al profesional en los turnos ya creados (cuadrantes en borrador) que le encajan.</summary>
    private async Task<int> AsignarSiOfreceAsync(Disponibilidad d, CancellationToken ct)
        => d.Tipo == TipoDisponibilidad.Disponible ? await autoAsignacion.AsignarProfesionalAsync(d.ProfesionalId, d.Desde, d.Hasta, ct) : 0;

    private async Task<Profesional> ProfesionalPropioAsync(CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
        return await db.Profesionales.FirstOrDefaultAsync(p => p.UsuarioId == uid, ct)
            ?? throw new ExcepcionNoEncontrado("Su usuario no está vinculado a ninguna ficha profesional.");
    }

    private async Task<IReadOnlyList<DisponibilidadDto>> ComponerAsync(List<Disponibilidad> lista, CancellationToken ct)
    {
        if (lista.Count == 0) return [];
        var profIds = lista.Select(d => d.ProfesionalId).Distinct().ToList();
        var profesionales = await db.Profesionales.Where(p => profIds.Contains(p.Id)).Select(p => new { p.Id, p.Nombre, p.Apellidos })
            .ToDictionaryAsync(p => p.Id, p => $"{p.Nombre} {p.Apellidos}", ct);
        var tipoIds = lista.Where(d => d.TipoTurnoId != null).Select(d => d.TipoTurnoId!.Value).Distinct().ToList();
        var tipos = await db.TiposTurno.Where(t => tipoIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Nombre, ct);
        var centroIds = lista.Where(d => d.CentroId != null).Select(d => d.CentroId!.Value).Distinct().ToList();
        var centros = await db.Centros.Where(c => centroIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Nombre, ct);

        return lista.Select(d => new DisponibilidadDto(d.Id, d.ProfesionalId, profesionales.GetValueOrDefault(d.ProfesionalId, "—"), d.Tipo, d.Desde, d.Hasta,
            d.Dias, d.TipoTurnoId, d.TipoTurnoId is { } t ? tipos.GetValueOrDefault(t) : null, d.Comentario, d.Estado, d.FechaDeclaracion, d.MotivoDecision,
            d.CentroId, d.CentroId is { } c ? centros.GetValueOrDefault(c) : null)).ToList();
    }
}
