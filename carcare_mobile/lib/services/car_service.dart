import 'package:dio/dio.dart';

import '../models/car.dart';
import '../models/car_model_ref.dart';
import '../models/requests.dart';
import 'api_client.dart';
import 'api_exception.dart';

/// Сервис работы с автомобилями клиента и справочником моделей.
class CarService {
  final ApiClient _api;
  CarService(this._api);

  Dio get _dio => _api.dio;

  /// Список автомобилей текущего клиента.
  Future<List<Car>> getCars() async {
    try {
      final res = await _dio.get('/cars');
      return (res.data as List)
          .map((e) => Car.fromJson(e as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  /// Справочник марок/моделей (публичный эндпоинт) для формы добавления авто.
  Future<List<CarModelRef>> getCarModels() async {
    try {
      final res = await _dio.get('/car-models');
      return (res.data as List)
          .map((e) => CarModelRef.fromJson(e as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  Future<Car> createCar(CarRequest request) async {
    try {
      final res = await _dio.post('/cars', data: request.toJson());
      return Car.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  Future<Car> updateCar(int carId, CarRequest request) async {
    try {
      final res = await _dio.put('/cars/$carId', data: request.toJson());
      return Car.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  Future<void> deleteCar(int carId) async {
    try {
      await _dio.delete('/cars/$carId');
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}
