# Desarrollo con el teléfono Android por cable USB: redirige el puerto de la API y lanza la app.
# Temporal, hasta que los servicios estén en el servidor.
$adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
& $adb reverse tcp:5292 tcp:5292
flutter run -d v8vwjroj5py5x8u8 --dart-define=API_BASE_URL=http://localhost:5292
