import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../helpers/constants.dart';
import '../models/auth_response.dart';
import '../models/user.dart';

/// Безопасное хранилище токенов и данных пользователя (flutter_secure_storage).
class TokenStorage {
  static const FlutterSecureStorage _storage = FlutterSecureStorage();

  /// Сохранить результат авторизации целиком.
  Future<void> saveAuth(AuthResponse auth) async {
    await _storage.write(key: StorageKeys.accessToken, value: auth.accessToken);
    await _storage.write(
        key: StorageKeys.refreshToken, value: auth.refreshToken);
    await _storage.write(
        key: StorageKeys.expiresAt,
        value: auth.accessTokenExpiresAt.toIso8601String());
    await _storage.write(
        key: StorageKeys.user, value: jsonEncode(auth.user.toJson()));
  }

  Future<String?> getAccessToken() =>
      _storage.read(key: StorageKeys.accessToken);

  Future<String?> getRefreshToken() =>
      _storage.read(key: StorageKeys.refreshToken);

  Future<DateTime?> getExpiresAt() async {
    final raw = await _storage.read(key: StorageKeys.expiresAt);
    if (raw == null) return null;
    return DateTime.tryParse(raw);
  }

  Future<User?> getUser() async {
    final raw = await _storage.read(key: StorageKeys.user);
    if (raw == null) return null;
    try {
      return User.fromJson(jsonDecode(raw) as Map<String, dynamic>);
    } catch (_) {
      return null;
    }
  }

  /// Есть ли сохранённый refresh-токен (т.е. потенциальная сессия).
  Future<bool> hasSession() async {
    final refresh = await getRefreshToken();
    return refresh != null && refresh.isNotEmpty;
  }

  Future<void> clear() async {
    await _storage.delete(key: StorageKeys.accessToken);
    await _storage.delete(key: StorageKeys.refreshToken);
    await _storage.delete(key: StorageKeys.expiresAt);
    await _storage.delete(key: StorageKeys.user);
  }
}
