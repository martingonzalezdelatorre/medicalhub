import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/auth/modelos.dart';
import '../../../core/errores/fallo_api.dart';
import '../../../l10n/app_localizations.dart';
import '../../../shared/validadores/validadores.dart';
import '../../../shared/widgets/estado_asincrono.dart';
import '../../../shared/widgets/utilidades_ui.dart';
import '../data/usuarios_repositorio.dart';

class PaginaUsuarios extends ConsumerWidget {
  const PaginaUsuarios({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    final usuarios = ref.watch(usuariosProvider);
    return Scaffold(
      floatingActionButton: FloatingActionButton.extended(
        icon: const Icon(Icons.person_add),
        label: Text(t.nuevoUsuario),
        onPressed: () async {
          final creado = await showDialog<bool>(context: context, builder: (_) => const _DialogoNuevoUsuario());
          if (creado == true) ref.invalidate(usuariosProvider);
        },
      ),
      body: EstadoAsincrono(
        valor: usuarios,
        alReintentar: () => ref.invalidate(usuariosProvider),
        constructor: (lista) => lista.isEmpty
            ? Center(child: Text(t.listaVacia))
            : RefreshIndicator(
                onRefresh: () => ref.refresh(usuariosProvider.future),
                child: ContenidoCentrado(
                  child: ListView.separated(
                    padding: const EdgeInsets.only(bottom: 88),
                    itemCount: lista.length,
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (_, i) => _FilaUsuario(usuario: lista[i]),
                  ),
                ),
              ),
      ),
    );
  }
}

class _FilaUsuario extends ConsumerWidget {
  const _FilaUsuario({required this.usuario});
  final UsuarioDto usuario;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final t = AppLocalizations.of(context);
    Future<void> ejecutar(Future<void> Function() accion) async {
      try {
        await accion();
        ref.invalidate(usuariosProvider);
        if (context.mounted) mostrarMensaje(context, t.operacionRealizada);
      } catch (e) {
        if (context.mounted) mostrarMensaje(context, mensajeDeFallo(t, e));
      }
    }

    final repo = ref.read(usuariosRepositorioProvider);
    return ListTile(
      leading: CircleAvatar(child: Icon(usuario.activo ? Icons.person : Icons.person_off)),
      title: Text(usuario.nombreCompleto),
      subtitle: Text('${usuario.email}\n${usuario.roles.map((r) => etiquetaRol(t, r)).join(' · ')}'),
      isThreeLine: true,
      trailing: Row(mainAxisSize: MainAxisSize.min, children: [
        if (!usuario.activo) Chip(label: Text(t.estadoInactivo)),
        PopupMenuButton<String>(
          onSelected: (accion) => switch (accion) {
            'estado' => ejecutar(() => repo.cambiarEstado(usuario.id, activo: !usuario.activo)),
            _ => ejecutar(() => repo.reiniciarMfa(usuario.id)),
          },
          itemBuilder: (_) => [
            PopupMenuItem(value: 'estado', child: Text(usuario.activo ? t.accionDesactivar : t.accionActivar)),
            if (usuario.mfaActivado) PopupMenuItem(value: 'mfa', child: Text(t.accionReiniciarMfa)),
          ],
        ),
      ]),
    );
  }
}

class _DialogoNuevoUsuario extends ConsumerStatefulWidget {
  const _DialogoNuevoUsuario();

  @override
  ConsumerState<_DialogoNuevoUsuario> createState() => _EstadoDialogoNuevoUsuario();
}

class _EstadoDialogoNuevoUsuario extends ConsumerState<_DialogoNuevoUsuario> {
  static const _minContrasena = 12;
  static const _asignables = [
    Rol.administradorSociedad,
    Rol.coordinadorMedico,
    Rol.profesionalSanitario,
    Rol.responsableCentro,
  ];

  final _formulario = GlobalKey<FormState>();
  final _correo = TextEditingController();
  final _nombre = TextEditingController();
  final _contrasena = TextEditingController();
  final _roles = <Rol>{Rol.profesionalSanitario};
  bool _enviando = false;
  FalloApi? _fallo;
  String? _errorGeneral;

  @override
  void dispose() {
    _correo.dispose();
    _nombre.dispose();
    _contrasena.dispose();
    super.dispose();
  }

  Future<void> _guardar() async {
    final t = AppLocalizations.of(context);
    if (!_formulario.currentState!.validate() || _roles.isEmpty) {
      setState(() => _errorGeneral = _roles.isEmpty ? t.validaRol : null);
      return;
    }
    setState(() {
      _enviando = true;
      _errorGeneral = null;
      _fallo = null;
    });
    try {
      await ref.read(usuariosRepositorioProvider).crear(
            email: _correo.text.trim(),
            nombreCompleto: _nombre.text.trim(),
            contrasena: _contrasena.text,
            roles: [for (final r in _roles) _nombreApi(r)],
          );
      if (mounted) Navigator.pop(context, true);
    } on FalloApi catch (f) {
      if (!mounted) return;
      setState(() {
        _fallo = f;
        _errorGeneral = f.errores.isEmpty ? mensajeDeFallo(t, f) : null;
      });
      _formulario.currentState!.validate(); // muestra los errores por campo devueltos por el backend
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  static String _nombreApi(Rol r) => switch (r) {
        Rol.administradorPlataforma => 'AdministradorPlataforma',
        Rol.administradorSociedad => 'AdministradorSociedad',
        Rol.coordinadorMedico => 'CoordinadorMedico',
        Rol.profesionalSanitario => 'ProfesionalSanitario',
        Rol.responsableCentro => 'ResponsableCentro',
      };

  @override
  Widget build(BuildContext context) {
    final t = AppLocalizations.of(context);
    final v = Validadores(t);
    return AlertDialog(
      title: Text(t.nuevoUsuario),
      content: SizedBox(
        width: 420,
        child: SingleChildScrollView(
          child: Form(
            key: _formulario,
            child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
              TextFormField(
                controller: _correo,
                decoration: InputDecoration(labelText: t.campoCorreo),
                keyboardType: TextInputType.emailAddress,
                validator: (x) => v.correo(x) ?? _fallo?.errorDe('Email'),
              ),
              TextFormField(
                controller: _nombre,
                decoration: InputDecoration(labelText: t.campoNombre),
                validator: (x) => v.obligatorio(x) ?? _fallo?.errorDe('NombreCompleto'),
              ),
              TextFormField(
                controller: _contrasena,
                decoration: InputDecoration(labelText: t.campoContrasena),
                obscureText: true,
                validator: (x) => v.longitudMinima(_minContrasena)(x) ?? _fallo?.errorDe('Contrasena'),
              ),
              const SizedBox(height: 16),
              Text(t.campoRoles, style: Theme.of(context).textTheme.labelLarge),
              for (final r in _asignables)
                CheckboxListTile(
                  dense: true,
                  contentPadding: EdgeInsets.zero,
                  title: Text(etiquetaRol(t, r)),
                  value: _roles.contains(r),
                  onChanged: (marcado) => setState(() => marcado == true ? _roles.add(r) : _roles.remove(r)),
                ),
              if (_errorGeneral != null)
                Text(_errorGeneral!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
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
