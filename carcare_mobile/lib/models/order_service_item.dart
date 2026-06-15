import 'package:json_annotation/json_annotation.dart';

part 'order_service_item.g.dart';

/// Услуга в составе заказа (с привязкой механика/бокса).
/// Соответствует вложенному объекту services[] в OrderDto (camelCase).
@JsonSerializable()
class OrderServiceItem {
  @JsonKey(name: 'orderServiceID')
  final int orderServiceId;

  @JsonKey(name: 'serviceID')
  final int serviceId;

  @JsonKey(name: 'serviceName')
  final String? serviceName;

  @JsonKey(name: 'mechanicID')
  final int? mechanicId;

  @JsonKey(name: 'mechanicName')
  final String? mechanicName;

  @JsonKey(name: 'bayID')
  final int? bayId;

  @JsonKey(name: 'bayName')
  final String? bayName;

  @JsonKey(name: 'startTime')
  final DateTime? startTime;

  @JsonKey(name: 'endTime')
  final DateTime? endTime;

  @JsonKey(name: 'status')
  final String? status;

  const OrderServiceItem({
    required this.orderServiceId,
    required this.serviceId,
    this.serviceName,
    this.mechanicId,
    this.mechanicName,
    this.bayId,
    this.bayName,
    this.startTime,
    this.endTime,
    this.status,
  });

  factory OrderServiceItem.fromJson(Map<String, dynamic> json) =>
      _$OrderServiceItemFromJson(json);
  Map<String, dynamic> toJson() => _$OrderServiceItemToJson(this);
}
