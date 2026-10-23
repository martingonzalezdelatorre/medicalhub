import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../../ausencias/presentation/etiquetas_ausencias.dart';
import '../../informes/data/informes_repositorio.dart' show formatearMinutos, misHorasProvider;
import '../data/fichaje_repositorio.dart';
import '../data/servicio_ubicacion.dart';

/// Fichaje del propio profesional: entrada, pausas y salida con la ubicación del dispositivo.
class PaginaFichaje extends ConsumerStatefulWidget {
  const PaginaFichaje({super.key});

  @override
  ConsumerState<PaginaFichaje> createState() => _PaginaFichajeState();
}

class _PaginaFichajeState extends ConsumerState<PaginaFichaje> {
  Timer? _reloj;
  bool _ocupado = false;
  FichajeDto? _ultimo;

  @override
  void initState() {
    super.initState();
    // El tiempo trabajado avanza en pantalla sin consultar a la API.
    _reloj = Timer.periodic(const Duration(seconds: 30), (_) {
      if (mounted) setState(() {});
    });
  }

  @override
  void dispose() {
    _reloj?.cancel();
    super.dispose();
  }

  void _refrescar() {
    ref.invalidate(estadoFichajeProvider);
    ref.invalidate(misFichajesProvider);
    ref.invalidate(misHorasProvider);
  }

  Future<void> _ejecutar(Future<FichajeDto> Function() accion) async {
    final t = AppLocalizations.of(context);
    setState(() => _ocupado = true);
    try {
      final f = await accion();
      if (!mounted) return;
      setState(() => _ultimo = f);
      _refrescar();
    } catch (e) {
      if (mounted) mostrarMensaje(context, mensajeDeFallo(t, e));
    } finally {
      if (mounted) setState(() => _ocupado = false);
    }
  }

  /// Pide la ubicación; si no se puede obtener, deja elegir entre fichar sin ella o cancelar.
  Future<({bool continuar, UbicacionDispositivo? ubicacion})> _ubicar() async {
    final t = AppLocalizations.of(context);
    final r = await ref.read(servicioUbicacionProvider).obtener();
    if (r.ubicacion != null || !mounted) return (continuar: r.ubicacion != null, ubicacion: r.ubicacion);
    final seguir = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        icon: const Icon(Icons.location_off_outlined),
        content: Text(t.fichajeUbicacionNoDisponible),
        actions: [
          TextButton(onPressed: () => Navigator.of(c).pop(false), child: Text(t.botonCancelar)),
          FilledButton(onPressed: () => Navigator.of(c).pop(true), child: Text(t.fichajeFicharSinUbicacion)),
        ],
      ),
    );
    return (continuar: seguir == true, ubicacion: null);
  }

  Future<void> _conUbicacion(Future<FichajeDto> Function(FichajeRepositorio repo, UbicacionDispositivo? u) accion) async {
    setState(() => _ocupado = true);
    final u = await _ubicar();
    if (!mounted) return;
    if (!u.continuar) {
      setState(() => _ocupado = false);
      return;
    }
    await _ejecutar(() => accion(ref.read(fichajeRepositorioProvider), u.ubicacion));
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final estado = ref.watch(estadoFichajeProvider);
    final historial = ref.watch(misFichajesProvider);
    final repo = ref.read(fichajeRepositorioProvider);

    return ContenidoCentrado(
      child: ListView(padding: const EdgeInsets.all(12), children: [
        EstadoAsincrono(
          valor: estado,
          alReintentar: () => ref.invalidate(estadoFichajeProvider),
          constructor: (e) => _TarjetaFichaje(
            estado: e,
            ocupado: _ocupado,
            onEntrada: () => _conUbicacion((r, u) => r.entrada(u)),
            onPausa: () => _ejecutar(repo.pausa),
            onReanudar: () => _ejecutar(repo.reanudar),
            onSalida: () => _conUbicacion((r, u) => r.salida(u)),
          ),
        ),
        if (_ultimo case final u?) _AvisoResultado(fichaje: u),
        const SizedBox(height: 8),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 4),
          child: Text(t.fichajeUbicacionAviso, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Theme.of(context).colorScheme.onSurfaceVariant)),
        ),
        const SizedBox(height: 16),
        Text(t.fichajeHistorial, style: Theme.of(context).textTheme.titleMedium),
        EstadoAsincrono(
          valor: historial,
          alReintentar: () => ref.invalidate(misFichajesProvider),
          constructor: (lista) => lista.isEmpty
              ? Padding(padding: const EdgeInsets.all(16), child: Text(t.fichajeSinHistorial))
              : Column(children: [for (final f in lista) _FilaHistorial(f)]),
        ),
      ]),
    );
  }
}

String _hora(DateTime d) {
  final l = d.toLocal();
  return '${l.hour.toString().padLeft(2, '0')}:${l.minute.toString().padLeft(2, '0')}';
}

class _TarjetaFichaje extends StatelessWidget {
  const _TarjetaFichaje({required this.estado, required this.ocupado, required this.onEntrada, required this.onPausa, required this.onReanudar, required this.onSalida});

  final EstadoFichajeDto estado;
  final bool ocupado;
  final VoidCallback onEntrada;
  final VoidCallback onPausa;
  final VoidCallback onReanudar;
  final VoidCallback onSalida;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final tema = Theme.of(context);
    final actual = estado.actual;
    final ahora = DateTime.now();

    final Widget contenido;
    if (actual != null) {
      final enPausa = actual.estado == EstadoFichaje.enPausa;
      contenido = Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(children: [
          Icon(enPausa ? Icons.pause_circle_outline : Icons.play_circle_outline, color: enPausa ? Colors.orange.shade800 : Colors.green.shade700),
          const SizedBox(width: 8),
          Text(enPausa ? t.fichajeEstadoEnPausa : t.fichajeEstadoEnCurso, style: tema.textTheme.titleMedium),
        ]),
        const SizedBox(height: 8),
        Text(t.fichajeTurnoActual(actual.turno)),
        Text(t.fichajeCentro(actual.centro)),
        Text(t.fichajeDesde(_hora(actual.inicioUtc))),
        const SizedBox(height: 8),
        Text(t.fichajeTrabajado(formatearMinutos(actual.trabajadosA(ahora))), style: tema.textTheme.headlineSmall),
        Text(t.fichajePausas(formatearMinutos(actual.pausaA(ahora)))),
        const SizedBox(height: 16),
        Wrap(spacing: 8, runSpacing: 8, children: [
          if (enPausa)
            FilledButton.icon(icon: const Icon(Icons.play_arrow), label: Text(t.fichajeReanudar), onPressed: ocupado ? null : onReanudar)
          else
            FilledButton.tonalIcon(icon: const Icon(Icons.pause), label: Text(t.fichajePausa), onPressed: ocupado ? null : onPausa),
          FilledButton.icon(icon: const Icon(Icons.logout), label: Text(t.fichajeSalida), onPressed: ocupado ? null : onSalida),
        ]),
      ]);
    } else if (estado.fichable case final f?) {
      contenido = Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(t.fichajeTurnoActual(f.turno), style: tema.textTheme.titleMedium),
        Text(t.fichajeCentro(f.centro)),
        const SizedBox(height: 16),
        FilledButton.icon(icon: const Icon(Icons.login), label: Text(t.fichajeEntrada), onPressed: ocupado ? null : onEntrada),
      ]);
    } else {
      contenido = Row(children: [
        Icon(Icons.event_busy_outlined, color: tema.colorScheme.outline),
        const SizedBox(width: 12),
        Expanded(child: Text(t.fichajeSinTurno)),
      ]);
    }
    return Card(child: Padding(padding: const EdgeInsets.all(16), child: contenido));
  }
}

class _AvisoResultado extends StatelessWidget {
  const _AvisoResultado({required this.fichaje});
  final FichajeDto fichaje;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final sinUbicacion = fichaje.entradaSinUbicacion || fichaje.salidaSinUbicacion == true;
    if (!fichaje.fueraDeZona && !sinUbicacion) return const SizedBox.shrink();
    return Card(
      color: Colors.orange.shade50,
      child: ListTile(
        leading: Icon(Icons.warning_amber_rounded, color: Colors.orange.shade800),
        title: Text(t.fichajeRegistrado),
        subtitle: Text(fichaje.fueraDeZona ? t.fichajeFueraDeZona : t.fichajeSinUbicacion),
      ),
    );
  }
}

class _FilaHistorial extends StatelessWidget {
  const _FilaHistorial(this.f);
  final FichajeDto f;

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final fin = f.finUtc == null ? '' : '–${_hora(f.finUtc!)}';
    return Card(
      child: ListTile(
        title: Text('${fechaHoraLocal(f.inicioUtc)}$fin'),
        subtitle: Text('${f.turno}\n${t.fichajeTrabajado(formatearMinutos(f.minutosTrabajados))} · ${t.fichajePausas(formatearMinutos(f.minutosPausa))}'),
        isThreeLine: true,
        trailing: f.fueraDeZona ? Chip(label: Text(t.fichajeFueraDeZonaEtiqueta), avatar: Icon(Icons.location_off_outlined, color: Colors.orange.shade800, size: 18)) : null,
      ),
    );
  }
}
