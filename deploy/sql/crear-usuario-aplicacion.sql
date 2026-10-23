/*
  Usuarios de base de datos con MÍNIMO PRIVILEGIO para MedicalHub.
  Ejecutar como administrador del servidor, conectado a la base MedicalHub (USE MedicalHub;), tras crear la base.
  Cambie las contraseñas por valores largos y aleatorios y guárdelas en un gestor de secretos.

  - medicalhub_app      : lo usa la API en producción. Solo lee y escribe datos (no puede alterar el esquema).
  - medicalhub_migrador : lo usa quien aplica las migraciones (script idempotente de backend/db). Puede alterar el esquema.
*/
CREATE LOGIN medicalhub_app WITH PASSWORD = N'<CAMBIAR-contraseña-app>', CHECK_POLICY = ON;
CREATE USER  medicalhub_app FOR LOGIN medicalhub_app;
ALTER ROLE db_datareader ADD MEMBER medicalhub_app;
ALTER ROLE db_datawriter ADD MEMBER medicalhub_app;

CREATE LOGIN medicalhub_migrador WITH PASSWORD = N'<CAMBIAR-contraseña-migrador>', CHECK_POLICY = ON;
CREATE USER  medicalhub_migrador FOR LOGIN medicalhub_migrador;
ALTER ROLE db_ddladmin ADD MEMBER medicalhub_migrador;
ALTER ROLE db_datareader ADD MEMBER medicalhub_migrador;
ALTER ROLE db_datawriter ADD MEMBER medicalhub_migrador;
GO
