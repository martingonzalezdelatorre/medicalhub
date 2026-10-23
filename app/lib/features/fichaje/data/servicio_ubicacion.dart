import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';

/// Posición del dispositivo en el momento de fichar.
class UbicacionDispositivo {
  const UbicacionDispositivo({required this.latitud, required this.longitud, this.precisionMetros});
  final double latitud;
  final double longitud;
  final double? precisionMetros;
}

/// Resultado de pedir la ubicación: o la posición, o `null` si no se pudo obtener (permiso, GPS apagado, sin señal).
class ResultadoUbicacion {
  const ResultadoUbicacion(this.ubicacion);
  final UbicacionDispositivo? ubicacion;
}

abstract class ServicioUbicacion {
  Future<ResultadoUbicacion> obtener();
}

class ServicioUbicacionGeolocator implements ServicioUbicacion {
  const ServicioUbicacionGeolocator();

  @override
  Future<ResultadoUbicacion> obtener() async {
    try {
      if (!await Geolocator.isLocationServiceEnabled()) return const ResultadoUbicacion(null);
      var permiso = await Geolocator.checkPermission();
      if (permiso == LocationPermission.denied) permiso = await Geolocator.requestPermission();
      if (permiso == LocationPermission.denied || permiso == LocationPermission.deniedForever) return const ResultadoUbicacion(null);
      final p = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(accuracy: LocationAccuracy.high, timeLimit: Duration(seconds: 15)),
      );
      return ResultadoUbicacion(UbicacionDispositivo(latitud: p.latitude, longitud: p.longitude, precisionMetros: p.accuracy));
    } catch (_) {
      return const ResultadoUbicacion(null);
    }
  }
}

final servicioUbicacionProvider = Provider<ServicioUbicacion>((ref) => const ServicioUbicacionGeolocator());
