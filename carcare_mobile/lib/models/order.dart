import 'package:json_annotation/json_annotation.dart';

import 'order_part_item.dart';
import 'order_service_item.dart';

part 'order.g.dart';

/// Заказ-наряд. Соответствует OrderDto сервера (camelCase JSON).
@JsonSerializable()
class Order {
  @JsonKey(name: 'orderID')
  final int orderId;

  @JsonKey(name: 'carID')
  final int carId;

  @JsonKey(name: 'carInfo')
  final String? carInfo;

  @JsonKey(name: 'clientID')
  final int clientId;

  @JsonKey(name: 'clientName')
  final String? clientName;

  @JsonKey(name: 'createdAt')
  final DateTime? createdAt;

  @JsonKey(name: 'scheduledDate')
  final DateTime? scheduledDate;

  /// Время в формате "HH:mm" (строка).
  @JsonKey(name: 'scheduledTime')
  final String? scheduledTime;

  @JsonKey(name: 'status')
  final String status;

  @JsonKey(name: 'mileage')
  final int? mileage;

  @JsonKey(name: 'notes')
  final String? notes;

  /// Сумма по запчастям (услуги на уровне заказа сервер отдельной суммой не отдаёт).
  @JsonKey(name: 'partsTotal', defaultValue: 0)
  final double partsTotal;

  @JsonKey(name: 'services', defaultValue: <OrderServiceItem>[])
  final List<OrderServiceItem> services;

  @JsonKey(name: 'parts', defaultValue: <OrderPartItem>[])
  final List<OrderPartItem> parts;

  const Order({
    required this.orderId,
    required this.carId,
    this.carInfo,
    required this.clientId,
    this.clientName,
    this.createdAt,
    this.scheduledDate,
    this.scheduledTime,
    required this.status,
    this.mileage,
    this.notes,
    this.partsTotal = 0,
    this.services = const [],
    this.parts = const [],
  });

  bool get isCancellable => status == 'Новый' || status == 'Назначен';

  factory Order.fromJson(Map<String, dynamic> json) => _$OrderFromJson(json);
  Map<String, dynamic> toJson() => _$OrderToJson(this);
}
