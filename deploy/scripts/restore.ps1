<#
.SYNOPSIS
  Restaura una copia de seguridad en una base de datos NUEVA y compara su contenido con el origen (opcional).
.EXAMPLE
  .\restore.ps1 -Servidor "(localdb)\MSSQLLocalDB" -Archivo D:\copias\MedicalHub-20261023-101500.bak -BaseDestino MedicalHub_Restaurada -BaseOrigen MedicalHub_Dev
.NOTES
  Por seguridad NO sobrescribe una base existente: use un nombre nuevo, verifique y, si procede, renombre o cambie la cadena de conexión.
  La contraseña (autenticación SQL) se lee de MEDICALHUB_SQL_PASSWORD.
#>
param(
    [Parameter(Mandatory)][string]$Servidor,
    [Parameter(Mandatory)][string]$Archivo,
    [Parameter(Mandatory)][string]$BaseDestino,
    [string]$BaseOrigen,
    [string]$Usuario
)
$ErrorActionPreference = 'Stop'
if ($BaseDestino -notmatch '^[A-Za-z0-9_]+$') { throw 'Nombre de base de datos no válido.' }
$auth = @(if ($Usuario) { '-U'; $Usuario; '-P'; $env:MEDICALHUB_SQL_PASSWORD } else { '-E' })
$ruta = $Archivo.Replace("'", "''")

# La base de destino no debe existir
$existe = sqlcmd -S $Servidor @auth -C -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = N'$BaseDestino'"
if ($LASTEXITCODE -ne 0) { throw 'No se pudo consultar el servidor.' }
if (($existe | Select-Object -First 1).Trim() -ne '0') { throw "La base '$BaseDestino' ya existe. Elija un nombre nuevo." }

# Ficheros lógicos de la copia -> ficheros físicos en la carpeta de datos del servidor
$carpetaDatos = (sqlcmd -S $Servidor @auth -C -h -1 -W -Q "SET NOCOUNT ON; SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(512))" | Select-Object -First 1).Trim()
$lista = sqlcmd -S $Servidor @auth -C -h -1 -W -s '|' -Q "SET NOCOUNT ON; RESTORE FILELISTONLY FROM DISK = N'$ruta' WITH CHECKSUM"
if ($LASTEXITCODE -ne 0) { throw 'La copia no se puede leer o está dañada.' }
$mover = @()
foreach ($linea in $lista) {
    $c = $linea.Split('|')
    if ($c.Count -lt 3) { continue }
    $logico = $c[0].Trim(); $tipo = $c[2].Trim()
    $ext = if ($tipo -eq 'L') { 'ldf' } else { 'mdf' }
    $mover += "MOVE N'$logico' TO N'$($carpetaDatos)${BaseDestino}_$logico.$ext'"
}
$sql = "RESTORE DATABASE [$BaseDestino] FROM DISK = N'$ruta' WITH CHECKSUM, RECOVERY, STATS = 10, " + ($mover -join ', ')
sqlcmd -S $Servidor @auth -b -C -Q $sql
if ($LASTEXITCODE -ne 0) { throw "La restauración ha fallado (código $LASTEXITCODE)." }
Write-Host "Restaurada en [$BaseDestino]."

if ($BaseOrigen) {
    & (Join-Path $PSScriptRoot 'verificar-restauracion.ps1') -Servidor $Servidor -BaseOrigen $BaseOrigen -BaseDestino $BaseDestino -Usuario $Usuario
}
