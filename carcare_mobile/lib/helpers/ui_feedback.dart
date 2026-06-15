import 'package:flutter/material.dart';

import 'app_theme.dart';

/// Унифицированные всплывающие сообщения (SnackBar).
class AppSnack {
  AppSnack._();

  static void success(BuildContext context, String message) {
    _show(context, message, const Color(0xFF4CAF50), Icons.check_circle);
  }

  static void error(BuildContext context, String message) {
    _show(context, message, const Color(0xFFF44336), Icons.error_outline);
  }

  static void info(BuildContext context, String message) {
    _show(context, message, AppColors.primary, Icons.info_outline);
  }

  static void _show(
      BuildContext context, String message, Color color, IconData icon) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          behavior: SnackBarBehavior.floating,
          backgroundColor: color,
          content: Row(
            children: [
              Icon(icon, color: Colors.white),
              const SizedBox(width: 12),
              Expanded(child: Text(message)),
            ],
          ),
        ),
      );
  }
}
