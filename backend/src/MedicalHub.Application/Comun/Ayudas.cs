using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Identidad;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Comun;

public static class AyudasAplicacion
{
    /// <summary>La organización siempre sale del token. Los usuarios de plataforma no operan sobre datos de sociedades.</summary>
    public static Guid ExigirOrganizacion(this IContextoActual contexto)
        => contexto.OrganizacionId ?? throw new ExcepcionProhibido("Esta operación requiere pertenecer a una sociedad.");

    /// <summary>Un responsable de centro (sin ser administrador ni coordinador) solo ve sus centros.</summary>
    public static bool EsAlcanceRestringido(this IContextoActual c)
        => c.TieneRol(Rol.ResponsableCentro) && !c.TieneRol(Rol.AdministradorSociedad) && !c.TieneRol(Rol.CoordinadorMedico);

    /// <summary>Comprueba que todos los ids existen en la organización (la consulta ya viene filtrada por sociedad).</summary>
    public static async Task ExigirExistentesAsync<T>(this IQueryable<T> consulta, IEnumerable<Guid>? ids, string campo,
        Validador v, CancellationToken ct) where T : Entidad
    {
        var distintos = ids?.Distinct().ToList() ?? [];
        if (distintos.Count == 0) return;
        var existentes = await consulta.CountAsync(x => distintos.Contains(x.Id), ct);
        if (existentes != distintos.Count) v.Agregar(campo, "Alguno de los elementos indicados no existe.");
    }
}
