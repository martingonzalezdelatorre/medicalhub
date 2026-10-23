import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/errores/fallo_api.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/validadores/validadores.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../data/organizaciones_repositorio.dart';

class PaginaOrganizaciones extends ConsumerWidget {
  const PaginaOrganizaciones({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final organizaciones = ref.watch(organizacionesProvider);
    return Scaffold(
      floatingActionButton: FloatingActionButton.extended(
        icon: const Icon(Icons.add_business),
        label: Text(t.nuevaOrganizacion),
        onPressed: () async {
          final creada = await showDialog<bool>(context: context, builder: (_) => const _DialogoNuevaOrganizacion());
          if (creada == true) ref.invalidate(organizacionesProvider);
        },
      ),
      body: EstadoAsincrono(
        valor: organizaciones,
        alReintentar: () => ref.invalidate(organizacionesProvider),
        constructor: (lista) => lista.isEmpty
            ? EstadoVacio(mensaje: t.listaVacia, icono: Icons.business_outlined)
            : ContenidoCentrado(
                child: ListView.builder(
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 88),
                  itemCount: lista.length,
                  itemBuilder: (_, i) => TarjetaElemento(
                    icono: Icons.business_outlined,
                    titulo: lista[i].nombre,
                    subtitulo: lista[i].zonaHoraria,
                    trailing: lista[i].activa ? null : Chip(label: Text(t.estadoInactivo)),
                  ),
                ),
              ),
      ),
    );
  }
}

class _DialogoNuevaOrganizacion extends ConsumerStatefulWidget {
  const _DialogoNuevaOrganizacion();

  @override
  ConsumerState<_DialogoNuevaOrganizacion> createState() => _EstadoDialogoNuevaOrganizacion();
}

class _EstadoDialogoNuevaOrganizacion extends ConsumerState<_DialogoNuevaOrganizacion> {
  static const _zonas = ['Europe/Madrid', 'Atlantic/Canary'];

  final _formulario = GlobalKey<FormState>();
  final _nombre = TextEditingController();
  final _correo = TextEditingController();
  final _nombreAdmin = TextEditingController();
  final _contrasena = TextEditingController();
  String _zona = _zonas.first;
  bool _mfa = true;
  bool _enviando = false;
  FalloApi? _fallo;
  String? _errorGeneral;

  @override
  void dispose() {
    _nombre.dispose();
    _correo.dispose();
    _nombreAdmin.dispose();
    _contrasena.dispose();
    super.dispose();
  }

  Future<void> _guardar() async {
    final t = AppLocalizations.of(context);
    if (!_formulario.currentState!.validate()) return;
    setState(() {
      _enviando = true;
      _fallo = null;
      _errorGeneral = null;
    });
    try {
      await ref.read(organizacionesRepositorioProvider).crear({
        'nombre': _nombre.text.trim(),
        'zonaHoraria': _zona,
        'mfaObligatorio': _mfa,
        'emailAdministrador': _correo.text.trim(),
        'nombreAdministrador': _nombreAdmin.text.trim(),
        'contrasenaAdministrador': _contrasena.text,
      });
      if (mounted) Navigator.pop(context, true);
    } on FalloApi catch (f) {
      if (!mounted) return;
      setState(() {
        _fallo = f;
        _errorGeneral = f.errores.isEmpty ? mensajeDeFallo(t, f) : null;
      });
      _formulario.currentState!.validate();
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final v = Validadores(t);
    return AlertDialog(
      title: Text(t.nuevaOrganizacion),
      content: SizedBox(
        width: 440,
        child: SingleChildScrollView(
          child: Form(
            key: _formulario,
            child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
              TextFormField(
                controller: _nombre,
                decoration: InputDecoration(labelText: t.campoNombreSociedad),
                validator: (x) => v.obligatorio(x) ?? _fallo?.errorDe('Nombre'),
              ),
              DropdownButtonFormField<String>(
                initialValue: _zona,
                decoration: InputDecoration(labelText: t.campoZonaHoraria),
                items: [for (final z in _zonas) DropdownMenuItem(value: z, child: Text(z))],
                onChanged: (z) => setState(() => _zona = z ?? _zona),
              ),
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                title: Text(t.campoMfaObligatorio),
                value: _mfa,
                onChanged: (x) => setState(() => _mfa = x),
              ),
              const Divider(),
              Text(t.datosAdministrador, style: Theme.of(context).textTheme.labelLarge),
              TextFormField(
                controller: _nombreAdmin,
                decoration: InputDecoration(labelText: t.campoNombre),
                validator: (x) => v.obligatorio(x) ?? _fallo?.errorDe('NombreAdministrador'),
              ),
              TextFormField(
                controller: _correo,
                decoration: InputDecoration(labelText: t.campoCorreo),
                keyboardType: TextInputType.emailAddress,
                validator: (x) => v.correo(x) ?? _fallo?.errorDe('EmailAdministrador'),
              ),
              TextFormField(
                controller: _contrasena,
                decoration: InputDecoration(labelText: t.campoContrasena),
                obscureText: true,
                validator: (x) => v.longitudMinima(12)(x) ?? _fallo?.errorDe('ContrasenaAdministrador'),
              ),
              if (_errorGeneral != null)
                Padding(
                  padding: const EdgeInsets.only(top: 12),
                  child: Text(_errorGeneral!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
                ),
            ]),
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: _enviando ? null : () => Navigator.pop(context), child: Text(t.botonCancelar)),
        FilledButton(onPressed: _enviando ? null : _guardar, child: Text(t.botonGuardar)),
      ],
    );
  }
}
