import 'dart:math' as math;

import 'package:flutter/material.dart';

/// Fondo animado: degradado, burbujas flotantes y una línea de electrocardiograma.
class FondoLoginAnimado extends StatefulWidget {
  const FondoLoginAnimado({super.key, this.child});

  final Widget? child;

  @override
  State<FondoLoginAnimado> createState() => _EstadoFondoLogin();
}

class _EstadoFondoLogin extends State<FondoLoginAnimado> with SingleTickerProviderStateMixin {
  late final AnimationController _controlador =
      AnimationController(vsync: this, duration: const Duration(seconds: 12));

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    // Accesibilidad: con «reducir movimiento» el fondo se queda quieto (y las pruebas de pantalla pueden terminar).
    if (MediaQuery.disableAnimationsOf(context)) {
      _controlador.stop();
    } else if (!_controlador.isAnimating) {
      _controlador.repeat();
    }
  }

  @override
  void dispose() {
    _controlador.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final esquema = Theme.of(context).colorScheme;
    final oscuro = Theme.of(context).brightness == Brightness.dark;
    final colores = oscuro
        ? [const Color(0xFF062A36), const Color(0xFF0B4A5E), const Color(0xFF0B6E8A)]
        : [const Color(0xFF0B6E8A), const Color(0xFF138FA8), const Color(0xFF3CC1C4)];
    return DecoratedBox(
      decoration: BoxDecoration(gradient: LinearGradient(begin: Alignment.topLeft, end: Alignment.bottomRight, colors: colores)),
      child: Stack(
        fit: StackFit.expand,
        children: [
          RepaintBoundary(
            child: AnimatedBuilder(
              animation: _controlador,
              builder: (_, _) => CustomPaint(painter: _PintorFondo(_controlador.value, esquema.onPrimary)),
            ),
          ),
          if (widget.child != null) widget.child!,
        ],
      ),
    );
  }
}

class _PintorFondo extends CustomPainter {
  _PintorFondo(this.t, this.color);

  final double t;
  final Color color;

  @override
  void paint(Canvas canvas, Size size) {
    // Burbujas flotantes.
    final relleno = Paint()..style = PaintingStyle.fill;
    for (var i = 0; i < 7; i++) {
      final fase = (t + i / 7) % 1.0;
      final x = size.width * ((i * 0.17 + 0.08) % 1.0) + math.sin((t * 2 * math.pi) + i) * 18;
      final y = size.height * (1.1 - fase * 1.3);
      final r = 24.0 + (i % 4) * 22;
      relleno.color = color.withValues(alpha: 0.07 + 0.04 * (i % 3));
      canvas.drawCircle(Offset(x, y), r, relleno);
    }

    // Línea de electrocardiograma que se desplaza.
    final linea = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 2.5
      ..strokeCap = StrokeCap.round
      ..strokeJoin = StrokeJoin.round
      ..color = color.withValues(alpha: 0.28);
    final base = size.height * 0.82;
    final ciclo = 220.0;
    final desplazamiento = t * ciclo;
    final ruta = Path();
    for (var x = -ciclo; x <= size.width + ciclo; x += ciclo) {
      final o = x - desplazamiento;
      if (x == -ciclo) ruta.moveTo(o, base);
      ruta
        ..lineTo(o + 60, base)
        ..lineTo(o + 75, base - 10)
        ..lineTo(o + 90, base)
        ..lineTo(o + 105, base)
        ..lineTo(o + 115, base + 14)
        ..lineTo(o + 130, base - 70)
        ..lineTo(o + 145, base + 36)
        ..lineTo(o + 155, base)
        ..lineTo(o + 180, base)
        ..lineTo(o + 195, base - 14)
        ..lineTo(o + 210, base)
        ..lineTo(o + ciclo, base);
    }
    canvas.drawPath(ruta, linea);
  }

  @override
  bool shouldRepaint(_PintorFondo old) => old.t != t || old.color != color;
}
