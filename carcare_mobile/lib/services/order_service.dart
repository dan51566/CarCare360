import 'package:dio/dio.dart';

import '../models/order.dart';
import '../models/requests.dart';
import 'api_client.dart';
import 'api_exception.dart';

/// Сервис работы с заказами клиента.
class OrderService {
  final ApiClient _api;
  OrderService(this._api);

  Dio get _dio => _api.dio;

  Future<List<Order>> getOrders() async {
    try {
      final res = await _dio.get('/orders');
      return (res.data as List)
          .map((e) => Order.fromJson(e as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  Future<Order> getOrder(int orderId) async {
    try {
      final res = await _dio.get('/orders/$orderId');
      return Order.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  Future<Order> createOrder(OrderCreateRequest request) async {
    try {
      final res = await _dio.post('/orders', data: request.toJson());
      return Order.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  /// Отмена заказа клиентом = мягкое удаление (DELETE /orders/{id}).
  /// Менять статус напрямую клиенту сервер не разрешает (это делает персонал).
  Future<void> cancelOrder(int orderId) async {
    try {
      await _dio.delete('/orders/$orderId');
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}
