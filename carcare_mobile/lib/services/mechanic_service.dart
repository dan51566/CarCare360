import 'package:dio/dio.dart';

import '../models/mechanic.dart';
import 'api_client.dart';
import 'api_exception.dart';

/// Сервис каталога механиков и избранного (Изменение №2, Доработка 3).
/// Все вызовы — клиентские эндпоинты /favorite-mechanics, защищённые JWT.
class MechanicService {
  final ApiClient _api;
  MechanicService(this._api);

  Dio get _dio => _api.dio;

  /// Каталог активных механиков с признаком избранного (избранные — первыми).
  Future<List<MechanicCatalogItem>> getCatalog() async {
    try {
      final res = await _dio.get('/favorite-mechanics/catalog');
      return (res.data as List)
          .map((e) => MechanicCatalogItem.fromJson(e as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  /// Добавить механика в избранное текущего клиента.
  Future<void> addFavorite(int mechanicId) async {
    try {
      await _dio.post('/favorite-mechanics/$mechanicId');
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  /// Убрать механика из избранного текущего клиента.
  Future<void> removeFavorite(int mechanicId) async {
    try {
      await _dio.delete('/favorite-mechanics/$mechanicId');
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}
