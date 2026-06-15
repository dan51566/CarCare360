import 'package:dio/dio.dart';

import '../models/service.dart';
import 'api_client.dart';
import 'api_exception.dart';

/// Сервис справочника услуг автосервиса.
class CatalogService {
  final ApiClient _api;
  CatalogService(this._api);

  Dio get _dio => _api.dio;

  /// Список доступных услуг (публичный эндпоинт).
  Future<List<Service>> getServices() async {
    try {
      final res = await _dio.get('/services');
      return (res.data as List)
          .map((e) => Service.fromJson(e as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}
