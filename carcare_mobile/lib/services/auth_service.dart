import 'package:dio/dio.dart';

import '../models/auth_response.dart';
import '../models/requests.dart';
import '../models/user.dart';
import 'api_client.dart';
import 'api_exception.dart';

/// Сервис аутентификации: вход, регистрация, выход, авто-вход.
class AuthService {
  final ApiClient _api;
  AuthService(this._api);

  Dio get _dio => _api.dio;

  /// Вход. Сохраняет токены и пользователя в защищённое хранилище.
  Future<User> login(String login, String password) async {
    try {
      final res = await _dio.post(
        '/auth/login',
        data: LoginRequest(login: login, password: password).toJson(),
      );
      final auth = AuthResponse.fromJson(res.data as Map<String, dynamic>);
      await _api.storage.saveAuth(auth);
      return auth.user;
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  /// Регистрация нового клиента с автоматическим входом.
  Future<User> register(RegisterRequest request) async {
    try {
      final res = await _dio.post('/auth/register', data: request.toJson());
      final auth = AuthResponse.fromJson(res.data as Map<String, dynamic>);
      await _api.storage.saveAuth(auth);
      return auth.user;
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  /// Выход: отзывает refresh-токен на сервере и очищает хранилище.
  Future<void> logout() async {
    final refresh = await _api.storage.getRefreshToken();
    try {
      if (refresh != null && refresh.isNotEmpty) {
        await _dio.post('/auth/logout', data: {'RefreshToken': refresh});
      }
    } on DioException {
      // Игнорируем ошибку logout — главное очистить локальную сессию.
    } finally {
      await _api.storage.clear();
    }
  }

  /// Авто-вход при запуске: возвращает пользователя из хранилища,
  /// при необходимости обновляя токен. null — сессии нет.
  Future<User?> tryAutoLogin() async {
    if (!await _api.storage.hasSession()) return null;

    final expiresAt = await _api.storage.getExpiresAt();
    final accessToken = await _api.storage.getAccessToken();
    final expired = accessToken == null ||
        expiresAt == null ||
        expiresAt.isBefore(DateTime.now().toUtc().add(const Duration(seconds: 30)));

    if (expired) {
      final ok = await _refresh();
      if (!ok) {
        await _api.storage.clear();
        return null;
      }
    }
    return _api.storage.getUser();
  }

  /// Обновление токенов по refresh-токену.
  Future<bool> _refresh() async {
    final refresh = await _api.storage.getRefreshToken();
    if (refresh == null || refresh.isEmpty) return false;
    try {
      final res =
          await _dio.post('/auth/refresh', data: {'RefreshToken': refresh});
      final auth = AuthResponse.fromJson(res.data as Map<String, dynamic>);
      await _api.storage.saveAuth(auth);
      return true;
    } on DioException {
      return false;
    }
  }
}
