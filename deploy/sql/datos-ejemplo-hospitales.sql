-- Hospitales, servicios y turnos de EJEMPLO para MedicalHub (solo desarrollo).
-- Sociedad: «MedicalHub Demo». Crea 2 hospitales con los servicios de Urgencias (Mañana / Tarde / Noche) y Quirófano (Mañana / Tarde),
-- y autoriza en ellos a los profesionales demo (medico@ y enfermeria@medicalhub.local) para que puedan declarar disponibilidad.
-- Idempotente: se puede ejecutar varias veces; no toca lo que ya exista con el mismo nombre.
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @OrgId uniqueidentifier = (SELECT Id FROM Organizaciones WHERE Nombre = N'MedicalHub Demo');
IF @OrgId IS NULL THROW 50000, 'No existe la sociedad MedicalHub Demo: ejecute antes crear-usuario-medico.sql.', 1;

DECLARE @Hospitales TABLE (Nombre nvarchar(200), Direccion nvarchar(300));
INSERT INTO @Hospitales VALUES (N'Hospital Universitario Norte', N'Calle Mayor 1, Madrid'), (N'Hospital General Sur', N'Avenida del Sur 25, Madrid');

DECLARE @Turnos TABLE (Servicio nvarchar(200), Nombre nvarchar(100), Inicio time, Fin time, Guardia bit);
INSERT INTO @Turnos VALUES
    (N'Urgencias', N'Mañana', '08:00', '15:00', 0), (N'Urgencias', N'Tarde', '15:00', '22:00', 0), (N'Urgencias', N'Noche', '22:00', '08:00', 1),
    (N'Quirófano', N'Mañana', '08:00', '15:00', 0), (N'Quirófano', N'Tarde', '15:00', '22:00', 0);

DECLARE @Servicios TABLE (Nombre nvarchar(200), Descripcion nvarchar(500));
INSERT INTO @Servicios VALUES (N'Urgencias', N'Servicio de urgencias'), (N'Quirófano', N'Bloque quirúrgico');

-- Centros
INSERT INTO Centros (Id, OrganizacionId, Nombre, Tipo, Direccion, ZonaHoraria, Activo, FechaCreacion)
SELECT NEWID(), @OrgId, h.Nombre, N'Hospital', h.Direccion, N'Europe/Madrid', 1, SYSDATETIMEOFFSET()
FROM @Hospitales h
WHERE NOT EXISTS (SELECT 1 FROM Centros c WHERE c.OrganizacionId = @OrgId AND c.Nombre = h.Nombre);

-- Servicios en cada hospital
INSERT INTO Servicios (Id, OrganizacionId, CentroId, Nombre, Descripcion, Activo, FechaCreacion)
SELECT NEWID(), @OrgId, c.Id, sv.Nombre, sv.Descripcion, 1, SYSDATETIMEOFFSET()
FROM Centros c
JOIN @Hospitales h ON h.Nombre = c.Nombre
CROSS JOIN @Servicios sv
WHERE c.OrganizacionId = @OrgId
  AND NOT EXISTS (SELECT 1 FROM Servicios s WHERE s.CentroId = c.Id AND s.Nombre = sv.Nombre);

-- Tipos de turno de cada servicio
INSERT INTO TiposTurno (Id, OrganizacionId, ServicioId, Nombre, HoraInicio, HoraFin, EsGuardia, Activo)
SELECT NEWID(), @OrgId, s.Id, t.Nombre, t.Inicio, t.Fin, t.Guardia, 1
FROM Servicios s
JOIN Centros c ON c.Id = s.CentroId
JOIN @Hospitales h ON h.Nombre = c.Nombre
JOIN @Turnos t ON t.Servicio = s.Nombre
WHERE c.OrganizacionId = @OrgId
  AND NOT EXISTS (SELECT 1 FROM TiposTurno x WHERE x.ServicioId = s.Id AND x.Nombre = t.Nombre);

-- Autorización de los profesionales demo en esos servicios
INSERT INTO ProfesionalServicios (ServicioId, ProfesionalId)
SELECT s.Id, p.Id
FROM Servicios s
JOIN Centros c ON c.Id = s.CentroId
JOIN @Hospitales h ON h.Nombre = c.Nombre
JOIN @Servicios sv ON sv.Nombre = s.Nombre
CROSS JOIN Profesionales p
WHERE c.OrganizacionId = @OrgId AND p.OrganizacionId = @OrgId
  AND NOT EXISTS (SELECT 1 FROM ProfesionalServicios ps WHERE ps.ServicioId = s.Id AND ps.ProfesionalId = p.Id);


-- Topes de cobertura de ejemplo por tipo de turno: cuántos médicos y cuánta enfermería hacen falta (es lo que genera los turnos del cuadrante
-- y lo que pinta el semáforo: verde = en su tope, amarillo = faltan, rojo = se pasa). Se editan en el servicio → «Requisitos de cobertura».
DECLARE @Medico uniqueidentifier = (SELECT Id FROM CategoriasProfesionales WHERE OrganizacionId = @OrgId AND Nombre = N'Médico');
DECLARE @Enfermeria uniqueidentifier = (SELECT Id FROM CategoriasProfesionales WHERE OrganizacionId = @OrgId AND Nombre = N'Enfermería');

IF @Medico IS NOT NULL AND @Enfermeria IS NOT NULL
BEGIN
    DECLARE @Topes TABLE (Servicio nvarchar(200), Turno nvarchar(100), Medicos int, Enfermeria int);
    INSERT INTO @Topes VALUES (N'Urgencias', N'Mañana', 4, 3), (N'Urgencias', N'Tarde', 3, 2), (N'Urgencias', N'Noche', 2, 2),
                              (N'Quirófano', N'Mañana', 4, 3), (N'Quirófano', N'Tarde', 3, 2);

    INSERT INTO RequisitosCobertura (Id, OrganizacionId, ServicioId, TipoTurnoId, NumeroProfesionales, CategoriaProfesionalId)
    SELECT NEWID(), @OrgId, s.Id, t.Id, x.Cantidad, x.Categoria
    FROM Servicios s
    JOIN Centros c ON c.Id = s.CentroId
    JOIN @Hospitales h ON h.Nombre = c.Nombre
    JOIN TiposTurno t ON t.ServicioId = s.Id
    JOIN @Topes tp ON tp.Servicio = s.Nombre AND tp.Turno = t.Nombre
    CROSS APPLY (VALUES (tp.Medicos, @Medico), (tp.Enfermeria, @Enfermeria)) x(Cantidad, Categoria)
    WHERE c.OrganizacionId = @OrgId
      AND NOT EXISTS (SELECT 1 FROM RequisitosCobertura r WHERE r.TipoTurnoId = t.Id AND r.CategoriaProfesionalId = x.Categoria);
END;

COMMIT;
