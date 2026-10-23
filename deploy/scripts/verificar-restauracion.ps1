<#
.SYNOPSIS
  Compara el número de filas de cada tabla entre la base de origen y la restaurada, y ejecuta DBCC CHECKDB sobre la restaurada.
  Termina con error si algo no coincide. Úselo justo después de restaurar y antes de dar la copia por buena.
#>
param(
    [Parameter(Mandatory)][string]$Servidor,
    [Parameter(Mandatory)][string]$BaseOrigen,
    [Parameter(Mandatory)][string]$BaseDestino,
    [string]$Usuario
)
$ErrorActionPreference = 'Stop'
foreach ($b in $BaseOrigen, $BaseDestino) { if ($b -notmatch '^[A-Za-z0-9_]+$') { throw 'Nombre de base de datos no válido.' } }
$auth = @(if ($Usuario) { '-U'; $Usuario; '-P'; $env:MEDICALHUB_SQL_PASSWORD } else { '-E' })

function Recuentos([string]$base) {
    $q = "SET NOCOUNT ON; SELECT s.name + '.' + t.name + '=' + CAST(SUM(p.rows) AS varchar(20)) FROM [$base].sys.tables t JOIN [$base].sys.schemas s ON s.schema_id = t.schema_id JOIN [$base].sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0,1) GROUP BY s.name, t.name ORDER BY 1"
    sqlcmd -S $Servidor @auth -C -h -1 -W -Q $q | Where-Object { $_ -match '=' }
}
$origen = Recuentos $BaseOrigen
$destino = Recuentos $BaseDestino
$dif = Compare-Object $origen $destino
if (-not $origen) { throw "No se han leído tablas de [$BaseOrigen]." }
if ($dif) { $dif | Format-Table | Out-String | Write-Host; throw 'El contenido de la base restaurada NO coincide con el origen.' }
Write-Host "Coinciden las $($origen.Count) tablas."

sqlcmd -S $Servidor @auth -b -C -Q "DBCC CHECKDB ([$BaseDestino]) WITH NO_INFOMSGS"
if ($LASTEXITCODE -ne 0) { throw 'DBCC CHECKDB ha detectado problemas en la base restaurada.' }
Write-Host 'DBCC CHECKDB correcto. Restauración verificada.'
