-- Usuario inicial para iniciar sesión en MedicalHub (solo desarrollo / primera puesta en marcha).
-- Correo:     admin@medicalhub.local
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

IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Email = N'admin@medicalhub.local')
BEGIN
    DECLARE @UsuarioId uniqueidentifier = NEWID();
    INSERT INTO Usuarios (Id, OrganizacionId, Email, NombreCompleto, HashContrasena, Activo, FechaCreacion,
                          IntentosFallidos, BloqueadoHasta, MfaActivado, MfaSecretoProtegido, MfaUltimoPaso)
    VALUES (@UsuarioId, @OrgId, N'admin@medicalhub.local', N'Administrador Demo',
            N'AQAAAAEAAYagAAAAEFhKwVu64mUlJTTRex6CvQ2IbS/lR+pB1QLiRBebSVucvYCx59bNOx1qO4jLJ9f+Og==', 1, SYSDATETIMEOFFSET(), 0, NULL, 0, NULL, 0);

    INSERT INTO UsuarioRoles (UsuarioId, Rol) VALUES (@UsuarioId, N'AdministradorSociedad');
END;

COMMIT;
