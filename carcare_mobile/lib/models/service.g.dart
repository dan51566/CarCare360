// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'service.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

Service _$ServiceFromJson(Map<String, dynamic> json) => Service(
  serviceId: (json['serviceID'] as num).toInt(),
  name: json['name'] as String,
  description: json['description'] as String?,
  normHour: (json['normHour'] as num).toDouble(),
  basePrice: (json['basePrice'] as num?)?.toDouble(),
);

Map<String, dynamic> _$ServiceToJson(Service instance) => <String, dynamic>{
  'serviceID': instance.serviceId,
  'name': instance.name,
  'description': instance.description,
  'normHour': instance.normHour,
  'basePrice': instance.basePrice,
};
