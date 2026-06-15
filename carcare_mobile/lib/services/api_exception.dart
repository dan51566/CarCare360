import 'package:dio/dio.dart';

/// Унифицированная ошибка обращения к API с понятным пользователю сообщением.
class ApiException implements Exception {
  final String message;
  final int? statusCode;

  ApiException(this.message, {this.statusCode});

  /// Преобразует DioException в ApiException, извлекая поле `error`
  /// из тела ответа (формат сервера: { "error": "...", "detail": "..." }).
  factory ApiException.fromDio(DioException e) {
    final status = e.response?.statusCode;
    final data = e.response?.data;

    String? serverMessage;
    if (data is Map && data['error'] is String) {
      serverMessage = data['error'] as String;
    }

    if (status == 429) {
      return ApiException(
        'Слишком много попыток. Подождите минуту и попробуйте снова.',
        statusCode: status,
      );
    }
    if (serverMessage != null) {
      return ApiException(serverMessage, statusCode: status);
    }

    switch (e.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.receiveTimeout:
      case DioExceptionType.sendTimeout:
        return ApiException('Превышено время ожидания сервера.',
            statusCode: status);
      case DioExceptionType.connectionError:
        return ApiException(
          'Не удалось подключиться к серверу. Проверьте, что API запущен.',
          statusCode: status,
        );
      default:
        if (status == 401) {
          return ApiException('Требуется авторизация.', statusCode: status);
        }
        if (status == 403) {
          return ApiException('Недостаточно прав для операции.',
              statusCode: status);
        }
        if (status == 404) {
          return ApiException('Объект не найден.', statusCode: status);
        }
        return ApiException('Ошибка сервера (${status ?? '—'}).',
            statusCode: status);
    }
  }

  @override
  String toString() => message;
}
