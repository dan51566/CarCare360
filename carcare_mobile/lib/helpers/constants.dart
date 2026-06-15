import 'dart:io' show Platform;

import 'package:flutter/foundation.dart';

/// Конфигурация подключения к серверному API CarCare360.
class ApiConfig {
  ApiConfig._();

  /// Порт, на котором запущен ASP.NET Core API (dev: `dotnet run --urls http://localhost:5009`).
  static const int port = 5009;

  /// Базовый URL API.
  ///
  /// Android-эмулятор изолирован и обращается к хост-машине по адресу `10.0.2.2`,
  /// поэтому `localhost` для него не подходит. На реальном устройстве сюда нужно
  /// подставить IP-адрес машины с API в локальной сети.
  static String get baseUrl {
    if (!kIsWeb && Platform.isAndroid) {
      return 'http://10.0.2.2:$port/api';
    }
    return 'http://localhost:$port/api';
  }
}

/// Ключи для безопасного хранилища (flutter_secure_storage).
class StorageKeys {
  StorageKeys._();

  static const String accessToken = 'cc_access_token';
  static const String refreshToken = 'cc_refresh_token';
  static const String expiresAt = 'cc_access_token_expires_at';
  static const String user = 'cc_user_json';
}
