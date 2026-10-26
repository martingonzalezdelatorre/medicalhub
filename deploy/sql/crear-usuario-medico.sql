-- Usuario médico (rol ProfesionalSanitario) para MedicalHub (solo desarrollo).
-- Correo:     medico@medicalhub.local
-- Contraseña: MedicalHub-Dev-2026!   (cámbiela tras el primer acceso)
-- Idempotente: se puede ejecutar varias veces.
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @OrgId uniqueidentifier = (SELECT Id FROM Organizaciones WHERE Nombre = N'MedicalHub Demo');
IF @OrgId IS NULL
BEGIN
    SET @OrgId = NEWID();
    INSERT INTO Organizaciones (Id, Nombre, ZonaHoraria, Activa, FechaCreacion, MfaObligatorio)
    VALUES (@OrgId, N'MedicalHub Demo', N'Europe/Madrid', 1, SYSDATETIMEOFFSET(), 0);
END;

IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Email = N'medico@medicalhub.local')
BEGIN
    DECLARE @UsuarioId uniqueidentifier = NEWID();
    INSERT INTO Usuarios (Id, OrganizacionId, Email, NombreCompleto, HashContrasena, Activo, FechaCreacion,
                          IntentosFallidos, BloqueadoHasta, MfaActivado, MfaSecretoProtegido, MfaUltimoPaso)
    VALUES (@UsuarioId, @OrgId, N'medico@medicalhub.local', N'Médico Demo',
            N'AQAAAAEAAYagAAAAEFhKwVu64mUlJTTRex6CvQ2IbS/lR+pB1QLiRBebSVucvYCx59bNOx1qO4jLJ9f+Og==', 1, SYSDATETIMEOFFSET(), 0, NULL, 0, NULL, 0);

    INSERT INTO UsuarioRoles (UsuarioId, Rol) VALUES (@UsuarioId, N'ProfesionalSanitario');
END;

-- Categoría profesional (se crea si no existe).
DECLARE @CategoriaId uniqueidentifier = (SELECT Id FROM CategoriasProfesionales WHERE OrganizacionId = @OrgId AND Nombre = N'Médico');
IF @CategoriaId IS NULL
BEGIN
    SET @CategoriaId = NEWID();
    INSERT INTO CategoriasProfesionales (Id, OrganizacionId, Nombre, Descripcion, Activa, FechaCreacion)
    VALUES (@CategoriaId, @OrgId, N'Médico', N'Facultativos médicos', 1, SYSDATETIMEOFFSET());
END;

-- Ficha profesional vinculada al usuario (un usuario sanitario no puede existir sin ficha).
DECLARE @UsuarioFichaId uniqueidentifier = (SELECT Id FROM Usuarios WHERE Email = N'medico@medicalhub.local');
IF NOT EXISTS (SELECT 1 FROM Profesionales WHERE UsuarioId = @UsuarioFichaId)
    INSERT INTO Profesionales (Id, OrganizacionId, UsuarioId, Nombre, Apellidos, Email, CategoriaId, Activo, FechaCreacion)
    VALUES (NEWID(), @OrgId, @UsuarioFichaId, N'Médico', N'Demo', N'medico@medicalhub.local', @CategoriaId, 1, SYSDATETIMEOFFSET());

COMMIT;
