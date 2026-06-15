import 'package:dio/dio.dart';

import '../models/requests.dart';
import '../models/user.dart';
import 'api_client.dart';
import 'api_exception.dart';

/// Сервис профиля клиента.
class ClientService {
  final ApiClient _api;
  ClientService(this._api);

  Dio get _dio => _api.dio;

  /// Обновление профиля (ФИО, email, телефон). PUT /api/clients/{userId}.
  Future<User> updateProfile(int userId, ClientUpdateRequest request) async {
    try {
      final res = await _dio.put('/clients/$userId', data: request.toJson());
      return User.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}
