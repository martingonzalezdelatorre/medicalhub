<#
.SYNOPSIS
  Copia de seguridad completa de la base de datos de MedicalHub (con suma de verificación y compresión).
.EXAMPLE
  .\backup.ps1 -Servidor "(localdb)\MSSQLLocalDB" -BaseDatos MedicalHub_Dev -Carpeta D:\copias
  .\backup.ps1 -Servidor sql01 -BaseDatos MedicalHub -Carpeta /var/opt/mssql/backup -Usuario backup_operador
.NOTES
  - La contraseña (si se usa autenticación SQL) se lee de la variable de entorno MEDICALHUB_SQL_PASSWORD, nunca de un parámetro.
  - La carpeta debe ser accesible por el servicio SQL Server (la ruta es la que ve el servidor, no el equipo que ejecuta el script).
  - Las copias contienen datos personales: cífrelas o guárdelas en un almacenamiento con acceso restringido.
#>
param(
    [Parameter(Mandatory)][string]$Servidor,
    [Parameter(Mandatory)][string]$BaseDatos,
    [Parameter(Mandatory)][string]$Carpeta,
    [string]$Usuario,
    # Las ediciones Express/LocalDB no admiten compresión: actívela en Standard/Enterprise.
    [switch]$Comprimir
)
$ErrorActionPreference = 'Stop'
if ($BaseDatos -notmatch '^[A-Za-z0-9_]+$') { throw 'Nombre de base de datos no válido.' }

$marca = Get-Date -Format 'yyyyMMdd-HHmmss'
$archivo = Join-Path $Carpeta "$BaseDatos-$marca.bak"
$auth = @(if ($Usuario) { '-U'; $Usuario; '-P'; $env:MEDICALHUB_SQL_PASSWORD } else { '-E' })

$ruta = $archivo.Replace("'", "''")
# En una sola línea: sqlcmd 15 no admite saltos de línea en -Q
$sql = "SET NOCOUNT ON; BACKUP DATABASE [$BaseDatos] TO DISK = N'$ruta' WITH FORMAT, INIT, $(if ($Comprimir) { 'COMPRESSION, ' })CHECKSUM, STATS = 10; RESTORE VERIFYONLY FROM DISK = N'$ruta' WITH CHECKSUM;"
sqlcmd -S $Servidor @auth -b -C -Q $sql
if ($LASTEXITCODE -ne 0) { throw "La copia de seguridad ha fallado (código $LASTEXITCODE)." }
Write-Host "Copia verificada: $archivo"
