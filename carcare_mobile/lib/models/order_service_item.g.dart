// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'order_service_item.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

OrderServiceItem _$OrderServiceItemFromJson(Map<String, dynamic> json) =>
    OrderServiceItem(
      orderServiceId: (json['orderServiceID'] as num).toInt(),
      serviceId: (json['serviceID'] as num).toInt(),
      serviceName: json['serviceName'] as String?,
      mechanicId: (json['mechanicID'] as num?)?.toInt(),
      mechanicName: json['mechanicName'] as String?,
      bayId: (json['bayID'] as num?)?.toInt(),
      bayName: json['bayName'] as String?,
      startTime: json['startTime'] == null
          ? null
          : DateTime.parse(json['startTime'] as String),
      endTime: json['endTime'] == null
          ? null
          : DateTime.parse(json['endTime'] as String),
      status: json['status'] as String?,
    );

Map<String, dynamic> _$OrderServiceItemToJson(OrderServiceItem instance) =>
    <String, dynamic>{
      'orderServiceID': instance.orderServiceId,
      'serviceID': instance.serviceId,
      'serviceName': instance.serviceName,
      'mechanicID': instance.mechanicId,
      'mechanicName': instance.mechanicName,
      'bayID': instance.bayId,
      'bayName': instance.bayName,
      'startTime': instance.startTime?.toIso8601String(),
      'endTime': instance.endTime?.toIso8601String(),
      'status': instance.status,
    };
