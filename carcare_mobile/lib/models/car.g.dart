// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'car.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

Car _$CarFromJson(Map<String, dynamic> json) => Car(
  carId: (json['carID'] as num).toInt(),
  clientId: (json['clientID'] as num).toInt(),
  clientName: json['clientName'] as String?,
  modelId: (json['modelID'] as num).toInt(),
  brandName: json['brandName'] as String?,
  modelName: json['modelName'] as String?,
  year: (json['year'] as num?)?.toInt(),
  vin: json['vin'] as String?,
  licensePlate: json['licensePlate'] as String,
  color: json['color'] as String?,
  mileage: (json['mileage'] as num?)?.toInt(),
);

Map<String, dynamic> _$CarToJson(Car instance) => <String, dynamic>{
  'carID': instance.carId,
  'clientID': instance.clientId,
  'clientName': instance.clientName,
  'modelID': instance.modelId,
  'brandName': instance.brandName,
  'modelName': instance.modelName,
  'year': instance.year,
  'vin': instance.vin,
  'licensePlate': instance.licensePlate,
  'color': instance.color,
  'mileage': instance.mileage,
};
