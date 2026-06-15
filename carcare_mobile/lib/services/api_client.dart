import 'package:dio/dio.dart';

import '../helpers/constants.dart';
import '../models/auth_response.dart';
import 'token_storage.dart';

/// HTTP-клиент приложения: единственный экземпляр Dio с перехватчиками
/// для подстановки JWT и автоматического обновления токена по 401.
class ApiClient {
  final TokenStorage storage;

  /// Основной Dio со всеми перехватчиками.
  late final Dio dio;

  /// Отдельный Dio без перехватчиков — для запроса refresh и повторов
  /// (чтобы не зацикливать обработку ошибок и не блокировать очередь).
  final Dio _bareDio = Dio();

  /// Вызывается, когда сессия окончательно недействительна (refresh не удался).
  void Function()? onSessionExpired;

  ApiClient(this.storage) {
    final options = BaseOptions(
      baseUrl: ApiConfig.baseUrl,
      connectTimeout: const Duration(seconds: 15),
      receiveTimeout: const Duration(seconds: 20),
      contentType: Headers.jsonContentType,
      // Не выбрасывать на 4xx автоматически — обрабатываем сами,
      // но оставляем стандартное поведение валидатора (>=400 → ошибка).
    );
    dio = Dio(options);
    _bareDio.options = options;

    dio.interceptors.add(
      QueuedInterceptorsWrapper(
        onRequest: (options, handler) async {
          final token = await storage.getAccessToken();
          if (token != null && token.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          handler.next(options);
        },
        onError: (error, handler) async {
          final status = error.response?.statusCode;
          final path = error.requestOptions.path;
          final isAuthEndpoint = path.contains('/auth/');
          final alreadyRetried = error.requestOptions.extra['__retried'] == true;

          if (status == 401 && !isAuthEndpoint && !alreadyRetried) {
            final refreshed = await _tryRefresh();
            if (refreshed) {
              try {
                final response = await _retry(error.requestOptions);
                return handler.resolve(response);
              } on DioException catch (retryError) {
                return handler.next(retryError);
              }
            } else {
              // Обновить не удалось — завершаем сессию.
              await storage.clear();
              onSessionExpired?.call();
            }
          }
          handler.next(error);
        },
      ),
    );
  }

  /// Повторяет исходный запрос со свежим токеном через bareDio.
  Future<Response<dynamic>> _retry(RequestOptions requestOptions) async {
    final token = await storage.getAccessToken();
    requestOptions.extra['__retried'] = true;
    requestOptions.headers['Authorization'] = 'Bearer $token';
    return _bareDio.fetch(requestOptions);
  }

  /// Пытается обновить токены по refresh-токену. true — успех.
  Future<bool> _tryRefresh() async {
    final refresh = await storage.getRefreshToken();
    if (refresh == null || refresh.isEmpty) return false;
    try {
      final response = await _bareDio.post(
        '/auth/refresh',
        data: {'RefreshToken': refresh},
      );
      final auth =
          AuthResponse.fromJson(response.data as Map<String, dynamic>);
      await storage.saveAuth(auth);
      return true;
    } catch (_) {
      return false;
    }
  }
}
