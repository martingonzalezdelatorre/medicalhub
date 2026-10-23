IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022134939_Inicial'
)
BEGIN
    CREATE TABLE [Organizaciones] (
        [Id] uniqueidentifier NOT NULL,
        [Nombre] nvarchar(200) NOT NULL,
        [ZonaHoraria] nvarchar(64) NOT NULL,
        [Activa] bit NOT NULL,
        [FechaCreacion] datetimeoffset NOT NULL,
        [VersionFila] rowversion NOT NULL,
        CONSTRAINT [PK_Organizaciones] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022134939_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Organizaciones_Nombre] ON [Organizaciones] ([Nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022134939_Inicial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022134939_Inicial', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022140426_IdentidadYMultiempresa'
)
BEGIN
    ALTER TABLE [Organizaciones] ADD [MfaObligatorio] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022140426_IdentidadYMultiempresa'
)
BEGIN
    CREATE TABLE [EventosSeguridad] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        [UsuarioId] uniqueidentifier NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [Email] nvarchar(254) NULL,
        [Ip] nvarchar(64) NULL,
        [Detalle] nvarchar(500) NULL,
        [Fecha] datetimeoffset NOT NULL,
        [VersionFila] rowversion NOT NULL,
        CONSTRAINT [PK_EventosSeguridad] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022140426_IdentidadYMultiempresa'
)
BEGIN
    CREATE TABLE [RegistrosAuditoria] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        [UsuarioId] uniqueidentifier NULL,
        [Entidad] nvarchar(100) NOT NULL,
        [EntidadId] nvarchar(64) NOT NULL,
        [Accion] nvarchar(30) NOT NULL,
        [Cambios] nvarchar(max) NULL,
        [IdCorrelacion] nvarchar(64) NULL,
        [Fecha] datetimeoffset NOT NULL,
        [VersionFila] rowversion NOT NULL,
        CONSTRAINT [PK_RegistrosAuditoria] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022140426_IdentidadYMultiempresa'
)
BEGIN
    CREATE TABLE [TokensRefresco] (
        [Id] uniqueidentifier NOT NULL,
        [UsuarioId] uniqueidentifier NOT NULL,
        [FamiliaId] uniqueidentifier NOT NULL,
        [HashToken] nvarchar(64) NOT NULL,
        [FechaCreacion] datetimeoffset NOT NULL,
        [Expira] datetimeoffset NOT NULL,
        [RevocadoEn] datetimeoffset NULL,
        [VersionFila] rowversion NOT NULL,
        CONSTRAINT [PK_TokensRefresco] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022140426_IdentidadYMultiempresa'
)
BEGIN
    CREATE TABLE [Usuarios] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        [Email] nvarchar(254) NOT NULL,
        [NombreCompleto] nvarchar(200) NOT NULL,
        [HashContrasena] nvarchar(512) NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetimeoffset NOT NULL,
        [IntentosFallidos] int NOT NULL,
        [BloqueadoHasta] datetimeoffset NULL,
        [MfaActivado] bit NOT NULL,
        [MfaSecretoProtegido] nvarchar(1024) NULL,
        [MfaUltimoPaso] bigint NOT NULL,
        [VersionFila] rowversion NOT NULL,
        CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022140426_IdentidadYMultiempresa'
)
BEGIN
    CREATE TABLE [UsuarioRoles] (
        [Rol] nvarchar(40) NOT NULL,
        [UsuarioId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UsuarioRoles] PRIMARY KEY ([UsuarioId], [Rol]),
        CONSTRAINT [FK_UsuarioRoles_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022140426_IdentidadYMultiempresa'
)
BEGIN
    CREATE INDEX [IX_EventosSeguridad_Fecha] ON [EventosSeguridad] ([Fecha]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022140426_IdentidadYMultiempresa'
)
BEGIN
    CREATE INDEX [IX_EventosSeguridad_UsuarioId] ON [EventosSeguridad] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022140426_IdentidadYMultiempresa'
)
BEGIN
    CREATE INDEX [IX_RegistrosAuditoria_Entidad_EntidadId] ON [RegistrosAuditoria] ([Entidad], [EntidadId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022140426_IdentidadYMultiempresa'
)
BEGIN
    CREATE INDEX [IX_RegistrosAuditoria_OrganizacionId_Fecha] ON [RegistrosAuditoria] ([OrganizacionId], [Fecha]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022140426_IdentidadYMultiempresa'
)
BEGIN
    CREATE INDEX [IX_TokensRefresco_FamiliaId] ON [TokensRefresco] ([FamiliaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022140426_IdentidadYMultiempresa'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TokensRefresco_HashToken] ON [TokensRefresco] ([HashToken]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022140426_IdentidadYMultiempresa'
)
BEGIN
    CREATE INDEX [IX_TokensRefresco_UsuarioId] ON [TokensRefresco] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022140426_IdentidadYMultiempresa'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuarios_Email] ON [Usuarios] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022140426_IdentidadYMultiempresa'
)
BEGIN
    CREATE INDEX [IX_Usuarios_OrganizacionId] ON [Usuarios] ([OrganizacionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022140426_IdentidadYMultiempresa'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022140426_IdentidadYMultiempresa', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE TABLE [CategoriasProfesionales] (
        [Id] uniqueidentifier NOT NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        [Nombre] nvarchar(120) NOT NULL,
        [Descripcion] nvarchar(500) NULL,
        [Activa] bit NOT NULL,
        [FechaCreacion] datetimeoffset NOT NULL,
        CONSTRAINT [PK_CategoriasProfesionales] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE TABLE [Centros] (
        [Id] uniqueidentifier NOT NULL,
        [Nombre] nvarchar(200) NOT NULL,
        [Tipo] nvarchar(30) NOT NULL,
        [Direccion] nvarchar(300) NULL,
        [ZonaHoraria] nvarchar(64) NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetimeoffset NOT NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_Centros] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE TABLE [Competencias] (
        [Id] uniqueidentifier NOT NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        [Nombre] nvarchar(120) NOT NULL,
        [Descripcion] nvarchar(500) NULL,
        [Activa] bit NOT NULL,
        [FechaCreacion] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Competencias] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE TABLE [Especialidades] (
        [Id] uniqueidentifier NOT NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        [Nombre] nvarchar(120) NOT NULL,
        [Descripcion] nvarchar(500) NULL,
        [Activa] bit NOT NULL,
        [FechaCreacion] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Especialidades] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE TABLE [Profesionales] (
        [Id] uniqueidentifier NOT NULL,
        [UsuarioId] uniqueidentifier NULL,
        [Nombre] nvarchar(100) NOT NULL,
        [Apellidos] nvarchar(150) NOT NULL,
        [Email] nvarchar(254) NULL,
        [Telefono] nvarchar(30) NULL,
        [NumeroColegiado] nvarchar(30) NULL,
        [CategoriaId] uniqueidentifier NOT NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetimeoffset NOT NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_Profesionales] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Profesionales_CategoriasProfesionales_CategoriaId] FOREIGN KEY ([CategoriaId]) REFERENCES [CategoriasProfesionales] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE TABLE [CentroResponsables] (
        [UsuarioId] uniqueidentifier NOT NULL,
        [CentroId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_CentroResponsables] PRIMARY KEY ([CentroId], [UsuarioId]),
        CONSTRAINT [FK_CentroResponsables_Centros_CentroId] FOREIGN KEY ([CentroId]) REFERENCES [Centros] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE TABLE [Servicios] (
        [Id] uniqueidentifier NOT NULL,
        [CentroId] uniqueidentifier NOT NULL,
        [Nombre] nvarchar(200) NOT NULL,
        [Descripcion] nvarchar(1000) NULL,
        [CoordinadorUsuarioId] uniqueidentifier NULL,
        [ConfiguracionReglas] nvarchar(4000) NULL,
        [Activo] bit NOT NULL,
        [FechaCreacion] datetimeoffset NOT NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_Servicios] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Servicios_Centros_CentroId] FOREIGN KEY ([CentroId]) REFERENCES [Centros] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE TABLE [ProfesionalCompetencias] (
        [CompetenciaId] uniqueidentifier NOT NULL,
        [ProfesionalId] uniqueidentifier NOT NULL,
        [VigenteHasta] date NULL,
        CONSTRAINT [PK_ProfesionalCompetencias] PRIMARY KEY ([ProfesionalId], [CompetenciaId]),
        CONSTRAINT [FK_ProfesionalCompetencias_Competencias_CompetenciaId] FOREIGN KEY ([CompetenciaId]) REFERENCES [Competencias] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProfesionalCompetencias_Profesionales_ProfesionalId] FOREIGN KEY ([ProfesionalId]) REFERENCES [Profesionales] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE TABLE [ProfesionalEspecialidades] (
        [EspecialidadId] uniqueidentifier NOT NULL,
        [ProfesionalId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ProfesionalEspecialidades] PRIMARY KEY ([ProfesionalId], [EspecialidadId]),
        CONSTRAINT [FK_ProfesionalEspecialidades_Especialidades_EspecialidadId] FOREIGN KEY ([EspecialidadId]) REFERENCES [Especialidades] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProfesionalEspecialidades_Profesionales_ProfesionalId] FOREIGN KEY ([ProfesionalId]) REFERENCES [Profesionales] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE TABLE [ProfesionalServicios] (
        [ServicioId] uniqueidentifier NOT NULL,
        [ProfesionalId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ProfesionalServicios] PRIMARY KEY ([ProfesionalId], [ServicioId]),
        CONSTRAINT [FK_ProfesionalServicios_Profesionales_ProfesionalId] FOREIGN KEY ([ProfesionalId]) REFERENCES [Profesionales] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProfesionalServicios_Servicios_ServicioId] FOREIGN KEY ([ServicioId]) REFERENCES [Servicios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE TABLE [ServicioCategorias] (
        [CategoriaProfesionalId] uniqueidentifier NOT NULL,
        [ServicioId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ServicioCategorias] PRIMARY KEY ([ServicioId], [CategoriaProfesionalId]),
        CONSTRAINT [FK_ServicioCategorias_CategoriasProfesionales_CategoriaProfesionalId] FOREIGN KEY ([CategoriaProfesionalId]) REFERENCES [CategoriasProfesionales] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServicioCategorias_Servicios_ServicioId] FOREIGN KEY ([ServicioId]) REFERENCES [Servicios] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE TABLE [ServicioCompetencias] (
        [CompetenciaId] uniqueidentifier NOT NULL,
        [ServicioId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ServicioCompetencias] PRIMARY KEY ([ServicioId], [CompetenciaId]),
        CONSTRAINT [FK_ServicioCompetencias_Competencias_CompetenciaId] FOREIGN KEY ([CompetenciaId]) REFERENCES [Competencias] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServicioCompetencias_Servicios_ServicioId] FOREIGN KEY ([ServicioId]) REFERENCES [Servicios] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE TABLE [TiposTurno] (
        [Id] uniqueidentifier NOT NULL,
        [ServicioId] uniqueidentifier NOT NULL,
        [Nombre] nvarchar(100) NOT NULL,
        [HoraInicio] time NOT NULL,
        [HoraFin] time NOT NULL,
        [EsGuardia] bit NOT NULL,
        [Activo] bit NOT NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_TiposTurno] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TiposTurno_Servicios_ServicioId] FOREIGN KEY ([ServicioId]) REFERENCES [Servicios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE TABLE [RequisitosCobertura] (
        [Id] uniqueidentifier NOT NULL,
        [ServicioId] uniqueidentifier NOT NULL,
        [TipoTurnoId] uniqueidentifier NOT NULL,
        [NumeroProfesionales] int NOT NULL,
        [CategoriaProfesionalId] uniqueidentifier NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_RequisitosCobertura] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RequisitosCobertura_CategoriasProfesionales_CategoriaProfesionalId] FOREIGN KEY ([CategoriaProfesionalId]) REFERENCES [CategoriasProfesionales] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequisitosCobertura_Servicios_ServicioId] FOREIGN KEY ([ServicioId]) REFERENCES [Servicios] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequisitosCobertura_TiposTurno_TipoTurnoId] FOREIGN KEY ([TipoTurnoId]) REFERENCES [TiposTurno] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CategoriasProfesionales_OrganizacionId_Nombre] ON [CategoriasProfesionales] ([OrganizacionId], [Nombre]) WHERE [OrganizacionId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE INDEX [IX_CentroResponsables_UsuarioId] ON [CentroResponsables] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Centros_OrganizacionId_Nombre] ON [Centros] ([OrganizacionId], [Nombre]) WHERE [OrganizacionId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Competencias_OrganizacionId_Nombre] ON [Competencias] ([OrganizacionId], [Nombre]) WHERE [OrganizacionId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Especialidades_OrganizacionId_Nombre] ON [Especialidades] ([OrganizacionId], [Nombre]) WHERE [OrganizacionId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE INDEX [IX_ProfesionalCompetencias_CompetenciaId] ON [ProfesionalCompetencias] ([CompetenciaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE INDEX [IX_Profesionales_CategoriaId] ON [Profesionales] ([CategoriaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE INDEX [IX_Profesionales_OrganizacionId_Apellidos_Nombre] ON [Profesionales] ([OrganizacionId], [Apellidos], [Nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Profesionales_OrganizacionId_NumeroColegiado] ON [Profesionales] ([OrganizacionId], [NumeroColegiado]) WHERE [NumeroColegiado] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Profesionales_UsuarioId] ON [Profesionales] ([UsuarioId]) WHERE [UsuarioId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE INDEX [IX_ProfesionalEspecialidades_EspecialidadId] ON [ProfesionalEspecialidades] ([EspecialidadId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE INDEX [IX_ProfesionalServicios_ServicioId] ON [ProfesionalServicios] ([ServicioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE INDEX [IX_RequisitosCobertura_CategoriaProfesionalId] ON [RequisitosCobertura] ([CategoriaProfesionalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE INDEX [IX_RequisitosCobertura_ServicioId] ON [RequisitosCobertura] ([ServicioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RequisitosCobertura_TipoTurnoId_CategoriaProfesionalId] ON [RequisitosCobertura] ([TipoTurnoId], [CategoriaProfesionalId]) WHERE [CategoriaProfesionalId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE INDEX [IX_ServicioCategorias_CategoriaProfesionalId] ON [ServicioCategorias] ([CategoriaProfesionalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE INDEX [IX_ServicioCompetencias_CompetenciaId] ON [ServicioCompetencias] ([CompetenciaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Servicios_CentroId_Nombre] ON [Servicios] ([CentroId], [Nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TiposTurno_ServicioId_Nombre] ON [TiposTurno] ([ServicioId], [Nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022142935_CatalogosEstructuraProfesionales'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022142935_CatalogosEstructuraProfesionales', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE TABLE [Cuadrantes] (
        [Id] uniqueidentifier NOT NULL,
        [ServicioId] uniqueidentifier NOT NULL,
        [Nombre] nvarchar(150) NOT NULL,
        [Desde] date NOT NULL,
        [Hasta] date NOT NULL,
        [Estado] nvarchar(20) NOT NULL,
        [Version] int NOT NULL,
        [FechaPublicacion] datetimeoffset NULL,
        [PublicadoPorUsuarioId] uniqueidentifier NULL,
        [FechaCreacion] datetimeoffset NOT NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_Cuadrantes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Cuadrantes_Servicios_ServicioId] FOREIGN KEY ([ServicioId]) REFERENCES [Servicios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE TABLE [PlantillasCuadrante] (
        [Id] uniqueidentifier NOT NULL,
        [ServicioId] uniqueidentifier NOT NULL,
        [Nombre] nvarchar(150) NOT NULL,
        [DiasPeriodo] int NOT NULL,
        [FechaCreacion] datetimeoffset NOT NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_PlantillasCuadrante] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlantillasCuadrante_Servicios_ServicioId] FOREIGN KEY ([ServicioId]) REFERENCES [Servicios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE TABLE [Turnos] (
        [Id] uniqueidentifier NOT NULL,
        [CuadranteId] uniqueidentifier NOT NULL,
        [ServicioId] uniqueidentifier NOT NULL,
        [TipoTurnoId] uniqueidentifier NOT NULL,
        [Fecha] date NOT NULL,
        [InicioUtc] datetimeoffset NOT NULL,
        [FinUtc] datetimeoffset NOT NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_Turnos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Turnos_Cuadrantes_CuadranteId] FOREIGN KEY ([CuadranteId]) REFERENCES [Cuadrantes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Turnos_Servicios_ServicioId] FOREIGN KEY ([ServicioId]) REFERENCES [Servicios] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Turnos_TiposTurno_TipoTurnoId] FOREIGN KEY ([TipoTurnoId]) REFERENCES [TiposTurno] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE TABLE [VersionesCuadrante] (
        [Id] uniqueidentifier NOT NULL,
        [CuadranteId] uniqueidentifier NOT NULL,
        [Numero] int NOT NULL,
        [FechaPublicacion] datetimeoffset NOT NULL,
        [UsuarioId] uniqueidentifier NULL,
        [TotalTurnos] int NOT NULL,
        [PlazasTotales] int NOT NULL,
        [PlazasCubiertas] int NOT NULL,
        [ContenidoJson] nvarchar(max) NOT NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_VersionesCuadrante] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VersionesCuadrante_Cuadrantes_CuadranteId] FOREIGN KEY ([CuadranteId]) REFERENCES [Cuadrantes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE TABLE [PlantillaLineas] (
        [Id] int NOT NULL IDENTITY,
        [DiaOffset] int NOT NULL,
        [TipoTurnoId] uniqueidentifier NOT NULL,
        [NumeroPlazas] int NOT NULL,
        [CategoriaProfesionalId] uniqueidentifier NULL,
        [PlantillaId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_PlantillaLineas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlantillaLineas_CategoriasProfesionales_CategoriaProfesionalId] FOREIGN KEY ([CategoriaProfesionalId]) REFERENCES [CategoriasProfesionales] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PlantillaLineas_PlantillasCuadrante_PlantillaId] FOREIGN KEY ([PlantillaId]) REFERENCES [PlantillasCuadrante] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PlantillaLineas_TiposTurno_TipoTurnoId] FOREIGN KEY ([TipoTurnoId]) REFERENCES [TiposTurno] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE TABLE [NecesidadesCobertura] (
        [Id] uniqueidentifier NOT NULL,
        [TurnoId] uniqueidentifier NOT NULL,
        [NumeroPlazas] int NOT NULL,
        [CategoriaProfesionalId] uniqueidentifier NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_NecesidadesCobertura] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NecesidadesCobertura_CategoriasProfesionales_CategoriaProfesionalId] FOREIGN KEY ([CategoriaProfesionalId]) REFERENCES [CategoriasProfesionales] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NecesidadesCobertura_Turnos_TurnoId] FOREIGN KEY ([TurnoId]) REFERENCES [Turnos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE TABLE [Asignaciones] (
        [Id] uniqueidentifier NOT NULL,
        [NecesidadId] uniqueidentifier NOT NULL,
        [TurnoId] uniqueidentifier NOT NULL,
        [ProfesionalId] uniqueidentifier NOT NULL,
        [Estado] nvarchar(25) NOT NULL,
        [InicioUtc] datetimeoffset NOT NULL,
        [FinUtc] datetimeoffset NOT NULL,
        [FechaAsignacion] datetimeoffset NOT NULL,
        [AsignadoPorUsuarioId] uniqueidentifier NULL,
        [FechaCancelacion] datetimeoffset NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_Asignaciones] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Asignaciones_NecesidadesCobertura_NecesidadId] FOREIGN KEY ([NecesidadId]) REFERENCES [NecesidadesCobertura] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Asignaciones_Profesionales_ProfesionalId] FOREIGN KEY ([ProfesionalId]) REFERENCES [Profesionales] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Asignaciones_Turnos_TurnoId] FOREIGN KEY ([TurnoId]) REFERENCES [Turnos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Asignaciones_NecesidadId_ProfesionalId] ON [Asignaciones] ([NecesidadId], [ProfesionalId]) WHERE [Estado] <> ''Cancelada''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE INDEX [IX_Asignaciones_ProfesionalId_InicioUtc_FinUtc] ON [Asignaciones] ([ProfesionalId], [InicioUtc], [FinUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE INDEX [IX_Asignaciones_TurnoId] ON [Asignaciones] ([TurnoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE INDEX [IX_Cuadrantes_ServicioId_Desde_Hasta] ON [Cuadrantes] ([ServicioId], [Desde], [Hasta]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE INDEX [IX_NecesidadesCobertura_CategoriaProfesionalId] ON [NecesidadesCobertura] ([CategoriaProfesionalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_NecesidadesCobertura_TurnoId_CategoriaProfesionalId] ON [NecesidadesCobertura] ([TurnoId], [CategoriaProfesionalId]) WHERE [CategoriaProfesionalId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE INDEX [IX_PlantillaLineas_CategoriaProfesionalId] ON [PlantillaLineas] ([CategoriaProfesionalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE INDEX [IX_PlantillaLineas_PlantillaId] ON [PlantillaLineas] ([PlantillaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE INDEX [IX_PlantillaLineas_TipoTurnoId] ON [PlantillaLineas] ([TipoTurnoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PlantillasCuadrante_ServicioId_Nombre] ON [PlantillasCuadrante] ([ServicioId], [Nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Turnos_CuadranteId_TipoTurnoId_Fecha] ON [Turnos] ([CuadranteId], [TipoTurnoId], [Fecha]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE INDEX [IX_Turnos_OrganizacionId_Fecha] ON [Turnos] ([OrganizacionId], [Fecha]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE INDEX [IX_Turnos_ServicioId_InicioUtc] ON [Turnos] ([ServicioId], [InicioUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE INDEX [IX_Turnos_TipoTurnoId] ON [Turnos] ([TipoTurnoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VersionesCuadrante_CuadranteId_Numero] ON [VersionesCuadrante] ([CuadranteId], [Numero]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022145422_CuadrantesTurnosAsignaciones'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022145422_CuadrantesTurnosAsignaciones', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022153317_ReglasYExcepciones'
)
BEGIN
    CREATE TABLE [ExcepcionesRegla] (
        [Id] uniqueidentifier NOT NULL,
        [AsignacionId] uniqueidentifier NOT NULL,
        [TurnoId] uniqueidentifier NOT NULL,
        [ProfesionalId] uniqueidentifier NOT NULL,
        [ClaveRegla] nvarchar(60) NOT NULL,
        [Codigo] nvarchar(60) NOT NULL,
        [ReglaId] uniqueidentifier NULL,
        [Mensaje] nvarchar(500) NOT NULL,
        [Justificacion] nvarchar(1000) NOT NULL,
        [AutorizadaPorUsuarioId] uniqueidentifier NOT NULL,
        [Fecha] datetimeoffset NOT NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_ExcepcionesRegla] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExcepcionesRegla_Asignaciones_AsignacionId] FOREIGN KEY ([AsignacionId]) REFERENCES [Asignaciones] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ExcepcionesRegla_Profesionales_ProfesionalId] FOREIGN KEY ([ProfesionalId]) REFERENCES [Profesionales] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ExcepcionesRegla_Turnos_TurnoId] FOREIGN KEY ([TurnoId]) REFERENCES [Turnos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022153317_ReglasYExcepciones'
)
BEGIN
    CREATE TABLE [ReglasPlanificacion] (
        [Id] uniqueidentifier NOT NULL,
        [Tipo] nvarchar(40) NOT NULL,
        [Nombre] nvarchar(150) NOT NULL,
        [Descripcion] nvarchar(500) NULL,
        [ServicioId] uniqueidentifier NULL,
        [Severidad] nvarchar(20) NOT NULL,
        [Activa] bit NOT NULL,
        [PermiteExcepcion] bit NOT NULL,
        [ExcepcionRequiereAdministrador] bit NOT NULL,
        [ParametrosJson] nvarchar(2000) NULL,
        [Fundamento] nvarchar(1000) NULL,
        [RevisadaEl] datetimeoffset NULL,
        [RevisadaPorUsuarioId] uniqueidentifier NULL,
        [FechaCreacion] datetimeoffset NOT NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_ReglasPlanificacion] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReglasPlanificacion_Servicios_ServicioId] FOREIGN KEY ([ServicioId]) REFERENCES [Servicios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022153317_ReglasYExcepciones'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExcepcionesRegla_AsignacionId_ClaveRegla] ON [ExcepcionesRegla] ([AsignacionId], [ClaveRegla]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022153317_ReglasYExcepciones'
)
BEGIN
    CREATE INDEX [IX_ExcepcionesRegla_OrganizacionId_Fecha] ON [ExcepcionesRegla] ([OrganizacionId], [Fecha]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022153317_ReglasYExcepciones'
)
BEGIN
    CREATE INDEX [IX_ExcepcionesRegla_ProfesionalId] ON [ExcepcionesRegla] ([ProfesionalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022153317_ReglasYExcepciones'
)
BEGIN
    CREATE INDEX [IX_ExcepcionesRegla_TurnoId] ON [ExcepcionesRegla] ([TurnoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022153317_ReglasYExcepciones'
)
BEGIN
    CREATE INDEX [IX_ReglasPlanificacion_OrganizacionId_Tipo] ON [ReglasPlanificacion] ([OrganizacionId], [Tipo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022153317_ReglasYExcepciones'
)
BEGIN
    CREATE INDEX [IX_ReglasPlanificacion_ServicioId] ON [ReglasPlanificacion] ([ServicioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022153317_ReglasYExcepciones'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022153317_ReglasYExcepciones', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    ALTER TABLE [Asignaciones] ADD [MotivoCancelacion] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    CREATE TABLE [Ausencias] (
        [Id] uniqueidentifier NOT NULL,
        [ProfesionalId] uniqueidentifier NOT NULL,
        [Tipo] nvarchar(30) NOT NULL,
        [Desde] date NOT NULL,
        [Hasta] date NOT NULL,
        [Comentario] nvarchar(300) NULL,
        [Estado] nvarchar(20) NOT NULL,
        [SolicitadaPorUsuarioId] uniqueidentifier NOT NULL,
        [FechaSolicitud] datetimeoffset NOT NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_Ausencias] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Ausencias_Profesionales_ProfesionalId] FOREIGN KEY ([ProfesionalId]) REFERENCES [Profesionales] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    CREATE TABLE [Disponibilidades] (
        [Id] uniqueidentifier NOT NULL,
        [ProfesionalId] uniqueidentifier NOT NULL,
        [Tipo] nvarchar(20) NOT NULL,
        [Desde] date NOT NULL,
        [Hasta] date NOT NULL,
        [Dias] int NOT NULL,
        [TipoTurnoId] uniqueidentifier NULL,
        [Comentario] nvarchar(300) NULL,
        [Estado] nvarchar(20) NOT NULL,
        [DeclaradaPorUsuarioId] uniqueidentifier NOT NULL,
        [FechaDeclaracion] datetimeoffset NOT NULL,
        [DecididaPorUsuarioId] uniqueidentifier NULL,
        [FechaDecision] datetimeoffset NULL,
        [MotivoDecision] nvarchar(500) NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_Disponibilidades] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Disponibilidades_Profesionales_ProfesionalId] FOREIGN KEY ([ProfesionalId]) REFERENCES [Profesionales] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Disponibilidades_TiposTurno_TipoTurnoId] FOREIGN KEY ([TipoTurnoId]) REFERENCES [TiposTurno] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    CREATE TABLE [DecisionesAusencia] (
        [Id] uniqueidentifier NOT NULL,
        [AusenciaId] uniqueidentifier NOT NULL,
        [Decision] nvarchar(20) NOT NULL,
        [UsuarioId] uniqueidentifier NOT NULL,
        [Fecha] datetimeoffset NOT NULL,
        [Motivo] nvarchar(500) NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_DecisionesAusencia] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DecisionesAusencia_Ausencias_AusenciaId] FOREIGN KEY ([AusenciaId]) REFERENCES [Ausencias] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    CREATE TABLE [Vacantes] (
        [Id] uniqueidentifier NOT NULL,
        [NecesidadId] uniqueidentifier NOT NULL,
        [TurnoId] uniqueidentifier NOT NULL,
        [AsignacionOriginalId] uniqueidentifier NOT NULL,
        [ProfesionalAusenteId] uniqueidentifier NOT NULL,
        [AusenciaId] uniqueidentifier NULL,
        [Motivo] nvarchar(200) NOT NULL,
        [Estado] nvarchar(20) NOT NULL,
        [FechaCreacion] datetimeoffset NOT NULL,
        [FechaCierre] datetimeoffset NULL,
        [AsignacionCubiertaId] uniqueidentifier NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_Vacantes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Vacantes_Asignaciones_AsignacionOriginalId] FOREIGN KEY ([AsignacionOriginalId]) REFERENCES [Asignaciones] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Vacantes_Ausencias_AusenciaId] FOREIGN KEY ([AusenciaId]) REFERENCES [Ausencias] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Vacantes_NecesidadesCobertura_NecesidadId] FOREIGN KEY ([NecesidadId]) REFERENCES [NecesidadesCobertura] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Vacantes_Profesionales_ProfesionalAusenteId] FOREIGN KEY ([ProfesionalAusenteId]) REFERENCES [Profesionales] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Vacantes_Turnos_TurnoId] FOREIGN KEY ([TurnoId]) REFERENCES [Turnos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    CREATE INDEX [IX_Ausencias_OrganizacionId_Estado] ON [Ausencias] ([OrganizacionId], [Estado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    CREATE INDEX [IX_Ausencias_ProfesionalId_Desde_Hasta] ON [Ausencias] ([ProfesionalId], [Desde], [Hasta]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    CREATE INDEX [IX_DecisionesAusencia_AusenciaId] ON [DecisionesAusencia] ([AusenciaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    CREATE INDEX [IX_Disponibilidades_OrganizacionId_Estado] ON [Disponibilidades] ([OrganizacionId], [Estado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    CREATE INDEX [IX_Disponibilidades_ProfesionalId_Desde_Hasta] ON [Disponibilidades] ([ProfesionalId], [Desde], [Hasta]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    CREATE INDEX [IX_Disponibilidades_TipoTurnoId] ON [Disponibilidades] ([TipoTurnoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Vacantes_AsignacionOriginalId] ON [Vacantes] ([AsignacionOriginalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    CREATE INDEX [IX_Vacantes_AusenciaId] ON [Vacantes] ([AusenciaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    CREATE INDEX [IX_Vacantes_NecesidadId] ON [Vacantes] ([NecesidadId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    CREATE INDEX [IX_Vacantes_OrganizacionId_Estado] ON [Vacantes] ([OrganizacionId], [Estado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    CREATE INDEX [IX_Vacantes_ProfesionalAusenteId] ON [Vacantes] ([ProfesionalAusenteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    CREATE INDEX [IX_Vacantes_TurnoId] ON [Vacantes] ([TurnoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023070301_AusenciasDisponibilidadVacantes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023070301_AusenciasDisponibilidadVacantes', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023074056_OfertasYNotificaciones'
)
BEGIN
    CREATE TABLE [Notificaciones] (
        [Id] uniqueidentifier NOT NULL,
        [UsuarioId] uniqueidentifier NOT NULL,
        [Tipo] nvarchar(30) NOT NULL,
        [Titulo] nvarchar(150) NOT NULL,
        [Mensaje] nvarchar(500) NOT NULL,
        [EntidadId] uniqueidentifier NULL,
        [Fecha] datetimeoffset NOT NULL,
        [FechaLectura] datetimeoffset NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_Notificaciones] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023074056_OfertasYNotificaciones'
)
BEGIN
    CREATE TABLE [OfertasSustitucion] (
        [Id] uniqueidentifier NOT NULL,
        [VacanteId] uniqueidentifier NOT NULL,
        [ProfesionalId] uniqueidentifier NOT NULL,
        [Estado] nvarchar(20) NOT NULL,
        [OfrecidaPorUsuarioId] uniqueidentifier NOT NULL,
        [FechaOferta] datetimeoffset NOT NULL,
        [Caduca] datetimeoffset NULL,
        [FechaRespuesta] datetimeoffset NULL,
        [MotivoRespuesta] nvarchar(300) NULL,
        [FechaDecision] datetimeoffset NULL,
        [DecididaPorUsuarioId] uniqueidentifier NULL,
        [MotivoDecision] nvarchar(300) NULL,
        [AsignacionId] uniqueidentifier NULL,
        [VersionFila] rowversion NOT NULL,
        [OrganizacionId] uniqueidentifier NULL,
        CONSTRAINT [PK_OfertasSustitucion] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OfertasSustitucion_Asignaciones_AsignacionId] FOREIGN KEY ([AsignacionId]) REFERENCES [Asignaciones] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OfertasSustitucion_Profesionales_ProfesionalId] FOREIGN KEY ([ProfesionalId]) REFERENCES [Profesionales] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OfertasSustitucion_Vacantes_VacanteId] FOREIGN KEY ([VacanteId]) REFERENCES [Vacantes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023074056_OfertasYNotificaciones'
)
BEGIN
    CREATE INDEX [IX_Notificaciones_UsuarioId_FechaLectura_Fecha] ON [Notificaciones] ([UsuarioId], [FechaLectura], [Fecha]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023074056_OfertasYNotificaciones'
)
BEGIN
    CREATE INDEX [IX_OfertasSustitucion_AsignacionId] ON [OfertasSustitucion] ([AsignacionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023074056_OfertasYNotificaciones'
)
BEGIN
    CREATE INDEX [IX_OfertasSustitucion_OrganizacionId_Estado] ON [OfertasSustitucion] ([OrganizacionId], [Estado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023074056_OfertasYNotificaciones'
)
BEGIN
    CREATE INDEX [IX_OfertasSustitucion_ProfesionalId_Estado] ON [OfertasSustitucion] ([ProfesionalId], [Estado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023074056_OfertasYNotificaciones'
)
BEGIN
    CREATE INDEX [IX_OfertasSustitucion_VacanteId_ProfesionalId] ON [OfertasSustitucion] ([VacanteId], [ProfesionalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261023074056_OfertasYNotificaciones'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023074056_OfertasYNotificaciones', N'10.0.8');
END;

COMMIT;
GO

