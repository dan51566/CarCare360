// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'order.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

Order _$OrderFromJson(Map<String, dynamic> json) => Order(
  orderId: (json['orderID'] as num).toInt(),
  carId: (json['carID'] as num).toInt(),
  carInfo: json['carInfo'] as String?,
  clientId: (json['clientID'] as num).toInt(),
  clientName: json['clientName'] as String?,
  createdAt: json['createdAt'] == null
      ? null
      : DateTime.parse(json['createdAt'] as String),
  scheduledDate: json['scheduledDate'] == null
      ? null
      : DateTime.parse(json['scheduledDate'] as String),
  scheduledTime: json['scheduledTime'] as String?,
  status: json['status'] as String,
  mileage: (json['mileage'] as num?)?.toInt(),
  notes: json['notes'] as String?,
  partsTotal: (json['partsTotal'] as num?)?.toDouble() ?? 0,
  services:
      (json['services'] as List<dynamic>?)
          ?.map((e) => OrderServiceItem.fromJson(e as Map<String, dynamic>))
          .toList() ??
      [],
  parts:
      (json['parts'] as List<dynamic>?)
          ?.map((e) => OrderPartItem.fromJson(e as Map<String, dynamic>))
          .toList() ??
      [],
);

Map<String, dynamic> _$OrderToJson(Order instance) => <String, dynamic>{
  'orderID': instance.orderId,
  'carID': instance.carId,
  'carInfo': instance.carInfo,
  'clientID': instance.clientId,
  'clientName': instance.clientName,
  'createdAt': instance.createdAt?.toIso8601String(),
  'scheduledDate': instance.scheduledDate?.toIso8601String(),
  'scheduledTime': instance.scheduledTime,
  'status': instance.status,
  'mileage': instance.mileage,
  'notes': instance.notes,
  'partsTotal': instance.partsTotal,
  'services': instance.services,
  'parts': instance.parts,
};
