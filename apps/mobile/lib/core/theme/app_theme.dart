import 'package:flutter/material.dart';

class AppTheme {
  static const Color brandPrimary = Color(0xFF302B6B);
  static const Color brandLight = Color(0xFFEEF0FF);
  static const Color background = Color(0xFFF7F8FC);
  static const Color surface = Color(0xFFFFFFFF);
  static const Color ink = Color(0xFF171A2B);
  static const Color muted = Color(0xFF667085);
  static const Color border = Color(0xFFDDE1EA);
  
  static const Color success = Color(0xFF047857);
  static const Color warning = Color(0xFFB45309);
  static const Color danger = Color(0xFFB42318);

  static ThemeData get lightTheme {
    return ThemeData(
      useMaterial3: true,
      scaffoldBackgroundColor: background,
      primaryColor: brandPrimary,
      colorScheme: ColorScheme.light(
        primary: brandPrimary,
        surface: surface,
        error: danger,
      ),
    );
  }
}
