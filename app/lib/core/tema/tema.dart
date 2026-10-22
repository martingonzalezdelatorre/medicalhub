import 'package:flutter/material.dart';

class TemaApp {
  static const _semilla = Color(0xFF0B6E8A);

  static ThemeData claro() => ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(seedColor: _semilla),
      );

  static ThemeData oscuro() => ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(seedColor: _semilla, brightness: Brightness.dark),
      );
}
