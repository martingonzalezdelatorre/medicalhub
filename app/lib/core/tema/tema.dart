import 'package:flutter/cupertino.dart' show CupertinoPageTransitionsBuilder;
import 'package:flutter/material.dart';

/// Puntos de corte de diseño adaptable (ancho disponible en píxeles lógicos).
class Pantalla {
  static const double movil = 600;
  static const double tableta = 1000;
  static const double escritorio = 1400;

  static bool esMovil(BuildContext c) => MediaQuery.sizeOf(c).width < movil;
  static bool esTableta(BuildContext c) {
    final a = MediaQuery.sizeOf(c).width;
    return a >= movil && a < tableta;
  }

  static bool esEscritorio(BuildContext c) => MediaQuery.sizeOf(c).width >= tableta;

  /// Margen lateral del contenido según el ancho.
  static double margen(double ancho) => ancho < movil ? 12 : (ancho < tableta ? 20 : 28);
}

class TemaApp {
  static const _semilla = Color(0xFF0B6E8A);
  static const double radio = 16;

  /// Degradado de marca (cabeceras, logotipo y acentos).
  static const degradadoMarca = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [Color(0xFF0B6E8A), Color(0xFF138FA8), Color(0xFF2BB3B8)],
  );

  /// Degradado más oscuro para la barra superior: el texto blanco mantiene contraste suficiente (≥ 4,5:1) en todo el ancho.
  static const degradadoBarra = LinearGradient(
    begin: Alignment.centerLeft,
    end: Alignment.centerRight,
    colors: [Color(0xFF0A5A73), Color(0xFF0B6E8A), Color(0xFF117F97)],
  );

  static ThemeData claro() => _construir(Brightness.light);
  static ThemeData oscuro() => _construir(Brightness.dark);

  static ThemeData _construir(Brightness brillo) {
    final claro = brillo == Brightness.light;
    final cs = ColorScheme.fromSeed(seedColor: _semilla, brightness: brillo).copyWith(
      primary: claro ? const Color(0xFF0B6E8A) : const Color(0xFF5CC8E0),
      surface: claro ? Colors.white : const Color(0xFF131A1F),
      surfaceContainerLowest: claro ? const Color(0xFFF3F7FA) : const Color(0xFF0E1417),
      surfaceContainerLow: claro ? const Color(0xFFF6F9FB) : const Color(0xFF182026),
      outlineVariant: claro ? const Color(0xFFE2E9EE) : const Color(0xFF2A353C),
    );
    final base = ThemeData(useMaterial3: true, colorScheme: cs, brightness: brillo);
    final texto = base.textTheme.apply(bodyColor: cs.onSurface, displayColor: cs.onSurface);
    final forma = RoundedRectangleBorder(borderRadius: BorderRadius.circular(20));
    OutlineInputBorder borde(Color c, [double w = 1]) =>
        OutlineInputBorder(borderRadius: BorderRadius.circular(12), borderSide: BorderSide(color: c, width: w));
    final botonForma = RoundedRectangleBorder(borderRadius: BorderRadius.circular(12));

    return base.copyWith(
      scaffoldBackgroundColor: cs.surfaceContainerLowest,
      textTheme: texto.copyWith(
        headlineMedium: texto.headlineMedium?.copyWith(fontWeight: FontWeight.w700, letterSpacing: -0.5),
        headlineSmall: texto.headlineSmall?.copyWith(fontWeight: FontWeight.w700, letterSpacing: -0.3),
        titleLarge: texto.titleLarge?.copyWith(fontWeight: FontWeight.w700, letterSpacing: -0.2),
        titleMedium: texto.titleMedium?.copyWith(fontWeight: FontWeight.w600),
        titleSmall: texto.titleSmall?.copyWith(fontWeight: FontWeight.w600),
        labelLarge: texto.labelLarge?.copyWith(fontWeight: FontWeight.w600),
      ),
      visualDensity: VisualDensity.adaptivePlatformDensity,
      pageTransitionsTheme: const PageTransitionsTheme(builders: {
        TargetPlatform.android: FadeForwardsPageTransitionsBuilder(),
        TargetPlatform.iOS: CupertinoPageTransitionsBuilder(),
        TargetPlatform.windows: FadeForwardsPageTransitionsBuilder(),
        TargetPlatform.macOS: FadeForwardsPageTransitionsBuilder(),
        TargetPlatform.linux: FadeForwardsPageTransitionsBuilder(),
      }),
      appBarTheme: AppBarTheme(
        centerTitle: false,
        elevation: 0,
        scrolledUnderElevation: 0,
        backgroundColor: cs.surface,
        surfaceTintColor: Colors.transparent,
        titleTextStyle: texto.titleLarge?.copyWith(fontWeight: FontWeight.w700),
        shape: Border(bottom: BorderSide(color: cs.outlineVariant)),
      ),
      cardTheme: CardThemeData(
        elevation: 0,
        margin: const EdgeInsets.symmetric(vertical: 6),
        color: cs.surface,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(radio), side: BorderSide(color: cs.outlineVariant)),
        clipBehavior: Clip.antiAlias,
      ),
      listTileTheme: ListTileThemeData(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        titleTextStyle: texto.titleSmall?.copyWith(color: cs.onSurface),
        subtitleTextStyle: texto.bodySmall?.copyWith(color: cs.onSurfaceVariant),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: claro ? Colors.white : cs.surfaceContainerLow,
        border: borde(cs.outlineVariant),
        enabledBorder: borde(cs.outlineVariant),
        focusedBorder: borde(cs.primary, 2),
        errorBorder: borde(cs.error),
        focusedErrorBorder: borde(cs.error, 2),
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(minimumSize: const Size(64, 46), shape: botonForma, textStyle: const TextStyle(fontWeight: FontWeight.w600)),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(style: ElevatedButton.styleFrom(minimumSize: const Size(64, 46), shape: botonForma)),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(minimumSize: const Size(64, 46), shape: botonForma, side: BorderSide(color: cs.outlineVariant)),
      ),
      textButtonTheme: TextButtonThemeData(style: TextButton.styleFrom(shape: botonForma)),
      floatingActionButtonTheme: FloatingActionButtonThemeData(
        elevation: 3,
        backgroundColor: cs.primary,
        foregroundColor: cs.onPrimary,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      ),
      chipTheme: ChipThemeData(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
        side: BorderSide(color: cs.outlineVariant),
        labelStyle: texto.labelMedium,
      ),
      dialogTheme: DialogThemeData(shape: forma, surfaceTintColor: Colors.transparent, backgroundColor: cs.surface),
      bottomSheetTheme: BottomSheetThemeData(
        showDragHandle: true,
        backgroundColor: cs.surface,
        surfaceTintColor: Colors.transparent,
        shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(28))),
      ),
      popupMenuTheme: PopupMenuThemeData(
        color: cs.surface,
        surfaceTintColor: Colors.transparent,
        elevation: 6,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14), side: BorderSide(color: cs.outlineVariant)),
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        backgroundColor: claro ? const Color(0xFF1B2A33) : const Color(0xFFE3EEF3),
        contentTextStyle: TextStyle(color: claro ? Colors.white : const Color(0xFF10181D)),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
      dividerTheme: DividerThemeData(color: cs.outlineVariant, space: 1, thickness: 1),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: cs.surface,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        indicatorColor: cs.primary.withAlpha(40),
        indicatorShape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        height: 64,
        labelBehavior: NavigationDestinationLabelBehavior.alwaysHide,
        labelTextStyle: WidgetStateProperty.resolveWith((s) => texto.labelSmall?.copyWith(
            fontWeight: FontWeight.w600, color: s.contains(WidgetState.selected) ? cs.primary : cs.onSurfaceVariant)),
        iconTheme: WidgetStateProperty.resolveWith(
            (s) => IconThemeData(color: s.contains(WidgetState.selected) ? cs.primary : cs.onSurfaceVariant)),
      ),
      navigationRailTheme: NavigationRailThemeData(
        backgroundColor: cs.surface,
        indicatorColor: cs.primary.withAlpha(30),
        indicatorShape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        selectedLabelTextStyle: texto.labelLarge?.copyWith(color: cs.primary, fontWeight: FontWeight.w600),
        unselectedLabelTextStyle: texto.labelLarge?.copyWith(color: cs.onSurfaceVariant),
      ),
      drawerTheme: DrawerThemeData(backgroundColor: cs.surface, surfaceTintColor: Colors.transparent),
      tabBarTheme: TabBarThemeData(dividerColor: cs.outlineVariant, labelStyle: texto.labelLarge, indicatorSize: TabBarIndicatorSize.label),
      dataTableTheme: DataTableThemeData(
        headingRowColor: WidgetStatePropertyAll(cs.primary.withAlpha(22)),
        headingTextStyle: texto.labelLarge?.copyWith(color: cs.primary, fontWeight: FontWeight.w700),
        dividerThickness: 1,
      ),
      segmentedButtonTheme: SegmentedButtonThemeData(style: SegmentedButton.styleFrom(shape: botonForma)),
      tooltipTheme: TooltipThemeData(
        decoration: BoxDecoration(color: const Color(0xFF1B2A33), borderRadius: BorderRadius.circular(8)),
        textStyle: const TextStyle(color: Colors.white, fontSize: 12),
      ),
    );
  }
}
